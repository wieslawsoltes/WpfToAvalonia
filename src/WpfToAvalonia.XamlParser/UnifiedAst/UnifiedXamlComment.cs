namespace WpfToAvalonia.XamlParser.UnifiedAst;

/// <summary>
/// Represents an XML comment in the XAML document.
/// Comments are preserved during transformation to maintain documentation and readability.
/// </summary>
public sealed class UnifiedXamlComment : UnifiedXamlNode
{
    /// <summary>
    /// Gets or sets the comment text (without the &lt;!-- and --&gt; delimiters).
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the position of the comment relative to its context.
    /// </summary>
    public CommentPosition Position { get; set; } = CommentPosition.Standalone;

    /// <summary>
    /// Gets or sets a value indicating whether the comment should be preserved during transformation.
    /// Default is true. Set to false to remove comments during processing.
    /// </summary>
    public bool Preserve { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this comment contains transformation metadata.
    /// Metadata comments (like // WpfToAvalonia:ignore) may be processed differently.
    /// </summary>
    public bool IsMetadata { get; set; }

    public override string ToString()
    {
        var preview = Text.Length > 50 ? Text.Substring(0, 47) + "..." : Text;
        return $"<!-- {preview} -->";
    }
}

/// <summary>
/// Defines the position of a comment relative to its surrounding content.
/// </summary>
public enum CommentPosition
{
    /// <summary>
    /// Comment is on its own line(s), not adjacent to any element.
    /// Example:
    /// &lt;!-- This is a standalone comment --&gt;
    /// &lt;Button /&gt;
    /// </summary>
    Standalone,

    /// <summary>
    /// Comment appears before an element on the same or previous line.
    /// Example:
    /// &lt;!-- Button configuration --&gt; &lt;Button /&gt;
    /// or
    /// &lt;!-- Button configuration --&gt;
    /// &lt;Button /&gt;
    /// </summary>
    BeforeElement,

    /// <summary>
    /// Comment appears after an element on the same line.
    /// Example:
    /// &lt;Button /&gt; &lt;!-- Primary action --&gt;
    /// </summary>
    AfterElement,

    /// <summary>
    /// Comment appears within an element's attributes.
    /// Example:
    /// &lt;Button
    ///     &lt;!-- Important property --&gt;
    ///     IsEnabled="True" /&gt;
    /// </summary>
    WithinAttributes,

    /// <summary>
    /// Comment appears within an element's content.
    /// Example:
    /// &lt;StackPanel&gt;
    ///     &lt;!-- Children go here --&gt;
    ///     &lt;Button /&gt;
    /// &lt;/StackPanel&gt;
    /// </summary>
    WithinContent
}
