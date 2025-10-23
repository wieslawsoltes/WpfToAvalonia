using Microsoft.CodeAnalysis;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Pipeline;

/// <summary>
/// Represents the shared context for a transformation operation.
/// </summary>
public sealed class TransformationContext
{
    /// <summary>
    /// Gets or sets the workspace containing the solution/projects to transform.
    /// </summary>
    public Microsoft.CodeAnalysis.Workspace? Workspace { get; set; }

    /// <summary>
    /// Gets or sets the solution being transformed.
    /// </summary>
    public Solution? Solution { get; set; }

    /// <summary>
    /// Gets or sets the project being transformed (if transforming a single project).
    /// </summary>
    public Microsoft.CodeAnalysis.Project? Project { get; set; }

    /// <summary>
    /// Gets or sets the mapping repository for WPF to Avalonia mappings.
    /// </summary>
    public required IMappingRepository MappingRepository { get; set; }

    /// <summary>
    /// Gets or sets the transformation configuration.
    /// </summary>
    public required TransformationConfiguration Configuration { get; set; }

    /// <summary>
    /// Gets the diagnostic collector for gathering warnings and errors.
    /// </summary>
    public DiagnosticCollector Diagnostics { get; } = new();

    /// <summary>
    /// Gets or sets a dictionary for storing arbitrary state during transformation.
    /// </summary>
    public Dictionary<string, object> State { get; } = new();

    /// <summary>
    /// Gets or sets the list of files that have been transformed.
    /// </summary>
    public List<string> TransformedFiles { get; } = new();

    /// <summary>
    /// Gets or sets the start time of the transformation.
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the end time of the transformation.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets a value indicating whether the transformation is a dry run (no actual changes).
    /// </summary>
    public bool IsDryRun => Configuration.DryRun;

    /// <summary>
    /// Gets the elapsed time of the transformation.
    /// </summary>
    public TimeSpan Elapsed => (EndTime ?? DateTime.UtcNow) - StartTime;
}
