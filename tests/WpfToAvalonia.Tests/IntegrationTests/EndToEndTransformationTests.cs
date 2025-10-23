using System.Xml.Linq;
using Xunit;
using WpfToAvalonia.XamlParser.UnifiedAst;
using WpfToAvalonia.XamlParser.Transformers;
using WpfToAvalonia.XamlParser.Serialization;
using WpfToAvalonia.Core.Diagnostics;

namespace WpfToAvalonia.Tests.IntegrationTests;

/// <summary>
/// End-to-end tests for the complete WPF to Avalonia transformation flow:
/// Parse → Transform → Serialize
/// </summary>
public class EndToEndTransformationTests
{
    [Fact]
    public void EndToEnd_SimpleWindow_TransformsAndSerializes()
    {
        // Arrange - WPF XAML
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
          xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
          x:Class=""MyApp.MainWindow"">
    <Button Content=""Click Me"" Visibility=""Visible"" />
</Window>";

        // Act - Parse, Transform, Serialize
        var diagnostics = new DiagnosticCollector();
        var xdoc = XDocument.Parse(wpfXaml);
        var document = new UnifiedXamlDocument(xdoc, diagnostics);

        var pipeline = TransformationPipeline.CreateDefault();
        var context = pipeline.Transform(document, diagnostics);

        var serializer = new UnifiedAstSerializer(diagnostics);
        var avaloniaXaml = serializer.SerializeToString(document);

        // Assert - Check Avalonia XAML
        Assert.Contains("https://github.com/avaloniaui", avaloniaXaml);
        Assert.Contains("IsVisible=\"True\"", avaloniaXaml);
        Assert.DoesNotContain("Visibility=\"Visible\"", avaloniaXaml);
        Assert.Contains("x:Class=\"MyApp.MainWindow\"", avaloniaXaml);
        Assert.Contains("<Button", avaloniaXaml);

        // Verify it parses as valid XML
        var avaloniaXdoc = XDocument.Parse(avaloniaXaml);
        Assert.NotNull(avaloniaXdoc.Root);
        Assert.Equal("Window", avaloniaXdoc.Root.Name.LocalName);
    }

    [Fact]
    public void EndToEnd_ListViewToListBox_TransformsCorrectly()
    {
        // Arrange
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ListView x:Name=""myListView"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" />
</Window>";

        // Act
        var diagnostics = new DiagnosticCollector();
        var xdoc = XDocument.Parse(wpfXaml);
        var document = new UnifiedXamlDocument(xdoc, diagnostics);

        var pipeline = TransformationPipeline.CreateDefault();
        pipeline.Transform(document, diagnostics);

        var serializer = new UnifiedAstSerializer(diagnostics);
        var avaloniaXaml = serializer.SerializeToString(document);

        // Assert
        Assert.Contains("<ListBox", avaloniaXaml);
        Assert.DoesNotContain("<ListView", avaloniaXaml);
        Assert.Contains("x:Name=\"myListView\"", avaloniaXaml);

        // Check warning was generated
        var warnings = diagnostics.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);
        Assert.Contains(warnings, w => w.Code == "TYPE_LISTVIEW_TO_LISTBOX");
    }

    [Fact]
    public void EndToEnd_ComplexHierarchy_PreservesStructure()
    {
        // Arrange
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
          xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <StackPanel>
        <TextBlock Text=""Hello"" />
        <Button Content=""Click"" Visibility=""Collapsed"" />
        <TextBox Text=""Enter text"" />
    </StackPanel>
</Window>";

        // Act
        var diagnostics = new DiagnosticCollector();
        var xdoc = XDocument.Parse(wpfXaml);
        var document = new UnifiedXamlDocument(xdoc, diagnostics);

        var pipeline = TransformationPipeline.CreateDefault();
        pipeline.Transform(document, diagnostics);

        var serializer = new UnifiedAstSerializer(diagnostics);
        var avaloniaXaml = serializer.SerializeToString(document);

        // Assert - Check structure is preserved
        var avaloniaXdoc = XDocument.Parse(avaloniaXaml);
        var stackPanel = avaloniaXdoc.Root?.Element(XName.Get("StackPanel", "https://github.com/avaloniaui"));
        Assert.NotNull(stackPanel);

        var children = stackPanel.Elements().ToList();
        Assert.Equal(3, children.Count);
        Assert.Equal("TextBlock", children[0].Name.LocalName);
        Assert.Equal("Button", children[1].Name.LocalName);
        Assert.Equal("TextBox", children[2].Name.LocalName);

        // Check transformations were applied
        Assert.Contains("IsVisible=\"False\"", avaloniaXaml);
        Assert.DoesNotContain("Visibility=", avaloniaXaml);
    }

    [Fact]
    public void EndToEnd_MultipleVisibilityValues_TransformsCorrectly()
    {
        // Arrange
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <StackPanel>
        <Button Content=""A"" Visibility=""Visible"" />
        <Button Content=""B"" Visibility=""Collapsed"" />
        <Button Content=""C"" Visibility=""Hidden"" />
    </StackPanel>
</Window>";

        // Act
        var diagnostics = new DiagnosticCollector();
        var xdoc = XDocument.Parse(wpfXaml);
        var document = new UnifiedXamlDocument(xdoc, diagnostics);

        var pipeline = TransformationPipeline.CreateDefault();
        pipeline.Transform(document, diagnostics);

        var serializer = new UnifiedAstSerializer(diagnostics);
        var avaloniaXaml = serializer.SerializeToString(document);

        // Assert
        var avaloniaXdoc = XDocument.Parse(avaloniaXaml);
        var buttons = avaloniaXdoc.Descendants().Where(e => e.Name.LocalName == "Button").ToList();

        Assert.Equal(3, buttons.Count);

        // Visible → IsVisible="True"
        var button1 = buttons.First(b => b.Attribute("Content")?.Value == "A");
        Assert.Equal("True", button1.Attribute("IsVisible")?.Value);

        // Collapsed → IsVisible="False"
        var button2 = buttons.First(b => b.Attribute("Content")?.Value == "B");
        Assert.Equal("False", button2.Attribute("IsVisible")?.Value);

        // Hidden → IsVisible="False" (with warning)
        var button3 = buttons.First(b => b.Attribute("Content")?.Value == "C");
        Assert.Equal("False", button3.Attribute("IsVisible")?.Value);

        // Check Hidden warning
        var warnings = diagnostics.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);
        Assert.Contains(warnings, w => w.Code == "VISIBILITY_HIDDEN_TO_FALSE");
    }

    [Fact]
    public void EndToEnd_XNamePreservation_MaintainsIdentifiers()
    {
        // Arrange
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
          xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
          x:Name=""mainWindow"">
    <Button x:Name=""submitButton"" Content=""Submit"" />
</Window>";

        // Act
        var diagnostics = new DiagnosticCollector();
        var xdoc = XDocument.Parse(wpfXaml);
        var document = new UnifiedXamlDocument(xdoc, diagnostics);

        var pipeline = TransformationPipeline.CreateDefault();
        pipeline.Transform(document, diagnostics);

        var serializer = new UnifiedAstSerializer(diagnostics);
        var avaloniaXaml = serializer.SerializeToString(document);

        // Assert
        Assert.Contains("x:Name=\"mainWindow\"", avaloniaXaml);
        Assert.Contains("x:Name=\"submitButton\"", avaloniaXaml);
    }

    [Fact]
    public void EndToEnd_DiagnosticComments_CanBeEnabled()
    {
        // Arrange
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ListView />
</Window>";

        // Act
        var diagnostics = new DiagnosticCollector();
        var xdoc = XDocument.Parse(wpfXaml);
        var document = new UnifiedXamlDocument(xdoc, diagnostics);

        var pipeline = TransformationPipeline.CreateDefault();
        pipeline.Transform(document, diagnostics);

        var options = new SerializationOptions { IncludeDiagnosticComments = true };
        var serializer = new UnifiedAstSerializer(diagnostics, options);
        var avaloniaXaml = serializer.SerializeToString(document);

        // Assert - Should contain diagnostic comments
        Assert.Contains("<!--", avaloniaXaml);
        Assert.Contains("TYPE_LISTVIEW_TO_LISTBOX", avaloniaXaml);
    }

    [Fact]
    public void EndToEnd_EmptyDocument_HandlesGracefully()
    {
        // Arrange
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" />";

        // Act
        var diagnostics = new DiagnosticCollector();
        var xdoc = XDocument.Parse(wpfXaml);
        var document = new UnifiedXamlDocument(xdoc, diagnostics);

        var pipeline = TransformationPipeline.CreateDefault();
        pipeline.Transform(document, diagnostics);

        var serializer = new UnifiedAstSerializer(diagnostics);
        var avaloniaXaml = serializer.SerializeToString(document);

        // Assert
        Assert.Contains("<Window", avaloniaXaml);
        Assert.Contains("https://github.com/avaloniaui", avaloniaXaml);

        // Should be valid XML
        var avaloniaXdoc = XDocument.Parse(avaloniaXaml);
        Assert.NotNull(avaloniaXdoc.Root);
    }
}
