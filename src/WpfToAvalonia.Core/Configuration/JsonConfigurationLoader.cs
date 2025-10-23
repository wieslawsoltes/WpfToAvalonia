using System.Text.Json;
using WpfToAvalonia.Core.Pipeline;

namespace WpfToAvalonia.Core.Configuration;

/// <summary>
/// JSON-based implementation of the configuration loader.
/// </summary>
public sealed class JsonConfigurationLoader : IConfigurationLoader
{
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonConfigurationLoader"/> class.
    /// </summary>
    public JsonConfigurationLoader()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
    }

    /// <inheritdoc />
    public async Task<TransformationConfiguration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Configuration file not found: {filePath}");

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return LoadFromJson(json);
    }

    /// <inheritdoc />
    public TransformationConfiguration LoadFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            throw new ArgumentNullException(nameof(json));

        var configuration = JsonSerializer.Deserialize<TransformationConfiguration>(json, _jsonOptions);
        if (configuration == null)
            throw new InvalidOperationException("Failed to deserialize configuration.");

        return configuration;
    }

    /// <inheritdoc />
    public async Task SaveToFileAsync(TransformationConfiguration configuration, string filePath, CancellationToken cancellationToken = default)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath));

        var json = JsonSerializer.Serialize(configuration, _jsonOptions);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    /// <inheritdoc />
    public TransformationConfiguration CreateDefault()
    {
        return new TransformationConfiguration
        {
            DryRun = false,
            CreateBackups = true,
            BackupDirectory = ".migration-backup",
            Strategy = TransformationStrategy.Conservative,
            EnableParallelProcessing = true,
            MaxDegreeOfParallelism = 0,
            IncludePatterns = new List<string> { "**/*.cs", "**/*.xaml", "**/*.axaml" },
            ExcludePatterns = new List<string> { "**/obj/**", "**/bin/**", "**/.migration-backup/**" },
            PreserveTrivia = true,
            PreserveFormatting = true,
            CustomMappingFiles = new List<string>(),
            RenameXamlToAxaml = true,
            Verbosity = VerbosityLevel.Normal
        };
    }
}
