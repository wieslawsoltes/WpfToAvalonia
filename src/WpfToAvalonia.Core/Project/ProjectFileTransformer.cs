using Microsoft.Build.Construction;
using WpfToAvalonia.Core.Diagnostics;

namespace WpfToAvalonia.Core.Project;

/// <summary>
/// Transforms WPF project files to Avalonia project files.
/// </summary>
public sealed class ProjectFileTransformer
{
    private readonly DiagnosticCollector _diagnostics;

    public ProjectFileTransformer(DiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    /// <summary>
    /// Transforms a WPF project to an Avalonia project.
    /// </summary>
    public TransformedProjectInfo Transform(ProjectInfo projectInfo, WpfProjectAnalysis analysis, ProjectTransformationOptions? options = null)
    {
        options ??= new ProjectTransformationOptions();

        var projectRoot = projectInfo.ProjectRoot;

        var result = new TransformedProjectInfo
        {
            OriginalProjectPath = projectInfo.FilePath,
            TransformedProjectPath = GetAvaloniaProjectPath(projectInfo.FilePath, options),
            TransformedProject = projectRoot
        };

        // Transform project properties
        TransformProjectProperties(projectRoot, analysis, options);

        // Transform package references
        TransformPackageReferences(projectRoot, analysis, options);

        // Transform XAML file items
        TransformXamlItems(projectRoot, analysis, options);

        // Add Avalonia-specific properties
        AddAvaloniaProperties(projectRoot, analysis, options);

        // Preserve custom properties and items
        // (already preserved by working with ProjectRootElement)

        return result;
    }

    /// <summary>
    /// Saves the transformed project to a file.
    /// </summary>
    public void SaveTransformedProject(TransformedProjectInfo transformedInfo)
    {
        var projectRoot = transformedInfo.TransformedProject;
        projectRoot.Save(transformedInfo.TransformedProjectPath);

        _diagnostics.AddInfo(
            "PROJECT_SAVED",
            $"Transformed project saved to: {transformedInfo.TransformedProjectPath}",
            transformedInfo.TransformedProjectPath);
    }

    private void TransformProjectProperties(ProjectRootElement projectRoot, WpfProjectAnalysis analysis, ProjectTransformationOptions options)
    {
        // Remove WPF-specific properties
        foreach (var propertyGroup in projectRoot.PropertyGroups.ToList())
        {
            // Remove UseWPF property
            var useWpfProperty = propertyGroup.Properties.FirstOrDefault(p => p.Name == "UseWPF");
            if (useWpfProperty != null)
            {
                propertyGroup.RemoveChild(useWpfProperty);
                _diagnostics.AddInfo("PROPERTY_REMOVED", "Removed UseWPF property", analysis.ProjectFilePath);
            }

            // Remove WPF project type GUIDs
            var projectTypeGuidsProperty = propertyGroup.Properties.FirstOrDefault(p => p.Name == "ProjectTypeGuids");
            if (projectTypeGuidsProperty != null)
            {
                propertyGroup.RemoveChild(projectTypeGuidsProperty);
                _diagnostics.AddInfo("PROPERTY_REMOVED", "Removed ProjectTypeGuids property", analysis.ProjectFilePath);
            }
        }

        // Update TargetFramework if needed
        if (options.UpdateTargetFramework && !string.IsNullOrEmpty(options.TargetFramework))
        {
            var propertyGroup = projectRoot.PropertyGroups.FirstOrDefault();
            if (propertyGroup != null)
            {
                var targetFrameworkProperty = propertyGroup.Properties.FirstOrDefault(p => p.Name == "TargetFramework");
                if (targetFrameworkProperty != null)
                {
                    var oldValue = targetFrameworkProperty.Value;
                    targetFrameworkProperty.Value = options.TargetFramework;
                    _diagnostics.AddInfo("PROPERTY_UPDATED", $"Updated TargetFramework from {oldValue} to {options.TargetFramework}", analysis.ProjectFilePath);
                }
            }
        }
    }

    private void TransformPackageReferences(ProjectRootElement projectRoot, WpfProjectAnalysis analysis, ProjectTransformationOptions options)
    {
        // Find or create ItemGroup for package references
        var packageReferenceGroup = projectRoot.ItemGroups
            .FirstOrDefault(ig => ig.Items.Any(i => i.ItemType == "PackageReference"));

        if (packageReferenceGroup == null)
        {
            packageReferenceGroup = projectRoot.AddItemGroup();
        }

        // Remove WPF package references
        foreach (var itemGroup in projectRoot.ItemGroups.ToList())
        {
            foreach (var item in itemGroup.Items.ToList())
            {
                if (item.ItemType == "PackageReference" &&
                    item.Include.Contains("WPF", StringComparison.OrdinalIgnoreCase))
                {
                    itemGroup.RemoveChild(item);
                    _diagnostics.AddInfo("PACKAGE_REMOVED", $"Removed WPF package reference: {item.Include}", analysis.ProjectFilePath);
                }
            }
        }

        // Add Avalonia package references
        AddAvaloniaPackageReference(packageReferenceGroup, "Avalonia", options.AvaloniaVersion);
        AddAvaloniaPackageReference(packageReferenceGroup, "Avalonia.Desktop", options.AvaloniaVersion);
        AddAvaloniaPackageReference(packageReferenceGroup, "Avalonia.Themes.Fluent", options.AvaloniaVersion);

        _diagnostics.AddInfo("PACKAGES_ADDED", $"Added Avalonia {options.AvaloniaVersion} package references", analysis.ProjectFilePath);
    }

    private void AddAvaloniaPackageReference(ProjectItemGroupElement itemGroup, string packageName, string version)
    {
        // Check if package already exists
        var existingPackage = itemGroup.Items.FirstOrDefault(i =>
            i.ItemType == "PackageReference" && i.Include == packageName);

        if (existingPackage == null)
        {
            var packageItem = itemGroup.AddItem("PackageReference", packageName);
            packageItem.AddMetadata("Version", version);
        }
    }

    private void TransformXamlItems(ProjectRootElement projectRoot, WpfProjectAnalysis analysis, ProjectTransformationOptions options)
    {
        if (!options.RenameXamlToAxaml)
        {
            return;
        }

        // Transform Page items (WPF XAML) to AvaloniaResource (Avalonia XAML)
        foreach (var itemGroup in projectRoot.ItemGroups)
        {
            foreach (var item in itemGroup.Items.ToList())
            {
                if (item.ItemType == "Page")
                {
                    var include = item.Include;

                    // Change .xaml to .axaml
                    if (include.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                    {
                        var newInclude = Path.ChangeExtension(include, ".axaml");
                        item.Include = newInclude;

                        _diagnostics.AddWarning("XAML_RENAME",
                            $"Update XAML file reference: {include} → {newInclude} (file must be renamed manually)",
                            analysis.ProjectFilePath);
                    }

                    // Change ItemType from Page to AvaloniaResource
                    item.ItemType = "AvaloniaResource";
                }

                // Transform ApplicationDefinition
                if (item.ItemType == "ApplicationDefinition")
                {
                    var include = item.Include;

                    if (include.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                    {
                        var newInclude = Path.ChangeExtension(include, ".axaml");
                        item.Include = newInclude;

                        _diagnostics.AddWarning("XAML_RENAME",
                            $"Update ApplicationDefinition file reference: {include} → {newInclude} (file must be renamed manually)",
                            analysis.ProjectFilePath);
                    }

                    // ApplicationDefinition becomes AvaloniaResource for Avalonia
                    item.ItemType = "AvaloniaResource";
                }
            }
        }
    }

    private void AddAvaloniaProperties(ProjectRootElement projectRoot, WpfProjectAnalysis analysis, ProjectTransformationOptions options)
    {
        var propertyGroup = projectRoot.PropertyGroups.FirstOrDefault();
        if (propertyGroup == null)
        {
            propertyGroup = projectRoot.AddPropertyGroup();
        }

        // Add BuiltInAvaloniaCompositor property if not present
        if (propertyGroup.Properties.All(p => p.Name != "BuiltInAvaloniaCompositor"))
        {
            propertyGroup.AddProperty("BuiltInAvaloniaCompositor", "managed");
            _diagnostics.AddInfo("PROPERTY_ADDED", "Added BuiltInAvaloniaCompositor property", analysis.ProjectFilePath);
        }

        // Add AvaloniaUseCompiledBindingsByDefault if option is set
        if (options.EnableCompiledBindings)
        {
            if (propertyGroup.Properties.All(p => p.Name != "AvaloniaUseCompiledBindingsByDefault"))
            {
                propertyGroup.AddProperty("AvaloniaUseCompiledBindingsByDefault", "true");
                _diagnostics.AddInfo("PROPERTY_ADDED", "Added AvaloniaUseCompiledBindingsByDefault property", analysis.ProjectFilePath);
            }
        }
    }

    private string GetAvaloniaProjectPath(string wpfProjectPath, ProjectTransformationOptions options)
    {
        if (!string.IsNullOrEmpty(options.OutputProjectPath))
        {
            return options.OutputProjectPath;
        }

        // Default: create in same directory with .Avalonia suffix
        var directory = Path.GetDirectoryName(wpfProjectPath) ?? "";
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(wpfProjectPath);
        var extension = Path.GetExtension(wpfProjectPath);

        return Path.Combine(directory, $"{fileNameWithoutExtension}.Avalonia{extension}");
    }
}

/// <summary>
/// Options for project file transformation.
/// </summary>
public sealed class ProjectTransformationOptions
{
    /// <summary>
    /// Avalonia version to use for package references.
    /// </summary>
    public string AvaloniaVersion { get; set; } = "11.2.2";

    /// <summary>
    /// Whether to update the TargetFramework property.
    /// </summary>
    public bool UpdateTargetFramework { get; set; } = false;

    /// <summary>
    /// Target framework to use (e.g., net8.0).
    /// </summary>
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Whether to rename XAML files from .xaml to .axaml.
    /// </summary>
    public bool RenameXamlToAxaml { get; set; } = true;

    /// <summary>
    /// Output path for the transformed project file.
    /// If null, creates a new file with .Avalonia suffix in the same directory.
    /// </summary>
    public string? OutputProjectPath { get; set; }

    /// <summary>
    /// Whether to enable compiled bindings by default.
    /// </summary>
    public bool EnableCompiledBindings { get; set; } = true;
}

/// <summary>
/// Contains information about a transformed project.
/// </summary>
public sealed class TransformedProjectInfo
{
    public required string OriginalProjectPath { get; init; }
    public required string TransformedProjectPath { get; init; }
    public required ProjectRootElement TransformedProject { get; set; }
}
