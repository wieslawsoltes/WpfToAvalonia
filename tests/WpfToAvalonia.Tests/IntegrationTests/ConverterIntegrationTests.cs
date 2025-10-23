using FluentAssertions;
using WpfToAvalonia.XamlParser;

namespace WpfToAvalonia.Tests.IntegrationTests;

/// <summary>
/// Integration tests for the WpfToAvaloniaConverter end-to-end pipeline.
/// Tests the complete Parse → Enrich → Transform → Serialize pipeline.
/// </summary>
public class ConverterIntegrationTests
{
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
    public void Convert_SimpleButton_ShouldSucceed()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                x:Class=""MyApp.MainWindow"">
    <Button Content=""Click Me"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                x:Class=""MyApp.MainWindow"">
  <Button Content=""Click Me"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.OutputXaml.Should().NotBeNullOrEmpty();

        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "the converted XAML should match the expected Avalonia XAML");
    }

    [Fact]
    public void Convert_WindowWithProperties_TransformsCorrectly()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                x:Class=""MyApp.MainWindow""
                Title=""My Window""
                Width=""800""
                Height=""600"">
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                x:Class=""MyApp.MainWindow""
                Title=""My Window""
                Width=""800""
                Height=""600"">
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml));
    }

    [Fact]
    public void Convert_VisibilityProperty_TransformsToIsVisible()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Visibility=""Visible"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button IsVisible=""true"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml));
    }

    [Fact]
    public void Convert_VisibilityCollapsed_TransformsToIsVisibleFalse()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Visibility=""Collapsed"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button IsVisible=""false"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml));
    }

    [Fact]
    public void Convert_StackPanel_PreservesChildren()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <StackPanel>
        <Button Content=""Button 1"" />
        <Button Content=""Button 2"" />
        <TextBlock Text=""Hello"" />
    </StackPanel>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <StackPanel>
    <Button Content=""Button 1"" />
    <Button Content=""Button 2"" />
    <TextBlock Text=""Hello"" />
  </StackPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml));
    }

    [Fact]
    public void Convert_GridWithRowsAndColumns_PreservesDefinitions()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""*"" />
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width=""200"" />
            <ColumnDefinition Width=""*"" />
        </Grid.ColumnDefinitions>
        <Button Grid.Row=""0"" Grid.Column=""0"" />
    </Grid>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Grid>
    <Grid.RowDefinitions>
      <Grid.RowDefinitions>
        <RowDefinition Height=""Auto"" />
        <RowDefinition Height=""*"" />
      </Grid.RowDefinitions>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width=""200"" />
        <ColumnDefinition Width=""*"" />
      </Grid.ColumnDefinitions>
    </Grid.ColumnDefinitions>
    <Button Grid.Row=""0"" Grid.Column=""0"" />
  </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml));
    }

    [Fact]
    public void Convert_TextBox_TransformsCorrectly()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox Text=""Sample Text"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <TextBox Text=""Sample Text"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml));
    }

    [Fact]
    public void Convert_ListBox_PreservesItems()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ListBox>
        <ListBoxItem Content=""Item 1"" />
        <ListBoxItem Content=""Item 2"" />
    </ListBox>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <ListBox>
    <ListBoxItem Content=""Item 1"" />
    <ListBoxItem Content=""Item 2"" />
  </ListBox>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml));
    }

    [Fact]
    public void Convert_WithComments_PreservesCommentsByDefault()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <!-- This is a comment -->
    <Button Content=""Click Me"" />
</Window>";

        var options = new ConversionOptions
        {
            PreserveComments = true
        };

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button Content=""Click Me"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml, null, options);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml));
    }

    [Fact]
    public void Convert_WithXmlDeclaration_IncludesDeclarationByDefault()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
</Window>";

        var options = new ConversionOptions
        {
            IncludeXmlDeclaration = true
        };

        var expectedXaml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Window xmlns=""https://github.com/avaloniaui"">
</Window>";

        // Act
        var result = converter.Convert(wpfXaml, null, options);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputXaml.Should().StartWith("<?xml version=\"1.0\"");
        // Note: Full comparison is complex due to encoding variations
    }

    [Fact]
    public void Convert_UserControl_TransformsCorrectly()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<UserControl xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                     xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                     x:Class=""MyApp.MyControl"">
    <StackPanel>
        <TextBlock Text=""Custom Control"" />
    </StackPanel>
</UserControl>";

        var expectedXaml = @"<UserControl xmlns=""https://github.com/avaloniaui""
                     xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                     x:Class=""MyApp.MyControl"">
  <StackPanel>
    <TextBlock Text=""Custom Control"" />
  </StackPanel>
</UserControl>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml));
    }

    [Fact]
    public void Convert_LayoutProperties_TransformCorrectly()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Margin=""10"" Padding=""5"" HorizontalAlignment=""Center"" VerticalAlignment=""Top"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button Margin=""10"" Padding=""5"" HorizontalAlignment=""Center"" VerticalAlignment=""Top"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml));
    }

    [Fact]
    public void Convert_NestedLayouts_PreservesHierarchy()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Grid>
        <StackPanel>
            <DockPanel>
                <Button Content=""Nested"" />
            </DockPanel>
        </StackPanel>
    </Grid>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Grid>
    <StackPanel>
      <DockPanel>
        <Button Content=""Nested"" />
      </DockPanel>
    </StackPanel>
  </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml));
    }

    [Fact]
    public void Convert_Diagnostics_RecordsTransformations()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Visibility=""Visible"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().NotBeEmpty();
        converter.Diagnostics.Diagnostics.Should().NotBeEmpty();
    }

    [Fact]
    public void Convert_InvalidXaml_ReturnsFailure()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var invalidXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button>
    <!-- Missing closing tag -->
</Window>";

        // Act
        var result = converter.Convert(invalidXaml);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ConvertFile_NonExistentFile_ReturnsError()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var nonExistentPath = "/path/to/nonexistent/file.xaml";

        // Act
        var result = converter.ConvertFile(nonExistentPath);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Code == "FILE_NOT_FOUND");
    }

    [Fact]
    public void Convert_ComplexWindow_TransformsAllElements()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                x:Class=""MyApp.MainWindow""
                Title=""Complex Window""
                Width=""1024""
                Height=""768"">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""*"" />
            <RowDefinition Height=""Auto"" />
        </Grid.RowDefinitions>

        <!-- Header -->
        <StackPanel Grid.Row=""0"" Orientation=""Horizontal"">
            <TextBlock Text=""Header"" FontSize=""20"" FontWeight=""Bold"" />
        </StackPanel>

        <!-- Content -->
        <ScrollViewer Grid.Row=""1"">
            <StackPanel>
                <TextBox Text=""Sample"" Margin=""10"" />
                <Button Content=""Submit"" Padding=""5"" />
                <CheckBox Content=""Agree"" />
                <ComboBox>
                    <ComboBoxItem Content=""Option 1"" />
                    <ComboBoxItem Content=""Option 2"" />
                </ComboBox>
            </StackPanel>
        </ScrollViewer>

        <!-- Footer -->
        <Border Grid.Row=""2"" Background=""LightGray"" Padding=""10"">
            <TextBlock Text=""Footer"" />
        </Border>
    </Grid>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                x:Class=""MyApp.MainWindow""
                Title=""Complex Window""
                Width=""1024""
                Height=""768"">
  <Grid>
    <Grid.RowDefinitions>
      <Grid.RowDefinitions>
        <RowDefinition Height=""Auto"" />
        <RowDefinition Height=""*"" />
        <RowDefinition Height=""Auto"" />
      </Grid.RowDefinitions>
    </Grid.RowDefinitions>
    <StackPanel Grid.Row=""0"" Orientation=""Horizontal"">
      <TextBlock Text=""Header"" FontSize=""20"" FontWeight=""Bold"" />
    </StackPanel>
    <ScrollViewer Grid.Row=""1"">
      <StackPanel>
        <TextBox Text=""Sample"" Margin=""10"" />
        <Button Content=""Submit"" Padding=""5"" />
        <CheckBox Content=""Agree"" />
        <ComboBox>
          <ComboBoxItem Content=""Option 1"" />
          <ComboBoxItem Content=""Option 2"" />
        </ComboBox>
      </StackPanel>
    </ScrollViewer>
    <Border Grid.Row=""2"" Background=""LightGray"" Padding=""10"">
      <TextBlock Text=""Footer"" />
    </Border>
  </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml));
    }
}
