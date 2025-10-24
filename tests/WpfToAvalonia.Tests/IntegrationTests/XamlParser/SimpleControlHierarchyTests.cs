using FluentAssertions;
using WpfToAvalonia.XamlParser;
using Xunit;

namespace WpfToAvalonia.Tests.IntegrationTests.XamlParser;

/// <summary>
/// Integration tests for simple control hierarchies.
/// Implements task 2.5.8.2.1: Test simple control hierarchies
/// Tests real WPF XAML with common control patterns.
/// </summary>
public class SimpleControlHierarchyTests
{
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
    public void Transform_SimpleStackPanel_WithButtons()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 Title=""Simple Window"" Width=""400"" Height=""300"">
    <StackPanel>
        <Button Content=""Click Me"" />
        <Button Content=""Cancel"" />
        <Button Content=""OK"" />
    </StackPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("Simple StackPanel should convert successfully");
        result.OutputXaml.Should().Contain("StackPanel", "StackPanel should be preserved");
        result.OutputXaml.Should().Contain("Button", "Buttons should be preserved");
        result.OutputXaml.Should().Contain("Click Me", "Button content should be preserved");
    }

    [Fact]
    public void Transform_GridWithRowsAndColumns()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""*"" />
            <RowDefinition Height=""Auto"" />
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width=""200"" />
            <ColumnDefinition Width=""*"" />
        </Grid.ColumnDefinitions>

        <TextBlock Grid.Row=""0"" Grid.Column=""0"" Text=""Header"" />
        <TextBox Grid.Row=""1"" Grid.Column=""0"" Grid.ColumnSpan=""2"" />
        <Button Grid.Row=""2"" Grid.Column=""1"" Content=""Submit"" />
    </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("Grid with rows and columns should convert");
        result.OutputXaml.Should().Contain("Grid.RowDefinitions", "Row definitions should be preserved");
        result.OutputXaml.Should().Contain("Grid.ColumnDefinitions", "Column definitions should be preserved");
        result.OutputXaml.Should().Contain("Grid.Row", "Grid.Row attached property should be present");
        result.OutputXaml.Should().Contain("Grid.ColumnSpan", "Grid.ColumnSpan should be present");
    }

    [Fact]
    public void Transform_DockPanel_WithDocking()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <DockPanel LastChildFill=""True"">
        <Menu DockPanel.Dock=""Top"">
            <MenuItem Header=""File"" />
        </Menu>
        <StatusBar DockPanel.Dock=""Bottom"">
            <TextBlock Text=""Ready"" />
        </StatusBar>
        <TextBox />
    </DockPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("DockPanel should convert");
        result.OutputXaml.Should().Contain("DockPanel", "DockPanel should be preserved");
        result.OutputXaml.Should().Contain("DockPanel.Dock", "Dock attached property should be preserved");
    }

    [Fact]
    public void Transform_WrapPanel_WithChildren()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <WrapPanel>
        <Button Content=""1"" Width=""50"" />
        <Button Content=""2"" Width=""50"" />
        <Button Content=""3"" Width=""50"" />
        <Button Content=""4"" Width=""50"" />
    </WrapPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("WrapPanel should convert");
        result.OutputXaml.Should().Contain("WrapPanel", "WrapPanel should be preserved");
    }

    [Fact]
    public void Transform_BorderWithChild()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Border BorderBrush=""Black"" BorderThickness=""2"" CornerRadius=""5"" Padding=""10"">
        <TextBlock Text=""Bordered Content"" />
    </Border>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("Border should convert");
        result.OutputXaml.Should().Contain("Border", "Border should be preserved");
        result.OutputXaml.Should().Contain("BorderBrush", "BorderBrush should be preserved");
        result.OutputXaml.Should().Contain("CornerRadius", "CornerRadius should be preserved");
    }

    [Fact]
    public void Transform_ScrollViewerWithContent()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ScrollViewer VerticalScrollBarVisibility=""Auto"">
        <StackPanel>
            <TextBlock Text=""Line 1"" />
            <TextBlock Text=""Line 2"" />
            <TextBlock Text=""Line 3"" />
        </StackPanel>
    </ScrollViewer>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("ScrollViewer should convert");
        result.OutputXaml.Should().Contain("ScrollViewer", "ScrollViewer should be preserved");
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

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("TabControl should convert");
        result.OutputXaml.Should().Contain("TabControl", "TabControl should be preserved");
        result.OutputXaml.Should().Contain("TabItem", "TabItem should be preserved");
    }

    [Fact]
    public void Transform_GroupBox_WithContent()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <GroupBox Header=""Settings"">
        <StackPanel>
            <CheckBox Content=""Option 1"" />
            <CheckBox Content=""Option 2"" />
        </StackPanel>
    </GroupBox>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("GroupBox should convert");
        result.OutputXaml.Should().Contain("GroupBox", "GroupBox should be preserved");
        result.OutputXaml.Should().Contain("CheckBox", "CheckBox should be preserved");
    }

    [Fact]
    public void Transform_Expander_WithContent()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Expander Header=""Details"" IsExpanded=""True"">
        <TextBlock Text=""Additional information here"" />
    </Expander>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("Expander should convert");
        result.OutputXaml.Should().Contain("Expander", "Expander should be preserved");
    }

    [Fact]
    public void Transform_ListBox_WithListBoxItems()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ListBox>
        <ListBoxItem Content=""Item 1"" />
        <ListBoxItem Content=""Item 2"" />
        <ListBoxItem Content=""Item 3"" />
    </ListBox>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("ListBox should convert");
        result.OutputXaml.Should().Contain("ListBox", "ListBox should be preserved");
        result.OutputXaml.Should().Contain("ListBoxItem", "ListBoxItem should be preserved");
    }

    [Fact]
    public void Transform_ComboBox_WithComboBoxItems()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ComboBox>
        <ComboBoxItem Content=""Option 1"" />
        <ComboBoxItem Content=""Option 2"" IsSelected=""True"" />
        <ComboBoxItem Content=""Option 3"" />
    </ComboBox>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("ComboBox should convert");
        result.OutputXaml.Should().Contain("ComboBox", "ComboBox should be preserved");
        result.OutputXaml.Should().Contain("ComboBoxItem", "ComboBoxItem should be preserved");
    }

    [Fact]
    public void Transform_TreeView_WithTreeViewItems()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TreeView>
        <TreeViewItem Header=""Root"">
            <TreeViewItem Header=""Child 1"" />
            <TreeViewItem Header=""Child 2"">
                <TreeViewItem Header=""Grandchild"" />
            </TreeViewItem>
        </TreeViewItem>
    </TreeView>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("TreeView should convert");
        result.OutputXaml.Should().Contain("TreeView", "TreeView should be preserved");
        result.OutputXaml.Should().Contain("TreeViewItem", "TreeViewItem should be preserved");
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
            <Separator />
            <MenuItem Header=""Exit"" />
        </MenuItem>
        <MenuItem Header=""Edit"">
            <MenuItem Header=""Cut"" />
            <MenuItem Header=""Copy"" />
            <MenuItem Header=""Paste"" />
        </MenuItem>
    </Menu>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("Menu should convert");
        result.OutputXaml.Should().Contain("Menu", "Menu should be preserved");
        result.OutputXaml.Should().Contain("MenuItem", "MenuItem should be preserved");
        result.OutputXaml.Should().Contain("Separator", "Separator should be preserved");
    }

    [Fact]
    public void Transform_ToolBar_WithControls()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ToolBar>
        <Button Content=""New"" />
        <Button Content=""Open"" />
        <Separator />
        <Button Content=""Save"" />
    </ToolBar>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("ToolBar should convert");
        result.OutputXaml.Should().Contain("ToolBar", "ToolBar should be preserved");
    }

    [Fact]
    public void Transform_UniformGrid_WithChildren()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <UniformGrid Rows=""2"" Columns=""2"">
        <Button Content=""1"" />
        <Button Content=""2"" />
        <Button Content=""3"" />
        <Button Content=""4"" />
    </UniformGrid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("UniformGrid should convert");
        result.OutputXaml.Should().Contain("UniformGrid", "UniformGrid should be preserved");
    }

    [Fact]
    public void Transform_Canvas_WithPositionedChildren()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Canvas Width=""400"" Height=""300"">
        <Rectangle Canvas.Left=""10"" Canvas.Top=""10"" Width=""100"" Height=""50"" Fill=""Blue"" />
        <Ellipse Canvas.Left=""150"" Canvas.Top=""100"" Width=""80"" Height=""80"" Fill=""Red"" />
    </Canvas>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("Canvas should convert");
        result.OutputXaml.Should().Contain("Canvas", "Canvas should be preserved");
        result.OutputXaml.Should().Contain("Canvas.Left", "Canvas.Left attached property should be present");
        result.OutputXaml.Should().Contain("Canvas.Top", "Canvas.Top attached property should be present");
    }

    [Fact]
    public void Transform_NestedPanels_ComplexHierarchy()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""*"" />
        </Grid.RowDefinitions>

        <StackPanel Grid.Row=""0"" Orientation=""Horizontal"">
            <Button Content=""Button 1"" />
            <Button Content=""Button 2"" />
        </StackPanel>

        <Border Grid.Row=""1"" BorderBrush=""Gray"" BorderThickness=""1"">
            <DockPanel>
                <TextBlock DockPanel.Dock=""Top"" Text=""Header"" />
                <ScrollViewer>
                    <WrapPanel>
                        <TextBlock Text=""Item 1"" Margin=""5"" />
                        <TextBlock Text=""Item 2"" Margin=""5"" />
                    </WrapPanel>
                </ScrollViewer>
            </DockPanel>
        </Border>
    </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("Complex nested panels should convert");
        result.OutputXaml.Should().Contain("Grid", "Grid should be preserved");
        result.OutputXaml.Should().Contain("StackPanel", "StackPanel should be preserved");
        result.OutputXaml.Should().Contain("Border", "Border should be preserved");
        result.OutputXaml.Should().Contain("DockPanel", "DockPanel should be preserved");
        result.OutputXaml.Should().Contain("ScrollViewer", "ScrollViewer should be preserved");
        result.OutputXaml.Should().Contain("WrapPanel", "WrapPanel should be preserved");
    }
}
