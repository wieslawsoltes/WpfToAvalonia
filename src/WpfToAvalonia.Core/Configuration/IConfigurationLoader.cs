using WpfToAvalonia.Core.Pipeline;

namespace WpfToAvalonia.Core.Configuration;

/// <summary>
/// Defines operations for loading transformation configuration.
/// </summary>
public interface IConfigurationLoader
{
    /// <summary>
    /// Loads configuration from a file.
    /// </summary>
    /// <param name="filePath">The path to the configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded configuration.</returns>
    Task<TransformationConfiguration> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads configuration from JSON string.
    /// </summary>
    /// <param name="json">The JSON configuration string.</param>
    /// <returns>The loaded configuration.</returns>
    TransformationConfiguration LoadFromJson(string json);

    /// <summary>
    /// Saves configuration to a file.
    /// </summary>
    /// <param name="configuration">The configuration to save.</param>
    /// <param name="filePath">The path to save the configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveToFileAsync(TransformationConfiguration configuration, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    /// <returns>A default transformation configuration.</returns>
    TransformationConfiguration CreateDefault();
}
