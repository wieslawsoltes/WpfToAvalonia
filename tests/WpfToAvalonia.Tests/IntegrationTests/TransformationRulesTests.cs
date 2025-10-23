using FluentAssertions;
using WpfToAvalonia.XamlParser;

namespace WpfToAvalonia.Tests.IntegrationTests;

/// <summary>
/// Integration tests for specific transformation rules.
/// Tests individual WPF → Avalonia transformations.
/// </summary>
public class TransformationRulesTests
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
    public void Transform_ListView_ConvertsToListBox()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ListView>
        <ListViewItem Content=""Item 1"" />
    </ListView>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <ListBox>
    <ListBoxItem Content=""Item 1"" />
  </ListBox>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "ListView should be converted to ListBox and ListViewItem to ListBoxItem");
    }

    [Fact]
    public void Transform_DataGrid_PreservesElement()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <DataGrid AutoGenerateColumns=""True"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <DataGrid AutoGenerateColumns=""True"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "DataGrid element should be preserved");
    }

    [Fact]
    public void Transform_VisibilityVisible_ToIsVisibleTrue()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Visibility=""Visible"" Content=""Test"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button IsVisible=""true"" Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "Visibility=Visible should be converted to IsVisible=true");
    }

    [Fact]
    public void Transform_VisibilityHidden_ToIsVisibleFalse()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Visibility=""Hidden"" Content=""Test"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button IsVisible=""false"" Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "Visibility=Hidden should be converted to IsVisible=false");
    }

    [Fact]
    public void Transform_CursorHand_MapsCorrectly()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Cursor=""Hand"" Content=""Test"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button Cursor=""Hand"" Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "Cursor=Hand should be preserved as it's valid in Avalonia");
    }

    [Fact]
    public void Transform_CursorSizeNS_ToSizeNorthSouth()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Cursor=""SizeNS"" Content=""Test"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button Cursor=""SizeNorthSouth"" Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "Cursor=SizeNS should be converted to SizeNorthSouth");
    }

    [Fact]
    public void Transform_ToolTip_ToToolTipTip()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button ToolTip=""This is a tooltip"" Content=""Test"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button ToolTip.Tip=""This is a tooltip"" Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "ToolTip should be converted to ToolTip.Tip");
    }

    [Fact]
    public void Transform_MultipleProperties_AllTransform()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Visibility=""Visible""
            ToolTip=""Tooltip text""
            Margin=""10""
            Padding=""5""
            Content=""Test"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button IsVisible=""true""
          ToolTip.Tip=""Tooltip text""
          Margin=""10""
          Padding=""5""
          Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "all properties should be transformed correctly");
    }

    [Fact]
    public void Transform_FontProperties_PreserveValues()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock FontFamily=""Arial""
               FontSize=""14""
               FontWeight=""Bold""
               FontStyle=""Italic""
               Text=""Test"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <TextBlock FontFamily=""Arial""
             FontSize=""14""
             FontWeight=""Bold""
             FontStyle=""Italic""
             Text=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "font properties should be preserved");
    }

    [Fact]
    public void Transform_ColorProperties_Preserve()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Foreground=""Red"" Background=""Blue"" BorderBrush=""Green"" Content=""Test"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button Foreground=""Red"" Background=""Blue"" BorderBrush=""Green"" Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "color properties should be preserved");
    }

    [Fact]
    public void Transform_SizeProperties_Preserve()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Width=""100""
            Height=""50""
            MinWidth=""80""
            MinHeight=""30""
            MaxWidth=""200""
            MaxHeight=""100""
            Content=""Test"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button Width=""100""
          Height=""50""
          MinWidth=""80""
          MinHeight=""30""
          MaxWidth=""200""
          MaxHeight=""100""
          Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "size properties should be preserved");
    }

    [Fact]
    public void Transform_AlignmentProperties_Preserve()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button HorizontalAlignment=""Left"" VerticalAlignment=""Top"" Content=""Test"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button HorizontalAlignment=""Left"" VerticalAlignment=""Top"" Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "alignment properties should be preserved");
    }

    [Fact]
    public void Transform_OpacityAndTransform_Preserve()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Opacity=""0.5"" RenderTransformOrigin=""0.5,0.5"" Content=""Test"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Button Opacity=""0.5"" RenderTransformOrigin=""0.5,0.5"" Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "opacity and transform properties should be preserved");
    }

    [Fact]
    public void Transform_UniformGrid_PreservesRowsAndColumns()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <UniformGrid Rows=""2"" Columns=""3"">
        <Button Content=""1"" />
        <Button Content=""2"" />
    </UniformGrid>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <UniformGrid Rows=""2"" Columns=""3"">
    <Button Content=""1"" />
    <Button Content=""2"" />
  </UniformGrid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "UniformGrid with Rows and Columns should be preserved");
    }

    [Fact]
    public void Transform_WrapPanel_PreservesOrientation()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <WrapPanel Orientation=""Vertical"">
        <Button Content=""1"" />
        <Button Content=""2"" />
    </WrapPanel>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <WrapPanel Orientation=""Vertical"">
    <Button Content=""1"" />
    <Button Content=""2"" />
  </WrapPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "WrapPanel with Orientation should be preserved");
    }

    [Fact]
    public void Transform_DockPanel_PreservesLastChildFill()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <DockPanel LastChildFill=""True"">
        <Button DockPanel.Dock=""Top"" Content=""Top"" />
        <Button Content=""Fill"" />
    </DockPanel>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <DockPanel LastChildFill=""True"">
    <Button DockPanel.Dock=""Top"" Content=""Top"" />
    <Button Content=""Fill"" />
  </DockPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "DockPanel with LastChildFill and DockPanel.Dock should be preserved");
    }

    [Fact]
    public void Transform_ScrollViewer_Preserves()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ScrollViewer VerticalScrollBarVisibility=""Auto"">
        <StackPanel>
            <Button Content=""1"" />
        </StackPanel>
    </ScrollViewer>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <ScrollViewer VerticalScrollBarVisibility=""Auto"">
    <StackPanel>
      <Button Content=""1"" />
    </StackPanel>
  </ScrollViewer>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "ScrollViewer should be preserved");
    }

    [Fact]
    public void Transform_TabControl_WithTabItems()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TabControl>
        <TabItem Header=""Tab 1"">
            <TextBlock Text=""Content 1"" />
        </TabItem>
        <TabItem Header=""Tab 2"">
            <TextBlock Text=""Content 2"" />
        </TabItem>
    </TabControl>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <TabControl>
    <TabItem Header=""Tab 1"">
      <TextBlock Text=""Content 1"" />
    </TabItem>
    <TabItem Header=""Tab 2"">
      <TextBlock Text=""Content 2"" />
    </TabItem>
  </TabControl>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "TabControl with TabItems should be preserved");
    }

    [Fact]
    public void Transform_Menu_WithMenuItems()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Menu>
        <MenuItem Header=""File"">
            <MenuItem Header=""New"" />
            <MenuItem Header=""Open"" />
        </MenuItem>
    </Menu>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Menu>
    <MenuItem Header=""File"">
      <MenuItem Header=""New"" />
      <MenuItem Header=""Open"" />
    </MenuItem>
  </Menu>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "Menu with nested MenuItems should be preserved");
    }

    [Fact]
    public void Transform_Expander_PreservesHeaderAndContent()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Expander Header=""Click to expand"">
        <TextBlock Text=""Hidden content"" />
    </Expander>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Expander Header=""Click to expand"">
    <TextBlock Text=""Hidden content"" />
  </Expander>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "Expander with Header and content should be preserved");
    }

    [Fact]
    public void Transform_GroupBox_PreservesHeader()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <GroupBox Header=""Group Title"">
        <StackPanel>
            <Button Content=""Button"" />
        </StackPanel>
    </GroupBox>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <GroupBox Header=""Group Title"">
    <StackPanel>
      <Button Content=""Button"" />
    </StackPanel>
  </GroupBox>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "GroupBox with Header should be preserved");
    }
}
