namespace WpfToAvalonia.XamlParser.Serialization;

/// <summary>
/// Options for XAML serialization.
/// </summary>
public sealed class XamlSerializationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to preserve formatting from the original document.
    /// </summary>
    public bool PreserveFormatting { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to preserve comments.
    /// </summary>
    public bool PreserveComments { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use self-closing tags where possible.
    /// Example: &lt;Button /&gt; instead of &lt;Button&gt;&lt;/Button&gt;
    /// </summary>
    public bool UseSelfClosingTags { get; set; } = true;

    /// <summary>
    /// Gets or sets the indentation string.
    /// </summary>
    public string IndentString { get; set; } = "    "; // 4 spaces

    /// <summary>
    /// Gets or sets a value indicating whether to add XML declaration.
    /// </summary>
    public bool IncludeXmlDeclaration { get; set; } = true;

    /// <summary>
    /// Gets or sets the XML encoding.
    /// </summary>
    public string Encoding { get; set; } = "utf-8";

    /// <summary>
    /// Gets or sets a value indicating whether to sort attributes alphabetically.
    /// </summary>
    public bool SortAttributes { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to put attributes on separate lines.
    /// </summary>
    public bool AttributesOnSeparateLines { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum line length before wrapping attributes.
    /// 0 = no wrapping based on length.
    /// </summary>
    public int MaxLineLength { get; set; } = 120;

    /// <summary>
    /// Gets or sets a value indicating whether to add transformation comments.
    /// </summary>
    public bool AddTransformationComments { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to include xmlns declarations.
    /// </summary>
    public bool IncludeXmlnsDeclarations { get; set; } = true;

    /// <summary>
    /// Gets or sets the newline string.
    /// </summary>
    public string NewLine { get; set; } = Environment.NewLine;
}
