namespace WpfToAvalonia.Mappings;

/// <summary>
/// Represents a mapping from a WPF type to an Avalonia type.
/// </summary>
public sealed class TypeMapping
{
    /// <summary>
    /// Gets or sets the fully qualified WPF type name (e.g., "System.Windows.Controls.Button").
    /// </summary>
    public required string WpfTypeName { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified Avalonia type name (e.g., "Avalonia.Controls.Button").
    /// </summary>
    public required string AvaloniaTypeName { get; set; }

    /// <summary>
    /// Gets or sets the WPF namespace.
    /// </summary>
    public required string WpfNamespace { get; set; }

    /// <summary>
    /// Gets or sets the Avalonia namespace.
    /// </summary>
    public required string AvaloniaNamespace { get; set; }

    /// <summary>
    /// Gets or sets the simple type name without namespace (e.g., "Button").
    /// </summary>
    public required string SimpleTypeName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the type name changed between WPF and Avalonia.
    /// </summary>
    public bool TypeNameChanged { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the mapping.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this mapping requires manual review.
    /// </summary>
    public bool RequiresManualReview { get; set; }

    /// <summary>
    /// Gets or sets the category of the type (e.g., "Control", "Panel", "Shape", etc.).
    /// </summary>
    public string? Category { get; set; }
}
