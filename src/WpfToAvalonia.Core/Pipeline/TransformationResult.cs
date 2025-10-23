using WpfToAvalonia.Core.Diagnostics;

namespace WpfToAvalonia.Core.Pipeline;

/// <summary>
/// Represents the result of a transformation operation.
/// </summary>
public sealed class TransformationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the transformation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the collection of diagnostics (warnings, errors, information).
    /// </summary>
    public required DiagnosticCollector Diagnostics { get; set; }

    /// <summary>
    /// Gets or sets the list of files that were transformed.
    /// </summary>
    public List<string> TransformedFiles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of files that were skipped.
    /// </summary>
    public List<string> SkippedFiles { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of files that failed to transform.
    /// </summary>
    public List<string> FailedFiles { get; set; } = new();

    /// <summary>
    /// Gets or sets transformation statistics.
    /// </summary>
    public TransformationStatistics Statistics { get; set; } = new();

    /// <summary>
    /// Gets or sets the elapsed time for the transformation.
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// Gets or sets any exception that occurred during transformation.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the transformation.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Statistics about a transformation operation.
/// </summary>
public sealed class TransformationStatistics
{
    /// <summary>
    /// Gets or sets the total number of files processed.
    /// </summary>
    public int TotalFilesProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of C# files transformed.
    /// </summary>
    public int CSharpFilesTransformed { get; set; }

    /// <summary>
    /// Gets or sets the number of XAML files transformed.
    /// </summary>
    public int XamlFilesTransformed { get; set; }

    /// <summary>
    /// Gets or sets the number of project files transformed.
    /// </summary>
    public int ProjectFilesTransformed { get; set; }

    /// <summary>
    /// Gets or sets the number of using directives changed.
    /// </summary>
    public int UsingDirectivesChanged { get; set; }

    /// <summary>
    /// Gets or sets the number of type references changed.
    /// </summary>
    public int TypeReferencesChanged { get; set; }

    /// <summary>
    /// Gets or sets the number of property references changed.
    /// </summary>
    public int PropertyReferencesChanged { get; set; }

    /// <summary>
    /// Gets or sets the number of event references changed.
    /// </summary>
    public int EventReferencesChanged { get; set; }

    /// <summary>
    /// Gets or sets the number of XAML namespaces changed.
    /// </summary>
    public int XamlNamespacesChanged { get; set; }

    /// <summary>
    /// Gets or sets the number of XAML controls changed.
    /// </summary>
    public int XamlControlsChanged { get; set; }

    /// <summary>
    /// Gets or sets the number of dependency properties converted.
    /// </summary>
    public int DependencyPropertiesConverted { get; set; }

    /// <summary>
    /// Gets or sets the number of manual review items flagged.
    /// </summary>
    public int ManualReviewItemsFlagged { get; set; }
}
