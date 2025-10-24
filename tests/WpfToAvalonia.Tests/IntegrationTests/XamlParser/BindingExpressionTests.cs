using FluentAssertions;
using WpfToAvalonia.XamlParser;
using Xunit;

namespace WpfToAvalonia.Tests.IntegrationTests.XamlParser;

/// <summary>
/// Integration tests for binding expressions.
/// Implements task 2.5.8.2.4: Test binding expressions
/// </summary>
public class BindingExpressionTests
{
    [Fact]
    public void Transform_ComplexBindingScenario()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 xmlns:local=""clr-namespace:MyApp"">
    <Window.Resources>
        <local:BoolToVisibilityConverter x:Key=""BoolToVis"" />
    </Window.Resources>
    <Grid>
        <TextBox x:Name=""InputBox"" Text=""{Binding UserInput, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"" />
        <TextBlock Text=""{Binding ElementName=InputBox, Path=Text}"" />
        <Button IsEnabled=""{Binding CanSubmit}""
                Visibility=""{Binding ShowSubmit, Converter={StaticResource BoolToVis}}"" />
    </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputXaml.Should().Contain("Binding");
    }

    [Fact]
    public void Transform_MultiBinding_WithStringFormat()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
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
        result.OutputXaml.Should().Contain("MultiBinding");
    }

    [Fact]
    public void Transform_ValidationRules_InBinding()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <TextBox>
        <TextBox.Text>
            <Binding Path=""Age"" UpdateSourceTrigger=""PropertyChanged""
                    ValidatesOnDataErrors=""True"" ValidatesOnExceptions=""True"">
                <Binding.ValidationRules>
                    <ExceptionValidationRule />
                </Binding.ValidationRules>
            </Binding>
        </TextBox.Text>
    </TextBox>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        // ValidationRules are a WPF-specific feature that may or may not generate diagnostics
        // We just verify the conversion succeeds
        result.OutputXaml.Should().NotBeNullOrEmpty();
    }
}
