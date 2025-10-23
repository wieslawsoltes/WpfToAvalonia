using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;

namespace WpfToAvalonia.Core.Project;

/// <summary>
/// Parses and analyzes MSBuild project files (.csproj).
/// </summary>
public sealed class ProjectFileParser
{
    private static bool _msbuildLocatorRegistered;
    private static readonly object _lockObject = new();

    /// <summary>
    /// Ensures MSBuild is located and registered.
    /// </summary>
    public static void EnsureMSBuildRegistered()
    {
        lock (_lockObject)
        {
            if (!_msbuildLocatorRegistered)
            {
                MSBuildLocator.RegisterDefaults();
                _msbuildLocatorRegistered = true;
            }
        }
    }

    /// <summary>
    /// Loads a project file for analysis.
    /// </summary>
    /// <param name="projectFilePath">Path to the .csproj file.</param>
    /// <returns>Project information.</returns>
    public ProjectInfo LoadProject(string projectFilePath)
    {
        if (!File.Exists(projectFilePath))
        {
            throw new FileNotFoundException($"Project file not found: {projectFilePath}");
        }

        EnsureMSBuildRegistered();

        // Load the project using ProjectRootElement (unevaluated)
        var projectRoot = ProjectRootElement.Open(projectFilePath);

        // Also load evaluated project for property resolution
        var project = new Microsoft.Build.Evaluation.Project(projectFilePath);

        return new ProjectInfo
        {
            FilePath = projectFilePath,
            ProjectRoot = projectRoot,
            Project = project
        };
    }

    /// <summary>
    /// Determines if a project is a WPF project.
    /// </summary>
    public bool IsWpfProject(ProjectInfo projectInfo)
    {
        var project = projectInfo.Project;

        // Check for WPF-specific properties
        var useWpf = project.GetPropertyValue("UseWPF");
        if (bool.TryParse(useWpf, out var useWpfBool) && useWpfBool)
        {
            return true;
        }

        // Check for legacy WPF project type GUIDs
        var projectTypeGuids = project.GetPropertyValue("ProjectTypeGuids");
        if (!string.IsNullOrEmpty(projectTypeGuids))
        {
            // {60dc8134-eba5-43b8-bcc9-bb4bc16c2548} is WPF project type GUID
            if (projectTypeGuids.Contains("60dc8134-eba5-43b8-bcc9-bb4bc16c2548", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Check for WPF-specific references
        foreach (var item in project.GetItems("Reference"))
        {
            var include = item.EvaluatedInclude;
            if (include.StartsWith("PresentationCore") ||
                include.StartsWith("PresentationFramework") ||
                include.StartsWith("WindowsBase"))
            {
                return true;
            }
        }

        // Check for WPF-specific package references
        foreach (var item in project.GetItems("PackageReference"))
        {
            var include = item.EvaluatedInclude;
            if (include.Contains("WPF", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Analyzes a WPF project and extracts relevant information.
    /// </summary>
    public WpfProjectAnalysis AnalyzeWpfProject(ProjectInfo projectInfo)
    {
        var analysis = new WpfProjectAnalysis
        {
            ProjectFilePath = projectInfo.FilePath
        };

        var project = projectInfo.Project;

        // Extract basic properties
        analysis.TargetFramework = project.GetPropertyValue("TargetFramework");
        analysis.TargetFrameworks = project.GetPropertyValue("TargetFrameworks");
        analysis.OutputType = project.GetPropertyValue("OutputType");
        analysis.RootNamespace = project.GetPropertyValue("RootNamespace");
        analysis.AssemblyName = project.GetPropertyValue("AssemblyName");
        analysis.UseWpf = bool.TryParse(project.GetPropertyValue("UseWPF"), out var useWpf) && useWpf;

        // Find WPF-specific references
        foreach (var item in project.GetItems("Reference"))
        {
            var include = item.EvaluatedInclude;
            if (IsWpfReference(include))
            {
                analysis.WpfReferences.Add(include);
            }
        }

        // Find WPF-specific package references
        foreach (var item in project.GetItems("PackageReference"))
        {
            var include = item.EvaluatedInclude;
            if (include.Contains("WPF", StringComparison.OrdinalIgnoreCase))
            {
                var version = item.GetMetadataValue("Version");
                analysis.WpfPackages.Add(new PackageReferenceInfo
                {
                    Name = include,
                    Version = version
                });
            }
        }

        // Find XAML files
        foreach (var item in project.GetItems("Page"))
        {
            analysis.XamlFiles.Add(item.EvaluatedInclude);
        }

        foreach (var item in project.GetItems("ApplicationDefinition"))
        {
            analysis.ApplicationDefinitionFile = item.EvaluatedInclude;
        }

        return analysis;
    }

    private bool IsWpfReference(string reference)
    {
        return reference.StartsWith("PresentationCore") ||
               reference.StartsWith("PresentationFramework") ||
               reference.StartsWith("WindowsBase") ||
               reference.StartsWith("System.Windows") ||
               reference.StartsWith("System.Xaml") ||
               reference.StartsWith("UIAutomationProvider") ||
               reference.StartsWith("UIAutomationTypes");
    }
}

/// <summary>
/// Contains information about a loaded project.
/// </summary>
public sealed class ProjectInfo
{
    /// <summary>
    /// Path to the project file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Unevaluated project root element.
    /// </summary>
    public required ProjectRootElement ProjectRoot { get; init; }

    /// <summary>
    /// Evaluated project.
    /// </summary>
    public required Microsoft.Build.Evaluation.Project Project { get; init; }
}

/// <summary>
/// Analysis results for a WPF project.
/// </summary>
public sealed class WpfProjectAnalysis
{
    public string ProjectFilePath { get; init; } = string.Empty;
    public string? TargetFramework { get; set; }
    public string? TargetFrameworks { get; set; }
    public string? OutputType { get; set; }
    public string? RootNamespace { get; set; }
    public string? AssemblyName { get; set; }
    public bool UseWpf { get; set; }
    public List<string> WpfReferences { get; init; } = new();
    public List<PackageReferenceInfo> WpfPackages { get; init; } = new();
    public List<string> XamlFiles { get; init; } = new();
    public string? ApplicationDefinitionFile { get; set; }
}

/// <summary>
/// Information about a NuGet package reference.
/// </summary>
public sealed class PackageReferenceInfo
{
    public required string Name { get; init; }
    public string? Version { get; init; }
}
