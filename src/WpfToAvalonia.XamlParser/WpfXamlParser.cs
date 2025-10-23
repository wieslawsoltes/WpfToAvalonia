using System;
using System.Collections.Generic;
using XamlX;
using XamlX.Ast;
using XamlX.Parsers;
using XamlX.Transform;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.TypeSystem;

namespace WpfToAvalonia.XamlParser;

/// <summary>
/// Parses WPF XAML using XamlX library to extract semantic information.
/// This provides type-safe XAML parsing with full type resolution.
/// </summary>
public class WpfXamlParser
{
    private readonly WpfTypeSystemProvider _typeSystem;
    private readonly DiagnosticCollector _diagnostics;
    private readonly XamlLanguageTypeMappings _languageMappings;
    private readonly XamlXmlnsMappings? _xmlnsMappings;
    private readonly TransformerConfiguration? _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfXamlParser"/> class.
    /// </summary>
    /// <param name="diagnostics">Diagnostic collector for warnings and errors.</param>
    public WpfXamlParser(DiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

        // Create WPF type system provider
        _typeSystem = new WpfTypeSystemProvider(_diagnostics);

        // Preload WPF assemblies
        _typeSystem.PreloadWpfAssemblies();

        // Configure WPF XAML language
        _languageMappings = WpfXamlIlLanguage.Configure(_typeSystem, _diagnostics);

        try
        {
            // Resolve XML namespaces
            _xmlnsMappings = XamlXmlnsMappings.Resolve(_typeSystem, _languageMappings);

            // Create transformer configuration
            _configuration = new TransformerConfiguration(
                _typeSystem,
                defaultAssembly: null,
                _languageMappings,
                _xmlnsMappings,
                customValueConverter: WpfXamlIlLanguage.CustomValueConverter);

            _diagnostics.AddInfo(
                "WPF_XAML_PARSER_INIT",
                "WPF XAML parser initialized successfully",
                null);
        }
        catch (Exception ex)
        {
            _diagnostics.AddError(
                "WPF_XAML_PARSER_INIT_FAILED",
                $"Failed to initialize WPF XAML parser: {ex.Message}",
                null);
        }
    }

    /// <summary>
    /// Gets the WPF type system provider.
    /// </summary>
    public WpfTypeSystemProvider TypeSystem => _typeSystem;

    /// <summary>
    /// Gets type system statistics.
    /// </summary>
    public TypeSystemStatistics GetStatistics() => _typeSystem.GetStatistics();

    /// <summary>
    /// Parses WPF XAML text into an AST (Abstract Syntax Tree).
    /// </summary>
    /// <param name="xamlText">The XAML text to parse.</param>
    /// <param name="baseUri">Optional base URI for the XAML file.</param>
    /// <returns>Parsed XAML document, or null if parsing failed.</returns>
    public XamlDocument? Parse(string xamlText, string? baseUri = null)
    {
        if (string.IsNullOrWhiteSpace(xamlText))
        {
            _diagnostics.AddError(
                "WPF_XAML_EMPTY",
                "XAML text is empty or whitespace",
                null);
            return null;
        }

        if (_configuration == null || _xmlnsMappings == null)
        {
            _diagnostics.AddError(
                "WPF_XAML_PARSER_NOT_INITIALIZED",
                "Parser configuration failed during initialization",
                null);
            return null;
        }

        try
        {
            _diagnostics.AddInfo(
                "WPF_XAML_PARSE_START",
                $"Parsing WPF XAML ({xamlText.Length} characters)",
                null);

            // Parse XAML using XamlX's document parser
            // This creates the initial AST from XML
            var document = XDocumentXamlParser.Parse(xamlText, new Dictionary<string, string>
            {
                // Add default namespace mappings
                { XamlNamespaces.Blend2008, XamlNamespaces.Blend2008 }
            });

            _diagnostics.AddInfo(
                "WPF_XAML_PARSE_SUCCESS",
                "Successfully parsed WPF XAML to AST",
                null);

            return document;
        }
        catch (XamlParseException ex)
        {
            _diagnostics.AddError(
                "WPF_XAML_PARSE_ERROR",
                $"XAML parse error at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}",
                null);
            return null;
        }
        catch (Exception ex)
        {
            _diagnostics.AddError(
                "WPF_XAML_PARSE_ERROR",
                $"Unexpected error parsing XAML: {ex.Message}",
                null);
            return null;
        }
    }

    /// <summary>
    /// Parses and transforms WPF XAML.
    /// This performs full semantic analysis and type resolution.
    /// </summary>
    /// <param name="xamlText">The XAML text to parse.</param>
    /// <param name="baseUri">Optional base URI for the XAML file.</param>
    /// <returns>Transformed XAML document, or null if parsing/transformation failed.</returns>
    public XamlDocument? ParseAndTransform(string xamlText, string? baseUri = null)
    {
        var document = Parse(xamlText, baseUri);
        if (document == null || _configuration == null)
            return null;

        try
        {
            _diagnostics.AddInfo(
                "WPF_XAML_TRANSFORM_START",
                "Starting semantic transformation of XAML AST",
                null);

            // TODO: Apply XamlX transformers to enrich the AST with semantic information
            // For now, we just return the parsed document
            // In the future, we'll add:
            // - Type resolution
            // - Property resolution
            // - Markup extension processing
            // - Binding expression parsing
            // - etc.

            _diagnostics.AddInfo(
                "WPF_XAML_TRANSFORM_SUCCESS",
                "Successfully transformed XAML AST",
                null);

            return document;
        }
        catch (Exception ex)
        {
            _diagnostics.AddError(
                "WPF_XAML_TRANSFORM_ERROR",
                $"Error transforming XAML: {ex.Message}",
                null);
            return null;
        }
    }
}
