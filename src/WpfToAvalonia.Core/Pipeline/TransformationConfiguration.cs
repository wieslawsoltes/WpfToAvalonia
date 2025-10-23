namespace WpfToAvalonia.Core.Pipeline;

/// <summary>
/// Configuration options for the transformation process.
/// </summary>
public sealed class TransformationConfiguration
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
    /// Gets or sets the transformation strategy (Aggressive or Conservative).
    /// </summary>
    public TransformationStrategy Strategy { get; set; } = TransformationStrategy.Conservative;

    /// <summary>
    /// Gets or sets a value indicating whether to enable parallel processing.
    /// </summary>
    public bool EnableParallelProcessing { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism (0 = unlimited).
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 0;

    /// <summary>
    /// Gets or sets file include patterns (glob patterns).
    /// </summary>
    public List<string> IncludePatterns { get; set; } = new() { "**/*.cs", "**/*.xaml", "**/*.axaml" };

    /// <summary>
    /// Gets or sets file exclude patterns (glob patterns).
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = new() { "**/obj/**", "**/bin/**" };

    /// <summary>
    /// Gets or sets a value indicating whether to preserve trivia (whitespace, comments).
    /// </summary>
    public bool PreserveTrivia { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to preserve formatting.
    /// </summary>
    public bool PreserveFormatting { get; set; } = true;

    /// <summary>
    /// Gets or sets custom mapping file paths.
    /// </summary>
    public List<string> CustomMappingFiles { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to rename .xaml files to .axaml.
    /// </summary>
    public bool RenameXamlToAxaml { get; set; } = true;

    /// <summary>
    /// Gets or sets the verbosity level for logging.
    /// </summary>
    public VerbosityLevel Verbosity { get; set; } = VerbosityLevel.Normal;
}

/// <summary>
/// Defines the transformation strategy.
/// </summary>
public enum TransformationStrategy
{
    /// <summary>
    /// Conservative: Only apply transformations that are guaranteed to be safe.
    /// </summary>
    Conservative,

    /// <summary>
    /// Aggressive: Apply all possible transformations, flagging uncertain ones for review.
    /// </summary>
    Aggressive
}

/// <summary>
/// Defines verbosity levels for logging.
/// </summary>
public enum VerbosityLevel
{
    /// <summary>
    /// Quiet: Minimal output.
    /// </summary>
    Quiet,

    /// <summary>
    /// Normal: Standard output.
    /// </summary>
    Normal,

    /// <summary>
    /// Verbose: Detailed output.
    /// </summary>
    Verbose,

    /// <summary>
    /// Diagnostic: Very detailed output for debugging.
    /// </summary>
    Diagnostic
}
