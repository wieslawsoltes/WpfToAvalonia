using System.Text.Json.Serialization;

namespace WpfToAvalonia.CLI.Configuration;

/// <summary>
/// Configuration for WPF to Avalonia migration.
/// Can be loaded from a JSON file or created programmatically.
/// </summary>
public class MigrationConfig
{
    /// <summary>
    /// Input file or directory path.
    /// </summary>
    [JsonPropertyName("input")]
    public string? Input { get; set; }

    /// <summary>
    /// Output directory path.
    /// </summary>
    [JsonPropertyName("output")]
    public string? Output { get; set; }

    /// <summary>
    /// XAML file pattern to match (e.g., "*.xaml").
    /// </summary>
    [JsonPropertyName("xamlPattern")]
    public string XamlPattern { get; set; } = "*.xaml";

    /// <summary>
    /// C# file pattern to match (e.g., "*.cs").
    /// </summary>
    [JsonPropertyName("csharpPattern")]
    public string CSharpPattern { get; set; } = "*.cs";

    /// <summary>
    /// Search directories recursively.
    /// </summary>
    [JsonPropertyName("recursive")]
    public bool Recursive { get; set; } = true;

    /// <summary>
    /// Perform a dry run without writing files.
    /// </summary>
    [JsonPropertyName("dryRun")]
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// Enable verbose output.
    /// </summary>
    [JsonPropertyName("verbose")]
    public bool Verbose { get; set; } = false;

    /// <summary>
    /// Directory patterns to exclude from search.
    /// </summary>
    [JsonPropertyName("exclude")]
    public string[] Exclude { get; set; } = new[] { "obj", "bin", ".vs", ".git", "packages" };

    /// <summary>
    /// Skip C# transformation (XAML only).
    /// </summary>
    [JsonPropertyName("skipCSharp")]
    public bool SkipCSharp { get; set; } = false;

    /// <summary>
    /// Skip XAML transformation (C# only).
    /// </summary>
    [JsonPropertyName("skipXaml")]
    public bool SkipXaml { get; set; } = false;

    /// <summary>
    /// Include diagnostic comments in transformed XAML files.
    /// </summary>
    [JsonPropertyName("includeDiagnosticComments")]
    public bool IncludeDiagnosticComments { get; set; } = false;

    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    public static MigrationConfig CreateDefault()
    {
        return new MigrationConfig();
    }

    /// <summary>
    /// Creates a configuration for XAML-only transformation.
    /// </summary>
    public static MigrationConfig CreateXamlOnly()
    {
        return new MigrationConfig
        {
            SkipCSharp = true,
            XamlPattern = "*.xaml"
        };
    }

    /// <summary>
    /// Creates a configuration for C#-only transformation.
    /// </summary>
    public static MigrationConfig CreateCSharpOnly()
    {
        return new MigrationConfig
        {
            SkipXaml = true,
            CSharpPattern = "*.cs"
        };
    }

    /// <summary>
    /// Creates a configuration for incremental migration (verbose + dry-run).
    /// </summary>
    public static MigrationConfig CreateIncremental()
    {
        return new MigrationConfig
        {
            Verbose = true,
            DryRun = true
        };
    }
}
