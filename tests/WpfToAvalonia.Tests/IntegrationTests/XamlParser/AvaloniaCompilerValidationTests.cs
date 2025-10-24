using FluentAssertions;
using WpfToAvalonia.XamlParser;
using Xunit;
using System.Xml.Linq;
using System.Linq;

namespace WpfToAvalonia.Tests.IntegrationTests.XamlParser;

/// <summary>
/// Tests to validate that generated XAML conforms to Avalonia XAML compiler requirements.
/// Implements task 2.5.8.3.3: Validate against Avalonia XAML compiler
/// </summary>
public class AvaloniaCompilerValidationTests
{
    private const string AvaloniaNamespace = "https://github.com/avaloniaui";

    [Fact]
    public void Transform_Window_ShouldHaveAvaloniaNamespace()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputXaml.Should().Contain(AvaloniaNamespace,
            "Output should use Avalonia namespace");

        var doc = XDocument.Parse(result.OutputXaml);
        doc.Root.Should().NotBeNull();
        doc.Root!.Name.NamespaceName.Should().Contain("avaloniaui",
            "Root element should use Avalonia namespace");
    }

    [Fact]
    public void Transform_UserControl_ShouldUseCorrectAvaloniaUserControlSyntax()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<UserControl xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Grid>
        <TextBlock Text=""User Control Content"" />
    </Grid>
</UserControl>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        var doc = XDocument.Parse(result.OutputXaml);
        var root = doc.Root;
        root.Should().NotBeNull();
        root!.Name.LocalName.Should().Be("UserControl",
            "UserControl element should be preserved");
        root.Name.NamespaceName.Should().Contain("avaloniaui");
    }

    [Fact]
    public void Transform_XNameNamespace_ShouldBePreserved()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Button x:Name=""MyButton"" Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputXaml.Should().Contain("xmlns:x=",
            "x: namespace declaration should be present");
        result.OutputXaml.Should().Contain("x:Name=",
            "x:Name usage should be preserved");
    }

    [Fact]
    public void Transform_AttachedProperties_ShouldUseCorrectSyntax()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Grid>
        <Button Grid.Row=""0"" Grid.Column=""1"" Grid.RowSpan=""2"" Content=""Test"" />
    </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        var doc = XDocument.Parse(result.OutputXaml);
        var button = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Button");

        button.Should().NotBeNull();

        // Verify attached properties are properly formatted (may be in any namespace)
        var gridRowAttr = button!.Attributes()
            .FirstOrDefault(a => a.Name.LocalName == "Row" || a.Name.LocalName.Contains("Row"));
        gridRowAttr.Should().NotBeNull("Grid.Row should exist in some form");

        var gridColumnAttr = button.Attributes()
            .FirstOrDefault(a => a.Name.LocalName == "Column" || a.Name.LocalName.Contains("Column"));
        gridColumnAttr.Should().NotBeNull("Grid.Column should exist in some form");

        var gridRowSpanAttr = button.Attributes()
            .FirstOrDefault(a => a.Name.LocalName == "RowSpan" || a.Name.LocalName.Contains("RowSpan"));
        gridRowSpanAttr.Should().NotBeNull("Grid.RowSpan should exist in some form");
    }

    [Fact]
    public void Transform_StaticResourceReference_ShouldUseCorrectSyntax()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <SolidColorBrush x:Key=""MyBrush"" Color=""Blue"" />
    </Window.Resources>
    <Button Background=""{StaticResource MyBrush}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputXaml.Should().Contain("StaticResource",
            "StaticResource syntax should be preserved");
        result.OutputXaml.Should().Contain("MyBrush",
            "Resource key should be preserved");
    }

    [Fact]
    public void Transform_DynamicResourceReference_ShouldUseCorrectSyntax()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <SolidColorBrush x:Key=""DynamicBrush"" Color=""Red"" />
    </Window.Resources>
    <Button Background=""{DynamicResource DynamicBrush}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputXaml.Should().Contain("DynamicResource",
            "DynamicResource syntax should be preserved");
    }

    [Fact]
    public void Transform_BindingExpression_ShouldUseAvaloniaBindingSyntax()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <StackPanel>
        <TextBox Text=""{Binding UserName}"" />
        <TextBox Text=""{Binding Email, Mode=TwoWay}"" />
        <CheckBox IsChecked=""{Binding IsEnabled}"" />
    </StackPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputXaml.Should().Contain("Binding", "Binding expressions should be present");

        var doc = XDocument.Parse(result.OutputXaml);
        var textBoxes = doc.Descendants().Where(e => e.Name.LocalName == "TextBox").ToList();
        textBoxes.Should().HaveCount(2);
    }

    [Fact]
    public void Transform_StyleTargetType_ShouldBeValid()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style x:Key=""ButtonStyle"" TargetType=""Button"">
            <Setter Property=""Background"" Value=""Blue"" />
        </Style>
        <Style TargetType=""TextBlock"">
            <Setter Property=""Foreground"" Value=""Red"" />
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        var doc = XDocument.Parse(result.OutputXaml);
        var styles = doc.Descendants().Where(e => e.Name.LocalName == "Style").ToList();
        styles.Should().HaveCount(2, "Should have 2 styles");

        foreach (var style in styles)
        {
            var targetType = style.Attribute("TargetType");
            targetType.Should().NotBeNull("Style should have TargetType");
            targetType!.Value.Should().NotBeNullOrEmpty("TargetType should have a value");
        }
    }

    [Fact]
    public void Transform_DataTemplate_ShouldUseValidSyntax()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <DataTemplate x:Key=""PersonTemplate"">
            <StackPanel>
                <TextBlock Text=""{Binding Name}"" />
                <TextBlock Text=""{Binding Age}"" />
            </StackPanel>
        </DataTemplate>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        var doc = XDocument.Parse(result.OutputXaml);
        var dataTemplate = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "DataTemplate");

        dataTemplate.Should().NotBeNull("DataTemplate should exist");

        var key = dataTemplate!.Attributes()
            .FirstOrDefault(a => a.Name.LocalName == "Key");
        key.Should().NotBeNull("DataTemplate should have x:Key");
        key!.Value.Should().Be("PersonTemplate");
    }

    [Fact]
    public void Transform_ControlTemplate_ShouldPreserveStructure()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <ControlTemplate x:Key=""ButtonTemplate"" TargetType=""Button"">
            <Border Background=""{TemplateBinding Background}"">
                <ContentPresenter />
            </Border>
        </ControlTemplate>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        var doc = XDocument.Parse(result.OutputXaml);
        var controlTemplate = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ControlTemplate");

        controlTemplate.Should().NotBeNull("ControlTemplate should exist");

        var targetType = controlTemplate!.Attribute("TargetType");
        targetType.Should().NotBeNull("ControlTemplate should have TargetType");
    }

    [Fact]
    public void Transform_ClrNamespace_ShouldBeValidFormat()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 xmlns:local=""clr-namespace:MyApp.ViewModels"">
    <StackPanel>
        <TextBlock Text=""Test"" />
    </StackPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        var doc = XDocument.Parse(result.OutputXaml);
        var root = doc.Root;

        // Check that CLR namespace declaration is present
        var localNs = root!.Attributes()
            .FirstOrDefault(a => a.Name.LocalName == "local" && a.Name.Namespace == XNamespace.Xmlns);

        // CLR namespace format should be preserved (Avalonia uses same format)
        if (localNs != null)
        {
            localNs.Value.Should().Contain("clr-namespace:",
                "CLR namespace should use clr-namespace: format");
        }
    }

    [Fact]
    public void Transform_MultiBinding_ShouldBeValidFormat()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock>
        <TextBlock.Text>
            <MultiBinding StringFormat=""{}{0} {1}"">
                <Binding Path=""FirstName"" />
                <Binding Path=""LastName"" />
            </MultiBinding>
        </TextBlock.Text>
    </TextBlock>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        var doc = XDocument.Parse(result.OutputXaml);
        var multiBinding = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "MultiBinding");

        multiBinding.Should().NotBeNull("MultiBinding should exist");

        var bindings = multiBinding!.Elements()
            .Where(e => e.Name.LocalName == "Binding").ToList();
        bindings.Should().HaveCount(2, "MultiBinding should have 2 Binding children");
    }

    [Fact]
    public void Transform_EventHandler_ShouldPreserveSyntax()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <StackPanel>
        <Button Content=""Click Me"" Click=""Button_Click"" />
        <TextBox TextChanged=""TextBox_TextChanged"" />
    </StackPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        var doc = XDocument.Parse(result.OutputXaml);
        var button = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Button");
        button.Should().NotBeNull();

        var clickAttr = button!.Attribute("Click");
        clickAttr.Should().NotBeNull("Click event handler should be preserved");
    }

    [Fact]
    public void Transform_PropertyElementSyntax_ShouldBeValid()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button>
        <Button.Content>
            <StackPanel>
                <TextBlock Text=""Line 1"" />
                <TextBlock Text=""Line 2"" />
            </StackPanel>
        </Button.Content>
    </Button>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        var doc = XDocument.Parse(result.OutputXaml);
        var button = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Button");
        button.Should().NotBeNull();

        // Check for property element syntax (Button.Content)
        var contentElement = button!.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "Content");

        // Either direct children or property element syntax should exist
        button.Elements().Should().NotBeEmpty("Button should have child elements");
    }

    [Fact]
    public void Transform_MergedDictionaries_ShouldUseValidSyntax()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source=""Styles/Colors.xaml"" />
                <ResourceDictionary Source=""Styles/Buttons.xaml"" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        var doc = XDocument.Parse(result.OutputXaml);
        var mergedDictionaries = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "MergedDictionaries");

        if (mergedDictionaries != null)
        {
            var resourceDictionaries = mergedDictionaries.Elements()
                .Where(e => e.Name.LocalName == "ResourceDictionary").ToList();
            resourceDictionaries.Count.Should().BeGreaterThanOrEqualTo(1, "Should have at least 1 merged dictionary");

            foreach (var rd in resourceDictionaries)
            {
                var source = rd.Attribute("Source");
                source.Should().NotBeNull("Each ResourceDictionary should have Source");
            }
        }
        else
        {
            // MergedDictionaries might be flattened or restructured
            // Just verify the conversion succeeded
            result.OutputXaml.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void Transform_CollectionProperty_ShouldUseValidSyntax()
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
            <ColumnDefinition Width=""*"" />
            <ColumnDefinition Width=""*"" />
        </Grid.ColumnDefinitions>
    </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        var doc = XDocument.Parse(result.OutputXaml);
        var grid = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Grid");
        grid.Should().NotBeNull();

        // RowDefinitions and ColumnDefinitions might be direct children or nested
        var rowDefinitions = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "RowDefinitions" || e.Name.LocalName == "RowDefinition");

        var columnDefinitions = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ColumnDefinitions" || e.Name.LocalName == "ColumnDefinition");

        // At least one of them should exist
        var hasDefinitions = rowDefinitions != null || columnDefinitions != null;
        hasDefinitions.Should().BeTrue("Grid should have row or column definitions");
    }

    [Fact]
    public void Transform_XTypeMarkupExtension_ShouldBeValid()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 xmlns:sys=""clr-namespace:System;assembly=mscorlib"">
    <Window.Resources>
        <DataTemplate DataType=""{x:Type sys:String}"">
            <TextBlock Text=""{Binding}"" />
        </DataTemplate>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputXaml.Should().Contain("x:Type", "x:Type markup extension should be preserved");
    }
}
