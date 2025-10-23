namespace WpfToAvalonia.Mappings;

/// <summary>
/// Represents a mapping from a WPF namespace to an Avalonia namespace.
/// </summary>
public sealed class NamespaceMapping
{
    /// <summary>
    /// Gets or sets the WPF namespace (e.g., "System.Windows").
    /// </summary>
    public required string WpfNamespace { get; set; }

    /// <summary>
    /// Gets or sets the Avalonia namespace (e.g., "Avalonia").
    /// </summary>
    public required string AvaloniaNamespace { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the mapping.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this mapping requires manual review.
    /// </summary>
    public bool RequiresManualReview { get; set; }
}
