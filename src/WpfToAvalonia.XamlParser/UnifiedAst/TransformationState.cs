namespace WpfToAvalonia.XamlParser.UnifiedAst;

/// <summary>
/// Represents the transformation state of a XAML node.
/// </summary>
public enum TransformationState
{
    /// <summary>
    /// Node has not been analyzed yet.
    /// </summary>
    Unanalyzed,

    /// <summary>
    /// Node has been parsed and analyzed.
    /// </summary>
    Analyzed,

    /// <summary>
    /// Node needs semantic analysis (XamlX parsing).
    /// </summary>
    NeedsSemanticAnalysis,

    /// <summary>
    /// Node is being transformed.
    /// </summary>
    Transforming,

    /// <summary>
    /// Node has been successfully transformed.
    /// </summary>
    Transformed,

    /// <summary>
    /// Node transformation was skipped.
    /// </summary>
    Skipped,

    /// <summary>
    /// Node transformation failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Node requires manual review.
    /// </summary>
    RequiresManualReview
}
