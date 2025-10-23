using FluentAssertions;
using WpfToAvalonia.XamlParser;

namespace WpfToAvalonia.Tests.IntegrationTests;

/// <summary>
/// Integration tests for style and template transformation rules.
/// Tests WPF → Avalonia style, trigger, and template transformations.
/// </summary>
public class StyleTransformationTests
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
    public void Transform_BasicStyle_PreservesStructure()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Setter Property=""Background"" Value=""Blue"" />
            <Setter Property=""Foreground"" Value=""White"" />
        </Style>
    </Window.Resources>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Window.Resources>
    <Style TargetType=""Button"">
      <Setter Property=""Background"" Value=""Blue"" />
      <Setter Property=""Foreground"" Value=""White"" />
    </Style>
  </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "basic style structure with setters should be preserved");
    }

    [Fact]
    public void Transform_StyleWithKey_PreservesKey()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style x:Key=""MyButtonStyle"" TargetType=""Button"">
            <Setter Property=""Background"" Value=""Blue"" />
        </Style>
    </Window.Resources>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Window.Resources>
    <Style x:Key=""MyButtonStyle"" TargetType=""Button"">
      <Setter Property=""Background"" Value=""Blue"" />
    </Style>
  </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "style x:Key should be preserved");
    }

    [Fact]
    public void Transform_StyleWithBasedOn_PreservesInheritance()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style x:Key=""BaseButtonStyle"" TargetType=""Button"">
            <Setter Property=""Padding"" Value=""10"" />
        </Style>
        <Style x:Key=""DerivedButtonStyle"" TargetType=""Button"" BasedOn=""{StaticResource BaseButtonStyle}"">
            <Setter Property=""Background"" Value=""Blue"" />
        </Style>
    </Window.Resources>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Window.Resources>
    <Window.Resources>
      <Style x:Key=""BaseButtonStyle"" TargetType=""Button"">
        <Setter Property=""Padding"" Value=""10"" />
      </Style>
      <Style x:Key=""DerivedButtonStyle"" TargetType=""Button"" BasedOn=""{StaticResource BaseButtonStyle}"">
        <Setter Property=""Background"" Value=""Blue"" />
      </Style>
    </Window.Resources>
  </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "BasedOn style inheritance should be preserved");
    }

    [Fact]
    public void Transform_SetterWithVisibility_TransformsToIsVisible()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Setter Property=""Visibility"" Value=""Visible"" />
        </Style>
    </Window.Resources>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Window.Resources>
    <Style TargetType=""Button"">
      <Setter Property=""IsVisible"" Value=""Visible"" />
    </Style>
  </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "Visibility property in setter should transform to IsVisible");
    }

    [Fact]
    public void Transform_SetterWithToolTip_TransformsToToolTipTip()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Setter Property=""ToolTip"" Value=""Click me"" />
        </Style>
    </Window.Resources>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Window.Resources>
    <Style TargetType=""Button"">
      <Setter Property=""ToolTip.Tip"" Value=""Click me"" />
    </Style>
  </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "ToolTip property in setter should transform to ToolTip.Tip");
    }

    [Fact]
    public void Transform_PropertyTrigger_AddsWarning()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <Trigger Property=""IsMouseOver"" Value=""True"">
                    <Setter Property=""Background"" Value=""Red"" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Window.Resources>
    <Window.Resources>
      <Style TargetType=""Button"">
        <Style.Triggers>
          <Trigger Property=""IsMouseOver"" Value=""True"">
            <Setter Property=""Background"" Value=""Red"" />
          </Trigger>
        </Style.Triggers>
      </Style>
      <Style Selector=""Button:pointerover"">
        <Setter Property=""Background"" Value=""Red"" />
      </Style>
    </Window.Resources>
  </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "triggers should be preserved but generate diagnostic warnings");
    }

    [Fact]
    public void Transform_DataTrigger_AddsWarning()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <DataTrigger Binding=""{Binding IsActive}"" Value=""True"">
                    <Setter Property=""Background"" Value=""Green"" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d => d.Severity == Core.Diagnostics.DiagnosticSeverity.Warning,
            "DataTriggers should generate warnings about manual conversion");
    }

    [Fact]
    public void Transform_ResourceDictionary_PreservesStructure()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <SolidColorBrush x:Key=""PrimaryBrush"" Color=""#FF0000"" />
    <Style TargetType=""Button"">
        <Setter Property=""Background"" Value=""{StaticResource PrimaryBrush}"" />
    </Style>
</ResourceDictionary>";

        var expectedXaml = @"<ResourceDictionary xmlns=""https://github.com/avaloniaui""
                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <SolidColorBrush x:Key=""PrimaryBrush"" Color=""#FF0000"" />
  <Style TargetType=""Button"">
    <Setter Property=""Background"" Value=""{StaticResource PrimaryBrush}"" />
  </Style>
</ResourceDictionary>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "ResourceDictionary structure should be preserved");
    }

    [Fact]
    public void Transform_MergedDictionaries_PreservesStructure()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source=""Styles/ButtonStyles.xaml"" />
        <ResourceDictionary Source=""Styles/TextStyles.xaml"" />
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>";

        var expectedXaml = @"<ResourceDictionary xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <ResourceDictionary.MergedDictionaries>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source=""Styles/ButtonStyles.xaml"" />
      <ResourceDictionary Source=""Styles/TextStyles.xaml"" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "MergedDictionaries should be preserved");
    }

    [Fact]
    public void Transform_ControlTemplate_PreservesStructure()
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

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
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
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "ControlTemplate structure should be preserved");
    }

    [Fact]
    public void Transform_TemplateBinding_PreservesBinding()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <ControlTemplate TargetType=""Button"">
            <Border Background=""{TemplateBinding Background}""
                    BorderBrush=""{TemplateBinding BorderBrush}""
                    BorderThickness=""{TemplateBinding BorderThickness}"">
                <ContentPresenter />
            </Border>
        </ControlTemplate>
    </Window.Resources>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Window.Resources>
    <ControlTemplate TargetType=""Button"">
      <Border Background=""{TemplateBinding Background}""
              BorderBrush=""{TemplateBinding BorderBrush}""
              BorderThickness=""{TemplateBinding BorderThickness}"">
        <ContentPresenter />
      </Border>
    </ControlTemplate>
  </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "TemplateBinding expressions should be preserved");
    }

    [Fact]
    public void Transform_DataTemplate_PreservesStructure()
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

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
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
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "DataTemplate structure should be preserved");
    }

    [Fact]
    public void Transform_ImplicitDataTemplate_PreservesDataType()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                xmlns:local=""clr-namespace:MyApp.Models"">
    <Window.Resources>
        <DataTemplate DataType=""{x:Type local:Person}"">
            <TextBlock Text=""{Binding Name}"" />
        </DataTemplate>
    </Window.Resources>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Window.Resources>
    <DataTemplate DataType=""{x:Type local:Person}"">
      <TextBlock Text=""{Binding Name}"" />
    </DataTemplate>
  </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "implicit DataTemplate with DataType should be preserved");
    }

    [Fact]
    public void Transform_ComplexStyle_HandlesAllFeatures()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style x:Key=""PrimaryButton"" TargetType=""Button"">
            <Setter Property=""Background"" Value=""#007ACC"" />
            <Setter Property=""Foreground"" Value=""White"" />
            <Setter Property=""Padding"" Value=""10,5"" />
            <Setter Property=""BorderThickness"" Value=""0"" />
            <Setter Property=""FontSize"" Value=""14"" />
            <Setter Property=""FontWeight"" Value=""SemiBold"" />
        </Style>
    </Window.Resources>
    <Button Style=""{StaticResource PrimaryButton}"" Content=""Click Me"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Window.Resources>
    <Style x:Key=""PrimaryButton"" TargetType=""Button"">
      <Setter Property=""Background"" Value=""#007ACC"" />
      <Setter Property=""Foreground"" Value=""White"" />
      <Setter Property=""Padding"" Value=""10,5"" />
      <Setter Property=""BorderThickness"" Value=""0"" />
      <Setter Property=""FontSize"" Value=""14"" />
      <Setter Property=""FontWeight"" Value=""SemiBold"" />
    </Style>
  </Window.Resources>
  <Button Style=""{StaticResource PrimaryButton}"" Content=""Click Me"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "complex style with multiple setters and usage should be preserved");
    }

    [Fact]
    public void Transform_StyleWithMultipleSetters_PreservesAll()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Window.Resources>
        <Style TargetType=""TextBlock"">
            <Setter Property=""FontFamily"" Value=""Arial"" />
            <Setter Property=""FontSize"" Value=""12"" />
            <Setter Property=""Foreground"" Value=""Black"" />
            <Setter Property=""Margin"" Value=""5"" />
        </Style>
    </Window.Resources>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Window.Resources>
    <Style TargetType=""TextBlock"">
      <Setter Property=""FontFamily"" Value=""Arial"" />
      <Setter Property=""FontSize"" Value=""12"" />
      <Setter Property=""Foreground"" Value=""Black"" />
      <Setter Property=""Margin"" Value=""5"" />
    </Style>
  </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "all setters in style should be preserved");
    }
}
