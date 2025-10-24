using FluentAssertions;
using WpfToAvalonia.XamlParser;
using Xunit;

namespace WpfToAvalonia.Tests.UnitTests.XamlParser;

/// <summary>
/// Unit tests for resource resolution in XAML parsing.
/// Implements task 2.5.8.1.3: Test resource resolution
/// </summary>
public class ResourceResolutionTests
{
    [Fact]
    public void Parse_WindowResources_Should_BeRecognized()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <SolidColorBrush x:Key=""MyBrush"" Color=""Red"" />
    </Window.Resources>
    <Button Background=""{StaticResource MyBrush}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Window.Resources should be parsed");
        result.OutputXaml.Should().Contain("Resources", "Resources section should be present");
    }

    [Fact]
    public void Parse_ApplicationResources_Should_BeRecognized()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Application xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Application.Resources>
        <SolidColorBrush x:Key=""AppBrush"" Color=""Blue"" />
    </Application.Resources>
</Application>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Application.Resources should be parsed");
        result.OutputXaml.Should().Contain("Resources", "Resources section should be preserved");
    }

    [Fact]
    public void Parse_ResourceDictionary_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                             xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <SolidColorBrush x:Key=""PrimaryBrush"" Color=""Green"" />
    <SolidColorBrush x:Key=""SecondaryBrush"" Color=""Yellow"" />
</ResourceDictionary>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("ResourceDictionary should be parsed");
        result.OutputXaml.Should().Contain("ResourceDictionary", "ResourceDictionary should be present");
    }

    [Fact]
    public void Parse_MergedDictionaries_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source=""Styles.xaml"" />
                <ResourceDictionary Source=""Colors.xaml"" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("MergedDictionaries should be parsed");
        result.OutputXaml.Should().Contain("MergedDictionaries", "MergedDictionaries should be recognized");
    }

    [Fact]
    public void Parse_StyleResource_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style x:Key=""MyButtonStyle"" TargetType=""Button"">
            <Setter Property=""Background"" Value=""Red"" />
        </Style>
    </Window.Resources>
    <Button Style=""{StaticResource MyButtonStyle}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Style resource should be parsed");
        result.OutputXaml.Should().Contain("Style", "Style should be present");
    }

    [Fact]
    public void Parse_DataTemplateResource_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <DataTemplate x:Key=""MyTemplate"">
            <TextBlock Text=""{Binding Name}"" />
        </DataTemplate>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("DataTemplate resource should be parsed");
        result.OutputXaml.Should().Contain("DataTemplate", "DataTemplate should be present");
    }

    [Fact]
    public void Parse_ControlTemplateResource_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <ControlTemplate x:Key=""MyControlTemplate"" TargetType=""Button"">
            <Border Background=""Blue"">
                <ContentPresenter />
            </Border>
        </ControlTemplate>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("ControlTemplate resource should be parsed");
        result.OutputXaml.Should().Contain("ControlTemplate", "ControlTemplate should be present");
    }

    [Fact]
    public void Parse_ColorResource_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Color x:Key=""PrimaryColor"">#FF0000</Color>
        <SolidColorBrush x:Key=""PrimaryBrush"" Color=""{StaticResource PrimaryColor}"" />
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Color resource should be parsed");
    }

    [Fact]
    public void Parse_ConverterResource_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 xmlns:local=""clr-namespace:MyApp"">
    <Window.Resources>
        <local:BooleanToVisibilityConverter x:Key=""BoolToVisConverter"" />
    </Window.Resources>
    <Button Visibility=""{Binding IsVisible, Converter={StaticResource BoolToVisConverter}}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Converter resource should be parsed");
    }

    [Fact]
    public void Parse_GeometryResource_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <PathGeometry x:Key=""MyPath"" Figures=""M 0,0 L 100,100"" />
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Geometry resource should be parsed");
    }

    [Fact]
    public void Parse_StoryboardResource_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Storyboard x:Key=""MyAnimation"">
            <DoubleAnimation Storyboard.TargetProperty=""Opacity"" From=""0"" To=""1"" Duration=""0:0:1"" />
        </Storyboard>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Storyboard resource should be parsed");
    }

    [Fact]
    public void Parse_ImplicitStyle_NoKey_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Setter Property=""Background"" Value=""Blue"" />
        </Style>
    </Window.Resources>
    <Button Content=""Implicit Style"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Implicit style (no x:Key) should be parsed");
        result.OutputXaml.Should().Contain("Style", "Style should be present");
    }

    [Fact]
    public void Parse_NestedResources_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <ResourceDictionary>
            <SolidColorBrush x:Key=""Brush1"" Color=""Red"" />
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary>
                    <SolidColorBrush x:Key=""Brush2"" Color=""Blue"" />
                </ResourceDictionary>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Nested resources should be parsed");
    }

    [Fact]
    public void Parse_TypedResource_WithXType_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 xmlns:sys=""clr-namespace:System;assembly=mscorlib"">
    <Window.Resources>
        <sys:String x:Key=""MyString"">Hello World</sys:String>
        <sys:Double x:Key=""MyDouble"">123.45</sys:Double>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Typed resources should be parsed");
    }

    [Fact]
    public void Parse_ResourceWithDynamicReference_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <SolidColorBrush x:Key=""MyBrush"" Color=""Red"" />
    </Window.Resources>
    <Button Background=""{DynamicResource MyBrush}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("DynamicResource reference should be parsed");
        result.OutputXaml.Should().Contain("DynamicResource", "DynamicResource should be preserved");
    }

    [Fact]
    public void Parse_FrameworkElementResources_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <StackPanel>
        <StackPanel.Resources>
            <SolidColorBrush x:Key=""LocalBrush"" Color=""Green"" />
        </StackPanel.Resources>
        <Button Background=""{StaticResource LocalBrush}"" />
    </StackPanel>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("FrameworkElement.Resources should be parsed");
    }

    [Fact]
    public void Parse_ResourceWithComplexValue_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <LinearGradientBrush x:Key=""GradientBrush"" StartPoint=""0,0"" EndPoint=""1,1"">
            <GradientStop Color=""Red"" Offset=""0"" />
            <GradientStop Color=""Blue"" Offset=""1"" />
        </LinearGradientBrush>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Resource with complex value should be parsed");
    }

    [Fact]
    public void Parse_ResourceReferenceInSetter_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <SolidColorBrush x:Key=""MyBrush"" Color=""Red"" />
        <Style TargetType=""Button"">
            <Setter Property=""Background"" Value=""{StaticResource MyBrush}"" />
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Resource reference in Setter should be parsed");
    }
}
