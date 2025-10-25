using System.Xml;
using System.Xml.Linq;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.Formatting;
using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Converters;

/// <summary>
/// Converts XML (System.Xml.Linq) representation to Unified AST.
/// This is the first stage of the hybrid parsing pipeline.
/// </summary>
public sealed class XmlToUnifiedConverter
{
    private readonly DiagnosticCollector _diagnostics;
    private string? _sourceXaml;
    private SourceBasedWhitespaceExtractor? _whitespaceExtractor;

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
    public UnifiedXamlDocument Convert(XDocument xDocument, string? filePath = null, string? sourceXaml = null)
    {
        _sourceXaml = sourceXaml;

        // Initialize source-based whitespace extractor if source is available
        if (!string.IsNullOrEmpty(sourceXaml))
        {
            _whitespaceExtractor = new SourceBasedWhitespaceExtractor(sourceXaml);
        }

        var document = new UnifiedXamlDocument
        {
            XmlDocument = xDocument,
            FilePath = filePath,
            DiagnosticCollector = _diagnostics,
            HasXmlDeclaration = xDocument.Declaration != null,
            Encoding = xDocument.Declaration?.Encoding ?? "UTF-8"
        };

        // Extract leading comments (before root element)
        if (xDocument.Root != null)
        {
            foreach (var node in xDocument.Nodes())
            {
                if (node == xDocument.Root)
                    break;

                if (node is XComment commentNode)
                {
                    var comment = ConvertComment(commentNode, null);
                    comment.Position = CommentPosition.BeforeElement;
                    document.LeadingComments.Add(comment);
                }
            }
        }

        // Convert root element
        if (xDocument.Root != null)
        {
            document.Root = ConvertElement(xDocument.Root, null, 0);

            // Populate symbol table
            PopulateSymbolTable(document);
        }

        // Extract trailing comments (after root element)
        if (xDocument.Root != null)
        {
            bool foundRoot = false;
            foreach (var node in xDocument.Nodes())
            {
                if (node == xDocument.Root)
                {
                    foundRoot = true;
                    continue;
                }

                if (foundRoot && node is XComment commentNode)
                {
                    var comment = ConvertComment(commentNode, null);
                    comment.Position = CommentPosition.AfterElement;
                    document.TrailingComments.Add(comment);
                }
            }
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
            SourceXmlElement = xElement,
            XmlNode = xElement,
            XmlNamespace = xElement.Name.Namespace,
            Parent = parent,
            SiblingIndex = siblingIndex
        };

        // Parse type name and namespace
        ParseTypeName(xElement.Name, out var typeName, out var typeNamespace);

        // Set legacy properties for backwards compatibility
        #pragma warning disable CS0618 // Type or member is obsolete
        element.TypeName = typeName;
        element.Namespace = typeNamespace;
        #pragma warning restore CS0618

        // Create strongly-typed TypeReference
        element.TypeReference = new QualifiedTypeName(typeName, typeNamespace);

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

        // Extract comments from child nodes
        foreach (var commentNode in xElement.Nodes().OfType<XComment>())
        {
            var comment = ConvertComment(commentNode, element);
            element.Comments.Add(comment);
        }

        // Handle text content
        if (xElement.Nodes().Any(n => n is XText))
        {
            element.TextContent = string.Concat(xElement.Nodes().OfType<XText>().Select(t => t.Value));
        }

        return element;
    }

    /// <summary>
    /// Converts an XComment to a UnifiedXamlComment.
    /// </summary>
    private UnifiedXamlComment ConvertComment(XComment xComment, UnifiedXamlNode? parent)
    {
        var comment = new UnifiedXamlComment
        {
            Text = xComment.Value,
            Parent = parent,
            Location = ExtractLocation(xComment)
        };

        // Determine comment position based on surrounding nodes
        if (xComment.Parent is XElement parentElement)
        {
            var previousSibling = xComment.PreviousNode;
            var nextSibling = xComment.NextNode;

            if (previousSibling == null && nextSibling != null)
            {
                // First node within parent - before content
                comment.Position = CommentPosition.WithinContent;
            }
            else if (nextSibling == null && previousSibling != null)
            {
                // Last node within parent - after content
                comment.Position = CommentPosition.WithinContent;
            }
            else if (previousSibling != null && nextSibling != null)
            {
                // Between elements - within content
                comment.Position = CommentPosition.WithinContent;
            }
            else
            {
                // Standalone
                comment.Position = CommentPosition.Standalone;
            }
        }
        else
        {
            // Document-level comment
            comment.Position = CommentPosition.Standalone;
        }

        return comment;
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
            SetPropertyValue(property, property.MarkupExtension);
        }
        else
        {
            SetPropertyValue(property, value);
        }

        // Extract location
        property.Location = ExtractLocation(attribute);

        // Extract formatting hints for attributes
        property.Formatting = ExtractAttributeFormatting(attribute, parent);

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
                var childElement = ConvertElement(children[0], parent, 0);
                SetPropertyValue(property, childElement);
            }
            else if (children.Count > 1)
            {
                // Multiple children: create a collection element
                var collectionElement = new UnifiedXamlElement
                {
                    #pragma warning disable CS0618
                    TypeName = fullName, // e.g., "Window.Resources"
                    #pragma warning restore CS0618
                    SourceXmlElement = element,
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

                SetPropertyValue(property, collectionElement);
            }
        }
        else if (!string.IsNullOrWhiteSpace(element.Value))
        {
            // Check for markup extension in text content
            var value = element.Value.Trim();
            if (IsMarkupExtension(value))
            {
                property.MarkupExtension = ParseMarkupExtension(value, property);
                SetPropertyValue(property, property.MarkupExtension);
            }
            else
            {
                SetPropertyValue(property, value);
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
        // Use source-based extraction if available for 100% accurate whitespace preservation
        if (_whitespaceExtractor != null)
        {
            var hints = _whitespaceExtractor.ExtractElementFormatting(element);

            // Capture comments
            hints.AssociatedComments.AddRange(
                element.Nodes()
                    .OfType<XComment>()
                    .Concat(element.DescendantNodes().OfType<XComment>())
            );

            // Capture original text for full preservation
            hints.OriginalText = element.ToString();

            return hints;
        }

        // Fallback to node-based extraction
        var fallbackHints = new FormattingHints();

        // Capture original text for full preservation
        fallbackHints.OriginalText = element.ToString();

        // Detect whitespace patterns
        var previousNode = element.PreviousNode;
        if (previousNode is XText textBefore)
        {
            fallbackHints.LeadingWhitespace = NormalizeLeadingWhitespace(textBefore.Value);
        }

        var nextNode = element.NextNode;
        if (nextNode is XText textAfter)
        {
            fallbackHints.TrailingWhitespace = NormalizeTrailingWhitespace(textAfter.Value);
            fallbackHints.HasNewlineAfter = textAfter.Value.Contains('\n');
        }

        // Capture comments
        fallbackHints.AssociatedComments.AddRange(
            element.Nodes()
                .OfType<XComment>()
                .Concat(element.DescendantNodes().OfType<XComment>())
        );

        return fallbackHints;
    }

    /// <summary>
    /// Normalizes leading whitespace to prevent extra blank lines.
    /// Keeps only the last newline with its indentation.
    /// </summary>
    private string NormalizeLeadingWhitespace(string whitespace)
    {
        if (string.IsNullOrEmpty(whitespace))
            return whitespace;

        // Check if there are any newlines
        if (!whitespace.Contains('\n') && !whitespace.Contains('\r'))
            return whitespace;

        // Split by newlines (handle both \r\n and \n)
        var lines = whitespace.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        // If there are multiple lines (meaning multiple newlines), keep only the last line as indentation
        // For example: "\n    \n    " becomes "\n    " (single newline + last indentation)
        if (lines.Length > 1)
        {
            // Return: newline + the indentation from the last line
            return "\n" + lines[^1];
        }

        // Single line - return as is (shouldn't happen if we got here, but safety check)
        return whitespace;
    }

    /// <summary>
    /// Normalizes trailing whitespace to prevent extra blank lines.
    /// Keeps only the first newline with minimal formatting.
    /// </summary>
    private string NormalizeTrailingWhitespace(string whitespace)
    {
        if (string.IsNullOrEmpty(whitespace))
            return whitespace;

        // Check if there are any newlines
        if (!whitespace.Contains('\n') && !whitespace.Contains('\r'))
            return whitespace;

        // Split by newlines (handle both \r\n and \n)
        var lines = whitespace.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        // Keep first newline and the indentation that follows
        if (lines.Length > 1)
        {
            return "\n" + lines[1];
        }

        // Just a newline
        return "\n";
    }

    /// <summary>
    /// Extracts formatting hints for attributes by parsing the element's XML string.
    /// Determines if the attribute should be on a new line based on the source formatting.
    /// </summary>
    private FormattingHints ExtractAttributeFormatting(XAttribute attribute, UnifiedXamlElement parent)
    {
        // Use source-based extraction if available for 100% accurate attribute whitespace preservation
        if (_whitespaceExtractor != null && parent.SourceXmlElement != null)
        {
            return _whitespaceExtractor.ExtractAttributeFormatting(attribute, parent.SourceXmlElement);
        }

        // Fallback to string-based extraction
        var hints = new FormattingHints();

        try
        {
            // Get the XML string of the parent element
            var xmlString = parent.SourceXmlElement?.ToString();
            if (string.IsNullOrEmpty(xmlString))
                return hints;

            // Find this attribute in the XML string
            var attributePattern = $@"{System.Text.RegularExpressions.Regex.Escape(attribute.Name.LocalName)}\s*=\s*""";
            var match = System.Text.RegularExpressions.Regex.Match(xmlString, attributePattern);

            if (match.Success)
            {
                // Look backwards from the match to find the preceding whitespace
                var precedingText = xmlString.Substring(0, match.Index);
                var lastNewlineIndex = precedingText.LastIndexOf('\n');

                if (lastNewlineIndex >= 0)
                {
                    // Extract whitespace from last newline to the attribute
                    var whitespace = precedingText.Substring(lastNewlineIndex);
                    hints.LeadingWhitespace = whitespace;
                    hints.PreserveLineBreak = true;
                }
                else
                {
                    // Attribute is on the same line as opening tag
                    // Check if there's whitespace before it
                    var precedingChar = precedingText.Length > 0 ? precedingText[^1] : '\0';
                    if (char.IsWhiteSpace(precedingChar))
                    {
                        hints.LeadingWhitespace = " ";
                    }
                }
            }
        }
        catch
        {
            // If extraction fails, return empty hints (will use default spacing)
        }

        return hints;
    }

    /// <summary>
    /// Sets the property value using both legacy and strongly-typed fields for backwards compatibility.
    /// </summary>
    private void SetPropertyValue(UnifiedXamlProperty property, object? value)
    {
        // Set legacy Value field for backwards compatibility
        #pragma warning disable CS0618 // Type or member is obsolete
        property.Value = value;
        #pragma warning restore CS0618

        // Set strongly-typed ValueTyped field
        if (value == null)
        {
            property.ValueTyped = PropertyValue.Null();
        }
        else if (value is string str)
        {
            property.ValueTyped = PropertyValue.FromString(str);
        }
        else if (value is UnifiedXamlElement element)
        {
            property.ValueTyped = PropertyValue.FromElement(element);
        }
        else if (value is UnifiedXamlMarkupExtension extension)
        {
            property.ValueTyped = PropertyValue.FromMarkupExtension(extension);
        }
        else
        {
            // Fallback for unexpected types - convert to string
            property.ValueTyped = PropertyValue.FromString(value.ToString() ?? string.Empty);
        }
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
