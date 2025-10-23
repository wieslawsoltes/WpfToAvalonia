using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Project;

namespace WpfToAvalonia.XamlParser;

/// <summary>
/// Options for migration operations.
/// </summary>
public sealed class MigrationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether this is a dry run (no actual changes).
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to create backups of files before transformation.
    /// </summary>
    public bool CreateBackups { get; set; } = true;

    /// <summary>
    /// Gets or sets the backup directory path (relative or absolute).
    /// </summary>
    public string BackupDirectory { get; set; } = ".migration-backup";

    /// <summary>
    /// Gets or sets the Avalonia version to use for package references.
    /// </summary>
    public string AvaloniaVersion { get; set; } = "11.2.2";

    /// <summary>
    /// Gets or sets a value indicating whether to update the TargetFramework property.
    /// </summary>
    public bool UpdateTargetFramework { get; set; } = false;

    /// <summary>
    /// Gets or sets the target framework to use (e.g., net8.0).
    /// </summary>
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to rename XAML files from .xaml to .axaml.
    /// </summary>
    public bool RenameXamlToAxaml { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable compiled bindings by default.
    /// </summary>
    public bool EnableCompiledBindings { get; set; } = true;

    /// <summary>
    /// Gets or sets the output path for the transformed project file.
    /// </summary>
    public string? OutputProjectPath { get; set; }
}

/// <summary>
/// Result of analyzing a project.
/// </summary>
public sealed class ProjectAnalysisResult
{
    public Core.Project.ProjectInfo? ProjectInfo { get; set; }
    public bool IsWpfProject { get; set; }
    public Core.Project.WpfProjectAnalysis? ProjectAnalysis { get; set; }
    public List<string> XamlFiles { get; set; } = new();
    public List<string> CSharpFiles { get; set; } = new();
}

/// <summary>
/// Result of migrating a single project.
/// </summary>
public sealed class MigrationResult
{
    public required string ProjectPath { get; init; }
    public bool Success { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan ElapsedTime => (EndTime ?? DateTime.UtcNow) - StartTime;

    public ProjectAnalysisResult? AnalysisResult { get; set; }
    public string? TransformedProjectPath { get; set; }

    public List<XamlTransformationResult> XamlTransformationResults { get; set; } = new();
    public List<CSharpTransformationResult> CSharpTransformationResults { get; set; } = new();

    public DiagnosticCollector Diagnostics { get; } = new();
    public Exception? Exception { get; set; }

    public List<MigrationStage> Stages { get; } = new();

    public void AddStage(string name, string description)
    {
        Stages.Add(new MigrationStage
        {
            Name = name,
            Description = description,
            StartTime = DateTime.UtcNow
        });
    }

    public MigrationStatistics GetStatistics()
    {
        return new MigrationStatistics
        {
            TotalXamlFiles = XamlTransformationResults.Count,
            SuccessfulXamlFiles = XamlTransformationResults.Count(r => r.Success),
            FailedXamlFiles = XamlTransformationResults.Count(r => !r.Success),
            TotalCSharpFiles = CSharpTransformationResults.Count,
            TotalErrors = Diagnostics.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error),
            TotalWarnings = Diagnostics.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning),
            TotalInfo = Diagnostics.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Info),
            ElapsedTime = ElapsedTime
        };
    }
}

/// <summary>
/// Result of migrating a solution.
/// </summary>
public sealed class SolutionMigrationResult
{
    public required string SolutionPath { get; init; }
    public bool Success { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan ElapsedTime => (EndTime ?? DateTime.UtcNow) - StartTime;

    public List<MigrationResult> ProjectResults { get; } = new();
    public DiagnosticCollector Diagnostics { get; } = new();
    public Exception? Exception { get; set; }

    public SolutionMigrationStatistics GetStatistics()
    {
        return new SolutionMigrationStatistics
        {
            TotalProjects = ProjectResults.Count,
            SuccessfulProjects = ProjectResults.Count(r => r.Success),
            FailedProjects = ProjectResults.Count(r => !r.Success),
            TotalXamlFiles = ProjectResults.Sum(r => r.XamlTransformationResults.Count),
            TotalCSharpFiles = ProjectResults.Sum(r => r.CSharpTransformationResults.Count),
            TotalErrors = ProjectResults.Sum(r => r.Diagnostics.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error)),
            TotalWarnings = ProjectResults.Sum(r => r.Diagnostics.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning)),
            ElapsedTime = ElapsedTime
        };
    }
}

/// <summary>
/// Result of transforming a XAML file.
/// </summary>
public sealed class XamlTransformationResult
{
    public required string OriginalPath { get; init; }
    public required string TransformedPath { get; init; }
    public string? OriginalContent { get; set; }
    public string? TransformedContent { get; set; }
    public bool Success { get; set; }
    public List<TransformationDiagnostic> Diagnostics { get; set; } = new();
}

/// <summary>
/// Result of transforming a C# file.
/// </summary>
public sealed class CSharpTransformationResult
{
    public required string OriginalPath { get; init; }
    public required string TransformedPath { get; init; }
    public string? OriginalContent { get; set; }
    public string? TransformedContent { get; set; }
    public bool Success { get; set; }
    public List<TransformationDiagnostic> Diagnostics { get; set; } = new();
}

/// <summary>
/// Represents a stage in the migration process.
/// </summary>
public sealed class MigrationStage
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime? EndTime { get; set; }
    public TimeSpan ElapsedTime => (EndTime ?? DateTime.UtcNow) - StartTime;
}

/// <summary>
/// Statistics about a migration operation.
/// </summary>
public sealed class MigrationStatistics
{
    public int TotalXamlFiles { get; init; }
    public int SuccessfulXamlFiles { get; init; }
    public int FailedXamlFiles { get; init; }
    public int TotalCSharpFiles { get; init; }
    public int TotalErrors { get; init; }
    public int TotalWarnings { get; init; }
    public int TotalInfo { get; init; }
    public TimeSpan ElapsedTime { get; init; }
}

/// <summary>
/// Statistics about a solution migration operation.
/// </summary>
public sealed class SolutionMigrationStatistics
{
    public int TotalProjects { get; init; }
    public int SuccessfulProjects { get; init; }
    public int FailedProjects { get; init; }
    public int TotalXamlFiles { get; init; }
    public int TotalCSharpFiles { get; init; }
    public int TotalErrors { get; init; }
    public int TotalWarnings { get; init; }
    public TimeSpan ElapsedTime { get; init; }
}

/// <summary>
/// Extended TransformedProjectInfo with diagnostics.
/// </summary>
public sealed class TransformedProjectInfo
{
    public required string OriginalProjectPath { get; init; }
    public required string TransformedProjectPath { get; init; }
    public required Microsoft.Build.Construction.ProjectRootElement TransformedProject { get; set; }
    public List<TransformationDiagnostic> Diagnostics { get; set; } = new();
}
