using System.Xml;
using System.Xml.Linq;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Converters;

/// <summary>
/// Converts XML (System.Xml.Linq) representation to Unified AST.
/// This is the first stage of the hybrid parsing pipeline.
/// </summary>
public sealed class XmlToUnifiedConverter
{
    private readonly DiagnosticCollector _diagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlToUnifiedConverter"/> class.
    /// </summary>
    public XmlToUnifiedConverter(DiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    /// <summary>
    /// Converts an XDocument to a UnifiedXamlDocument.
    /// </summary>
    public UnifiedXamlDocument Convert(XDocument xDocument, string? filePath = null)
    {
        var document = new UnifiedXamlDocument
        {
            XmlDocument = xDocument,
            FilePath = filePath,
            DiagnosticCollector = _diagnostics,
            HasXmlDeclaration = xDocument.Declaration != null,
            Encoding = xDocument.Declaration?.Encoding ?? "UTF-8"
        };

        // Convert root element
        if (xDocument.Root != null)
        {
            document.Root = ConvertElement(xDocument.Root, null, 0);

            // Populate symbol table
            PopulateSymbolTable(document);
        }

        return document;
    }

    /// <summary>
    /// Converts an XElement to a UnifiedXamlElement.
    /// </summary>
    private UnifiedXamlElement ConvertElement(XElement xElement, UnifiedXamlElement? parent, int siblingIndex)
    {
        var element = new UnifiedXamlElement
        {
            XmlElement = xElement,
            XmlNode = xElement,
            XmlNamespace = xElement.Name.Namespace,
            Parent = parent,
            SiblingIndex = siblingIndex
        };

        // Parse type name and namespace
        ParseTypeName(xElement.Name, out var typeName, out var typeNamespace);
        element.TypeName = typeName;
        element.Namespace = typeNamespace;

        // Extract source location
        element.Location = ExtractLocation(xElement);

        // Extract formatting hints
        element.Formatting = ExtractFormatting(xElement);

        // Process attributes (properties and XAML directives)
        int propertyIndex = 0;
        foreach (var attribute in xElement.Attributes())
        {
            // Skip namespace declarations
            if (attribute.IsNamespaceDeclaration)
            {
                continue;
            }

            // Check for XAML directives
            if (attribute.Name.Namespace == XNamespace.Xml ||
                attribute.Name.LocalName.StartsWith("x:") ||
                attribute.Name.Namespace.NamespaceName.Contains("schemas.microsoft.com/winfx/2006/xaml"))
            {
                ProcessXamlDirective(element, attribute);
            }
            else
            {
                var property = ConvertAttribute(attribute, element, propertyIndex++);
                element.AddProperty(property);
            }
        }

        // Process child elements
        int childIndex = 0;
        foreach (var childElement in xElement.Elements())
        {
            // Check if this is a property element
            if (IsPropertyElement(childElement))
            {
                var property = ConvertPropertyElement(childElement, element, propertyIndex++);
                element.AddProperty(property);
            }
            else
            {
                var child = ConvertElement(childElement, element, childIndex++);
                element.AddChild(child);
            }
        }

        // Handle text content
        if (xElement.Nodes().Any(n => n is XText))
        {
            element.TextContent = string.Concat(xElement.Nodes().OfType<XText>().Select(t => t.Value));
        }

        return element;
    }

    /// <summary>
    /// Converts an XAttribute to a UnifiedXamlProperty.
    /// </summary>
    private UnifiedXamlProperty ConvertAttribute(XAttribute attribute, UnifiedXamlElement parent, int siblingIndex)
    {
        var property = new UnifiedXamlProperty
        {
            XmlAttribute = attribute,
            // Note: XAttribute doesn't inherit from XNode, so we don't set XmlNode here
            Parent = parent,
            SiblingIndex = siblingIndex,
            Kind = PropertyKind.Attribute
        };

        // Parse property name (handle attached properties)
        var fullName = attribute.Name.LocalName;
        var (attachedOwner, propertyName) = UnifiedXamlProperty.ParseAttachedProperty(fullName);

        property.PropertyName = propertyName;
        property.AttachedOwnerType = attachedOwner;

        if (attachedOwner != null)
        {
            property.Kind = PropertyKind.AttachedProperty;
        }

        // Parse value (check for markup extensions)
        var value = attribute.Value;
        if (IsMarkupExtension(value))
        {
            property.MarkupExtension = ParseMarkupExtension(value, property);
        }
        else
        {
            property.Value = value;
        }

        // Extract location
        property.Location = ExtractLocation(attribute);

        return property;
    }

    /// <summary>
    /// Converts a property element to a UnifiedXamlProperty.
    /// </summary>
    private UnifiedXamlProperty ConvertPropertyElement(XElement element, UnifiedXamlElement parent, int siblingIndex)
    {
        var property = new UnifiedXamlProperty
        {
            XmlPropertyElement = element,
            XmlNode = element,
            Parent = parent,
            SiblingIndex = siblingIndex,
            Kind = PropertyKind.PropertyElement
        };

        // Parse property name
        var fullName = element.Name.LocalName;
        var dotIndex = fullName.IndexOf('.');
        if (dotIndex > 0)
        {
            property.PropertyName = fullName.Substring(dotIndex + 1);
        }
        else
        {
            property.PropertyName = fullName;
        }

        // Extract location and formatting
        property.Location = ExtractLocation(element);
        property.Formatting = ExtractFormatting(element);

        // Process child content
        if (element.HasElements)
        {
            var children = element.Elements().ToList();
            if (children.Count == 1)
            {
                // Single child: set as property value directly
                property.Value = ConvertElement(children[0], parent, 0);
            }
            else if (children.Count > 1)
            {
                // Multiple children: create a collection element
                var collectionElement = new UnifiedXamlElement
                {
                    TypeName = fullName, // e.g., "Window.Resources"
                    XmlElement = element,
                    XmlNode = element,
                    Parent = parent,
                    Location = ExtractLocation(element),
                    Formatting = ExtractFormatting(element)
                };

                int childIndex = 0;
                foreach (var childXml in children)
                {
                    var child = ConvertElement(childXml, collectionElement, childIndex++);
                    collectionElement.AddChild(child);
                }

                property.Value = collectionElement;
            }
        }
        else if (!string.IsNullOrWhiteSpace(element.Value))
        {
            // Check for markup extension in text content
            var value = element.Value.Trim();
            if (IsMarkupExtension(value))
            {
                property.MarkupExtension = ParseMarkupExtension(value, property);
            }
            else
            {
                property.Value = value;
            }
        }

        return property;
    }

    /// <summary>
    /// Parses a markup extension string.
    /// </summary>
    private UnifiedXamlMarkupExtension? ParseMarkupExtension(string value, UnifiedXamlProperty parentProperty)
    {
        if (!IsMarkupExtension(value))
        {
            return null;
        }

        // Remove curly braces
        var content = value.Trim().Substring(1, value.Trim().Length - 2).Trim();

        var extension = new UnifiedXamlMarkupExtension
        {
            Parent = parentProperty
        };

        // Parse extension name
        var spaceIndex = content.IndexOf(' ');
        if (spaceIndex > 0)
        {
            extension.ExtensionName = content.Substring(0, spaceIndex);
            var parameters = content.Substring(spaceIndex + 1).Trim();
            ParseMarkupExtensionParameters(extension, parameters);
        }
        else
        {
            extension.ExtensionName = content;
        }

        // Populate specific extension types
        PopulateExtensionDetails(extension);

        return extension;
    }

    /// <summary>
    /// Parses markup extension parameters.
    /// </summary>
    private void ParseMarkupExtensionParameters(UnifiedXamlMarkupExtension extension, string parameters)
    {
        // Simple parameter parsing (this can be enhanced for nested extensions)
        if (!parameters.Contains('='))
        {
            // Positional argument
            extension.PositionalArgument = parameters;
        }
        else
        {
            // Named parameters
            var parts = parameters.Split(',');
            foreach (var part in parts)
            {
                var equalIndex = part.IndexOf('=');
                if (equalIndex > 0)
                {
                    var key = part.Substring(0, equalIndex).Trim();
                    var value = part.Substring(equalIndex + 1).Trim();
                    extension.SetParameter(key, value);
                }
            }
        }
    }

    /// <summary>
    /// Populates extension-specific details based on extension type.
    /// </summary>
    private void PopulateExtensionDetails(UnifiedXamlMarkupExtension extension)
    {
        switch (extension.GetExtensionType())
        {
            case MarkupExtensionType.Binding:
                extension.Binding = new BindingExpression
                {
                    Path = extension.PositionalArgument?.ToString(),
                    Mode = extension.GetParameter<string>("Mode"),
                    UpdateSourceTrigger = extension.GetParameter<string>("UpdateSourceTrigger"),
                    Converter = extension.GetParameter<string>("Converter"),
                    ConverterParameter = extension.GetParameter<object>("ConverterParameter"),
                    StringFormat = extension.GetParameter<string>("StringFormat"),
                    ElementName = extension.GetParameter<string>("ElementName")
                };
                break;

            case MarkupExtensionType.StaticResource:
            case MarkupExtensionType.DynamicResource:
                extension.Resource = new ResourceReference
                {
                    ResourceKey = extension.PositionalArgument?.ToString(),
                    IsDynamic = extension.GetExtensionType() == MarkupExtensionType.DynamicResource
                };
                break;

            case MarkupExtensionType.Type:
                extension.Type = new TypeReference
                {
                    TypeName = extension.PositionalArgument?.ToString()
                };
                break;

            case MarkupExtensionType.Static:
                extension.Static = new StaticReference
                {
                    MemberName = extension.PositionalArgument?.ToString()
                };
                break;
        }
    }

    /// <summary>
    /// Processes XAML directives (x:Name, x:Class, etc.).
    /// </summary>
    private void ProcessXamlDirective(UnifiedXamlElement element, XAttribute attribute)
    {
        var name = attribute.Name.LocalName;
        var value = attribute.Value;

        switch (name)
        {
            case "Name":
                element.XName = value;
                break;
            case "Key":
                element.XKey = value;
                break;
            case "Class":
                element.XClass = value;
                break;
            case "FieldModifier":
                element.XFieldModifier = value;
                break;
            case "Shared":
                element.XShared = bool.TryParse(value, out var shared) ? shared : null;
                break;
        }
    }

    /// <summary>
    /// Populates the symbol table with named elements and type usage.
    /// </summary>
    private void PopulateSymbolTable(UnifiedXamlDocument document)
    {
        if (document.Root == null)
        {
            return;
        }

        // Register namespace prefixes from root element
        foreach (var ns in document.Root.XmlElement?.Attributes()
            .Where(a => a.IsNamespaceDeclaration) ?? Enumerable.Empty<XAttribute>())
        {
            var prefix = ns.Name.LocalName == "xmlns" ? "" : ns.Name.LocalName;
            document.Symbols.RegisterNamespacePrefix(prefix, ns.Value);
        }

        // Walk the tree and populate symbol table
        foreach (var element in document.Root.DescendantsAndSelf())
        {
            // Register named elements
            if (!string.IsNullOrEmpty(element.XName))
            {
                document.Symbols.RegisterNamedElement(element.XName, element);
            }

            // Register type usage
            document.Symbols.RegisterTypeUsage(element.TypeName, element);
        }
    }

    /// <summary>
    /// Extracts source location from XML node.
    /// </summary>
    private SourceLocation ExtractLocation(XObject xObject)
    {
        var location = new SourceLocation();

        if (xObject is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
        {
            location.Line = lineInfo.LineNumber;
            location.Column = lineInfo.LinePosition;
        }

        return location;
    }

    /// <summary>
    /// Extracts formatting hints from XML element.
    /// </summary>
    private FormattingHints ExtractFormatting(XElement element)
    {
        var hints = new FormattingHints();

        // Capture original text for full preservation
        hints.OriginalText = element.ToString();

        // Detect whitespace patterns
        var previousNode = element.PreviousNode;
        if (previousNode is XText textBefore)
        {
            hints.LeadingWhitespace = textBefore.Value;
        }

        var nextNode = element.NextNode;
        if (nextNode is XText textAfter)
        {
            hints.TrailingWhitespace = textAfter.Value;
            hints.HasNewlineAfter = textAfter.Value.Contains('\n');
        }

        // Capture comments
        hints.AssociatedComments.AddRange(
            element.Nodes()
                .OfType<XComment>()
                .Concat(element.DescendantNodes().OfType<XComment>())
        );

        return hints;
    }

    /// <summary>
    /// Parses a type name into local name and namespace.
    /// </summary>
    private void ParseTypeName(XName xName, out string typeName, out string? typeNamespace)
    {
        typeName = xName.LocalName;
        typeNamespace = xName.Namespace == XNamespace.None ? null : xName.Namespace.NamespaceName;
    }

    /// <summary>
    /// Determines if an element is a property element.
    /// </summary>
    private bool IsPropertyElement(XElement element)
    {
        return element.Name.LocalName.Contains('.');
    }

    /// <summary>
    /// Determines if a string is a markup extension.
    /// </summary>
    private bool IsMarkupExtension(string value)
    {
        return value.Trim().StartsWith("{") && value.Trim().EndsWith("}") && !value.Trim().StartsWith("{}");
    }
}
