using FluentAssertions;
using WpfToAvalonia.XamlParser;
using Xunit;

namespace WpfToAvalonia.Tests.IntegrationTests.XamlParser;

/// <summary>
/// Integration tests for large production-like XAML files.
/// Implements task 2.5.8.2.5: Test large production XAML files
/// </summary>
public class ProductionXamlTests
{
    [Fact]
    public void Transform_CompleteMainWindow()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 xmlns:local=""clr-namespace:MyApp""
                 Title=""My Application"" Width=""800"" Height=""600"">
    <Window.Resources>
        <local:BoolToVisibilityConverter x:Key=""BoolToVis"" />
        <Style x:Key=""HeaderStyle"" TargetType=""TextBlock"">
            <Setter Property=""FontSize"" Value=""16"" />
            <Setter Property=""FontWeight"" Value=""Bold"" />
        </Style>
    </Window.Resources>

    <DockPanel>
        <Menu DockPanel.Dock=""Top"">
            <MenuItem Header=""File"">
                <MenuItem Header=""New"" Command=""{Binding NewCommand}"" />
                <MenuItem Header=""Open"" Command=""{Binding OpenCommand}"" />
                <Separator />
                <MenuItem Header=""Exit"" Command=""{Binding ExitCommand}"" />
            </MenuItem>
        </Menu>

        <StatusBar DockPanel.Dock=""Bottom"">
            <TextBlock Text=""{Binding StatusMessage}"" />
        </StatusBar>

        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width=""200"" />
                <ColumnDefinition Width=""*"" />
            </Grid.ColumnDefinitions>

            <TreeView Grid.Column=""0"" ItemsSource=""{Binding NavigationItems}"">
                <TreeView.ItemTemplate>
                    <HierarchicalDataTemplate ItemsSource=""{Binding Children}"">
                        <TextBlock Text=""{Binding Name}"" />
                    </HierarchicalDataTemplate>
                </TreeView.ItemTemplate>
            </TreeView>

            <TabControl Grid.Column=""1"" ItemsSource=""{Binding OpenDocuments}"">
                <TabControl.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Text=""{Binding Title}"" />
                    </DataTemplate>
                </TabControl.ItemTemplate>
                <TabControl.ContentTemplate>
                    <DataTemplate>
                        <ScrollViewer>
                            <TextBox Text=""{Binding Content, Mode=TwoWay}""
                                    AcceptsReturn=""True""
                                    VerticalScrollBarVisibility=""Auto"" />
                        </ScrollViewer>
                    </DataTemplate>
                </TabControl.ContentTemplate>
            </TabControl>
        </Grid>
    </DockPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("Production-like XAML should convert successfully");
        result.OutputXaml.Should().Contain("DockPanel");
        result.OutputXaml.Should().Contain("Menu");
        result.OutputXaml.Should().Contain("TreeView");
        result.OutputXaml.Should().Contain("TabControl");
    }

    [Fact]
    public void Transform_DataEntryForm()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <ScrollViewer>
        <StackPanel Margin=""20"">
            <TextBlock Text=""Customer Information"" FontSize=""20"" FontWeight=""Bold"" />

            <Grid Margin=""0,20,0,0"">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width=""150"" />
                    <ColumnDefinition Width=""*"" />
                </Grid.ColumnDefinitions>
                <Grid.RowDefinitions>
                    <RowDefinition Height=""Auto"" />
                    <RowDefinition Height=""Auto"" />
                    <RowDefinition Height=""Auto"" />
                    <RowDefinition Height=""Auto"" />
                    <RowDefinition Height=""Auto"" />
                </Grid.RowDefinitions>

                <TextBlock Grid.Row=""0"" Grid.Column=""0"" Text=""First Name:"" VerticalAlignment=""Center"" />
                <TextBox Grid.Row=""0"" Grid.Column=""1"" Text=""{Binding FirstName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"" />

                <TextBlock Grid.Row=""1"" Grid.Column=""0"" Text=""Last Name:"" VerticalAlignment=""Center"" Margin=""0,10,0,0"" />
                <TextBox Grid.Row=""1"" Grid.Column=""1"" Text=""{Binding LastName, Mode=TwoWay}"" Margin=""0,10,0,0"" />

                <TextBlock Grid.Row=""2"" Grid.Column=""0"" Text=""Email:"" VerticalAlignment=""Center"" Margin=""0,10,0,0"" />
                <TextBox Grid.Row=""2"" Grid.Column=""1"" Text=""{Binding Email, Mode=TwoWay}"" Margin=""0,10,0,0"" />

                <TextBlock Grid.Row=""3"" Grid.Column=""0"" Text=""Active:"" VerticalAlignment=""Center"" Margin=""0,10,0,0"" />
                <CheckBox Grid.Row=""3"" Grid.Column=""1"" IsChecked=""{Binding IsActive}"" Margin=""0,10,0,0"" />
            </Grid>

            <StackPanel Orientation=""Horizontal"" HorizontalAlignment=""Right"" Margin=""0,20,0,0"">
                <Button Content=""Save"" Command=""{Binding SaveCommand}"" Width=""80"" Margin=""0,0,10,0"" />
                <Button Content=""Cancel"" Command=""{Binding CancelCommand}"" Width=""80"" />
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("Data entry form should convert");
        result.OutputXaml.Should().Contain("TextBox");
        result.OutputXaml.Should().Contain("CheckBox");
        result.OutputXaml.Should().Contain("Binding");
    }

    [Fact]
    public void Transform_DataGridWithColumns()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <DataGrid ItemsSource=""{Binding Customers}"" AutoGenerateColumns=""False"">
        <DataGrid.Columns>
            <DataGridTextColumn Header=""ID"" Binding=""{Binding Id}"" Width=""50"" />
            <DataGridTextColumn Header=""Name"" Binding=""{Binding Name}"" Width=""200"" />
            <DataGridCheckBoxColumn Header=""Active"" Binding=""{Binding IsActive}"" Width=""80"" />
            <DataGridTemplateColumn Header=""Actions"" Width=""100"">
                <DataGridTemplateColumn.CellTemplate>
                    <DataTemplate>
                        <Button Content=""Edit"" Command=""{Binding DataContext.EditCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}""
                                CommandParameter=""{Binding}"" />
                    </DataTemplate>
                </DataGridTemplateColumn.CellTemplate>
            </DataGridTemplateColumn>
        </DataGrid.Columns>
    </DataGrid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("DataGrid with columns should convert");
        result.OutputXaml.Should().Contain("DataGrid");
    }
}
