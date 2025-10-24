using FluentAssertions;
using WpfToAvalonia.XamlParser;
using Xunit;

namespace WpfToAvalonia.Tests.IntegrationTests.XamlParser;

/// <summary>
/// Integration tests for styles and resource dictionaries.
/// Implements task 2.5.8.2.3: Test style and resource dictionaries
/// </summary>
public class StyleAndResourceTests
{
    [Fact]
    public void Transform_Style_WithSetters()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style x:Key=""MyButtonStyle"" TargetType=""Button"">
            <Setter Property=""Background"" Value=""Blue"" />
            <Setter Property=""Foreground"" Value=""White"" />
            <Setter Property=""Padding"" Value=""10,5"" />
        </Style>
    </Window.Resources>
    <Button Style=""{StaticResource MyButtonStyle}"" Content=""Styled Button"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputXaml.Should().Contain("Style");
        result.OutputXaml.Should().Contain("Setter");
    }

    [Fact]
    public void Transform_Style_WithBasedOn()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style x:Key=""BaseButtonStyle"" TargetType=""Button"">
            <Setter Property=""FontSize"" Value=""14"" />
        </Style>
        <Style x:Key=""DerivedStyle"" TargetType=""Button"" BasedOn=""{StaticResource BaseButtonStyle}"">
            <Setter Property=""Background"" Value=""Red"" />
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputXaml.Should().Contain("BasedOn");
    }

    [Fact]
    public void Transform_MergedResourceDictionaries()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source=""Styles/ButtonStyles.xaml"" />
                <ResourceDictionary Source=""Styles/Colors.xaml"" />
            </ResourceDictionary.MergedDictionaries>
            <SolidColorBrush x:Key=""LocalBrush"" Color=""Green"" />
        </ResourceDictionary>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputXaml.Should().Contain("MergedDictionaries");
    }

    [Fact]
    public void Transform_ComplexResourceDictionary()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                             xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Color x:Key=""PrimaryColor"">#007ACC</Color>
    <SolidColorBrush x:Key=""PrimaryBrush"" Color=""{StaticResource PrimaryColor}"" />

    <Style x:Key=""PrimaryButton"" TargetType=""Button"">
        <Setter Property=""Background"" Value=""{StaticResource PrimaryBrush}"" />
        <Setter Property=""Foreground"" Value=""White"" />
    </Style>

    <DataTemplate x:Key=""PersonTemplate"">
        <TextBlock Text=""{Binding Name}"" />
    </DataTemplate>
</ResourceDictionary>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputXaml.Should().Contain("ResourceDictionary");
    }
}
