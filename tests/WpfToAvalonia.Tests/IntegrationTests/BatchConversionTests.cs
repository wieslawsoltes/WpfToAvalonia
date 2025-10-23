using FluentAssertions;
using WpfToAvalonia.XamlParser;

namespace WpfToAvalonia.Tests.IntegrationTests;

/// <summary>
/// Integration tests for batch conversion functionality.
/// </summary>
public class BatchConversionTests : IDisposable
{
    private readonly string _tempDirectory;

    public BatchConversionTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"WpfToAvaloniaTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Normalizes XAML by removing extra whitespace and newlines for comparison.
    /// </summary>
    private static string NormalizeXaml(string xaml)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            xaml.Trim(),
            @"\s+",
            " ")
            .Replace("> <", "><")
            .Replace(" >", ">")
            .Replace(" =", "=");
    }

    [Fact]
    public void ConvertFile_CreatesOutputFile()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var inputPath = Path.Combine(_tempDirectory, "input.xaml");
        var outputPath = Path.Combine(_tempDirectory, "output.xaml");

        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""Test"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button Content=""Test"" />
</Window>";

        File.WriteAllText(inputPath, wpfXaml);

        // Act
        var result = converter.ConvertFile(inputPath, outputPath);

        // Assert
        result.Success.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();

        var outputContent = File.ReadAllText(outputPath);
        NormalizeXaml(outputContent).Should().Be(NormalizeXaml(expectedXaml));
    }

    [Fact]
    public void ConvertFile_WithDefaultOutputPath_CreatesAvaloniaFile()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var inputPath = Path.Combine(_tempDirectory, "MainWindow.xaml");

        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""Test"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button Content=""Test"" />
</Window>";

        File.WriteAllText(inputPath, wpfXaml);

        // Act
        var result = converter.ConvertFile(inputPath);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputPath.Should().Be(Path.Combine(_tempDirectory, "MainWindow.avalonia.xaml"));
        File.Exists(result.OutputPath).Should().BeTrue();

        var outputContent = File.ReadAllText(result.OutputPath);
        NormalizeXaml(outputContent).Should().Be(NormalizeXaml(expectedXaml));
    }

    [Fact]
    public void ConvertBatch_MultipleFiles_ConvertsAll()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();

        var file1 = Path.Combine(_tempDirectory, "Window1.xaml");
        var file2 = Path.Combine(_tempDirectory, "Window2.xaml");
        var file3 = Path.Combine(_tempDirectory, "UserControl1.xaml");

        var wpfXaml1 = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""Window 1"" />
</Window>";

        var wpfXaml2 = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock Text=""Window 2"" />
</Window>";

        var wpfXaml3 = @"<UserControl xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <StackPanel />
</UserControl>";

        File.WriteAllText(file1, wpfXaml1);
        File.WriteAllText(file2, wpfXaml2);
        File.WriteAllText(file3, wpfXaml3);

        var inputPaths = new[] { file1, file2, file3 };
        var outputDirectory = Path.Combine(_tempDirectory, "output");

        var expectedXaml1 = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button Content=""Window 1"" />
</Window>";

        var expectedXaml2 = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <TextBlock Text=""Window 2"" />
</Window>";

        var expectedXaml3 = @"<UserControl xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <StackPanel />
</UserControl>";

        // Act
        var result = converter.ConvertBatch(inputPaths, outputDirectory);

        // Assert
        result.TotalCount.Should().Be(3);
        result.SuccessCount.Should().Be(3);
        result.FailureCount.Should().Be(0);

        var output1 = Path.Combine(outputDirectory, "Window1.xaml");
        var output2 = Path.Combine(outputDirectory, "Window2.xaml");
        var output3 = Path.Combine(outputDirectory, "UserControl1.xaml");

        File.Exists(output1).Should().BeTrue();
        File.Exists(output2).Should().BeTrue();
        File.Exists(output3).Should().BeTrue();

        NormalizeXaml(File.ReadAllText(output1)).Should().Be(NormalizeXaml(expectedXaml1));
        NormalizeXaml(File.ReadAllText(output2)).Should().Be(NormalizeXaml(expectedXaml2));
        NormalizeXaml(File.ReadAllText(output3)).Should().Be(NormalizeXaml(expectedXaml3));
    }

    [Fact]
    public void ConvertBatch_WithSomeFailures_ReportsCorrectCounts()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();

        var validFile = Path.Combine(_tempDirectory, "Valid.xaml");
        var invalidFile = Path.Combine(_tempDirectory, "Invalid.xaml");

        File.WriteAllText(validFile, @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""Valid"" />
</Window>");

        File.WriteAllText(invalidFile, @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button> <!-- Missing closing tag
</Window>");

        var inputPaths = new[] { validFile, invalidFile };

        // Act
        var result = converter.ConvertBatch(inputPaths);

        // Assert
        result.TotalCount.Should().Be(2);
        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
    }

    [Fact]
    public void ConvertFile_CreatesOutputDirectory_IfNotExists()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var inputPath = Path.Combine(_tempDirectory, "input.xaml");
        var outputPath = Path.Combine(_tempDirectory, "nested", "output", "result.xaml");

        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""Test"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button Content=""Test"" />
</Window>";

        File.WriteAllText(inputPath, wpfXaml);

        // Act
        var result = converter.ConvertFile(inputPath, outputPath);

        // Assert
        result.Success.Should().BeTrue();
        File.Exists(outputPath).Should().BeTrue();
        Directory.Exists(Path.Combine(_tempDirectory, "nested", "output")).Should().BeTrue();

        var outputContent = File.ReadAllText(outputPath);
        NormalizeXaml(outputContent).Should().Be(NormalizeXaml(expectedXaml));
    }

    [Fact]
    public void ConvertFile_WithOptions_AppliesOptions()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var inputPath = Path.Combine(_tempDirectory, "input.xaml");
        var outputPath = Path.Combine(_tempDirectory, "output.xaml");

        var wpfXaml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""Test"" />
</Window>";

        File.WriteAllText(inputPath, wpfXaml);

        var options = new ConversionOptions
        {
            IncludeXmlDeclaration = true,
            UseSelfClosingTags = true,
            PreserveFormatting = false
        };

        // Act
        var result = converter.ConvertFile(inputPath, outputPath, options);

        // Assert
        result.Success.Should().BeTrue();

        var outputContent = File.ReadAllText(outputPath);
        outputContent.Should().StartWith("<?xml version=\"1.0\"");
    }

    [Fact]
    public void ConvertBatch_EmptyList_ReturnsEmptyResult()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var inputPaths = Array.Empty<string>();

        // Act
        var result = converter.ConvertBatch(inputPaths);

        // Assert
        result.TotalCount.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(0);
    }

    [Fact]
    public void ConvertFile_OverwritesExistingFile()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var inputPath = Path.Combine(_tempDirectory, "input.xaml");
        var outputPath = Path.Combine(_tempDirectory, "output.xaml");

        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""New Content"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button Content=""New Content"" />
</Window>";

        File.WriteAllText(inputPath, wpfXaml);
        File.WriteAllText(outputPath, "Old content");

        // Act
        var result = converter.ConvertFile(inputPath, outputPath);

        // Assert
        result.Success.Should().BeTrue();

        var outputContent = File.ReadAllText(outputPath);
        outputContent.Should().NotContain("Old content");
        NormalizeXaml(outputContent).Should().Be(NormalizeXaml(expectedXaml));
    }
}
