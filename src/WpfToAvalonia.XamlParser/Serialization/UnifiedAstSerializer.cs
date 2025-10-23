using System.Text;
using System.Xml;
using System.Xml.Linq;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Serialization;

/// <summary>
/// Serializes a Unified AST back to XAML (XDocument/XElement).
/// Preserves formatting where possible and applies transformation results.
/// </summary>
public sealed class UnifiedAstSerializer
{
    private readonly DiagnosticCollector _diagnostics;
    private readonly SerializationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedAstSerializer"/> class.
    /// </summary>
    public UnifiedAstSerializer(DiagnosticCollector diagnostics, SerializationOptions? options = null)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _options = options ?? new SerializationOptions();
    }

    /// <summary>
    /// Serializes a UnifiedXamlDocument to XDocument.
    /// </summary>
    public XDocument Serialize(UnifiedXamlDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        _diagnostics.AddInfo(
            "SERIALIZATION_START",
            "Starting XAML serialization",
            document.FilePath);

        var xDocument = new XDocument();

        // Add XML declaration if original had one
        if (document.HasXmlDeclaration)
        {
            xDocument.Declaration = new XDeclaration("1.0", document.Encoding, null);
        }

        // Serialize root element
        if (document.Root != null)
        {
            var rootElement = SerializeElement(document.Root);
            xDocument.Add(rootElement);

            // Add diagnostic comments if requested
            if (_options.IncludeDiagnosticComments)
            {
                AddDiagnosticComments(document, xDocument);
            }
        }

        _diagnostics.AddInfo(
            "SERIALIZATION_COMPLETE",
            "XAML serialization completed",
            document.FilePath);

        return xDocument;
    }

    /// <summary>
    /// Serializes a UnifiedXamlElement to XElement.
    /// </summary>
    private XElement SerializeElement(UnifiedXamlElement element)
    {
        // Get the element name (may have been transformed)
        var elementName = GetElementName(element);
        var xElement = new XElement(elementName);

        // Add namespace declarations from root element
        if (element.Parent == null)
        {
            AddNamespaceDeclarations(element, xElement);
        }

        // Serialize attributes (simple properties)
        SerializeAttributes(element, xElement);

        // Serialize child elements and property elements
        SerializeChildren(element, xElement);

        // Preserve whitespace if configured
        if (_options.PreserveWhitespace && element.XmlElement != null)
        {
            PreserveWhitespace(element.XmlElement, xElement);
        }

        return xElement;
    }

    /// <summary>
    /// Gets the element name (namespace + local name).
    /// </summary>
    private XName GetElementName(UnifiedXamlElement element)
    {
        var localName = element.TypeName;

        // If using Avalonia namespaces, always use transformed namespace
        if (_options.UseAvaloniaNamespaces)
        {
            // Use the transformed namespace from the element
            var ns = element.Namespace ?? "https://github.com/avaloniaui";
            return XName.Get(localName, ns);
        }

        // Check if element has custom namespace in metadata
        var customNamespace = element.GetMetadata<string>("TransformedNamespace");
        if (!string.IsNullOrEmpty(customNamespace))
        {
            return XName.Get(localName, customNamespace);
        }

        // Use original XML element's name if available and preserving original
        if (element.XmlElement != null && !_options.UseAvaloniaNamespaces)
        {
            // Preserve the namespace from original XML
            return element.XmlElement.Name;
        }

        // Fallback to type name with element's namespace or no namespace
        if (!string.IsNullOrEmpty(element.Namespace))
        {
            return XName.Get(localName, element.Namespace);
        }

        return XName.Get(localName);
    }

    /// <summary>
    /// Adds namespace declarations to the root element.
    /// </summary>
    private void AddNamespaceDeclarations(UnifiedXamlElement element, XElement xElement)
    {
        // Add default Avalonia namespace
        if (_options.UseAvaloniaNamespaces)
        {
            xElement.Add(new XAttribute("xmlns", "https://github.com/avaloniaui"));
            xElement.Add(new XAttribute(XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"));
        }
        else if (element.XmlElement != null)
        {
            // Preserve original namespaces
            foreach (var attr in element.XmlElement.Attributes())
            {
                if (attr.IsNamespaceDeclaration)
                {
                    xElement.Add(new XAttribute(attr.Name, attr.Value));
                }
            }
        }
    }

    /// <summary>
    /// Serializes element attributes (simple properties).
    /// </summary>
    private void SerializeAttributes(UnifiedXamlElement element, XElement xElement)
    {
        // Serialize special x: namespace attributes first
        var xNamespace = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");

        if (!string.IsNullOrEmpty(element.XClass))
        {
            xElement.SetAttributeValue(xNamespace + "Class", element.XClass);
        }

        if (!string.IsNullOrEmpty(element.XName))
        {
            xElement.SetAttributeValue(xNamespace + "Name", element.XName);
        }

        if (!string.IsNullOrEmpty(element.XKey))
        {
            xElement.SetAttributeValue(xNamespace + "Key", element.XKey);
        }

        if (!string.IsNullOrEmpty(element.XFieldModifier))
        {
            xElement.SetAttributeValue(xNamespace + "FieldModifier", element.XFieldModifier);
        }

        if (element.XShared.HasValue)
        {
            xElement.SetAttributeValue(xNamespace + "Shared", element.XShared.Value ? "True" : "False");
        }

        if (!string.IsNullOrEmpty(element.XTypeArguments))
        {
            xElement.SetAttributeValue(xNamespace + "TypeArguments", element.XTypeArguments);
        }

        // Serialize regular properties
        foreach (var property in element.Properties)
        {
            if (property.Kind == PropertyKind.Attribute && property.Value is string stringValue)
            {
                var attrName = GetPropertyName(property);
                xElement.SetAttributeValue(attrName, stringValue);
            }
        }
    }

    /// <summary>
    /// Gets the property name (may include namespace prefix).
    /// </summary>
    private XName GetPropertyName(UnifiedXamlProperty property)
    {
        var propertyName = property.PropertyName;

        // Handle x: namespace properties (x:Name, x:Class, etc.)
        if (propertyName.StartsWith("x:"))
        {
            var localName = propertyName.Substring(2);
            return XName.Get(localName, "http://schemas.microsoft.com/winfx/2006/xaml");
        }

        return XName.Get(propertyName);
    }

    /// <summary>
    /// Serializes child elements and property elements.
    /// </summary>
    private void SerializeChildren(UnifiedXamlElement element, XElement xElement)
    {
        // Serialize property elements first
        foreach (var property in element.Properties)
        {
            if (property.Kind == PropertyKind.PropertyElement)
            {
                var propertyElement = SerializePropertyElement(property);
                xElement.Add(propertyElement);
            }
        }

        // Then serialize child elements
        foreach (var child in element.Children)
        {
            var childElement = SerializeElement(child);
            xElement.Add(childElement);
        }

        // Add text content if present
        if (!string.IsNullOrEmpty(element.TextContent))
        {
            xElement.Value = element.TextContent;
        }
    }

    /// <summary>
    /// Serializes a property element (e.g., <Button.Content>).
    /// </summary>
    private XElement SerializePropertyElement(UnifiedXamlProperty property)
    {
        var parentElement = property.Parent as UnifiedXamlElement;
        var parentTypeName = parentElement?.TypeName ?? "Unknown";
        var propertyElementName = $"{parentTypeName}.{property.PropertyName}";

        // Use parent's namespace for property element
        XElement propertyElement;
        if (parentElement != null)
        {
            var parentNamespace = _options.UseAvaloniaNamespaces
                ? (parentElement.Namespace ?? "https://github.com/avaloniaui")
                : parentElement.Namespace;

            if (!string.IsNullOrEmpty(parentNamespace))
            {
                propertyElement = new XElement(XName.Get(propertyElementName, parentNamespace));
            }
            else
            {
                propertyElement = new XElement(propertyElementName);
            }
        }
        else
        {
            propertyElement = new XElement(propertyElementName);
        }

        if (property.Value is UnifiedXamlElement elementValue)
        {
            var valueElement = SerializeElement(elementValue);
            propertyElement.Add(valueElement);
        }
        else if (property.Value is string stringValue)
        {
            propertyElement.Value = stringValue;
        }

        return propertyElement;
    }

    /// <summary>
    /// Preserves whitespace from original XML element.
    /// </summary>
    private void PreserveWhitespace(XElement originalElement, XElement newElement)
    {
        // This is a simplified implementation
        // A more sophisticated version would preserve exact whitespace patterns
        if (originalElement.Nodes().OfType<XText>().Any(t => string.IsNullOrWhiteSpace(t.Value)))
        {
            // Original had whitespace, preserve indentation style
            // For now, we'll let XDocument handle formatting
        }
    }

    /// <summary>
    /// Adds diagnostic comments to the document for manual review items.
    /// </summary>
    private void AddDiagnosticComments(UnifiedXamlDocument document, XDocument xDocument)
    {
        // Combine diagnostics from both the document and the diagnostics collector
        var allDiagnostics = new List<TransformationDiagnostic>();
        allDiagnostics.AddRange(document.CollectAllDiagnostics());
        allDiagnostics.AddRange(_diagnostics.Diagnostics.OfType<TransformationDiagnostic>());

        var warnings = allDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();
        var errors = allDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        if (warnings.Count > 0 || errors.Count > 0)
        {
            var commentBuilder = new StringBuilder();
            commentBuilder.AppendLine();
            commentBuilder.AppendLine("WPF to Avalonia Conversion - Manual Review Required:");
            commentBuilder.AppendLine("=" .PadRight(70, '='));

            if (errors.Count > 0)
            {
                commentBuilder.AppendLine($"\nERRORS ({errors.Count}):");
                foreach (var error in errors.Take(10)) // Limit to first 10
                {
                    commentBuilder.AppendLine($"  [{error.Code}] Line {error.Line}: {error.Message}");
                }
                if (errors.Count > 10)
                {
                    commentBuilder.AppendLine($"  ... and {errors.Count - 10} more errors");
                }
            }

            if (warnings.Count > 0)
            {
                commentBuilder.AppendLine($"\nWARNINGS ({warnings.Count}):");
                foreach (var warning in warnings.Take(10)) // Limit to first 10
                {
                    commentBuilder.AppendLine($"  [{warning.Code}] Line {warning.Line}: {warning.Message}");
                }
                if (warnings.Count > 10)
                {
                    commentBuilder.AppendLine($"  ... and {warnings.Count - 10} more warnings");
                }
            }

            commentBuilder.AppendLine("\nPlease review and address these issues manually.");
            commentBuilder.AppendLine("=" .PadRight(70, '='));

            var comment = new XComment(commentBuilder.ToString());
            xDocument.AddFirst(comment);
        }
    }

    /// <summary>
    /// Serializes to string with formatting options.
    /// </summary>
    public string SerializeToString(UnifiedXamlDocument document)
    {
        var xDocument = Serialize(document);

        var settings = new XmlWriterSettings
        {
            Indent = _options.Indent,
            IndentChars = _options.IndentChars,
            NewLineChars = _options.NewLineChars,
            NewLineHandling = NewLineHandling.Replace,
            OmitXmlDeclaration = !document.HasXmlDeclaration,
            Encoding = Encoding.UTF8
        };

        using var stringWriter = new StringWriter();
        using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
        {
            xDocument.Save(xmlWriter);
            xmlWriter.Flush();
        }
        return stringWriter.ToString();
    }
}

/// <summary>
/// Options for XAML serialization.
/// </summary>
public sealed class SerializationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to preserve whitespace from original XML.
    /// </summary>
    public bool PreserveWhitespace { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include diagnostic comments in output.
    /// </summary>
    public bool IncludeDiagnosticComments { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use Avalonia namespaces in output.
    /// </summary>
    public bool UseAvaloniaNamespaces { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to indent the output.
    /// </summary>
    public bool Indent { get; set; } = true;

    /// <summary>
    /// Gets or sets the characters to use for indentation.
    /// </summary>
    public string IndentChars { get; set; } = "    "; // 4 spaces

    /// <summary>
    /// Gets or sets the characters to use for newlines.
    /// </summary>
    public string NewLineChars { get; set; } = "\n";

    /// <summary>
    /// Gets or sets a value indicating whether to validate output against schema.
    /// </summary>
    public bool ValidateOutput { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of diagnostic comments to include.
    /// </summary>
    public int MaxDiagnosticComments { get; set; } = 20;
}
