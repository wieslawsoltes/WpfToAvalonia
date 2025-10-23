using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Visitors;

/// <summary>
/// Base class for visitors that traverse the Unified XAML AST.
/// Provides default depth-first traversal implementation.
/// </summary>
public abstract class UnifiedXamlVisitorBase : IUnifiedXamlVisitor
{
    /// <summary>
    /// Gets or sets a value indicating whether to visit children of elements.
    /// </summary>
    protected bool VisitChildren { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to visit properties of elements.
    /// </summary>
    protected bool VisitProperties { get; set; } = true;

    /// <summary>
    /// Visits a XAML document.
    /// </summary>
    public virtual void VisitDocument(UnifiedXamlDocument document)
    {
        if (document.Root != null)
        {
            VisitElement(document.Root);
        }
    }

    /// <summary>
    /// Visits a XAML element.
    /// </summary>
    public virtual void VisitElement(UnifiedXamlElement element)
    {
        // Visit properties first
        if (VisitProperties)
        {
            foreach (var property in element.Properties)
            {
                VisitProperty(property);
            }
        }

        // Then visit children
        if (VisitChildren)
        {
            foreach (var child in element.Children)
            {
                VisitElement(child);
            }
        }
    }

    /// <summary>
    /// Visits a XAML property.
    /// </summary>
    public virtual void VisitProperty(UnifiedXamlProperty property)
    {
        // Visit markup extension if present
        if (property.MarkupExtension != null)
        {
            VisitMarkupExtension(property.MarkupExtension);
        }

        // If the value is an element, visit it
        if (property.Value is UnifiedXamlElement elementValue)
        {
            VisitElement(elementValue);
        }
    }

    /// <summary>
    /// Visits a markup extension.
    /// </summary>
    public virtual void VisitMarkupExtension(UnifiedXamlMarkupExtension markupExtension)
    {
        // Visit nested markup extensions in parameters
        foreach (var param in markupExtension.Parameters.Values)
        {
            if (param is UnifiedXamlMarkupExtension nestedExtension)
            {
                VisitMarkupExtension(nestedExtension);
            }
        }
    }
}

/// <summary>
/// Base class for visitors that transform the Unified XAML AST.
/// Provides default implementation that clones the tree.
/// </summary>
public abstract class UnifiedXamlTransformVisitorBase : IUnifiedXamlTransformVisitor
{
    /// <summary>
    /// Gets or sets a value indicating whether to transform children of elements.
    /// </summary>
    protected bool TransformChildren { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to transform properties of elements.
    /// </summary>
    protected bool TransformProperties { get; set; } = true;

    /// <summary>
    /// Transforms a XAML document.
    /// </summary>
    public virtual UnifiedXamlDocument? TransformDocument(UnifiedXamlDocument document)
    {
        var transformed = new UnifiedXamlDocument
        {
            XmlDocument = document.XmlDocument,
            FilePath = document.FilePath,
            SemanticDocument = document.SemanticDocument,
            DiagnosticCollector = document.DiagnosticCollector,
            Encoding = document.Encoding,
            HasXmlDeclaration = document.HasXmlDeclaration
        };

        // Copy metadata
        foreach (var kvp in document.Metadata)
        {
            transformed.Metadata[kvp.Key] = kvp.Value;
        }

        // Copy diagnostics
        foreach (var diagnostic in document.Diagnostics)
        {
            transformed.Diagnostics.Add(diagnostic);
        }

        // Transform root element
        if (document.Root != null)
        {
            transformed.Root = TransformElement(document.Root);
            if (transformed.Root != null)
            {
                transformed.Root.Parent = null;
            }
        }

        return transformed;
    }

    /// <summary>
    /// Transforms a XAML element.
    /// </summary>
    public virtual UnifiedXamlElement? TransformElement(UnifiedXamlElement element)
    {
        var transformed = new UnifiedXamlElement
        {
            // XML Layer
            XmlElement = element.XmlElement,
            XmlNode = element.XmlNode,
            XmlPath = element.XmlPath,
            XmlNamespace = element.XmlNamespace,

            // XamlX Layer
            SemanticObject = element.SemanticObject,
            SemanticNode = element.SemanticNode,
            ElementType = element.ElementType,
            ResolvedType = element.ResolvedType,

            // Roslyn Layer
            CodeBehindSymbol = element.CodeBehindSymbol,

            // Unified Structure
            TypeName = element.TypeName,
            Namespace = element.Namespace,
            TextContent = element.TextContent,

            // Special XAML Directives
            XName = element.XName,
            XKey = element.XKey,
            XClass = element.XClass,
            XFieldModifier = element.XFieldModifier,
            XShared = element.XShared,

            // Content Properties
            UsesContentProperty = element.UsesContentProperty,
            ContentPropertyName = element.ContentPropertyName,

            // Metadata
            Location = element.Location,
            Formatting = element.Formatting,
            State = element.State,
            Parent = element.Parent,
            SiblingIndex = element.SiblingIndex
        };

        // Copy metadata
        foreach (var kvp in element.Metadata)
        {
            transformed.Metadata[kvp.Key] = kvp.Value;
        }

        // Copy diagnostics
        foreach (var diagnostic in element.Diagnostics)
        {
            transformed.Diagnostics.Add(diagnostic);
        }

        // Transform properties
        if (TransformProperties)
        {
            foreach (var property in element.Properties)
            {
                var transformedProperty = TransformProperty(property);
                if (transformedProperty != null)
                {
                    transformed.AddProperty(transformedProperty);
                }
            }
        }

        // Transform children
        if (TransformChildren)
        {
            foreach (var child in element.Children)
            {
                var transformedChild = TransformElement(child);
                if (transformedChild != null)
                {
                    transformed.AddChild(transformedChild);
                }
            }
        }

        return transformed;
    }

    /// <summary>
    /// Transforms a XAML property.
    /// </summary>
    public virtual UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property)
    {
        var transformed = new UnifiedXamlProperty
        {
            // XML Layer
            XmlAttribute = property.XmlAttribute,
            XmlPropertyElement = property.XmlPropertyElement,
            XmlNode = property.XmlNode,
            XmlPath = property.XmlPath,

            // XamlX Layer
            SemanticProperty = property.SemanticProperty,
            SemanticNode = property.SemanticNode,
            PropertyInfo = property.PropertyInfo,
            PropertyType = property.PropertyType,
            ResolvedType = property.ResolvedType,

            // Roslyn Layer
            CodeBehindSymbol = property.CodeBehindSymbol,

            // Unified Structure
            PropertyName = property.PropertyName,
            Value = property.Value,
            Kind = property.Kind,

            // Attached Properties
            AttachedOwnerType = property.AttachedOwnerType,

            // Type Conversion
            TypeConverterName = property.TypeConverterName,
            RequiresTypeConversion = property.RequiresTypeConversion,

            // Metadata
            Location = property.Location,
            Formatting = property.Formatting,
            State = property.State,
            Parent = property.Parent,
            SiblingIndex = property.SiblingIndex
        };

        // Copy metadata
        foreach (var kvp in property.Metadata)
        {
            transformed.Metadata[kvp.Key] = kvp.Value;
        }

        // Copy diagnostics
        foreach (var diagnostic in property.Diagnostics)
        {
            transformed.Diagnostics.Add(diagnostic);
        }

        // Transform markup extension
        if (property.MarkupExtension != null)
        {
            transformed.MarkupExtension = TransformMarkupExtension(property.MarkupExtension);
        }

        // Transform element value
        if (property.Value is UnifiedXamlElement elementValue)
        {
            transformed.Value = TransformElement(elementValue);
        }

        return transformed;
    }

    /// <summary>
    /// Transforms a markup extension.
    /// </summary>
    public virtual UnifiedXamlMarkupExtension? TransformMarkupExtension(UnifiedXamlMarkupExtension markupExtension)
    {
        var transformed = new UnifiedXamlMarkupExtension
        {
            // XamlX Layer
            SemanticExtension = markupExtension.SemanticExtension,
            SemanticNode = markupExtension.SemanticNode,
            ResolvedType = markupExtension.ResolvedType,

            // Roslyn Layer
            CodeBehindSymbol = markupExtension.CodeBehindSymbol,

            // Unified Structure
            ExtensionName = markupExtension.ExtensionName,
            PositionalArgument = markupExtension.PositionalArgument,

            // Specific Extension Types
            Binding = markupExtension.Binding,
            Resource = markupExtension.Resource,
            Type = markupExtension.Type,
            Static = markupExtension.Static,

            // Metadata
            Location = markupExtension.Location,
            Formatting = markupExtension.Formatting,
            State = markupExtension.State,
            Parent = markupExtension.Parent,
            SiblingIndex = markupExtension.SiblingIndex
        };

        // Copy metadata
        foreach (var kvp in markupExtension.Metadata)
        {
            transformed.Metadata[kvp.Key] = kvp.Value;
        }

        // Copy diagnostics
        foreach (var diagnostic in markupExtension.Diagnostics)
        {
            transformed.Diagnostics.Add(diagnostic);
        }

        // Copy and transform parameters
        foreach (var param in markupExtension.Parameters)
        {
            if (param.Value is UnifiedXamlMarkupExtension nestedExtension)
            {
                transformed.Parameters[param.Key] = TransformMarkupExtension(nestedExtension);
            }
            else
            {
                transformed.Parameters[param.Key] = param.Value;
            }
        }

        return transformed;
    }
}

/// <summary>
/// Base class for visitors that collect results from the Unified XAML AST.
/// </summary>
public abstract class UnifiedXamlCollectorVisitor<T> : IUnifiedXamlVisitor<List<T>>
{
    /// <summary>
    /// Gets the collected results.
    /// </summary>
    protected List<T> Results { get; } = new();

    /// <summary>
    /// Visits a XAML document and collects results.
    /// </summary>
    public virtual List<T> VisitDocument(UnifiedXamlDocument document)
    {
        Results.Clear();

        if (document.Root != null)
        {
            VisitElement(document.Root);
        }

        return Results;
    }

    /// <summary>
    /// Visits a XAML element and collects results.
    /// </summary>
    public virtual List<T> VisitElement(UnifiedXamlElement element)
    {
        // Visit properties
        foreach (var property in element.Properties)
        {
            VisitProperty(property);
        }

        // Visit children
        foreach (var child in element.Children)
        {
            VisitElement(child);
        }

        return Results;
    }

    /// <summary>
    /// Visits a XAML property and collects results.
    /// </summary>
    public virtual List<T> VisitProperty(UnifiedXamlProperty property)
    {
        // Visit markup extension
        if (property.MarkupExtension != null)
        {
            VisitMarkupExtension(property.MarkupExtension);
        }

        // Visit element value
        if (property.Value is UnifiedXamlElement elementValue)
        {
            VisitElement(elementValue);
        }

        return Results;
    }

    /// <summary>
    /// Visits a markup extension and collects results.
    /// </summary>
    public virtual List<T> VisitMarkupExtension(UnifiedXamlMarkupExtension markupExtension)
    {
        // Visit nested markup extensions
        foreach (var param in markupExtension.Parameters.Values)
        {
            if (param is UnifiedXamlMarkupExtension nestedExtension)
            {
                VisitMarkupExtension(nestedExtension);
            }
        }

        return Results;
    }
}
