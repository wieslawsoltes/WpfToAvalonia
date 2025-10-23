using System.Xml.Linq;
using Xunit;
using WpfToAvalonia.XamlParser.UnifiedAst;
using WpfToAvalonia.XamlParser.Transformers;
using WpfToAvalonia.Core.Diagnostics;

namespace WpfToAvalonia.Tests.IntegrationTests;

/// <summary>
/// Integration tests for the transformation pipeline.
/// Tests the complete WPF to Avalonia XAML transformation flow.
/// </summary>
public class TransformationPipelineTests
{
    [Fact]
    public void Transform_SimpleButton_AppliesAllTransformations()
    {
        // Arrange
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Button Content=""Click Me"" Visibility=""Visible"" />
</Window>";

        var diagnostics = new DiagnosticCollector();
        var xdoc = XDocument.Parse(wpfXaml);
        var document = new UnifiedXamlDocument(xdoc, diagnostics);
        var pipeline = TransformationPipeline.CreateDefault();

        // Act
        var context = pipeline.Transform(document, diagnostics);

        // Assert
        Assert.NotNull(document.Root);

        // Check namespace transformation
        Assert.Equal("https://github.com/avaloniaui", document.Root.Namespace);

        // Check type transformation (Button stays Button)
        var button = document.Root.Children.FirstOrDefault();
        Assert.NotNull(button);
        Assert.Equal("Button", button.TypeName);

        // Check property transformation (Visibility → IsVisible)
        var visibilityProp = button.Properties.FirstOrDefault(p => p.PropertyName == "IsVisible");
        Assert.NotNull(visibilityProp);
        Assert.Equal("True", visibilityProp.Value);

        // Check statistics
        Assert.True(context.Statistics.NamespacesTransformed > 0);
        Assert.True(context.Statistics.PropertiesTransformed > 0);
    }

    [Fact]
    public void Transform_ListViewToListBox_AddsWarning()
    {
        // Arrange
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ListView />
</Window>";

        var diagnostics = new DiagnosticCollector();
        var xdoc = XDocument.Parse(wpfXaml);
        var document = new UnifiedXamlDocument(xdoc, diagnostics);
        var pipeline = TransformationPipeline.CreateDefault();

        // Act
        var context = pipeline.Transform(document, diagnostics);

        // Assert
        var listBox = document.Root?.Children.FirstOrDefault();
        Assert.NotNull(listBox);
        Assert.Equal("ListBox", listBox.TypeName);

        // Should have warning about ListView → ListBox conversion
        var warnings = diagnostics.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);
        Assert.Contains(warnings, w => w.Code == "TYPE_LISTVIEW_TO_LISTBOX");

        Assert.True(context.Statistics.WarningsGenerated > 0);
    }

    [Fact]
    public void Transform_HiddenVisibility_AddsWarning()
    {
        // Arrange
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Visibility=""Hidden"" />
</Window>";

        var diagnostics = new DiagnosticCollector();
        var xdoc = XDocument.Parse(wpfXaml);
        var document = new UnifiedXamlDocument(xdoc, diagnostics);
        var pipeline = TransformationPipeline.CreateDefault();

        // Act
        var context = pipeline.Transform(document, diagnostics);

        // Assert
        var button = document.Root?.Children.FirstOrDefault();
        Assert.NotNull(button);

        var isVisibleProp = button.Properties.FirstOrDefault(p => p.PropertyName == "IsVisible");
        Assert.NotNull(isVisibleProp);
        Assert.Equal("False", isVisibleProp.Value);

        // Should have warning about Hidden → False mapping
        var warnings = diagnostics.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);
        Assert.Contains(warnings, w => w.Code == "VISIBILITY_HIDDEN_TO_FALSE");
    }

    [Fact]
    public void Transform_CollapsedVisibility_NoWarning()
    {
        // Arrange
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Visibility=""Collapsed"" />
</Window>";

        var diagnostics = new DiagnosticCollector();
        var xdoc = XDocument.Parse(wpfXaml);
        var document = new UnifiedXamlDocument(xdoc, diagnostics);
        var pipeline = TransformationPipeline.CreateDefault();

        // Act
        var context = pipeline.Transform(document, diagnostics);

        // Assert
        var button = document.Root?.Children.FirstOrDefault();
        Assert.NotNull(button);

        var isVisibleProp = button.Properties.FirstOrDefault(p => p.PropertyName == "IsVisible");
        Assert.NotNull(isVisibleProp);
        Assert.Equal("False", isVisibleProp.Value);

        // Should NOT have warning about Collapsed (it's expected)
        var warnings = diagnostics.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);
        Assert.DoesNotContain(warnings, w => w.Code == "VISIBILITY_HIDDEN_TO_FALSE");
    }

    [Fact]
    public void Transform_ComplexDocument_AppliesAllTransformations()
    {
        // Arrange
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                  x:Class=""MyApp.MainWindow"">
    <StackPanel>
        <Button Content=""Show"" Visibility=""Visible"" />
        <Button Content=""Hide"" Visibility=""Collapsed"" />
        <ListView x:Name=""myList"" />
        <TextBlock Text=""Hello World"" Visibility=""Hidden"" />
    </StackPanel>
</Window>";

        var diagnostics = new DiagnosticCollector();
        var xdoc = XDocument.Parse(wpfXaml);
        var document = new UnifiedXamlDocument(xdoc, diagnostics);
        var pipeline = TransformationPipeline.CreateDefault();

        // Act
        var context = pipeline.Transform(document, diagnostics);

        // Assert
        Assert.NotNull(document.Root);
        Assert.Equal("https://github.com/avaloniaui", document.Root.Namespace);
        Assert.Equal("Window", document.Root.TypeName);

        var stackPanel = document.Root.Children.FirstOrDefault();
        Assert.NotNull(stackPanel);
        Assert.Equal("StackPanel", stackPanel.TypeName);

        // Check all children were transformed
        Assert.Equal(4, stackPanel.Children.Count);

        // Button 1: Visibility="Visible" → IsVisible="True"
        var button1 = stackPanel.Children[0];
        Assert.Equal("Button", button1.TypeName);
        var isVisible1 = button1.Properties.FirstOrDefault(p => p.PropertyName == "IsVisible");
        Assert.NotNull(isVisible1);
        Assert.Equal("True", isVisible1.Value);

        // Button 2: Visibility="Collapsed" → IsVisible="False"
        var button2 = stackPanel.Children[1];
        Assert.Equal("Button", button2.TypeName);
        var isVisible2 = button2.Properties.FirstOrDefault(p => p.PropertyName == "IsVisible");
        Assert.NotNull(isVisible2);
        Assert.Equal("False", isVisible2.Value);

        // ListView → ListBox
        var listBox = stackPanel.Children[2];
        Assert.Equal("ListBox", listBox.TypeName);
        Assert.Equal("myList", listBox.XName);

        // TextBlock: Visibility="Hidden" → IsVisible="False"
        var textBlock = stackPanel.Children[3];
        Assert.Equal("TextBlock", textBlock.TypeName);
        var isVisible3 = textBlock.Properties.FirstOrDefault(p => p.PropertyName == "IsVisible");
        Assert.NotNull(isVisible3);
        Assert.Equal("False", isVisible3.Value);

        // Check statistics
        Assert.True(context.Statistics.ElementsTransformed >= 1); // ListView → ListBox
        Assert.True(context.Statistics.PropertiesTransformed >= 3); // 3 Visibility transformations
        Assert.True(context.Statistics.NamespacesTransformed > 0);
        Assert.True(context.Statistics.WarningsGenerated >= 2); // ListView warning + Hidden warning
    }

    [Fact]
    public void Transform_CustomPipeline_ExecutesInPriorityOrder()
    {
        // Arrange
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Visibility=""Visible"" />
</Window>";

        var diagnostics = new DiagnosticCollector();
        var xdoc = XDocument.Parse(wpfXaml);
        var document = new UnifiedXamlDocument(xdoc, diagnostics);
        var pipeline = new TransformationPipeline();

        // Add transformers in reverse priority order to test sorting
        pipeline.AddTransformer(new PropertyTransformer());  // Priority 30
        pipeline.AddTransformer(new TypeTransformer());      // Priority 20
        pipeline.AddTransformer(new NamespaceTransformer()); // Priority 10

        // Act
        var context = pipeline.Transform(document, diagnostics);
        var executionOrder = pipeline.GetTransformers();

        // Assert
        Assert.Equal(3, executionOrder.Count);
        Assert.Equal("NamespaceTransformer", executionOrder[0].Name);
        Assert.Equal("TypeTransformer", executionOrder[1].Name);
        Assert.Equal("PropertyTransformer", executionOrder[2].Name);

        // Verify transformations were applied
        Assert.Equal("https://github.com/avaloniaui", document.Root?.Namespace);
        var button = document.Root?.Children.FirstOrDefault();
        var isVisible = button?.Properties.FirstOrDefault(p => p.PropertyName == "IsVisible");
        Assert.NotNull(isVisible);
        Assert.Equal("True", isVisible.Value);
    }

    [Fact]
    public void Transform_EmptyPipeline_DoesNothing()
    {
        // Arrange
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Visibility=""Visible"" />
</Window>";

        var diagnostics = new DiagnosticCollector();
        var xdoc = XDocument.Parse(wpfXaml);
        var document = new UnifiedXamlDocument(xdoc, diagnostics);
        var pipeline = new TransformationPipeline(); // No transformers added

        // Act
        var context = pipeline.Transform(document, diagnostics);

        // Assert
        Assert.Equal(0, pipeline.TransformerCount);
        Assert.Equal(0, context.Statistics.ElementsTransformed);
        Assert.Equal(0, context.Statistics.PropertiesTransformed);
        Assert.Equal(0, context.Statistics.NamespacesTransformed);

        // Document should remain unchanged
        Assert.Equal("http://schemas.microsoft.com/winfx/2006/xaml/presentation", document.Root?.Namespace);
        var button = document.Root?.Children.FirstOrDefault();
        var visibility = button?.Properties.FirstOrDefault(p => p.PropertyName == "Visibility");
        Assert.NotNull(visibility);
        Assert.Equal("Visible", visibility.Value);
    }
}
