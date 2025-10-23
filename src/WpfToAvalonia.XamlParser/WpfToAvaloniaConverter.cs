using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.Enrichment;
using WpfToAvalonia.XamlParser.Serialization;
using WpfToAvalonia.XamlParser.Transformation;
using WpfToAvalonia.XamlParser.TypeSystem;
using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser;

/// <summary>
/// Main entry point for converting WPF XAML to Avalonia XAML.
/// Coordinates the entire pipeline: Parse → Enrich → Transform → Serialize.
/// </summary>
public sealed class WpfToAvaloniaConverter
{
    private readonly DiagnosticCollector _diagnostics;
    private readonly UnifiedXamlParser _parser;
    private readonly EnrichmentPipeline _enrichmentPipeline;
    private readonly TransformationEngine _transformationEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfToAvaloniaConverter"/> class.
    /// </summary>
    public WpfToAvaloniaConverter()
    {
        _diagnostics = new DiagnosticCollector();

        // Create type resolver
        var typeResolver = new ReflectionTypeResolver();

        // Initialize parser
        _parser = new UnifiedXamlParser(_diagnostics);

        // Initialize enrichment pipeline
        _enrichmentPipeline = new EnrichmentPipeline(typeResolver, _diagnostics);

        // Initialize transformation engine
        _transformationEngine = new TransformationEngine(_diagnostics);
        _transformationEngine.RegisterDefaultRules();
    }

    /// <summary>
    /// Initializes a new instance with a custom type resolver.
    /// </summary>
    public WpfToAvaloniaConverter(IXamlTypeResolver typeResolver)
    {
        _diagnostics = new DiagnosticCollector();
        _parser = new UnifiedXamlParser(_diagnostics);
        _enrichmentPipeline = new EnrichmentPipeline(typeResolver, _diagnostics);
        _transformationEngine = new TransformationEngine(_diagnostics);
        _transformationEngine.RegisterDefaultRules();
    }

    /// <summary>
    /// Gets the diagnostic collector.
    /// </summary>
    public DiagnosticCollector Diagnostics => _diagnostics;

    /// <summary>
    /// Converts WPF XAML file to Avalonia XAML.
    /// </summary>
    public ConversionResult ConvertFile(string inputPath, string? outputPath = null, ConversionOptions? options = null)
    {
        options ??= new ConversionOptions();

        // Determine output path
        outputPath ??= GetDefaultOutputPath(inputPath);

        // Read input file
        if (!File.Exists(inputPath))
        {
            _diagnostics.AddError("FILE_NOT_FOUND", $"Input file not found: {inputPath}", inputPath);
            return new ConversionResult
            {
                Success = false,
                InputPath = inputPath,
                Diagnostics = _diagnostics.Diagnostics.ToList()
            };
        }

        var xamlContent = File.ReadAllText(inputPath);

        // Convert
        var result = Convert(xamlContent, inputPath, options);

        // Write output if successful
        if (result.Success && !string.IsNullOrEmpty(result.OutputXaml))
        {
            try
            {
                // Create output directory if needed
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                File.WriteAllText(outputPath, result.OutputXaml);
                result.OutputPath = outputPath;

                _diagnostics.AddInfo(
                    "CONVERSION_SUCCESS",
                    $"Conversion successful: {inputPath} → {outputPath}",
                    inputPath);
            }
            catch (Exception ex)
            {
                _diagnostics.AddError(
                    "FILE_WRITE_ERROR",
                    $"Failed to write output file: {ex.Message}",
                    outputPath);
                result.Success = false;
            }
        }

        result.Diagnostics = _diagnostics.Diagnostics.ToList();
        return result;
    }

    /// <summary>
    /// Converts WPF XAML string to Avalonia XAML string.
    /// </summary>
    public ConversionResult Convert(string wpfXaml, string? sourcePath = null, ConversionOptions? options = null)
    {
        options ??= new ConversionOptions();

        var result = new ConversionResult
        {
            InputPath = sourcePath,
            Success = true
        };

        try
        {
            // Phase 1: Parse WPF XAML
            _diagnostics.AddInfo("PHASE_PARSE", "Parsing WPF XAML...", sourcePath);
            var document = _parser.Parse(wpfXaml, sourcePath);

            if (document.Root == null)
            {
                _diagnostics.AddError("PARSE_FAILED", "Failed to parse XAML document", sourcePath);
                result.Success = false;
                result.Diagnostics = _diagnostics.Diagnostics.ToList();
                return result;
            }

            // Phase 2: Enrich with type information (optional, based on options)
            if (options.EnableTypeResolution)
            {
                _diagnostics.AddInfo("PHASE_ENRICH", "Enriching AST with type information...", sourcePath);
                _enrichmentPipeline.Enrich(document);
            }

            // Phase 3: Transform WPF → Avalonia
            _diagnostics.AddInfo("PHASE_TRANSFORM", "Transforming WPF to Avalonia...", sourcePath);
            var transformOptions = new TransformationOptions
            {
                PreserveComments = options.PreserveComments,
                PreserveFormatting = options.PreserveFormatting,
                AddTransformationComments = options.AddTransformationComments,
                TargetAvaloniaVersion = options.TargetAvaloniaVersion,
                UseCompiledBindings = options.UseCompiledBindings,
                UseAvaloniaBindingSyntax = options.UseAvaloniaBindingSyntax
            };
            _transformationEngine.Transform(document, transformOptions);

            // Phase 4: Serialize to Avalonia XAML
            _diagnostics.AddInfo("PHASE_SERIALIZE", "Serializing to Avalonia XAML...", sourcePath);
            var serializationOptions = new XamlSerializationOptions
            {
                PreserveFormatting = options.PreserveFormatting,
                PreserveComments = options.PreserveComments,
                UseSelfClosingTags = options.UseSelfClosingTags,
                IndentString = options.IndentString,
                IncludeXmlDeclaration = options.IncludeXmlDeclaration,
                Encoding = options.Encoding,
                AddTransformationComments = options.AddTransformationComments
            };

            var writer = new XamlWriter(serializationOptions);
            result.OutputXaml = writer.WriteDocument(document);
            result.Document = document;

            _diagnostics.AddInfo("CONVERSION_COMPLETE", "Conversion completed successfully", sourcePath);
        }
        catch (Exception ex)
        {
            _diagnostics.AddError(
                "CONVERSION_ERROR",
                $"Conversion failed: {ex.Message}\n{ex.StackTrace}",
                sourcePath);
            result.Success = false;
        }

        result.Diagnostics = _diagnostics.Diagnostics.ToList();
        return result;
    }

    /// <summary>
    /// Converts multiple XAML files.
    /// </summary>
    public BatchConversionResult ConvertBatch(IEnumerable<string> inputPaths, string? outputDirectory = null, ConversionOptions? options = null)
    {
        var batchResult = new BatchConversionResult();

        foreach (var inputPath in inputPaths)
        {
            string? outputPath = null;
            if (outputDirectory != null)
            {
                var fileName = Path.GetFileName(inputPath);
                outputPath = Path.Combine(outputDirectory, fileName);
            }

            var result = ConvertFile(inputPath, outputPath, options);
            batchResult.Results.Add(result);

            if (result.Success)
            {
                batchResult.SuccessCount++;
            }
            else
            {
                batchResult.FailureCount++;
            }
        }

        return batchResult;
    }

    /// <summary>
    /// Gets the default output path for a given input path.
    /// </summary>
    private string GetDefaultOutputPath(string inputPath)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);

        return Path.Combine(directory, $"{fileNameWithoutExt}.avalonia{extension}");
    }
}

/// <summary>
/// Options for XAML conversion.
/// </summary>
public sealed class ConversionOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable type resolution.
    /// </summary>
    public bool EnableTypeResolution { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to preserve comments.
    /// </summary>
    public bool PreserveComments { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to preserve formatting.
    /// </summary>
    public bool PreserveFormatting { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to add transformation comments.
    /// </summary>
    public bool AddTransformationComments { get; set; } = false;

    /// <summary>
    /// Gets or sets the target Avalonia version.
    /// </summary>
    public string TargetAvaloniaVersion { get; set; } = "11.0";

    /// <summary>
    /// Gets or sets a value indicating whether to use compiled bindings.
    /// </summary>
    public bool UseCompiledBindings { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to use Avalonia-specific binding syntax.
    /// When true, converts ElementName bindings to # syntax (e.g., #ElementName.Property).
    /// </summary>
    public bool UseAvaloniaBindingSyntax { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to use self-closing tags.
    /// </summary>
    public bool UseSelfClosingTags { get; set; } = true;

    /// <summary>
    /// Gets or sets the indentation string.
    /// </summary>
    public string IndentString { get; set; } = "    ";

    /// <summary>
    /// Gets or sets a value indicating whether to include XML declaration.
    /// </summary>
    public bool IncludeXmlDeclaration { get; set; } = true;

    /// <summary>
    /// Gets or sets the encoding.
    /// </summary>
    public string Encoding { get; set; } = "utf-8";
}

/// <summary>
/// Result of a XAML conversion.
/// </summary>
public sealed class ConversionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the conversion was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the input file path.
    /// </summary>
    public string? InputPath { get; set; }

    /// <summary>
    /// Gets or sets the output file path.
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Gets or sets the output XAML string.
    /// </summary>
    public string? OutputXaml { get; set; }

    /// <summary>
    /// Gets or sets the transformed document.
    /// </summary>
    public UnifiedXamlDocument? Document { get; set; }

    /// <summary>
    /// Gets or sets the diagnostics.
    /// </summary>
    public List<TransformationDiagnostic> Diagnostics { get; set; } = new();

    /// <summary>
    /// Gets errors from diagnostics.
    /// </summary>
    public IEnumerable<TransformationDiagnostic> Errors =>
        Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Gets warnings from diagnostics.
    /// </summary>
    public IEnumerable<TransformationDiagnostic> Warnings =>
        Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);
}

/// <summary>
/// Result of a batch conversion.
/// </summary>
public sealed class BatchConversionResult
{
    /// <summary>
    /// Gets the individual conversion results.
    /// </summary>
    public List<ConversionResult> Results { get; } = new();

    /// <summary>
    /// Gets or sets the number of successful conversions.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed conversions.
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Gets the total number of conversions.
    /// </summary>
    public int TotalCount => Results.Count;

    /// <summary>
    /// Gets all diagnostics from all conversions.
    /// </summary>
    public IEnumerable<TransformationDiagnostic> AllDiagnostics =>
        Results.SelectMany(r => r.Diagnostics);
}
