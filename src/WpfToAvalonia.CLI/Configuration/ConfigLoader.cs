using System.Text.Json;

namespace WpfToAvalonia.CLI.Configuration;

/// <summary>
/// Loads and saves migration configuration files.
/// </summary>
public static class ConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Loads configuration from a JSON file.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <returns>The loaded configuration.</returns>
    /// <exception cref="FileNotFoundException">If the configuration file doesn't exist.</exception>
    /// <exception cref="JsonException">If the configuration file is invalid.</exception>
    public static async Task<MigrationConfig> LoadAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        var json = await File.ReadAllTextAsync(filePath);
        var config = JsonSerializer.Deserialize<MigrationConfig>(json, JsonOptions);

        if (config == null)
        {
            throw new JsonException("Failed to deserialize configuration file.");
        }

        return config;
    }

    /// <summary>
    /// Loads configuration from a JSON file synchronously.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <returns>The loaded configuration.</returns>
    public static MigrationConfig Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<MigrationConfig>(json, JsonOptions);

        if (config == null)
        {
            throw new JsonException("Failed to deserialize configuration file.");
        }

        return config;
    }

    /// <summary>
    /// Saves configuration to a JSON file.
    /// </summary>
    /// <param name="config">The configuration to save.</param>
    /// <param name="filePath">Path to save the configuration file.</param>
    public static async Task SaveAsync(MigrationConfig config, string filePath)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Saves configuration to a JSON file synchronously.
    /// </summary>
    /// <param name="config">The configuration to save.</param>
    /// <param name="filePath">Path to save the configuration file.</param>
    public static void Save(MigrationConfig config, string filePath)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Tries to load configuration from a file, returns default if it doesn't exist.
    /// </summary>
    /// <param name="filePath">Path to the configuration file.</param>
    /// <returns>The loaded configuration or default if file doesn't exist.</returns>
    public static async Task<MigrationConfig> TryLoadOrDefaultAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return MigrationConfig.CreateDefault();
        }

        try
        {
            return await LoadAsync(filePath);
        }
        catch
        {
            return MigrationConfig.CreateDefault();
        }
    }

    /// <summary>
    /// Looks for a configuration file in common locations.
    /// </summary>
    /// <param name="startDirectory">The directory to start searching from.</param>
    /// <returns>The path to the configuration file, or null if not found.</returns>
    public static string? FindConfigFile(string? startDirectory = null)
    {
        var searchDir = startDirectory ?? Directory.GetCurrentDirectory();

        // Common configuration file names
        var configNames = new[]
        {
            "wpf2avalonia.json",
            ".wpf2avalonia.json",
            "wpf-to-avalonia.json",
            "migration.config.json"
        };

        // Search current directory and parent directories
        var currentDir = new DirectoryInfo(searchDir);
        while (currentDir != null)
        {
            foreach (var configName in configNames)
            {
                var configPath = Path.Combine(currentDir.FullName, configName);
                if (File.Exists(configPath))
                {
                    return configPath;
                }
            }

            currentDir = currentDir.Parent;
        }

        return null;
    }
}
