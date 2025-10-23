using Microsoft.CodeAnalysis;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Workspace;
using WpfToAvalonia.Core.Project;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.XamlParser;

/// <summary>
/// Orchestrates the end-to-end migration of WPF projects to Avalonia.
/// </summary>
public sealed class MigrationOrchestrator
{
    private readonly IWorkspaceManager _workspaceManager;
    private readonly IMappingRepository _mappingRepository;
    private readonly WpfToAvaloniaConverter _xamlConverter;
    private readonly ProjectFileParser _projectParser;

    public MigrationOrchestrator(
        IWorkspaceManager workspaceManager,
        IMappingRepository mappingRepository,
        WpfToAvaloniaConverter xamlConverter,
        ProjectFileParser projectParser)
    {
        _workspaceManager = workspaceManager ?? throw new ArgumentNullException(nameof(workspaceManager));
        _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
        _xamlConverter = xamlConverter ?? throw new ArgumentNullException(nameof(xamlConverter));
        _projectParser = projectParser ?? throw new ArgumentNullException(nameof(projectParser));
    }

    /// <summary>
    /// Migrates a single project from WPF to Avalonia.
    /// </summary>
    public async Task<MigrationResult> MigrateProjectAsync(
        string projectPath,
        MigrationOptions options,
        CancellationToken cancellationToken = default)
    {
        var result = new MigrationResult
        {
            ProjectPath = projectPath,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Stage 1: Analysis
            result.AddStage("Analysis", "Analyzing WPF project...");
            var analysisResult = await AnalyzeProjectAsync(projectPath, options, cancellationToken);
            result.AnalysisResult = analysisResult;

            if (!analysisResult.IsWpfProject)
            {
                result.Success = false;
                result.Diagnostics.AddError("NOT_WPF_PROJECT", $"Project is not a WPF project: {projectPath}", projectPath);
                return result;
            }

            // Stage 2: Backup (if not dry run)
            if (!options.DryRun && options.CreateBackups)
            {
                result.AddStage("Backup", "Creating backup of original files...");
                await CreateBackupAsync(projectPath, analysisResult, options, cancellationToken);
            }

            // Stage 3: Transform Project File
            result.AddStage("ProjectFile", "Transforming project file...");
            var projectDiagnostics = new DiagnosticCollector();
            var projectTransformResult = await TransformProjectFileAsync(
                analysisResult.ProjectInfo!,
                analysisResult.ProjectAnalysis!,
                options,
                projectDiagnostics,
                cancellationToken);

            result.TransformedProjectPath = projectTransformResult.TransformedProjectPath;
            result.Diagnostics.AddRange(projectDiagnostics.Diagnostics);

            // Stage 4: Transform XAML Files
            result.AddStage("XAML", "Transforming XAML files...");
            var xamlResults = await TransformXamlFilesAsync(
                analysisResult.XamlFiles,
                options,
                cancellationToken);

            result.XamlTransformationResults = xamlResults;
            foreach (var xamlResult in xamlResults)
            {
                result.Diagnostics.AddRange(xamlResult.Diagnostics);
            }

            // Stage 5: Transform C# Files (currently limited)
            result.AddStage("CSharp", "Transforming C# files...");
            var csharpResults = await TransformCSharpFilesAsync(
                analysisResult.CSharpFiles,
                options,
                cancellationToken);

            result.CSharpTransformationResults = csharpResults;

            // Stage 6: Validation
            result.AddStage("Validation", "Validating transformed output...");
            await ValidateTransformationAsync(result, options, cancellationToken);

            // Stage 7: Write Files (if not dry run)
            if (!options.DryRun)
            {
                result.AddStage("Writing", "Writing transformed files to disk...");
                await WriteTransformedFilesAsync(result, options, cancellationToken);
            }

            result.Success = result.Diagnostics.ErrorCount == 0;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Exception = ex;
            result.Diagnostics.AddError("MIGRATION_FAILED", $"Migration failed: {ex.Message}", projectPath);
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Migrates a solution from WPF to Avalonia.
    /// </summary>
    public async Task<SolutionMigrationResult> MigrateSolutionAsync(
        string solutionPath,
        MigrationOptions options,
        CancellationToken cancellationToken = default)
    {
        var result = new SolutionMigrationResult
        {
            SolutionPath = solutionPath,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Load solution
            var solution = await _workspaceManager.LoadSolutionAsync(solutionPath, cancellationToken);

            // Find all WPF projects
            var wpfProjects = new List<Microsoft.CodeAnalysis.Project>();
            foreach (var project in solution.Projects)
            {
                if (IsWpfProject(project))
                {
                    wpfProjects.Add(project);
                }
            }

            if (wpfProjects.Count == 0)
            {
                result.Diagnostics.AddWarning("NO_WPF_PROJECTS", "No WPF projects found in solution", solutionPath);
                result.Success = true;
                return result;
            }

            // Migrate each project
            foreach (var project in wpfProjects)
            {
                var projectResult = await MigrateProjectAsync(project.FilePath!, options, cancellationToken);
                result.ProjectResults.Add(projectResult);
            }

            result.Success = result.ProjectResults.All(r => r.Success);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Exception = ex;
            result.Diagnostics.AddError("SOLUTION_MIGRATION_FAILED", $"Solution migration failed: {ex.Message}", solutionPath);
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
        }

        return result;
    }

    private async Task<ProjectAnalysisResult> AnalyzeProjectAsync(
        string projectPath,
        MigrationOptions options,
        CancellationToken cancellationToken)
    {
        var result = new ProjectAnalysisResult();

        // Load and analyze project file
        var projectInfo = _projectParser.LoadProject(projectPath);
        var isWpf = _projectParser.IsWpfProject(projectInfo);

        result.ProjectInfo = projectInfo;
        result.IsWpfProject = isWpf;

        if (!isWpf)
        {
            return result;
        }

        result.ProjectAnalysis = _projectParser.AnalyzeWpfProject(projectInfo);

        // Find XAML files
        var projectDir = Path.GetDirectoryName(projectPath)!;
        result.XamlFiles = Directory.GetFiles(projectDir, "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/obj/") && !f.Contains("\\obj\\"))
            .Where(f => !f.Contains("/bin/") && !f.Contains("\\bin\\"))
            .ToList();

        // Find C# files
        result.CSharpFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/obj/") && !f.Contains("\\obj\\"))
            .Where(f => !f.Contains("/bin/") && !f.Contains("\\bin\\"))
            .ToList();

        return result;
    }

    private async Task CreateBackupAsync(
        string projectPath,
        ProjectAnalysisResult analysis,
        MigrationOptions options,
        CancellationToken cancellationToken)
    {
        var projectDir = Path.GetDirectoryName(projectPath)!;
        var backupDir = Path.Combine(projectDir, options.BackupDirectory);

        Directory.CreateDirectory(backupDir);

        // Backup project file
        var projectFileName = Path.GetFileName(projectPath);
        File.Copy(projectPath, Path.Combine(backupDir, projectFileName), overwrite: true);

        // Backup XAML files
        foreach (var xamlFile in analysis.XamlFiles)
        {
            var relativePath = Path.GetRelativePath(projectDir, xamlFile);
            var backupPath = Path.Combine(backupDir, relativePath);
            var backupFileDir = Path.GetDirectoryName(backupPath)!;
            Directory.CreateDirectory(backupFileDir);
            File.Copy(xamlFile, backupPath, overwrite: true);
        }

        // Backup C# files
        foreach (var csFile in analysis.CSharpFiles)
        {
            var relativePath = Path.GetRelativePath(projectDir, csFile);
            var backupPath = Path.Combine(backupDir, relativePath);
            var backupFileDir = Path.GetDirectoryName(backupPath)!;
            Directory.CreateDirectory(backupFileDir);
            File.Copy(csFile, backupPath, overwrite: true);
        }

        await Task.CompletedTask;
    }

    private async Task<TransformedProjectInfo> TransformProjectFileAsync(
        Core.Project.ProjectInfo projectInfo,
        Core.Project.WpfProjectAnalysis analysis,
        MigrationOptions options,
        DiagnosticCollector diagnostics,
        CancellationToken cancellationToken)
    {
        var transformer = new ProjectFileTransformer(diagnostics);

        var transformOptions = new ProjectTransformationOptions
        {
            AvaloniaVersion = options.AvaloniaVersion,
            UpdateTargetFramework = options.UpdateTargetFramework,
            TargetFramework = options.TargetFramework,
            RenameXamlToAxaml = options.RenameXamlToAxaml,
            EnableCompiledBindings = options.EnableCompiledBindings,
            OutputProjectPath = options.DryRun ? null : GetTransformedProjectPath(projectInfo.FilePath, options)
        };

        var coreResult = transformer.Transform(projectInfo, analysis, transformOptions);

        if (!options.DryRun)
        {
            transformer.SaveTransformedProject(coreResult);
        }

        // Map Core.Project.TransformedProjectInfo to XamlParser.TransformedProjectInfo
        var result = new TransformedProjectInfo
        {
            OriginalProjectPath = coreResult.OriginalProjectPath,
            TransformedProjectPath = coreResult.TransformedProjectPath,
            TransformedProject = coreResult.TransformedProject,
            Diagnostics = diagnostics.Diagnostics.ToList()
        };

        await Task.CompletedTask;
        return result;
    }

    private async Task<List<XamlTransformationResult>> TransformXamlFilesAsync(
        List<string> xamlFiles,
        MigrationOptions options,
        CancellationToken cancellationToken)
    {
        var results = new List<XamlTransformationResult>();

        foreach (var xamlFile in xamlFiles)
        {
            try
            {
                var xamlContent = await File.ReadAllTextAsync(xamlFile, cancellationToken);
                var converted = _xamlConverter.Convert(xamlContent);

                var result = new XamlTransformationResult
                {
                    OriginalPath = xamlFile,
                    TransformedPath = GetTransformedXamlPath(xamlFile, options),
                    OriginalContent = xamlContent,
                    TransformedContent = converted.OutputXaml ?? string.Empty,
                    Diagnostics = converted.Diagnostics.ToList(),
                    Success = converted.Success
                };

                results.Add(result);
            }
            catch (Exception ex)
            {
                results.Add(new XamlTransformationResult
                {
                    OriginalPath = xamlFile,
                    TransformedPath = xamlFile,
                    Success = false,
                    Diagnostics = new List<TransformationDiagnostic>
                    {
                        new() { Severity = Core.Diagnostics.DiagnosticSeverity.Error, Code = "XAML_TRANSFORM_FAILED", Message = ex.Message, FilePath = xamlFile }
                    }
                });
            }
        }

        return results;
    }

    private async Task<List<CSharpTransformationResult>> TransformCSharpFilesAsync(
        List<string> csharpFiles,
        MigrationOptions options,
        CancellationToken cancellationToken)
    {
        // TODO: Implement C# transformation using Roslyn
        // For now, just return empty results
        var results = new List<CSharpTransformationResult>();

        foreach (var csFile in csharpFiles)
        {
            results.Add(new CSharpTransformationResult
            {
                OriginalPath = csFile,
                TransformedPath = csFile,
                Success = true,
                Diagnostics = new List<TransformationDiagnostic>
                {
                    new() { Severity = Core.Diagnostics.DiagnosticSeverity.Info, Code = "CSHARP_NOT_IMPLEMENTED", Message = "C# transformation not yet implemented", FilePath = csFile }
                }
            });
        }

        await Task.CompletedTask;
        return results;
    }

    private async Task ValidateTransformationAsync(
        MigrationResult result,
        MigrationOptions options,
        CancellationToken cancellationToken)
    {
        // Validate XAML well-formedness
        foreach (var xamlResult in result.XamlTransformationResults)
        {
            if (xamlResult.Success && !string.IsNullOrEmpty(xamlResult.TransformedContent))
            {
                try
                {
                    System.Xml.Linq.XDocument.Parse(xamlResult.TransformedContent);
                }
                catch (Exception ex)
                {
                    result.Diagnostics.AddError("INVALID_XAML", $"Transformed XAML is not well-formed: {ex.Message}", xamlResult.TransformedPath);
                }
            }
        }

        await Task.CompletedTask;
    }

    private async Task WriteTransformedFilesAsync(
        MigrationResult result,
        MigrationOptions options,
        CancellationToken cancellationToken)
    {
        // Write XAML files
        foreach (var xamlResult in result.XamlTransformationResults.Where(r => r.Success))
        {
            var outputPath = xamlResult.TransformedPath;
            var outputDir = Path.GetDirectoryName(outputPath)!;
            Directory.CreateDirectory(outputDir);

            await File.WriteAllTextAsync(outputPath, xamlResult.TransformedContent, cancellationToken);

            // If renaming, delete old file
            if (options.RenameXamlToAxaml && xamlResult.OriginalPath != xamlResult.TransformedPath)
            {
                if (File.Exists(xamlResult.OriginalPath))
                {
                    File.Delete(xamlResult.OriginalPath);
                }
            }
        }
    }

    private bool IsWpfProject(Microsoft.CodeAnalysis.Project project)
    {
        // Simple heuristic: check if project has WPF references
        return project.MetadataReferences.Any(r =>
            r.Display?.Contains("PresentationCore") == true ||
            r.Display?.Contains("PresentationFramework") == true);
    }

    private string GetTransformedProjectPath(string originalPath, MigrationOptions options)
    {
        if (!string.IsNullOrEmpty(options.OutputProjectPath))
        {
            return options.OutputProjectPath;
        }

        var directory = Path.GetDirectoryName(originalPath) ?? "";
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
        var extension = Path.GetExtension(originalPath);

        return Path.Combine(directory, $"{fileNameWithoutExtension}.Avalonia{extension}");
    }

    private string GetTransformedXamlPath(string originalPath, MigrationOptions options)
    {
        if (options.RenameXamlToAxaml && originalPath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
        {
            return Path.ChangeExtension(originalPath, ".axaml");
        }

        return originalPath;
    }
}
