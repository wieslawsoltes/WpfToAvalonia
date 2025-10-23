using System;
using System.Xml.Linq;
using XamlX.Ast;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser;

/// <summary>
/// Hybrid XAML parser that combines XML parsing (for formatting preservation)
/// with XamlX parsing (for semantic type information).
/// </summary>
/// <remarks>
/// This dual-parsing strategy provides:
/// - XML Layer: Fast parsing, format preservation, whitespace handling, structural transformations
/// - XamlX Layer: Semantic analysis, type resolution, markup extension evaluation, validation
/// - Unified AST: Bridge between XML and XamlX representations for optimal transformations
/// </remarks>
public class HybridXamlParser
{
    private readonly WpfXamlParser _semanticParser;
    private readonly DiagnosticCollector _diagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridXamlParser"/> class.
    /// </summary>
    /// <param name="diagnostics">Diagnostic collector for warnings and errors.</param>
    public HybridXamlParser(DiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _semanticParser = new WpfXamlParser(_diagnostics);
    }

    /// <summary>
    /// Gets the WPF semantic parser.
    /// </summary>
    public WpfXamlParser SemanticParser => _semanticParser;

    /// <summary>
    /// Parses WPF XAML using dual parsing strategy.
    /// </summary>
    /// <param name="xamlText">The XAML text to parse.</param>
    /// <param name="baseUri">Optional base URI for the XAML file.</param>
    /// <returns>Unified XAML document combining XML and semantic information, or null if parsing failed.</returns>
    public UnifiedXamlDocument? Parse(string xamlText, string? baseUri = null)
    {
        if (string.IsNullOrWhiteSpace(xamlText))
        {
            _diagnostics.AddError(
                "HYBRID_XAML_EMPTY",
                "XAML text is empty or whitespace",
                null);
            return null;
        }

        _diagnostics.AddInfo(
            "HYBRID_XAML_PARSE_START",
            $"Starting hybrid XAML parsing ({xamlText.Length} characters)",
            null);

        // Phase 1: Parse with XDocument (XML layer)
        XDocument? xmlDoc = ParseXmlLayer(xamlText);
        if (xmlDoc == null)
            return null;

        // Phase 2: Parse with XamlX (semantic layer)
        XamlDocument? semanticDoc = ParseSemanticLayer(xamlText, baseUri);
        if (semanticDoc == null)
        {
            // Even if semantic parsing fails, we can still work with XML layer
            _diagnostics.AddWarning(
                "HYBRID_XAML_SEMANTIC_FAILED",
                "Semantic parsing failed, using XML-only mode",
                null);
        }

        // Phase 3: Merge into unified AST
        UnifiedXamlDocument unifiedDoc = MergeToUnifiedAst(xmlDoc, semanticDoc);

        _diagnostics.AddInfo(
            "HYBRID_XAML_PARSE_SUCCESS",
            "Hybrid XAML parsing completed successfully",
            null);

        return unifiedDoc;
    }

    /// <summary>
    /// Parses XAML using XDocument to preserve formatting.
    /// </summary>
    private XDocument? ParseXmlLayer(string xamlText)
    {
        try
        {
            _diagnostics.AddInfo(
                "HYBRID_XML_PARSE_START",
                "Parsing XML layer (XDocument)",
                null);

            // Parse with options to preserve whitespace and line info
            var xmlDoc = XDocument.Parse(
                xamlText,
                LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            _diagnostics.AddInfo(
                "HYBRID_XML_PARSE_SUCCESS",
                $"XML layer parsed successfully: root element = {xmlDoc.Root?.Name}",
                null);

            return xmlDoc;
        }
        catch (System.Xml.XmlException ex)
        {
            _diagnostics.AddError(
                "HYBRID_XML_PARSE_ERROR",
                $"XML parse error at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}",
                null);
            return null;
        }
        catch (Exception ex)
        {
            _diagnostics.AddError(
                "HYBRID_XML_PARSE_ERROR",
                $"Unexpected error parsing XML: {ex.Message}",
                null);
            return null;
        }
    }

    /// <summary>
    /// Parses XAML using XamlX to extract semantic information.
    /// </summary>
    private XamlDocument? ParseSemanticLayer(string xamlText, string? baseUri)
    {
        try
        {
            _diagnostics.AddInfo(
                "HYBRID_SEMANTIC_PARSE_START",
                "Parsing semantic layer (XamlX)",
                null);

            var semanticDoc = _semanticParser.Parse(xamlText, baseUri);

            if (semanticDoc != null)
            {
                _diagnostics.AddInfo(
                    "HYBRID_SEMANTIC_PARSE_SUCCESS",
                    "Semantic layer parsed successfully",
                    null);
            }

            return semanticDoc;
        }
        catch (Exception ex)
        {
            _diagnostics.AddWarning(
                "HYBRID_SEMANTIC_PARSE_ERROR",
                $"Semantic parsing failed: {ex.Message}",
                null);
            return null;
        }
    }

    /// <summary>
    /// Merges XML and semantic ASTs into a unified representation.
    /// </summary>
    private UnifiedXamlDocument MergeToUnifiedAst(XDocument xmlDoc, XamlDocument? semanticDoc)
    {
        _diagnostics.AddInfo(
            "HYBRID_MERGE_START",
            "Merging XML and semantic ASTs into unified representation",
            null);

        // Create unified document from XML
        var unifiedDoc = new UnifiedXamlDocument(xmlDoc, _diagnostics);

        // If we have semantic information, enrich the unified AST
        if (semanticDoc != null)
        {
            try
            {
                EnrichWithSemanticInfo(unifiedDoc, semanticDoc);

                _diagnostics.AddInfo(
                    "HYBRID_MERGE_SUCCESS",
                    "Successfully merged XML and semantic information",
                    null);
            }
            catch (Exception ex)
            {
                _diagnostics.AddWarning(
                    "HYBRID_MERGE_ERROR",
                    $"Failed to enrich with semantic info: {ex.Message}",
                    null);
            }
        }
        else
        {
            _diagnostics.AddInfo(
                "HYBRID_MERGE_XML_ONLY",
                "Using XML-only unified AST (no semantic enrichment)",
                null);
        }

        return unifiedDoc;
    }

    /// <summary>
    /// Enriches unified AST with semantic information from XamlX.
    /// </summary>
    private void EnrichWithSemanticInfo(UnifiedXamlDocument unifiedDoc, XamlDocument semanticDoc)
    {
        // TODO: Implement semantic enrichment
        // This will:
        // 1. Walk both ASTs in parallel
        // 2. Attach type information to unified nodes
        // 3. Add resolved property metadata
        // 4. Store markup extension semantic info
        // 5. Validate consistency between layers

        _diagnostics.AddInfo(
            "HYBRID_ENRICH_PENDING",
            "Semantic enrichment not yet implemented - placeholder",
            null);
    }

    /// <summary>
    /// Parses multiple XAML documents in a group.
    /// This is useful for handling resource includes and merged dictionaries.
    /// </summary>
    /// <param name="xamlDocuments">Collection of XAML documents to parse.</param>
    /// <returns>Array of unified documents.</returns>
    public UnifiedXamlDocument?[] ParseGroup(params (string xaml, string? baseUri)[] xamlDocuments)
    {
        _diagnostics.AddInfo(
            "HYBRID_GROUP_PARSE_START",
            $"Parsing group of {xamlDocuments.Length} XAML documents",
            null);

        var results = new UnifiedXamlDocument?[xamlDocuments.Length];

        for (int i = 0; i < xamlDocuments.Length; i++)
        {
            var (xaml, baseUri) = xamlDocuments[i];
            results[i] = Parse(xaml, baseUri);
        }

        _diagnostics.AddInfo(
            "HYBRID_GROUP_PARSE_COMPLETE",
            $"Group parsing complete: {results.Count(d => d != null)}/{xamlDocuments.Length} succeeded",
            null);

        return results;
    }
}
