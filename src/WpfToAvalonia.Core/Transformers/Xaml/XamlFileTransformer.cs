using System.Xml.Linq;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Pipeline;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.Xaml;

/// <summary>
/// Orchestrates XAML file transformations from WPF to Avalonia.
/// </summary>
public sealed class XamlFileTransformer : ITransformer
{
    private readonly DiagnosticCollector _diagnostics;
    private readonly IMappingRepository _mappingRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="XamlFileTransformer"/> class.
    /// </summary>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <param name="mappingRepository">The mapping repository.</param>
    public XamlFileTransformer(
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
    }

    /// <summary>
    /// Gets the name of the transformer.
    /// </summary>
    public string Name => "XAML File Transformer";

    /// <summary>
    /// Gets the priority of this transformer.
    /// </summary>
    public int Priority => 20; // Run after C# transformations

    /// <summary>
    /// Determines whether this transformer can handle the context.
    /// </summary>
    public bool CanTransform(TransformationContext context)
    {
        return true; // Always can transform XAML files
    }

    /// <summary>
    /// Transforms XAML files in the context.
    /// </summary>
    public async Task TransformAsync(
        TransformationContext context,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        try
        {
            // Find all XAML files
            var xamlFiles = FindXamlFiles(context);

            _diagnostics.AddInfo(
                DiagnosticCodes.GeneralInfo,
                $"Found {xamlFiles.Count} XAML files to transform",
                null);

            // Transform each XAML file
            foreach (var xamlFile in xamlFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (context.Configuration.DryRun)
                {
                    _diagnostics.AddInfo(
                        DiagnosticCodes.GeneralInfo,
                        $"[Dry Run] Would transform XAML file: {xamlFile}",
                        xamlFile);
                    continue;
                }

                await TransformXamlFileAsync(xamlFile, context, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _diagnostics.AddWarning(
                DiagnosticCodes.GeneralWarning,
                "XAML transformation was cancelled",
                null);
            throw;
        }
        catch (Exception ex)
        {
            _diagnostics.AddError(
                DiagnosticCodes.GeneralError,
                $"XAML transformation failed: {ex.Message}",
                null);
        }
    }

    /// <summary>
    /// Transforms a single XAML file.
    /// </summary>
    private async Task TransformXamlFileAsync(
        string filePath,
        TransformationContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            _diagnostics.AddInfo(
                DiagnosticCodes.GeneralInfo,
                $"Transforming XAML file: {filePath}",
                filePath);

            // Parse the XAML file
            var parser = new XamlParser(_diagnostics);
            var document = parser.ParseFile(filePath);

            if (document?.Root == null)
            {
                _diagnostics.AddError(
                    DiagnosticCodes.XamlParseError,
                    $"Failed to parse XAML file or file has no root element",
                    filePath);
                return;
            }

            // Create backup if configured
            if (context.Configuration.CreateBackups)
            {
                var backupPath = filePath + ".bak";
                File.Copy(filePath, backupPath, overwrite: true);
                _diagnostics.AddInfo(
                    DiagnosticCodes.FileBackedUp,
                    $"Created backup: {backupPath}",
                    filePath);
            }

            // Apply namespace transformation to root
            var namespaceTransformer = new XamlNamespaceTransformer(_diagnostics, _mappingRepository)
            {
                FilePath = filePath
            };
            var transformedRoot = namespaceTransformer.TransformRoot(document.Root);
            document = new XDocument(transformedRoot);

            // Apply control transformation recursively
            var controlTransformer = new XamlControlTransformer(_diagnostics, _mappingRepository)
            {
                FilePath = filePath
            };
            var controlTransformedRoot = controlTransformer.VisitRecursive(document.Root);
            document = new XDocument(controlTransformedRoot);

            // Apply property transformation recursively
            var propertyTransformer = new XamlPropertyTransformer(_diagnostics, _mappingRepository)
            {
                FilePath = filePath
            };
            var propertyTransformedRoot = propertyTransformer.VisitRecursive(document.Root);
            document = new XDocument(propertyTransformedRoot);

            // Save the transformed document
            parser.SaveFile(document, filePath);

            var totalChanges = namespaceTransformer.NamespacesChanged +
                              controlTransformer.ControlsChanged +
                              propertyTransformer.PropertiesChanged;

            _diagnostics.AddInfo(
                DiagnosticCodes.FileTransformed,
                $"XAML transformation complete: {totalChanges} total changes " +
                $"({namespaceTransformer.NamespacesChanged} namespaces, " +
                $"{controlTransformer.ControlsChanged} controls, " +
                $"{propertyTransformer.PropertiesChanged} properties)",
                filePath);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _diagnostics.AddError(
                DiagnosticCodes.XamlTransformationError,
                $"Failed to transform XAML file: {ex.Message}",
                filePath);
        }
    }

    /// <summary>
    /// Finds all XAML files in the project.
    /// </summary>
    private List<string> FindXamlFiles(TransformationContext context)
    {
        var xamlFiles = new List<string>();

        // If we have a project loaded, get XAML files from it
        if (context.Project != null)
        {
            var projectDirectory = Path.GetDirectoryName(context.Project.FilePath);
            if (!string.IsNullOrEmpty(projectDirectory))
            {
                xamlFiles.AddRange(Directory.GetFiles(
                    projectDirectory,
                    "*.xaml",
                    SearchOption.AllDirectories));
            }
        }
        // Otherwise, use the configuration patterns
        else if (context.Configuration.IncludePatterns?.Any() == true)
        {
            // Filter include patterns for XAML files only
            var xamlPatterns = context.Configuration.IncludePatterns
                .Where(p => p.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) ||
                           p.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var pattern in xamlPatterns)
            {
                var directory = Path.GetDirectoryName(pattern) ?? ".";
                var fileName = Path.GetFileName(pattern);

                if (Directory.Exists(directory))
                {
                    xamlFiles.AddRange(Directory.GetFiles(
                        directory,
                        fileName,
                        SearchOption.AllDirectories));
                }
            }
        }

        // Filter based on exclude patterns
        if (context.Configuration.ExcludePatterns?.Any() == true)
        {
            xamlFiles = xamlFiles.Where(file =>
                !context.Configuration.ExcludePatterns.Any(pattern =>
                    file.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return xamlFiles;
    }
}
