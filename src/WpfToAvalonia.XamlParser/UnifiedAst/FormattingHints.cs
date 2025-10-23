using System.Xml.Linq;

namespace WpfToAvalonia.XamlParser.UnifiedAst;

/// <summary>
/// Stores formatting information to preserve the original XAML appearance.
/// </summary>
public sealed class FormattingHints
{
    /// <summary>
    /// Gets or sets the whitespace before this node.
    /// </summary>
    public string? LeadingWhitespace { get; set; }

    /// <summary>
    /// Gets or sets the whitespace after this node.
    /// </summary>
    public string? TrailingWhitespace { get; set; }

    /// <summary>
    /// Gets or sets the inner whitespace (between opening and closing tags).
    /// </summary>
    public string? InnerWhitespace { get; set; }

    /// <summary>
    /// Gets or sets the indentation level.
    /// </summary>
    public int IndentLevel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to preserve line breaks before this node.
    /// </summary>
    public bool PreserveLineBreak { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there's a newline after this node.
    /// </summary>
    public bool HasNewlineAfter { get; set; }

    /// <summary>
    /// Gets the comments associated with this node.
    /// </summary>
    public List<XComment> AssociatedComments { get; } = new();

    /// <summary>
    /// Gets or sets the original text representation (for fallback).
    /// </summary>
    public string? OriginalText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this element should be on a single line.
    /// </summary>
    public bool SingleLine { get; set; }

    /// <summary>
    /// Gets or sets the indentation string (tabs or spaces).
    /// </summary>
    public string IndentString { get; set; } = "    "; // Default: 4 spaces
}
