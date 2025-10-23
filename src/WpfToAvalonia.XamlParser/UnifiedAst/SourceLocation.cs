namespace WpfToAvalonia.XamlParser.UnifiedAst;

/// <summary>
/// Represents a location in the source XAML file.
/// </summary>
public sealed class SourceLocation
{
    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Gets or sets the character position in the file.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets the length of the source span.
    /// </summary>
    public int Length { get; set; }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(FilePath))
        {
            return $"{FilePath}({Line},{Column})";
        }

        return $"({Line},{Column})";
    }
}
