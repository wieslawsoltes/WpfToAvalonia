namespace WpfToAvalonia.Mappings;

/// <summary>
/// Represents a mapping from a WPF property to an Avalonia property.
/// </summary>
public sealed class PropertyMapping
{
    /// <summary>
    /// Gets or sets the WPF property name (e.g., "Visibility").
    /// </summary>
    public required string WpfPropertyName { get; set; }

    /// <summary>
    /// Gets or sets the Avalonia property name (e.g., "IsVisible").
    /// </summary>
    public required string AvaloniaPropertyName { get; set; }

    /// <summary>
    /// Gets or sets the owner type name (optional, if property mapping is type-specific).
    /// </summary>
    public string? OwnerTypeName { get; set; }

    /// <summary>
    /// Gets or sets the WPF property type (e.g., "Visibility").
    /// </summary>
    public string? WpfPropertyType { get; set; }

    /// <summary>
    /// Gets or sets the Avalonia property type (e.g., "bool").
    /// </summary>
    public string? AvaloniaPropertyType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the property type changed.
    /// </summary>
    public bool TypeChanged { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is an attached property.
    /// </summary>
    public bool IsAttachedProperty { get; set; }

    /// <summary>
    /// Gets or sets the value conversion rule (e.g., "Visibility.Visible -> true").
    /// </summary>
    public string? ValueConversionRule { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the mapping.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this mapping requires manual review.
    /// </summary>
    public bool RequiresManualReview { get; set; }
}
