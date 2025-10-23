using FluentAssertions;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Project;
using Microsoft.Build.Construction;

namespace WpfToAvalonia.Tests.UnitTests;

/// <summary>
/// Tests for project file parsing and transformation.
/// </summary>
public class ProjectFileTransformationTests : IClassFixture<MSBuildTestFixture>, IDisposable
{
    private readonly List<string> _tempFiles = new();

    public ProjectFileTransformationTests(MSBuildTestFixture fixture)
    {
        // Fixture ensures MSBuild is registered
    }

    public void Dispose()
    {
        // Clean up temporary files
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    private string CreateTempProjectFile(string content)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csproj");
        File.WriteAllText(tempFile, content);
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    [Fact]
    public void LoadProject_ValidWpfProject_LoadsSuccessfully()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
</Project>";

        var projectPath = CreateTempProjectFile(projectContent);
        var parser = new ProjectFileParser();

        // Act
        var projectInfo = parser.LoadProject(projectPath);

        // Assert
        projectInfo.Should().NotBeNull();
        projectInfo.FilePath.Should().Be(projectPath);
        projectInfo.ProjectRoot.Should().NotBeNull();
        projectInfo.Project.Should().NotBeNull();
    }

    [Fact]
    public void IsWpfProject_ProjectWithUseWPF_ReturnsTrue()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
</Project>";

        var projectPath = CreateTempProjectFile(projectContent);
        var parser = new ProjectFileParser();
        var projectInfo = parser.LoadProject(projectPath);

        // Act
        var isWpf = parser.IsWpfProject(projectInfo);

        // Assert
        isWpf.Should().BeTrue();
    }

    [Fact]
    public void IsWpfProject_ProjectWithoutWpf_ReturnsFalse()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";

        var projectPath = CreateTempProjectFile(projectContent);
        var parser = new ProjectFileParser();
        var projectInfo = parser.LoadProject(projectPath);

        // Act
        var isWpf = parser.IsWpfProject(projectInfo);

        // Assert
        isWpf.Should().BeFalse();
    }

    [Fact]
    public void AnalyzeWpfProject_ExtractsBasicProperties()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <RootNamespace>MyWpfApp</RootNamespace>
    <AssemblyName>MyWpfApp</AssemblyName>
  </PropertyGroup>
</Project>";

        var projectPath = CreateTempProjectFile(projectContent);
        var parser = new ProjectFileParser();
        var projectInfo = parser.LoadProject(projectPath);

        // Act
        var analysis = parser.AnalyzeWpfProject(projectInfo);

        // Assert
        analysis.Should().NotBeNull();
        analysis.TargetFramework.Should().Be("net8.0-windows");
        analysis.OutputType.Should().Be("WinExe");
        analysis.RootNamespace.Should().Be("MyWpfApp");
        analysis.AssemblyName.Should().Be("MyWpfApp");
        analysis.UseWpf.Should().BeTrue();
    }

    [Fact]
    public void Transform_RemovesUseWpfProperty()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
</Project>";

        var projectPath = CreateTempProjectFile(projectContent);
        var parser = new ProjectFileParser();
        var projectInfo = parser.LoadProject(projectPath);
        var analysis = parser.AnalyzeWpfProject(projectInfo);

        var diagnostics = new DiagnosticCollector();
        var transformer = new ProjectFileTransformer(diagnostics);

        // Act
        var result = transformer.Transform(projectInfo, analysis);

        // Assert
        var transformedProject = result.TransformedProject;
        var useWpfProperty = transformedProject.PropertyGroups
            .SelectMany(pg => pg.Properties)
            .FirstOrDefault(p => p.Name == "UseWPF");

        useWpfProperty.Should().BeNull();
    }

    [Fact]
    public void Transform_AddsAvaloniaPackageReferences()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
</Project>";

        var projectPath = CreateTempProjectFile(projectContent);
        var parser = new ProjectFileParser();
        var projectInfo = parser.LoadProject(projectPath);
        var analysis = parser.AnalyzeWpfProject(projectInfo);

        var diagnostics = new DiagnosticCollector();
        var transformer = new ProjectFileTransformer(diagnostics);

        // Act
        var result = transformer.Transform(projectInfo, analysis);

        // Assert
        var transformedProject = result.TransformedProject;
        var packageReferences = transformedProject.ItemGroups
            .SelectMany(ig => ig.Items)
            .Where(i => i.ItemType == "PackageReference")
            .Select(i => i.Include)
            .ToList();

        packageReferences.Should().Contain("Avalonia");
        packageReferences.Should().Contain("Avalonia.Desktop");
        packageReferences.Should().Contain("Avalonia.Themes.Fluent");
    }

    [Fact]
    public void Transform_AddsAvaloniaProperties()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
</Project>";

        var projectPath = CreateTempProjectFile(projectContent);
        var parser = new ProjectFileParser();
        var projectInfo = parser.LoadProject(projectPath);
        var analysis = parser.AnalyzeWpfProject(projectInfo);

        var diagnostics = new DiagnosticCollector();
        var transformer = new ProjectFileTransformer(diagnostics);

        // Act
        var result = transformer.Transform(projectInfo, analysis);

        // Assert
        var transformedProject = result.TransformedProject;
        var properties = transformedProject.PropertyGroups
            .SelectMany(pg => pg.Properties)
            .ToDictionary(p => p.Name, p => p.Value);

        properties.Should().ContainKey("BuiltInAvaloniaCompositor");
        properties["BuiltInAvaloniaCompositor"].Should().Be("managed");

        properties.Should().ContainKey("AvaloniaUseCompiledBindingsByDefault");
        properties["AvaloniaUseCompiledBindingsByDefault"].Should().Be("true");
    }

    [Fact]
    public void Transform_WithXamlFiles_UpdatesFileReferences()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
  <ItemGroup>
    <Page Include=""MainWindow.xaml"" />
    <ApplicationDefinition Include=""App.xaml"" />
  </ItemGroup>
</Project>";

        var projectPath = CreateTempProjectFile(projectContent);
        var parser = new ProjectFileParser();
        var projectInfo = parser.LoadProject(projectPath);
        var analysis = parser.AnalyzeWpfProject(projectInfo);

        var diagnostics = new DiagnosticCollector();
        var transformer = new ProjectFileTransformer(diagnostics);

        // Act
        var result = transformer.Transform(projectInfo, analysis);

        // Assert
        var transformedProject = result.TransformedProject;
        var avaloniaResources = transformedProject.ItemGroups
            .SelectMany(ig => ig.Items)
            .Where(i => i.ItemType == "AvaloniaResource")
            .Select(i => i.Include)
            .ToList();

        avaloniaResources.Should().Contain("MainWindow.axaml");
        avaloniaResources.Should().Contain("App.axaml");
    }

    [Fact]
    public void Transform_WithCustomOutputPath_UsesSpecifiedPath()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
</Project>";

        var projectPath = CreateTempProjectFile(projectContent);
        var parser = new ProjectFileParser();
        var projectInfo = parser.LoadProject(projectPath);
        var analysis = parser.AnalyzeWpfProject(projectInfo);

        var diagnostics = new DiagnosticCollector();
        var transformer = new ProjectFileTransformer(diagnostics);

        var customOutputPath = Path.Combine(Path.GetTempPath(), $"custom_{Guid.NewGuid()}.csproj");
        _tempFiles.Add(customOutputPath);

        var options = new ProjectTransformationOptions
        {
            OutputProjectPath = customOutputPath
        };

        // Act
        var result = transformer.Transform(projectInfo, analysis, options);

        // Assert
        result.TransformedProjectPath.Should().Be(customOutputPath);
    }

    [Fact]
    public void Transform_GeneratesDiagnostics()
    {
        // Arrange
        var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
</Project>";

        var projectPath = CreateTempProjectFile(projectContent);
        var parser = new ProjectFileParser();
        var projectInfo = parser.LoadProject(projectPath);
        var analysis = parser.AnalyzeWpfProject(projectInfo);

        var diagnostics = new DiagnosticCollector();
        var transformer = new ProjectFileTransformer(diagnostics);

        // Act
        var result = transformer.Transform(projectInfo, analysis);

        // Assert
        diagnostics.Diagnostics.Should().NotBeEmpty();
        diagnostics.Diagnostics.Should().Contain(d => d.Code == "PROPERTY_REMOVED");
        diagnostics.Diagnostics.Should().Contain(d => d.Code == "PACKAGES_ADDED");
        diagnostics.Diagnostics.Should().Contain(d => d.Code == "PROPERTY_ADDED");
    }
}
