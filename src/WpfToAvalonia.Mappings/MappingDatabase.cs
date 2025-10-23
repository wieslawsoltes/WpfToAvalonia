namespace WpfToAvalonia.Mappings;

/// <summary>
/// The central database containing all WPF to Avalonia mappings.
/// </summary>
public sealed class MappingDatabase
{
    /// <summary>
    /// Gets or sets the collection of namespace mappings.
    /// </summary>
    public List<NamespaceMapping> NamespaceMappings { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of type mappings.
    /// </summary>
    public List<TypeMapping> TypeMappings { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of property mappings.
    /// </summary>
    public List<PropertyMapping> PropertyMappings { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of event mappings.
    /// </summary>
    public List<EventMapping> EventMappings { get; set; } = new();

    /// <summary>
    /// Gets or sets the database version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
