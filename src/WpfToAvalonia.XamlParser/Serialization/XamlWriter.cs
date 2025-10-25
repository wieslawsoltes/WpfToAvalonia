using System.Text;
using System.Xml.Linq;
using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Serialization;

/// <summary>
/// Writes Unified XAML AST to XAML text.
/// </summary>
public sealed class XamlWriter
{
    private readonly XamlSerializationOptions _options;
    private readonly StringBuilder _output;
    private int _indentLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="XamlWriter"/> class.
    /// </summary>
    public XamlWriter(XamlSerializationOptions? options = null)
    {
        _options = options ?? new XamlSerializationOptions();
        _output = new StringBuilder();
        _indentLevel = 0;
    }

    /// <summary>
    /// Writes a XAML document to string.
    /// </summary>
    public string WriteDocument(UnifiedXamlDocument document)
    {
        _output.Clear();
        _indentLevel = 0;

        // Write XML declaration
        if (_options.IncludeXmlDeclaration && document.HasXmlDeclaration)
        {
            _output.Append($"<?xml version=\"1.0\" encoding=\"{_options.Encoding}\"?>");
            _output.Append(_options.NewLine);
        }

        // Write root element
        if (document.Root != null)
        {
            WriteElement(document.Root);
        }

        return _output.ToString();
    }

    /// <summary>
    /// Writes an element.
    /// </summary>
    private void WriteElement(UnifiedXamlElement element)
    {
        // Write leading whitespace/comments if preserving formatting
        if (_options.PreserveFormatting && element.Formatting.LeadingWhitespace != null)
        {
            // Normalize whitespace to prevent extra blank lines
            var whitespace = NormalizeLeadingWhitespace(element.Formatting.LeadingWhitespace);
            _output.Append(whitespace);
        }
        else if (!_options.PreserveFormatting)
        {
            WriteIndent();
        }

        // Write opening tag
        _output.Append('<');
        WriteElementName(element);

        // Write xmlns declarations (for root element)
        if (element.Parent == null && _options.IncludeXmlnsDeclarations)
        {
            WriteXmlnsDeclarations(element);
        }

        // Write properties as attributes
        WriteAttributes(element);

        // Check if element has content
        bool hasContent = element.Children.Count > 0 ||
                         !string.IsNullOrEmpty(element.TextContent) ||
                         element.Properties.Any(p => p.Kind == PropertyKind.PropertyElement);

        if (!hasContent && _options.UseSelfClosingTags)
        {
            // Self-closing tag
            _output.Append(" />");
            if (!_options.PreserveFormatting)
            {
                _output.Append(_options.NewLine);
            }
        }
        else
        {
            // Close opening tag
            _output.Append('>');

            // Write text content if any
            if (!string.IsNullOrEmpty(element.TextContent))
            {
                // When preserving formatting, skip whitespace-only TextContent
                // because whitespace between elements is handled by LeadingWhitespace
                var isWhitespaceOnly = string.IsNullOrWhiteSpace(element.TextContent);

                if (!(_options.PreserveFormatting && isWhitespaceOnly))
                {
                    if (_options.PreserveFormatting && element.Formatting.InnerWhitespace != null)
                    {
                        _output.Append(element.Formatting.InnerWhitespace);
                    }
                    _output.Append(element.TextContent);
                }
            }
            else if (hasContent && !_options.PreserveFormatting)
            {
                _output.Append(_options.NewLine);
            }

            // Write property elements
            _indentLevel++;
            foreach (var property in element.Properties.Where(p => p.Kind == PropertyKind.PropertyElement))
            {
                WritePropertyElement(property, element);
            }

            // Write child elements
            foreach (var child in element.Children)
            {
                WriteElement(child);
            }
            _indentLevel--;

            // Write closing tag
            if (hasContent && element.Children.Count > 0 && !_options.PreserveFormatting)
            {
                WriteIndent();
            }
            _output.Append("</");
            WriteElementName(element);
            _output.Append('>');
            if (!_options.PreserveFormatting)
            {
                _output.Append(_options.NewLine);
            }
        }

        // Write trailing whitespace if preserving formatting
        if (_options.PreserveFormatting && element.Formatting.TrailingWhitespace != null)
        {
            _output.Append(element.Formatting.TrailingWhitespace);
        }
    }

    /// <summary>
    /// Writes element name (with prefix if needed).
    /// </summary>
    private void WriteElementName(UnifiedXamlElement element)
    {
        // For now, just write the type name
        // TODO: Add prefix based on namespace
        _output.Append(element.TypeName);
    }

    /// <summary>
    /// Writes xmlns declarations for the root element.
    /// </summary>
    private void WriteXmlnsDeclarations(UnifiedXamlElement rootElement)
    {
        // Default Avalonia namespace
        _output.Append(" xmlns=\"https://github.com/avaloniaui\"");

        // x: namespace (for x:Name, x:Class, etc.)
        _output.Append(" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"");

        // Add any custom namespace declarations from the symbol table
        // TODO: Extract from document.Symbols.NamespacePrefixes
    }

    /// <summary>
    /// Writes attributes (properties with attribute syntax).
    /// </summary>
    private void WriteAttributes(UnifiedXamlElement element)
    {
        var attributes = element.Properties
            .Where(p => p.Kind == PropertyKind.Attribute || p.Kind == PropertyKind.AttachedProperty)
            .ToList();

        // Write x:Name, x:Class, etc. first
        WriteXamlDirectiveAttributes(element);

        // Sort if requested
        if (_options.SortAttributes)
        {
            attributes = attributes.OrderBy(p => p.PropertyName).ToList();
        }

        foreach (var property in attributes)
        {
            // Use preserved formatting if available, otherwise fall back to options
            if (_options.PreserveFormatting && property.Formatting.LeadingWhitespace != null)
            {
                _output.Append(property.Formatting.LeadingWhitespace);
            }
            else if (_options.AttributesOnSeparateLines)
            {
                _output.Append(_options.NewLine);
                WriteIndent();
                _output.Append(_options.IndentString);
            }
            else
            {
                _output.Append(' ');
            }

            WriteAttribute(property);
        }
    }

    /// <summary>
    /// Writes XAML directive attributes (x:Name, x:Class, etc.).
    /// </summary>
    private void WriteXamlDirectiveAttributes(UnifiedXamlElement element)
    {
        if (!string.IsNullOrEmpty(element.XName))
        {
            _output.Append(" x:Name=\"");
            _output.Append(EscapeAttributeValue(element.XName));
            _output.Append('"');
        }

        if (!string.IsNullOrEmpty(element.XKey))
        {
            _output.Append(" x:Key=\"");
            _output.Append(EscapeAttributeValue(element.XKey));
            _output.Append('"');
        }

        if (!string.IsNullOrEmpty(element.XClass))
        {
            _output.Append(" x:Class=\"");
            _output.Append(EscapeAttributeValue(element.XClass));
            _output.Append('"');
        }

        if (!string.IsNullOrEmpty(element.XFieldModifier))
        {
            _output.Append(" x:FieldModifier=\"");
            _output.Append(EscapeAttributeValue(element.XFieldModifier));
            _output.Append('"');
        }

        if (element.XShared.HasValue)
        {
            _output.Append(" x:Shared=\"");
            _output.Append(element.XShared.Value.ToString().ToLower());
            _output.Append('"');
        }
    }

    /// <summary>
    /// Writes a single attribute.
    /// </summary>
    private void WriteAttribute(UnifiedXamlProperty property)
    {
        // Write property name
        _output.Append(property.GetFullPropertyName());
        _output.Append("=\"");

        // Write property value
        if (property.MarkupExtension != null)
        {
            WriteMarkupExtension(property.MarkupExtension);
        }
        else
        {
            var value = property.GetStringValue() ?? string.Empty;
            _output.Append(EscapeAttributeValue(value));
        }

        _output.Append('"');
    }

    /// <summary>
    /// Writes a property element.
    /// </summary>
    private void WritePropertyElement(UnifiedXamlProperty property, UnifiedXamlElement parentElement)
    {
        if (!_options.PreserveFormatting)
        {
            WriteIndent();
        }
        _output.Append('<');
        _output.Append(parentElement.TypeName);
        _output.Append('.');
        _output.Append(property.PropertyName);
        _output.Append('>');
        if (!_options.PreserveFormatting)
        {
            _output.Append(_options.NewLine);
        }

        // Write property value
        _indentLevel++;
        if (property.Value is UnifiedXamlElement elementValue)
        {
            WriteElement(elementValue);
        }
        else if (property.MarkupExtension != null)
        {
            if (!_options.PreserveFormatting)
            {
                WriteIndent();
            }
            WriteMarkupExtension(property.MarkupExtension);
            if (!_options.PreserveFormatting)
            {
                _output.Append(_options.NewLine);
            }
        }
        else if (property.Value != null)
        {
            if (!_options.PreserveFormatting)
            {
                WriteIndent();
            }
            _output.Append(property.Value.ToString());
            if (!_options.PreserveFormatting)
            {
                _output.Append(_options.NewLine);
            }
        }
        _indentLevel--;

        if (!_options.PreserveFormatting)
        {
            WriteIndent();
        }
        _output.Append("</");
        _output.Append(parentElement.TypeName);
        _output.Append('.');
        _output.Append(property.PropertyName);
        _output.Append('>');
        if (!_options.PreserveFormatting)
        {
            _output.Append(_options.NewLine);
        }
    }

    /// <summary>
    /// Writes a markup extension.
    /// </summary>
    private void WriteMarkupExtension(UnifiedXamlMarkupExtension extension)
    {
        _output.Append('{');
        _output.Append(extension.ExtensionName);

        if (extension.PositionalArgument != null)
        {
            _output.Append(' ');
            _output.Append(extension.PositionalArgument);
        }
        else if (extension.Parameters.Count > 0)
        {
            _output.Append(' ');
            var first = true;
            foreach (var param in extension.Parameters)
            {
                if (!first)
                {
                    _output.Append(", ");
                }
                first = false;

                _output.Append(param.Key);
                _output.Append('=');

                if (param.Value is UnifiedXamlMarkupExtension nestedExtension)
                {
                    WriteMarkupExtension(nestedExtension);
                }
                else
                {
                    _output.Append(param.Value?.ToString() ?? string.Empty);
                }
            }
        }

        _output.Append('}');
    }

    /// <summary>
    /// Writes indentation.
    /// </summary>
    private void WriteIndent()
    {
        for (int i = 0; i < _indentLevel; i++)
        {
            _output.Append(_options.IndentString);
        }
    }

    /// <summary>
    /// Escapes attribute value for XML.
    /// </summary>
    private string EscapeAttributeValue(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
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
}
