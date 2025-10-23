namespace WpfToAvalonia.Mappings;

/// <summary>
/// Defines operations for loading and querying mapping data.
/// </summary>
public interface IMappingRepository
{
    /// <summary>
    /// Loads the mapping database from storage.
    /// </summary>
    /// <returns>The loaded mapping database.</returns>
    Task<MappingDatabase> LoadAsync();

    /// <summary>
    /// Saves the mapping database to storage.
    /// </summary>
    /// <param name="database">The database to save.</param>
    Task SaveAsync(MappingDatabase database);

    /// <summary>
    /// Finds a namespace mapping for the given WPF namespace.
    /// </summary>
    /// <param name="wpfNamespace">The WPF namespace to lookup.</param>
    /// <returns>The namespace mapping, or null if not found.</returns>
    NamespaceMapping? FindNamespaceMapping(string wpfNamespace);

    /// <summary>
    /// Finds a type mapping for the given WPF type name.
    /// </summary>
    /// <param name="wpfTypeName">The fully qualified WPF type name.</param>
    /// <returns>The type mapping, or null if not found.</returns>
    TypeMapping? FindTypeMapping(string wpfTypeName);

    /// <summary>
    /// Finds a property mapping for the given property name and optional owner type.
    /// </summary>
    /// <param name="wpfPropertyName">The WPF property name.</param>
    /// <param name="ownerTypeName">Optional owner type name for type-specific mappings.</param>
    /// <returns>The property mapping, or null if not found.</returns>
    PropertyMapping? FindPropertyMapping(string wpfPropertyName, string? ownerTypeName = null);

    /// <summary>
    /// Finds an event mapping for the given event name and optional owner type.
    /// </summary>
    /// <param name="wpfEventName">The WPF event name.</param>
    /// <param name="ownerTypeName">Optional owner type name for type-specific mappings.</param>
    /// <returns>The event mapping, or null if not found.</returns>
    EventMapping? FindEventMapping(string wpfEventName, string? ownerTypeName = null);

    /// <summary>
    /// Gets all namespace mappings.
    /// </summary>
    IReadOnlyList<NamespaceMapping> GetAllNamespaceMappings();

    /// <summary>
    /// Gets all type mappings.
    /// </summary>
    IReadOnlyList<TypeMapping> GetAllTypeMappings();

    /// <summary>
    /// Gets all property mappings.
    /// </summary>
    IReadOnlyList<PropertyMapping> GetAllPropertyMappings();

    /// <summary>
    /// Gets all event mappings.
    /// </summary>
    IReadOnlyList<EventMapping> GetAllEventMappings();
}
