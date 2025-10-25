using System.Xml.Linq;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Transformers.Xaml;
using WpfToAvalonia.XamlParser.Converters;
using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser;

/// <summary>
/// Main entry point for parsing XAML into the Unified AST.
/// Combines XML parsing with the Unified AST conversion.
/// </summary>
public sealed class UnifiedXamlParser
{
    private readonly DiagnosticCollector _diagnostics;
    private readonly Core.Transformers.Xaml.XamlParser _xmlParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedXamlParser"/> class.
    /// </summary>
    public UnifiedXamlParser(DiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _xmlParser = new Core.Transformers.Xaml.XamlParser(diagnostics);
    }

    /// <summary>
    /// Parses XAML from a file path.
    /// </summary>
    public UnifiedXamlDocument ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"XAML file not found: {filePath}", filePath);
        }

        var content = File.ReadAllText(filePath);
        return Parse(content, filePath);
    }

    /// <summary>
    /// Parses XAML from a string.
    /// </summary>
    public UnifiedXamlDocument Parse(string xamlContent, string? filePath = null)
    {
        try
        {
            // Stage 1: Parse XML
            var xDocument = _xmlParser.ParseString(xamlContent, filePath);

            if (xDocument == null)
            {
                // Return empty document if parsing failed
                return new UnifiedXamlDocument
                {
                    FilePath = filePath,
                    DiagnosticCollector = _diagnostics
                };
            }

            // Stage 2: Convert to Unified AST
            var converter = new XmlToUnifiedConverter(_diagnostics);
            var document = converter.Convert(xDocument, filePath, xamlContent);

            return document;
        }
        catch (Exception ex)
        {
            _diagnostics.AddError("XAML_PARSE_ERROR",
                $"Failed to parse XAML: {ex.Message}",
                filePath);

            // Return empty document
            return new UnifiedXamlDocument
            {
                FilePath = filePath,
                DiagnosticCollector = _diagnostics
            };
        }
    }

    /// <summary>
    /// Parses XAML from an XDocument (already parsed XML).
    /// </summary>
    public UnifiedXamlDocument ParseFromXDocument(XDocument xDocument, string? filePath = null)
    {
        var converter = new XmlToUnifiedConverter(_diagnostics);
        return converter.Convert(xDocument, filePath);
    }

    /// <summary>
    /// Validates a Unified XAML document.
    /// </summary>
    public bool Validate(UnifiedXamlDocument document)
    {
        var hasErrors = false;

        // Check for basic structural issues
        if (document.Root == null)
        {
            _diagnostics.AddError("XAML_NO_ROOT",
                "XAML document has no root element",
                document.FilePath);
            hasErrors = true;
        }

        // Collect all diagnostics from the tree
        var allDiagnostics = document.CollectAllDiagnostics();
        foreach (var diagnostic in allDiagnostics)
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    /// <summary>
    /// Gets the diagnostic collector.
    /// </summary>
    public DiagnosticCollector Diagnostics => _diagnostics;
}
