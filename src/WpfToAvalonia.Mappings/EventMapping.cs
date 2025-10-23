namespace WpfToAvalonia.Mappings;

/// <summary>
/// Represents a mapping from a WPF event to an Avalonia event.
/// </summary>
public sealed class EventMapping
{
    /// <summary>
    /// Gets or sets the WPF event name (e.g., "Loaded").
    /// </summary>
    public required string WpfEventName { get; set; }

    /// <summary>
    /// Gets or sets the Avalonia event name (e.g., "Loaded").
    /// </summary>
    public required string AvaloniaEventName { get; set; }

    /// <summary>
    /// Gets or sets the owner type name (optional, if event mapping is type-specific).
    /// </summary>
    public string? OwnerTypeName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a routed event.
    /// </summary>
    public bool IsRoutedEvent { get; set; }

    /// <summary>
    /// Gets or sets the routing strategy (Tunnel, Bubble, Direct).
    /// </summary>
    public string? RoutingStrategy { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the mapping.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this mapping requires manual review.
    /// </summary>
    public bool RequiresManualReview { get; set; }
}
