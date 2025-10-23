using System.Text.Json;

namespace WpfToAvalonia.Mappings;

/// <summary>
/// JSON-based implementation of the mapping repository.
/// </summary>
public sealed class JsonMappingRepository : IMappingRepository
{
    private readonly string _filePath;
    private MappingDatabase? _database;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonMappingRepository"/> class.
    /// </summary>
    /// <param name="filePath">The path to the JSON mapping file.</param>
    public JsonMappingRepository(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async Task<MappingDatabase> LoadAsync()
    {
        if (_database != null)
        {
            return _database;
        }

        if (!File.Exists(_filePath))
        {
            _database = new MappingDatabase();
            return _database;
        }

        var json = await File.ReadAllTextAsync(_filePath);
        _database = JsonSerializer.Deserialize<MappingDatabase>(json, _jsonOptions)
                    ?? new MappingDatabase();
        return _database;
    }

    /// <inheritdoc />
    public async Task SaveAsync(MappingDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        database.LastUpdated = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(database, _jsonOptions);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(_filePath, json);
        _database = database;
    }

    /// <inheritdoc />
    public NamespaceMapping? FindNamespaceMapping(string wpfNamespace)
    {
        EnsureLoaded();
        return _database!.NamespaceMappings
            .FirstOrDefault(m => m.WpfNamespace.Equals(wpfNamespace, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public TypeMapping? FindTypeMapping(string wpfTypeName)
    {
        EnsureLoaded();
        return _database!.TypeMappings
            .FirstOrDefault(m => m.WpfTypeName.Equals(wpfTypeName, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public PropertyMapping? FindPropertyMapping(string wpfPropertyName, string? ownerTypeName = null)
    {
        EnsureLoaded();

        // First, try to find a type-specific mapping
        if (!string.IsNullOrEmpty(ownerTypeName))
        {
            var typeSpecificMapping = _database!.PropertyMappings
                .FirstOrDefault(m =>
                    m.WpfPropertyName.Equals(wpfPropertyName, StringComparison.Ordinal) &&
                    m.OwnerTypeName != null &&
                    m.OwnerTypeName.Equals(ownerTypeName, StringComparison.Ordinal));

            if (typeSpecificMapping != null)
            {
                return typeSpecificMapping;
            }
        }

        // Fall back to a general mapping (no specific owner type)
        return _database!.PropertyMappings
            .FirstOrDefault(m =>
                m.WpfPropertyName.Equals(wpfPropertyName, StringComparison.Ordinal) &&
                m.OwnerTypeName == null);
    }

    /// <inheritdoc />
    public EventMapping? FindEventMapping(string wpfEventName, string? ownerTypeName = null)
    {
        EnsureLoaded();

        // First, try to find a type-specific mapping
        if (!string.IsNullOrEmpty(ownerTypeName))
        {
            var typeSpecificMapping = _database!.EventMappings
                .FirstOrDefault(m =>
                    m.WpfEventName.Equals(wpfEventName, StringComparison.Ordinal) &&
                    m.OwnerTypeName != null &&
                    m.OwnerTypeName.Equals(ownerTypeName, StringComparison.Ordinal));

            if (typeSpecificMapping != null)
            {
                return typeSpecificMapping;
            }
        }

        // Fall back to a general mapping (no specific owner type)
        return _database!.EventMappings
            .FirstOrDefault(m =>
                m.WpfEventName.Equals(wpfEventName, StringComparison.Ordinal) &&
                m.OwnerTypeName == null);
    }

    /// <inheritdoc />
    public IReadOnlyList<NamespaceMapping> GetAllNamespaceMappings()
    {
        EnsureLoaded();
        return _database!.NamespaceMappings.AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<TypeMapping> GetAllTypeMappings()
    {
        EnsureLoaded();
        return _database!.TypeMappings.AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<PropertyMapping> GetAllPropertyMappings()
    {
        EnsureLoaded();
        return _database!.PropertyMappings.AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<EventMapping> GetAllEventMappings()
    {
        EnsureLoaded();
        return _database!.EventMappings.AsReadOnly();
    }

    private void EnsureLoaded()
    {
        if (_database == null)
        {
            throw new InvalidOperationException("Mapping database has not been loaded. Call LoadAsync() first.");
        }
    }
}
