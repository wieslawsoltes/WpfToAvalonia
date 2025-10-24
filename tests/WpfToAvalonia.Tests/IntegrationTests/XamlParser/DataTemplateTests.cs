using FluentAssertions;
using WpfToAvalonia.XamlParser;
using Xunit;

namespace WpfToAvalonia.Tests.IntegrationTests.XamlParser;

/// <summary>
/// Integration tests for complex data templates.
/// Implements task 2.5.8.2.2: Test complex data templates
/// Tests real WPF XAML with DataTemplate, ItemTemplate, ContentTemplate, etc.
/// </summary>
public class DataTemplateTests
{
    [Fact]
    public void Transform_ListBox_WithItemTemplate()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <ListBox>
        <ListBox.ItemTemplate>
            <DataTemplate>
                <StackPanel Orientation=""Horizontal"">
                    <TextBlock Text=""{Binding Name}"" FontWeight=""Bold"" />
                    <TextBlock Text="" - "" />
                    <TextBlock Text=""{Binding Description}"" />
                </StackPanel>
            </DataTemplate>
        </ListBox.ItemTemplate>
    </ListBox>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("ListBox with ItemTemplate should convert");
        result.OutputXaml.Should().Contain("DataTemplate", "DataTemplate should be preserved");
        result.OutputXaml.Should().Contain("{Binding Name}", "Binding should be preserved");
    }

    [Fact]
    public void Transform_ItemsControl_WithComplexDataTemplate()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <ItemsControl>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Border BorderBrush=""Gray"" BorderThickness=""1"" Margin=""5"" Padding=""10"">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height=""Auto"" />
                            <RowDefinition Height=""Auto"" />
                        </Grid.RowDefinitions>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""Auto"" />
                            <ColumnDefinition Width=""*"" />
                        </Grid.ColumnDefinitions>

                        <TextBlock Grid.Row=""0"" Grid.Column=""0"" Text=""Name:"" Margin=""0,0,5,0"" />
                        <TextBlock Grid.Row=""0"" Grid.Column=""1"" Text=""{Binding Name}"" />
                        <TextBlock Grid.Row=""1"" Grid.Column=""0"" Text=""Value:"" Margin=""0,0,5,0"" />
                        <TextBlock Grid.Row=""1"" Grid.Column=""1"" Text=""{Binding Value}"" />
                    </Grid>
                </Border>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("ItemsControl with complex template should convert");
        result.OutputXaml.Should().Contain("DataTemplate", "DataTemplate should be preserved");
        result.OutputXaml.Should().Contain("Grid", "Grid layout should be preserved");
    }

    [Fact]
    public void Transform_ContentControl_WithContentTemplate()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <ContentControl>
        <ContentControl.ContentTemplate>
            <DataTemplate>
                <StackPanel>
                    <TextBlock Text=""{Binding Title}"" FontSize=""20"" />
                    <TextBlock Text=""{Binding Subtitle}"" FontSize=""14"" Foreground=""Gray"" />
                </StackPanel>
            </DataTemplate>
        </ContentControl.ContentTemplate>
    </ContentControl>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("ContentControl with ContentTemplate should convert");
        result.OutputXaml.Should().Contain("ContentTemplate", "ContentTemplate should be preserved");
    }

    [Fact]
    public void Transform_DataTemplate_WithDataTriggers()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <DataTemplate x:Key=""PersonTemplate"">
            <TextBlock Text=""{Binding Name}"">
                <TextBlock.Style>
                    <Style TargetType=""TextBlock"">
                        <Style.Triggers>
                            <DataTrigger Binding=""{Binding IsActive}"" Value=""True"">
                                <Setter Property=""Foreground"" Value=""Green"" />
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </TextBlock.Style>
            </TextBlock>
        </DataTemplate>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("DataTemplate with triggers should convert");
        result.Diagnostics.Should().Contain(d => d.Message.Contains("Trigger") || d.Message.Contains("trigger"),
            "Should report trigger transformation");
    }

    [Fact]
    public void Transform_HierarchicalDataTemplate()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <TreeView>
        <TreeView.Resources>
            <HierarchicalDataTemplate DataType=""{x:Type local:Category}"" ItemsSource=""{Binding Items}"">
                <TextBlock Text=""{Binding Name}"" FontWeight=""Bold"" />
            </HierarchicalDataTemplate>
        </TreeView.Resources>
    </TreeView>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("HierarchicalDataTemplate should convert");
        result.OutputXaml.Should().Contain("HierarchicalDataTemplate", "HierarchicalDataTemplate should be preserved");
    }

    [Fact]
    public void Transform_ImplicitDataTemplate_WithDataType()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 xmlns:local=""clr-namespace:MyApp"">
    <Window.Resources>
        <DataTemplate DataType=""{x:Type local:Person}"">
            <StackPanel>
                <TextBlock Text=""{Binding FirstName}"" />
                <TextBlock Text=""{Binding LastName}"" />
            </StackPanel>
        </DataTemplate>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("Implicit DataTemplate should convert");
        result.OutputXaml.Should().Contain("DataTemplate", "DataTemplate should be preserved");
        result.OutputXaml.Should().Contain("DataType", "DataType should be preserved");
    }

    [Fact]
    public void Transform_DataTemplate_WithConverters()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 xmlns:local=""clr-namespace:MyApp"">
    <Window.Resources>
        <local:BooleanToVisibilityConverter x:Key=""BoolToVisConverter"" />
        <DataTemplate x:Key=""ItemTemplate"">
            <StackPanel>
                <TextBlock Text=""{Binding Name}"" />
                <TextBlock Text=""{Binding Details}""
                          Visibility=""{Binding HasDetails, Converter={StaticResource BoolToVisConverter}}"" />
            </StackPanel>
        </DataTemplate>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("DataTemplate with converters should convert");
        result.OutputXaml.Should().Contain("Converter", "Converter reference should be preserved");
    }

    [Fact]
    public void Transform_ItemsPanelTemplate()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ListBox>
        <ListBox.ItemsPanel>
            <ItemsPanelTemplate>
                <WrapPanel Orientation=""Horizontal"" />
            </ItemsPanelTemplate>
        </ListBox.ItemsPanel>
    </ListBox>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("ItemsPanelTemplate should convert");
        result.OutputXaml.Should().Contain("ItemsPanel", "ItemsPanel should be preserved");
    }

    [Fact]
    public void Transform_DataTemplate_WithMultiBinding()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 xmlns:local=""clr-namespace:MyApp"">
    <Window.Resources>
        <local:FullNameConverter x:Key=""FullNameConverter"" />
        <DataTemplate x:Key=""PersonTemplate"">
            <TextBlock>
                <TextBlock.Text>
                    <MultiBinding Converter=""{StaticResource FullNameConverter}"">
                        <Binding Path=""FirstName"" />
                        <Binding Path=""LastName"" />
                    </MultiBinding>
                </TextBlock.Text>
            </TextBlock>
        </DataTemplate>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("DataTemplate with MultiBinding should convert");
        result.OutputXaml.Should().Contain("MultiBinding", "MultiBinding should be preserved");
    }

    [Fact]
    public void Transform_DataTemplate_WithRelativeSourceBinding()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ListBox>
        <ListBox.ItemTemplate>
            <DataTemplate>
                <Border Background=""{Binding Background, RelativeSource={RelativeSource AncestorType=ListBox}}"">
                    <TextBlock Text=""{Binding}"" />
                </Border>
            </DataTemplate>
        </ListBox.ItemTemplate>
    </ListBox>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("DataTemplate with RelativeSource should convert");
        result.OutputXaml.Should().Contain("RelativeSource", "RelativeSource should be preserved");
    }

    [Fact]
    public void Transform_DataTemplate_WithElementNameBinding()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <StackPanel>
        <Slider x:Name=""FontSizeSlider"" Minimum=""10"" Maximum=""30"" Value=""14"" />
        <ListBox>
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text=""{Binding}"" FontSize=""{Binding Value, ElementName=FontSizeSlider}"" />
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </StackPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("DataTemplate with ElementName binding should convert");
        result.OutputXaml.Should().Contain("ElementName", "ElementName binding should be preserved");
    }

    [Fact]
    public void Transform_NestedDataTemplates()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <ItemsControl>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Border BorderBrush=""Black"" BorderThickness=""1"" Margin=""5"">
                    <StackPanel>
                        <TextBlock Text=""{Binding GroupName}"" FontWeight=""Bold"" />
                        <ItemsControl ItemsSource=""{Binding Items}"">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <TextBlock Text=""{Binding Name}"" Margin=""10,0,0,0"" />
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </StackPanel>
                </Border>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("Nested DataTemplates should convert");
        result.OutputXaml.Should().Contain("DataTemplate", "DataTemplate should be preserved");
    }

    [Fact]
    public void Transform_DataTemplate_WithDataTemplateSelector()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 xmlns:local=""clr-namespace:MyApp"">
    <Window.Resources>
        <local:PersonDataTemplateSelector x:Key=""PersonTemplateSelector"">
            <local:PersonDataTemplateSelector.AdultTemplate>
                <DataTemplate>
                    <TextBlock Text=""{Binding Name}"" Foreground=""Blue"" />
                </DataTemplate>
            </local:PersonDataTemplateSelector.AdultTemplate>
            <local:PersonDataTemplateSelector.ChildTemplate>
                <DataTemplate>
                    <TextBlock Text=""{Binding Name}"" Foreground=""Green"" />
                </DataTemplate>
            </local:PersonDataTemplateSelector.ChildTemplate>
        </local:PersonDataTemplateSelector>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("DataTemplateSelector should convert");
        // DataTemplateSelector is C# code, XAML may need adjustments
    }
}
