using FluentAssertions;
using WpfToAvalonia.XamlParser;

namespace WpfToAvalonia.Tests.IntegrationTests;

/// <summary>
/// Integration tests for binding transformation rules.
/// Tests WPF → Avalonia binding syntax transformations.
/// </summary>
public class BindingTransformationTests
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
    public void Transform_BasicBinding_PreservesBindingSyntax()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock Text=""{Binding UserName}"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <TextBlock Text=""{Binding UserName}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "the binding syntax should be preserved in the conversion");
    }

    [Fact]
    public void Transform_BindingWithMode_PreservesMode()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox Text=""{Binding UserName, Mode=TwoWay}"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <TextBox Text=""{Binding UserName, Mode=TwoWay}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "the binding mode should be preserved");
    }

    [Fact]
    public void Transform_BindingWithUpdateSourceTrigger_RemovesParameter()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox Text=""{Binding UserName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <TextBox Text=""{Binding UserName, Mode=TwoWay}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "UpdateSourceTrigger should be removed as it's the default in Avalonia");
    }

    [Fact]
    public void Transform_ElementNameBinding_PreservesElementName()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <StackPanel>
        <TextBox x:Name=""MyTextBox"" Text=""Hello"" />
        <TextBlock Text=""{Binding ElementName=MyTextBox, Path=Text}"" />
    </StackPanel>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <StackPanel>
    <TextBox x:Name=""MyTextBox"" Text=""Hello"" />
    <TextBlock Text=""{Binding ElementName=MyTextBox, Path=Text}"" />
  </StackPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "ElementName binding should be preserved");
    }

    [Fact]
    public void Transform_ElementNameBinding_WithAvaloniaStyle_ConvertsToHashSyntax()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <StackPanel>
        <TextBox x:Name=""MyTextBox"" Text=""Hello"" />
        <TextBlock Text=""{Binding ElementName=MyTextBox, Path=Text}"" />
    </StackPanel>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <StackPanel>
    <TextBox x:Name=""MyTextBox"" Text=""Hello"" />
    <TextBlock Text=""{Binding #MyTextBox.Text}"" />
  </StackPanel>
</Window>";

        var options = new ConversionOptions
        {
            UseAvaloniaBindingSyntax = true
        };

        // Act
        var result = converter.Convert(wpfXaml, null, options);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "when UseAvaloniaBindingSyntax is true, should convert to # syntax");
    }

    [Fact]
    public void Transform_RelativeSourceSelfBinding_ConvertsToSelfSyntax()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Border Width=""{Binding RelativeSource={RelativeSource Self}, Path=ActualHeight}"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <Border Width=""{Binding $self.ActualHeight}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "RelativeSource Self should transform to $self syntax");
    }

    [Fact]
    public void Transform_RelativeSourceFindAncestor_ConvertsToParentSyntax()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <StackPanel>
        <TextBlock Text=""{Binding RelativeSource={RelativeSource FindAncestor, AncestorType=Window}, Path=Title}"" />
    </StackPanel>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <StackPanel>
    <TextBlock Text=""{Binding $parent[Window].Title}"" />
  </StackPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "FindAncestor should transform to $parent[Type] syntax");
    }

    [Fact]
    public void Transform_BindingWithConverter_PreservesConverter()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock Text=""{Binding IsActive, Converter={StaticResource BoolToVisibilityConverter}}"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <TextBlock Text=""{Binding IsActive, Converter={StaticResource BoolToVisibilityConverter}}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "converter references should be preserved");
    }

    [Fact]
    public void Transform_BindingWithStringFormat_PreservesStringFormat()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock Text=""{Binding Price, StringFormat='${0:F2}'}"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <TextBlock Text=""{Binding Price, StringFormat='${0:F2}'}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "StringFormat should be preserved");
    }

    [Fact]
    public void Transform_MultiBinding_PreservesStructure()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock>
        <TextBlock.Text>
            <MultiBinding StringFormat=""{0} {1}"">
                <Binding Path=""FirstName"" />
                <Binding Path=""LastName"" />
            </MultiBinding>
        </TextBlock.Text>
    </TextBlock>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <TextBlock>
    <TextBlock.Text>
      <MultiBinding StringFormat=""{0} {1}"">
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
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "MultiBinding structure should be preserved");
    }

    [Fact]
    public void Transform_CompiledBindingOption_ConvertsToCompiledBinding()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock Text=""{Binding UserName}"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <TextBlock Text=""{CompiledBinding UserName}"" />
</Window>";

        var options = new ConversionOptions
        {
            UseCompiledBindings = true
        };

        // Act
        var result = converter.Convert(wpfXaml, null, options);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "when UseCompiledBindings is true, should convert to CompiledBinding");
    }

    [Fact]
    public void Transform_BindingWithPath_PreservesPath()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock Text=""{Binding Path=User.Address.Street}"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <TextBlock Text=""{Binding Path=User.Address.Street}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "complex binding paths should be preserved");
    }

    [Fact]
    public void Transform_BindingWithVisibilityPath_TransformsToIsVisible()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Border Visibility=""{Binding Parent.Visibility}"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <Border IsVisible=""{Binding Parent.IsVisible}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "Visibility property in bindings should be transformed to IsVisible");
    }

    [Fact]
    public void Transform_BindingWithFallbackValue_PreservesFallback()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock Text=""{Binding UserName, FallbackValue='Unknown'}"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <TextBlock Text=""{Binding UserName, FallbackValue='Unknown'}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "FallbackValue should be preserved");
    }

    [Fact]
    public void Transform_BindingWithTargetNullValue_PreservesTargetNullValue()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock Text=""{Binding UserName, TargetNullValue='N/A'}"" />
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <TextBlock Text=""{Binding UserName, TargetNullValue='N/A'}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "TargetNullValue should be preserved");
    }

    [Fact]
    public void Transform_ComplexBindingScenario_HandlesAllFeatures()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Grid>
        <TextBox x:Name=""InputBox"" Text=""{Binding InputValue, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"" />
        <TextBlock Text=""{Binding ElementName=InputBox, Path=Text}"" />
        <Border Width=""{Binding RelativeSource={RelativeSource Self}, Path=ActualHeight}"" />
    </Grid>
</Window>";

        var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Grid>
    <TextBox x:Name=""InputBox"" Text=""{Binding InputValue, Mode=TwoWay}"" />
    <TextBlock Text=""{Binding ElementName=InputBox, Path=Text}"" />
    <Border Width=""{Binding $self.ActualHeight}"" />
  </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
            "complex binding scenarios should be handled correctly with UpdateSourceTrigger removed and RelativeSource transformed");
    }
}
