using System.Xml;
using System.Xml.Linq;
using WpfToAvalonia.Core.Diagnostics;

namespace WpfToAvalonia.Core.Transformers.Xaml;

/// <summary>
/// Parses XAML files while preserving formatting and structure.
/// </summary>
public sealed class XamlParser
{
    private readonly DiagnosticCollector _diagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="XamlParser"/> class.
    /// </summary>
    /// <param name="diagnostics">The diagnostic collector.</param>
    public XamlParser(DiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    /// <summary>
    /// Parses a XAML file from a file path.
    /// </summary>
    /// <param name="filePath">The path to the XAML file.</param>
    /// <returns>The parsed XAML document, or null if parsing failed.</returns>
    public XDocument? ParseFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            _diagnostics.AddError(
                DiagnosticCodes.XamlParseError,
                $"XAML file not found: {filePath}",
                filePath);
            return null;
        }

        try
        {
            var settings = new XmlReaderSettings
            {
                IgnoreWhitespace = false,
                IgnoreComments = false,
                DtdProcessing = DtdProcessing.Ignore
            };

            using var reader = XmlReader.Create(filePath, settings);
            var document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
            return document;
        }
        catch (XmlException ex)
        {
            _diagnostics.AddError(
                DiagnosticCodes.XamlParseError,
                $"Failed to parse XAML file: {ex.Message}",
                filePath,
                ex.LineNumber,
                ex.LinePosition);
            return null;
        }
        catch (Exception ex)
        {
            _diagnostics.AddError(
                DiagnosticCodes.XamlParseError,
                $"Unexpected error parsing XAML file: {ex.Message}",
                filePath);
            return null;
        }
    }

    /// <summary>
    /// Parses XAML content from a string.
    /// </summary>
    /// <param name="xaml">The XAML content.</param>
    /// <param name="filePath">Optional file path for diagnostic reporting.</param>
    /// <returns>The parsed XAML document, or null if parsing failed.</returns>
    public XDocument? ParseString(string xaml, string? filePath = null)
    {
        if (string.IsNullOrEmpty(xaml))
        {
            throw new ArgumentNullException(nameof(xaml));
        }

        try
        {
            var settings = new XmlReaderSettings
            {
                IgnoreWhitespace = false,
                IgnoreComments = false,
                DtdProcessing = DtdProcessing.Ignore
            };

            using var reader = XmlReader.Create(new StringReader(xaml), settings);
            var document = XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            return document;
        }
        catch (XmlException ex)
        {
            _diagnostics.AddError(
                DiagnosticCodes.XamlParseError,
                $"Failed to parse XAML content: {ex.Message}",
                filePath,
                ex.LineNumber,
                ex.LinePosition);
            return null;
        }
        catch (Exception ex)
        {
            _diagnostics.AddError(
                DiagnosticCodes.XamlParseError,
                $"Unexpected error parsing XAML content: {ex.Message}",
                filePath);
            return null;
        }
    }

    /// <summary>
    /// Saves a XAML document to a file, preserving formatting.
    /// </summary>
    /// <param name="document">The XAML document to save.</param>
    /// <param name="filePath">The path where to save the file.</param>
    public void SaveFile(XDocument document, string filePath)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        try
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                OmitXmlDeclaration = false,
                Encoding = System.Text.Encoding.UTF8
            };

            using var writer = XmlWriter.Create(filePath, settings);
            document.Save(writer);
        }
        catch (Exception ex)
        {
            _diagnostics.AddError(
                DiagnosticCodes.FileError,
                $"Failed to save XAML file: {ex.Message}",
                filePath);
        }
    }

    /// <summary>
    /// Converts a XAML document to a string.
    /// </summary>
    /// <param name="document">The XAML document.</param>
    /// <returns>The XAML as a string.</returns>
    public string ToString(XDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "    ",
            OmitXmlDeclaration = false,
            Encoding = System.Text.Encoding.UTF8
        };

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        document.Save(xmlWriter);
        return stringWriter.ToString();
    }
}
