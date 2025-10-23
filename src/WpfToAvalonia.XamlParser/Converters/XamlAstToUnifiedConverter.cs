using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.TypeSystem;
using WpfToAvalonia.XamlParser.UnifiedAst;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace WpfToAvalonia.XamlParser.Converters;

/// <summary>
/// Converts XamlX AST (semantic layer) to Unified AST.
/// This enriches the Unified AST with type information and resolved semantics.
/// </summary>
public sealed class XamlAstToUnifiedConverter
{
    private readonly DiagnosticCollector _diagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="XamlAstToUnifiedConverter"/> class.
    /// </summary>
    public XamlAstToUnifiedConverter(DiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    /// <summary>
    /// Converts a XamlX document to a UnifiedXamlDocument.
    /// </summary>
    public UnifiedXamlDocument Convert(XamlDocument xamlXDocument, string? filePath = null)
    {
        var document = new UnifiedXamlDocument
        {
            FilePath = filePath,
            DiagnosticCollector = _diagnostics,
            SemanticDocument = xamlXDocument
        };

        // Convert root element
        if (xamlXDocument.Root is XamlAstObjectNode rootObject)
        {
            document.Root = ConvertObjectNode(rootObject, null, 0);
        }

        return document;
    }

    /// <summary>
    /// Enriches an existing UnifiedXamlElement with XamlX semantic information.
    /// This is the primary use case - adding type info to XML-based nodes.
    /// </summary>
    public void EnrichElement(UnifiedXamlElement element, IXamlAstNode xamlXNode)
    {
        if (xamlXNode is XamlAstObjectNode objectNode)
        {
            element.SemanticObject = objectNode;
            element.SemanticNode = objectNode;

            // Extract type information
            if (objectNode.Type is IXamlAstTypeReference typeRef)
            {
                element.ResolvedType = ConvertTypeReference(typeRef);
                element.ElementType = element.ResolvedType;
            }

            // Process children recursively
            int propertyIndex = 0;
            int childIndex = 0;

            foreach (var child in objectNode.Children)
            {
                if (child is XamlAstXamlPropertyValueNode propertyNode)
                {
                    // Find or create corresponding property in unified element
                    var property = FindOrCreateProperty(element, propertyNode, propertyIndex++);
                    EnrichProperty(property, propertyNode);
                }
                else if (child is XamlAstObjectNode childObject)
                {
                    // Find or create corresponding child element
                    var childElement = FindOrCreateChild(element, childObject, childIndex++);
                    EnrichElement(childElement, childObject);
                }
            }
        }
    }

    /// <summary>
    /// Converts a XamlAstObjectNode to a UnifiedXamlElement.
    /// </summary>
    private UnifiedXamlElement ConvertObjectNode(XamlAstObjectNode objectNode, UnifiedXamlElement? parent, int siblingIndex)
    {
        var element = new UnifiedXamlElement
        {
            SemanticObject = objectNode,
            SemanticNode = objectNode,
            Parent = parent,
            SiblingIndex = siblingIndex
        };

        // Extract type information
        if (objectNode.Type is IXamlAstTypeReference typeRef)
        {
            element.ResolvedType = ConvertTypeReference(typeRef);
            element.ElementType = element.ResolvedType;
            element.TypeName = element.ResolvedType?.Name ?? "Unknown";
        }

        // Extract source location
        element.Location = ConvertLineInfo(objectNode);

        // Process children
        int propertyIndex = 0;
        int childIndex = 0;

        foreach (var child in objectNode.Children)
        {
            if (child is XamlAstXamlPropertyValueNode propertyNode)
            {
                var property = ConvertPropertyNode(propertyNode, element, propertyIndex++);
                element.AddProperty(property);
            }
            else if (child is XamlAstObjectNode childObject)
            {
                var childElement = ConvertObjectNode(childObject, element, childIndex++);
                element.AddChild(childElement);
            }
            else if (child is XamlAstTextNode textNode)
            {
                element.TextContent = textNode.Text;
            }
        }

        return element;
    }

    /// <summary>
    /// Converts a XamlAstXamlPropertyValueNode to a UnifiedXamlProperty.
    /// </summary>
    private UnifiedXamlProperty ConvertPropertyNode(XamlAstXamlPropertyValueNode propertyNode, UnifiedXamlElement parent, int siblingIndex)
    {
        var property = new UnifiedXamlProperty
        {
            SemanticProperty = propertyNode,
            SemanticNode = propertyNode,
            Parent = parent,
            SiblingIndex = siblingIndex,
            Kind = propertyNode.IsAttributeSyntax ? PropertyKind.Attribute : PropertyKind.PropertyElement
        };

        // Extract property name and type info
        if (propertyNode.Property is IXamlAstPropertyReference propertyRef)
        {
            property.PropertyName = ExtractPropertyName(propertyRef);
            property.PropertyInfo = ConvertPropertyReference(propertyRef);

            // Extract property type
            if (propertyRef is XamlAstClrProperty clrProperty && clrProperty.Getter != null)
            {
                property.PropertyType = ConvertType(clrProperty.Getter.ReturnType);
            }
        }

        // Extract source location
        property.Location = ConvertLineInfo(propertyNode);

        // Convert property values
        if (propertyNode.Values.Count > 0)
        {
            var firstValue = propertyNode.Values[0];

            if (firstValue is XamlAstObjectNode objectValue)
            {
                property.Value = ConvertObjectNode(objectValue, parent, 0);
            }
            else if (firstValue is XamlAstTextNode textValue)
            {
                property.Value = textValue.Text;
            }
            else if (IsMarkupExtension(firstValue))
            {
                property.MarkupExtension = ConvertMarkupExtension(firstValue, property);
            }
        }

        return property;
    }

    /// <summary>
    /// Enriches an existing UnifiedXamlProperty with XamlX semantic information.
    /// </summary>
    private void EnrichProperty(UnifiedXamlProperty property, XamlAstXamlPropertyValueNode propertyNode)
    {
        property.SemanticProperty = propertyNode;
        property.SemanticNode = propertyNode;

        // Extract property info
        if (propertyNode.Property is IXamlAstPropertyReference propertyRef)
        {
            property.PropertyInfo = ConvertPropertyReference(propertyRef);

            // Extract property type
            if (propertyRef is XamlAstClrProperty clrProperty && clrProperty.Getter != null)
            {
                property.PropertyType = ConvertType(clrProperty.Getter.ReturnType);
            }
        }

        // Update location
        property.Location = ConvertLineInfo(propertyNode);
    }

    /// <summary>
    /// Converts an IXamlAstTypeReference to IXamlType.
    /// </summary>
    private TypeSystem.IXamlType? ConvertTypeReference(IXamlAstTypeReference typeRef)
    {
        if (typeRef is XamlAstClrTypeReference clrTypeRef)
        {
            return ConvertType(clrTypeRef.Type);
        }

        return null;
    }

    /// <summary>
    /// Converts an IXamlType to our internal IXamlType interface.
    /// </summary>
    private TypeSystem.IXamlType? ConvertType(XamlX.TypeSystem.IXamlType? xamlType)
    {
        if (xamlType == null)
        {
            return null;
        }

        // Create a wrapper that implements our IXamlType interface
        return new XamlTypeWrapper(xamlType);
    }

    /// <summary>
    /// Converts an IXamlAstPropertyReference to IXamlProperty.
    /// </summary>
    private TypeSystem.IXamlProperty? ConvertPropertyReference(IXamlAstPropertyReference propertyRef)
    {
        if (propertyRef is XamlAstClrProperty clrProperty)
        {
            return new XamlPropertyWrapperFromAst(clrProperty);
        }

        return null;
    }

    /// <summary>
    /// Extracts the property name from a property reference.
    /// </summary>
    private string ExtractPropertyName(IXamlAstPropertyReference propertyRef)
    {
        if (propertyRef is XamlAstClrProperty clrProperty)
        {
            return clrProperty.Name ?? "Unknown";
        }

        return "Unknown";
    }

    /// <summary>
    /// Converts XamlX AST node line info to SourceLocation.
    /// </summary>
    private SourceLocation ConvertLineInfo(IXamlAstNode? node)
    {
        if (node == null)
        {
            return new SourceLocation();
        }

        return new SourceLocation
        {
            Line = node.Line,
            Column = node.Position
        };
    }

    /// <summary>
    /// Checks if a node is a markup extension.
    /// </summary>
    private bool IsMarkupExtension(IXamlAstValueNode node)
    {
        // XamlX represents markup extensions as object nodes with special types
        if (node is XamlAstObjectNode objNode)
        {
            var typeName = objNode.Type.GetClrType()?.FullName;
            return typeName != null && typeName.EndsWith("Extension");
        }

        return false;
    }

    /// <summary>
    /// Converts a markup extension node to UnifiedXamlMarkupExtension.
    /// </summary>
    private UnifiedXamlMarkupExtension ConvertMarkupExtension(IXamlAstValueNode node, UnifiedXamlProperty parent)
    {
        var extension = new UnifiedXamlMarkupExtension
        {
            SemanticExtension = node,
            SemanticNode = node,
            Parent = parent
        };

        if (node is XamlAstObjectNode objNode)
        {
            // Extract extension name
            var typeName = objNode.Type.GetClrType()?.Name ?? "Unknown";
            if (typeName.EndsWith("Extension"))
            {
                typeName = typeName.Substring(0, typeName.Length - "Extension".Length);
            }
            extension.ExtensionName = typeName;

            // Extract parameters from properties
            foreach (var child in objNode.Children)
            {
                if (child is XamlAstXamlPropertyValueNode propNode)
                {
                    var propName = ExtractPropertyName(propNode.Property);
                    if (propNode.Values.Count > 0)
                    {
                        var value = propNode.Values[0];
                        if (value is XamlAstTextNode textNode)
                        {
                            extension.SetParameter(propName, textNode.Text);
                        }
                    }
                }
            }
        }

        return extension;
    }

    /// <summary>
    /// Finds or creates a property in the unified element that corresponds to the XamlX property node.
    /// </summary>
    private UnifiedXamlProperty FindOrCreateProperty(UnifiedXamlElement element, XamlAstXamlPropertyValueNode propertyNode, int index)
    {
        var propertyName = ExtractPropertyName(propertyNode.Property);

        // Try to find existing property by name
        var existing = element.GetProperty(propertyName);
        if (existing != null)
        {
            return existing;
        }

        // Create new property
        var property = new UnifiedXamlProperty
        {
            PropertyName = propertyName,
            Parent = element,
            SiblingIndex = index,
            Kind = propertyNode.IsAttributeSyntax ? PropertyKind.Attribute : PropertyKind.PropertyElement
        };

        element.AddProperty(property);
        return property;
    }

    /// <summary>
    /// Finds or creates a child element that corresponds to the XamlX object node.
    /// </summary>
    private UnifiedXamlElement FindOrCreateChild(UnifiedXamlElement element, XamlAstObjectNode childNode, int index)
    {
        // For now, just create a new child
        // In the future, we could try to match by type or other criteria
        var child = new UnifiedXamlElement
        {
            Parent = element,
            SiblingIndex = index
        };

        element.AddChild(child);
        return child;
    }
}

/// <summary>
/// Wrapper for XamlX.TypeSystem.IXamlType to implement our IXamlType interface.
/// </summary>
internal class XamlTypeWrapper : TypeSystem.IXamlType
{
    private readonly XamlX.TypeSystem.IXamlType _xamlXType;

    public XamlTypeWrapper(XamlX.TypeSystem.IXamlType xamlXType)
    {
        _xamlXType = xamlXType;
    }

    public object Id => _xamlXType;
    public string Name => _xamlXType.Name;
    public string FullName => _xamlXType.FullName;
    public string? Namespace => _xamlXType.Namespace;
    public bool IsPublic => _xamlXType.IsPublic;
    public bool IsValueType => _xamlXType.IsValueType;
    public bool IsEnum => _xamlXType.IsEnum;

    public TypeSystem.IXamlAssembly? Assembly => _xamlXType.Assembly != null
        ? new XamlAssemblyWrapper(_xamlXType.Assembly)
        : null;

    public IReadOnlyList<TypeSystem.IXamlProperty> Properties =>
        _xamlXType.Properties.Select(p => new XamlPropertyWrapper(p)).ToList();

    public IReadOnlyList<TypeSystem.IXamlType> Interfaces =>
        _xamlXType.Interfaces.Select(i => new XamlTypeWrapper(i)).ToList();

    public TypeSystem.IXamlType? BaseType => _xamlXType.BaseType != null
        ? new XamlTypeWrapper(_xamlXType.BaseType)
        : null;

    public bool IsAssignableFrom(TypeSystem.IXamlType type)
    {
        if (type is XamlTypeWrapper wrapper)
        {
            return _xamlXType.IsAssignableFrom(wrapper._xamlXType);
        }
        return false;
    }
}

/// <summary>
/// Wrapper for XamlX.TypeSystem.IXamlAssembly to implement our IXamlAssembly interface.
/// </summary>
internal class XamlAssemblyWrapper : TypeSystem.IXamlAssembly
{
    private readonly XamlX.TypeSystem.IXamlAssembly _xamlXAssembly;

    public XamlAssemblyWrapper(XamlX.TypeSystem.IXamlAssembly xamlXAssembly)
    {
        _xamlXAssembly = xamlXAssembly;
    }

    public string Name => _xamlXAssembly.Name;

    public TypeSystem.IXamlType? FindType(string fullName)
    {
        var type = _xamlXAssembly.FindType(fullName);
        return type != null ? new XamlTypeWrapper(type) : null;
    }
}

/// <summary>
/// Wrapper for XamlX.TypeSystem.IXamlProperty to implement our IXamlProperty interface.
/// </summary>
internal class XamlPropertyWrapper : TypeSystem.IXamlProperty
{
    private readonly XamlX.TypeSystem.IXamlProperty _xamlXProperty;

    public XamlPropertyWrapper(XamlX.TypeSystem.IXamlProperty xamlXProperty)
    {
        _xamlXProperty = xamlXProperty;
    }

    public string Name => _xamlXProperty.Name;
    public TypeSystem.IXamlType PropertyType => new XamlTypeWrapper(_xamlXProperty.PropertyType);
    public TypeSystem.IXamlType? DeclaringType => _xamlXProperty.Getter?.DeclaringType != null
        ? new XamlTypeWrapper(_xamlXProperty.Getter.DeclaringType)
        : null;

    public bool IsAttached => false; // XamlX doesn't directly expose this
    public bool CanRead => _xamlXProperty.Getter != null;
    public bool CanWrite => _xamlXProperty.Setter != null;
}

/// <summary>
/// Wrapper for XamlAstClrProperty (from AST) to implement our IXamlProperty interface.
/// </summary>
internal class XamlPropertyWrapperFromAst : TypeSystem.IXamlProperty
{
    private readonly XamlAstClrProperty _clrProperty;

    public XamlPropertyWrapperFromAst(XamlAstClrProperty clrProperty)
    {
        _clrProperty = clrProperty;
    }

    public string Name => _clrProperty.Name;
    public TypeSystem.IXamlType PropertyType => _clrProperty.Getter != null
        ? new XamlTypeWrapper(_clrProperty.Getter.ReturnType)
        : throw new InvalidOperationException("Property has no getter");
    public TypeSystem.IXamlType? DeclaringType => new XamlTypeWrapper(_clrProperty.DeclaringType);

    public bool IsAttached => false; // Would need additional detection logic
    public bool CanRead => _clrProperty.Getter != null;
    public bool CanWrite => _clrProperty.Setters.Count > 0;
}
