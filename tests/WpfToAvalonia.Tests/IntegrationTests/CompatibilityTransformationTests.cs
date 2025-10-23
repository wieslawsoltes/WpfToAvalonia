using FluentAssertions;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser;
using WpfToAvalonia.XamlParser.Transformation;
using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.Tests.IntegrationTests;

/// <summary>
/// Integration tests for WPF compatibility transformers that convert WPF-specific features
/// to Avalonia equivalents (Triggers → Style Selectors, etc.)
/// </summary>
public class CompatibilityTransformationTests
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
    public void Transform_SimpleTrigger_ConvertsToStyleSelector()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
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

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        var expectedPatterns = new[]
        {
            "Selector=",
            ":pointerover",
            "Background",
            "Red"
        };

        foreach (var pattern in expectedPatterns)
        {
            result.OutputXaml.Should().Contain(pattern,
                $"trigger should convert to :pointerover style selector and contain '{pattern}'");
        }
    }

    [Fact]
    public void Transform_PressedTrigger_ConvertsToStyleSelector()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <Trigger Property=""IsPressed"" Value=""True"">
                    <Setter Property=""Background"" Value=""Blue"" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        var expectedPatterns = new[]
        {
            "Selector=",
            ":pressed",
            "Background",
            "Blue"
        };

        foreach (var pattern in expectedPatterns)
        {
            result.OutputXaml.Should().Contain(pattern,
                $"trigger should convert to :pressed style selector and contain '{pattern}'");
        }
    }

    [Fact]
    public void Transform_FocusTrigger_ConvertsToStyleSelector()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""TextBox"">
            <Style.Triggers>
                <Trigger Property=""IsFocused"" Value=""True"">
                    <Setter Property=""BorderBrush"" Value=""Green"" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        var expectedPatterns = new[]
        {
            "Selector=",
            ":focus",
            "BorderBrush",
            "Green"
        };

        foreach (var pattern in expectedPatterns)
        {
            result.OutputXaml.Should().Contain(pattern,
                $"trigger should convert to :focus style selector and contain '{pattern}'");
        }
    }

    [Fact]
    public void Transform_DisabledTrigger_ConvertsToStyleSelector()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <Trigger Property=""IsEnabled"" Value=""False"">
                    <Setter Property=""Foreground"" Value=""Gray"" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        var expectedPatterns = new[]
        {
            "Selector=",
            ":disabled",
            "Foreground",
            "Gray"
        };

        foreach (var pattern in expectedPatterns)
        {
            result.OutputXaml.Should().Contain(pattern,
                $"trigger should convert to :disabled style selector and contain '{pattern}'");
        }
    }

    [Fact]
    public void Transform_MultipleTriggers_ConvertsToMultipleStyleSelectors()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <Trigger Property=""IsMouseOver"" Value=""True"">
                    <Setter Property=""Background"" Value=""Red"" />
                </Trigger>
                <Trigger Property=""IsPressed"" Value=""True"">
                    <Setter Property=""Background"" Value=""Blue"" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        var expectedPatterns = new[]
        {
            "Selector=",
            ":pointerover",
            ":pressed",
            "Background"
        };

        foreach (var pattern in expectedPatterns)
        {
            result.OutputXaml.Should().Contain(pattern,
                $"multiple triggers should convert to multiple style selectors and contain '{pattern}'");
        }
    }

    [Fact]
    public void Transform_SelectedTrigger_ConvertsToStyleSelector()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""ListBoxItem"">
            <Style.Triggers>
                <Trigger Property=""IsSelected"" Value=""True"">
                    <Setter Property=""Background"" Value=""Orange"" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        var expectedPatterns = new[]
        {
            "Selector=",
            ":selected",
            "Background",
            "Orange"
        };

        foreach (var pattern in expectedPatterns)
        {
            result.OutputXaml.Should().Contain(pattern,
                $"trigger should convert to :selected style selector and contain '{pattern}'");
        }
    }

    [Fact]
    public void Transform_CheckedTrigger_ConvertsToStyleSelector()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""CheckBox"">
            <Style.Triggers>
                <Trigger Property=""IsChecked"" Value=""True"">
                    <Setter Property=""Foreground"" Value=""Green"" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        var expectedPatterns = new[]
        {
            "Selector=",
            ":checked",
            "Foreground",
            "Green"
        };

        foreach (var pattern in expectedPatterns)
        {
            result.OutputXaml.Should().Contain(pattern,
                $"trigger should convert to :checked style selector and contain '{pattern}'");
        }
    }

    [Fact]
    public void Transform_StyleWithRegularSettersAndTriggers_PreservesBoth()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Setter Property=""Background"" Value=""White"" />
            <Setter Property=""Foreground"" Value=""Black"" />
            <Style.Triggers>
                <Trigger Property=""IsMouseOver"" Value=""True"">
                    <Setter Property=""Background"" Value=""Red"" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        var expectedPatterns = new[]
        {
            "Selector=",
            ":pointerover",
            "Background",
            "White",
            "Foreground",
            "Black",
            "Red"
        };

        foreach (var pattern in expectedPatterns)
        {
            result.OutputXaml.Should().Contain(pattern,
                $"both regular setters and trigger should be preserved and contain '{pattern}'");
        }
    }

    [Fact]
    public void Transform_UnsupportedTrigger_GeneratesWarning()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <Trigger Property=""CustomProperty"" Value=""True"">
                    <Setter Property=""Background"" Value=""Red"" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        // Should have the trigger but not convert it
        result.OutputXaml.Should().Contain("<Trigger",
            "unsupported triggers should be preserved in output");
    }

    [Fact]
    public void Transform_DataTrigger_SimpleBinding_ProvidesConverterSuggestion()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
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
        result.Diagnostics.Should().Contain(d =>
            d.Code == "DATATRIGGER_CONVERTER_PATTERN" &&
            d.Message.Contains("value converter") &&
            d.Message.Contains("IsActive"),
            "should provide converter pattern suggestion for DataTrigger");
    }

    [Fact]
    public void Transform_DataTrigger_MultipleSetters_ProvidesBehaviorSuggestion()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <DataTrigger Binding=""{Binding IsActive}"" Value=""True"">
                    <Setter Property=""Background"" Value=""Green"" />
                    <Setter Property=""Foreground"" Value=""White"" />
                    <Setter Property=""BorderBrush"" Value=""DarkGreen"" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "DATATRIGGER_BEHAVIOR_PATTERN" &&
            d.Message.Contains("Avalonia.Xaml.Interactivity") &&
            d.Message.Contains("3"),
            "should provide behavior pattern suggestion for DataTrigger with multiple setters");
    }

    [Fact]
    public void Transform_DataTrigger_NoBinding_ProvidesBasicWarning()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <DataTrigger>
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
        result.Diagnostics.Should().Contain(d =>
            d.Code == "DATATRIGGER_NOT_SUPPORTED" &&
            d.Severity == Core.Diagnostics.DiagnosticSeverity.Warning,
            "should provide warning for DataTrigger without binding");
    }

    [Fact]
    public void Transform_DataTrigger_WithPath_ExtractsBindingPath()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <DataTrigger Binding=""{Binding Path=User.IsAdmin}"" Value=""True"">
                    <Setter Property=""Visibility"" Value=""Visible"" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Message.Contains("User.IsAdmin") ||
            d.Message.Contains("converter"),
            "should extract and mention the binding path in diagnostic");
    }

    [Fact]
    public void Transform_DataTrigger_BooleanValue_ProvidesSuggestion()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""TextBlock"">
            <Style.Triggers>
                <DataTrigger Binding=""{Binding HasError}"" Value=""False"">
                    <Setter Property=""Foreground"" Value=""Green"" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code.Contains("DATATRIGGER") &&
            (d.Message.Contains("converter") || d.Message.Contains("behavior")),
            "should provide conversion suggestion for boolean DataTrigger");
    }

    [Fact]
    public void Transform_EventTrigger_MouseEnter_ProvidesStyleAnimationSuggestion()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <EventTrigger RoutedEvent=""MouseEnter"">
                    <BeginStoryboard>
                        <Storyboard>
                            <DoubleAnimation Storyboard.TargetProperty=""Opacity"" To=""0.7"" Duration=""0:0:0.2"" />
                        </Storyboard>
                    </BeginStoryboard>
                </EventTrigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "EVENTTRIGGER_STYLE_ANIMATION" &&
            d.Message.Contains(":pointerover") &&
            d.Message.Contains("Opacity"),
            "should suggest style animation for MouseEnter event");
    }

    [Fact]
    public void Transform_EventTrigger_GotFocus_ProvidesStyleAnimationSuggestion()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""TextBox"">
            <Style.Triggers>
                <EventTrigger RoutedEvent=""GotFocus"">
                    <BeginStoryboard>
                        <Storyboard>
                            <ColorAnimation Storyboard.TargetProperty=""BorderBrush.Color"" To=""Blue"" Duration=""0:0:0.3"" />
                        </Storyboard>
                    </BeginStoryboard>
                </EventTrigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "EVENTTRIGGER_STYLE_ANIMATION" &&
            d.Message.Contains(":focus") &&
            d.Message.Contains("BorderBrush.Color"),
            "should suggest style animation for GotFocus event");
    }

    [Fact]
    public void Transform_EventTrigger_LoadedEvent_ProvidesCodeBehindSuggestion()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Grid"">
            <Style.Triggers>
                <EventTrigger RoutedEvent=""Loaded"">
                    <BeginStoryboard>
                        <Storyboard>
                            <DoubleAnimation Storyboard.TargetProperty=""Opacity"" From=""0"" To=""1"" Duration=""0:0:0.5"" />
                        </Storyboard>
                    </BeginStoryboard>
                </EventTrigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "EVENTTRIGGER_STYLE_ANIMATION" &&
            d.Message.Contains(":loaded") &&
            d.Message.Contains("Opacity"),
            "should suggest animation approach for Loaded event");
    }

    [Fact]
    public void Transform_EventTrigger_NoRoutedEvent_ProvidesBasicWarning()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <EventTrigger>
                    <BeginStoryboard>
                        <Storyboard>
                            <DoubleAnimation Storyboard.TargetProperty=""Opacity"" To=""0.5"" Duration=""0:0:0.2"" />
                        </Storyboard>
                    </BeginStoryboard>
                </EventTrigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "EVENTTRIGGER_NOT_SUPPORTED" &&
            d.Severity == Core.Diagnostics.DiagnosticSeverity.Warning,
            "should provide warning for EventTrigger without RoutedEvent");
    }

    [Fact]
    public void Transform_EventTrigger_MultipleAnimations_ExtractsAllDetails()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <EventTrigger RoutedEvent=""MouseEnter"">
                    <BeginStoryboard>
                        <Storyboard>
                            <DoubleAnimation Storyboard.TargetProperty=""Opacity"" To=""0.8"" Duration=""0:0:0.2"" />
                            <ColorAnimation Storyboard.TargetProperty=""Background.Color"" To=""Red"" Duration=""0:0:0.2"" />
                        </Storyboard>
                    </BeginStoryboard>
                </EventTrigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "EVENTTRIGGER_STYLE_ANIMATION" &&
            d.Message.Contains("Opacity") &&
            d.Message.Contains("Background.Color"),
            "should extract details about all animations in the storyboard");
    }

    [Fact]
    public void Transform_MultiTrigger_AllMappable_ProvidesCompositeSelectorSuggestion()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <MultiTrigger>
                    <MultiTrigger.Conditions>
                        <Condition Property=""IsMouseOver"" Value=""True"" />
                        <Condition Property=""IsPressed"" Value=""True"" />
                    </MultiTrigger.Conditions>
                    <Setter Property=""Background"" Value=""Red"" />
                </MultiTrigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "MULTITRIGGER_COMPOSITE_SELECTOR" &&
            d.Message.Contains(":pointerover:pressed"),
            "should suggest composite selector for mappable MultiTrigger");
    }

    [Fact]
    public void Transform_MultiTrigger_FocusAndSelection_ProvidesCompositeSelectorSuggestion()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""ListBoxItem"">
            <Style.Triggers>
                <MultiTrigger>
                    <MultiTrigger.Conditions>
                        <Condition Property=""IsFocused"" Value=""True"" />
                        <Condition Property=""IsSelected"" Value=""True"" />
                    </MultiTrigger.Conditions>
                    <Setter Property=""BorderBrush"" Value=""Blue"" />
                    <Setter Property=""BorderThickness"" Value=""2"" />
                </MultiTrigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "MULTITRIGGER_COMPOSITE_SELECTOR" &&
            d.Message.Contains(":focus:selected"),
            "should suggest composite selector for focus and selection");
    }

    [Fact]
    public void Transform_MultiTrigger_PartiallyMappable_ProvidesPartialSuggestion()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <MultiTrigger>
                    <MultiTrigger.Conditions>
                        <Condition Property=""IsMouseOver"" Value=""True"" />
                        <Condition Property=""CustomProperty"" Value=""True"" />
                    </MultiTrigger.Conditions>
                    <Setter Property=""Background"" Value=""Green"" />
                </MultiTrigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "MULTITRIGGER_PARTIAL_MAPPING" &&
            d.Message.Contains(":pointerover") &&
            d.Message.Contains("CustomProperty"),
            "should provide partial suggestion for partially mappable MultiTrigger");
    }

    [Fact]
    public void Transform_MultiTrigger_NoMappable_ProvidesBehaviorSuggestion()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <MultiTrigger>
                    <MultiTrigger.Conditions>
                        <Condition Property=""CustomProperty1"" Value=""True"" />
                        <Condition Property=""CustomProperty2"" Value=""False"" />
                    </MultiTrigger.Conditions>
                    <Setter Property=""Visibility"" Value=""Visible"" />
                </MultiTrigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "MULTITRIGGER_BEHAVIOR_PATTERN" &&
            d.Message.Contains("multi-binding") ||(d.Message.Contains("Multi-binding") || d.Message.Contains("MultiBinding")),
            "should suggest behavior pattern for non-mappable MultiTrigger");
    }

    [Fact]
    public void Transform_MultiTrigger_NoConditions_ProvidesBasicWarning()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Style.Triggers>
                <MultiTrigger>
                    <Setter Property=""Background"" Value=""Red"" />
                </MultiTrigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "MULTITRIGGER_NOT_SUPPORTED" &&
            d.Severity == Core.Diagnostics.DiagnosticSeverity.Warning,
            "should provide warning for MultiTrigger without conditions");
    }

    [Fact]
    public void Transform_VisualStateManager_CommonStates_ProvidesPseudoclassSuggestion()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Grid>
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name=""CommonStates"">
                <VisualState x:Name=""Normal"" />
                <VisualState x:Name=""MouseOver"">
                    <Storyboard>
                        <ColorAnimation Storyboard.TargetProperty=""Background.Color"" To=""LightBlue"" Duration=""0:0:0.2"" />
                    </Storyboard>
                </VisualState>
                <VisualState x:Name=""Pressed"">
                    <Storyboard>
                        <ColorAnimation Storyboard.TargetProperty=""Background.Color"" To=""DarkBlue"" Duration=""0:0:0.1"" />
                    </Storyboard>
                </VisualState>
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
    </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "VSM_PSEUDOCLASS_PATTERN" &&
            d.Message.Contains(":pointerover") &&
            d.Message.Contains("CommonStates"),
            "should suggest pseudoclass pattern for CommonStates VSM");
    }

    [Fact]
    public void Transform_VisualStateManager_FocusStates_ProvidesPseudoclassSuggestion()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Grid>
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name=""FocusStates"">
                <VisualState x:Name=""Focused"">
                    <Storyboard>
                        <ColorAnimation Storyboard.TargetProperty=""BorderBrush.Color"" To=""Blue"" Duration=""0:0:0.2"" />
                    </Storyboard>
                </VisualState>
                <VisualState x:Name=""Unfocused"" />
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
    </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "VSM_PSEUDOCLASS_PATTERN" &&
            d.Message.Contains(":focus") &&
            d.Message.Contains("FocusStates"),
            "should suggest pseudoclass pattern for FocusStates VSM");
    }

    [Fact]
    public void Transform_VisualStateManager_CustomStateGroup_ProvidesStyleClassSuggestion()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Grid>
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name=""CustomStates"">
                <VisualState x:Name=""StateA"">
                    <Storyboard>
                        <DoubleAnimation Storyboard.TargetProperty=""Opacity"" To=""0.5"" Duration=""0:0:0.3"" />
                    </Storyboard>
                </VisualState>
                <VisualState x:Name=""StateB"">
                    <Storyboard>
                        <DoubleAnimation Storyboard.TargetProperty=""Opacity"" To=""1.0"" Duration=""0:0:0.3"" />
                    </Storyboard>
                </VisualState>
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
    </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "VSM_STYLECLASS_PATTERN" &&
            d.Message.Contains("style classes") &&
            d.Message.Contains("CustomStates"),
            "should suggest style class pattern for custom state groups");
    }

    [Fact]
    public void Transform_VisualStateManager_MultipleGroups_ProvidesComprehensiveGuidance()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Grid>
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name=""CommonStates"">
                <VisualState x:Name=""Normal"" />
                <VisualState x:Name=""MouseOver"" />
            </VisualStateGroup>
            <VisualStateGroup x:Name=""FocusStates"">
                <VisualState x:Name=""Focused"" />
                <VisualState x:Name=""Unfocused"" />
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
    </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "VSM_COMPREHENSIVE_GUIDE" &&
            d.Message.Contains("2 group(s)") &&
            d.Message.Contains("CommonStates") &&
            d.Message.Contains("FocusStates"),
            "should provide comprehensive guidance for multiple VSM groups");
    }

    [Fact]
    public void Transform_VisualStateManager_CheckStates_ProvidesPseudoclassSuggestion()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Grid>
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name=""CheckStates"">
                <VisualState x:Name=""Checked"">
                    <Storyboard>
                        <ColorAnimation Storyboard.TargetProperty=""Background.Color"" To=""Green"" Duration=""0:0:0.2"" />
                    </Storyboard>
                </VisualState>
                <VisualState x:Name=""Unchecked"" />
                <VisualState x:Name=""Indeterminate"">
                    <Storyboard>
                        <ColorAnimation Storyboard.TargetProperty=""Background.Color"" To=""Gray"" Duration=""0:0:0.2"" />
                    </Storyboard>
                </VisualState>
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
    </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "VSM_PSEUDOCLASS_PATTERN" &&
            d.Message.Contains(":checked") &&
            d.Message.Contains(":indeterminate"),
            "should suggest pseudoclass pattern for CheckStates");
    }

    [Fact]
    public void Transform_StyleWithControlTemplate_ProvidesControlThemeGuidance()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style x:Key=""CustomButtonStyle"" TargetType=""Button"">
            <Setter Property=""Background"" Value=""Blue"" />
            <Setter Property=""Foreground"" Value=""White"" />
            <Setter Property=""Template"">
                <Setter.Value>
                    <ControlTemplate TargetType=""Button"">
                        <Border Background=""{TemplateBinding Background}"">
                            <ContentPresenter />
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "STYLE_TO_CONTROLTHEME" &&
            d.Message.Contains("ControlTheme") &&
            d.Message.Contains("CustomButtonStyle"),
            "should suggest ControlTheme for style with template");
    }

    [Fact]
    public void Transform_StyleWithControlTemplateAndTriggers_SuggestsNestedStyles()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Setter Property=""Background"" Value=""Gray"" />
            <Setter Property=""Template"">
                <Setter.Value>
                    <ControlTemplate TargetType=""Button"">
                        <Border Background=""{TemplateBinding Background}"">
                            <ContentPresenter />
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
            <Style.Triggers>
                <Trigger Property=""IsMouseOver"" Value=""True"">
                    <Setter Property=""Background"" Value=""LightGray"" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "STYLE_TO_CONTROLTHEME" &&
            d.Message.Contains("ControlTheme") &&
            d.Message.Contains("Button"),
            "should suggest ControlTheme with nested styles for template with triggers");
    }

    [Fact]
    public void Transform_StyleWithControlTemplateNoKey_SuggestsXTypeKey()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Setter Property=""Template"">
                <Setter.Value>
                    <ControlTemplate TargetType=""Button"">
                        <Border>
                            <ContentPresenter />
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "STYLE_TO_CONTROLTHEME" &&
            d.Message.Contains("x:Key=\"{x:Type ControlType}\"") &&
            d.Message.Contains("applies to all instances"),
            "should suggest x:Type key for styles without explicit key");
    }

    [Fact]
    public void Transform_StyleWithoutControlTemplate_NoControlThemeGuidance()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style x:Key=""SimpleButtonStyle"" TargetType=""Button"">
            <Setter Property=""Background"" Value=""Blue"" />
            <Setter Property=""Foreground"" Value=""White"" />
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        // Should not have STYLE_TO_CONTROLTHEME diagnostic for styles without templates
        result.Diagnostics.Should().NotContain(d => d.Code == "STYLE_TO_CONTROLTHEME",
            "should not suggest ControlTheme for styles without templates");
    }

    [Fact]
    public void Transform_StyleWithBasedOn_PreservesBasedOnInGuidance()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style x:Key=""DerivedButtonStyle"" TargetType=""Button"" BasedOn=""{StaticResource BaseStyle}"">
            <Setter Property=""Template"">
                <Setter.Value>
                    <ControlTemplate TargetType=""Button"">
                        <Border>
                            <ContentPresenter />
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().Contain(d =>
            d.Code == "STYLE_TO_CONTROLTHEME" &&
            d.Message.Contains("BasedOn"),
            "should mention BasedOn in ControlTheme guidance");
    }

    #region Markup Extension Tests

    [Fact]
    public void Transform_XArrayMarkupExtension_ProvidesGuidance()
    {
        // Arrange - Test the transformer directly
        var markupExtension = new UnifiedXamlMarkupExtension
        {
            ExtensionName = "x:Array"
        };
        markupExtension.Parameters["Type"] = "String";

        var document = new UnifiedXamlDocument { FilePath = "test.xaml" };
        var context = new TransformationContext(document, new TransformationOptions());

        var transformer = new WpfToAvalonia.XamlParser.Transformation.Rules.XArrayMarkupExtensionTransformer();

        // Act
        transformer.TransformMarkupExtension(markupExtension, context);

        // Assert
        markupExtension.Diagnostics.Should().Contain(d =>
            d.Code == "XARRAY_NOT_SUPPORTED" &&
            d.Message.Contains("NOT supported in Avalonia") &&
            d.Message.Contains("Option 1"),
            "should provide guidance for x:Array conversion");
    }

    [Fact]
    public void Transform_XStaticMarkupExtension_ValidatesWithMember()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <TextBlock Text=""{x:Static sys:Environment.MachineName}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        // Should have a transformation record for x:Static
        result.Diagnostics.Should().NotContain(d => d.Code == "XSTATIC_NO_MEMBER",
            "should not warn about x:Static with valid member");
    }

    [Fact]
    public void Transform_XStaticWithoutMember_ProvidesWarning()
    {
        // Arrange - Test the transformer directly
        var markupExtension = new UnifiedXamlMarkupExtension
        {
            ExtensionName = "x:Static"
            // No PositionalArgument or Member parameter
        };

        var document = new UnifiedXamlDocument { FilePath = "test.xaml" };
        var context = new TransformationContext(document, new TransformationOptions());

        var transformer = new WpfToAvalonia.XamlParser.Transformation.Rules.XStaticMarkupExtensionTransformer();

        // Act
        transformer.TransformMarkupExtension(markupExtension, context);

        // Assert
        markupExtension.Diagnostics.Should().Contain(d =>
            d.Code == "XSTATIC_NO_MEMBER" &&
            d.Message.Contains("requires a Member parameter"),
            "should warn about x:Static without member");
    }

    [Fact]
    public void Transform_XTypeMarkupExtension_Compatible()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <ContentControl Content=""{x:Type Button}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        // x:Type is compatible, should not have warnings
        result.Diagnostics.Should().NotContain(d =>
            d.Severity == DiagnosticSeverity.Warning &&
            d.Message.Contains("x:Type") &&
            d.Message.Contains("NOT supported"),
            "x:Type should be compatible with Avalonia");
    }

    [Fact]
    public void Transform_XNullMarkupExtension_Compatible()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <ContentControl Content=""{x:Null}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();
        // x:Null is compatible, should not have warnings
        result.Diagnostics.Should().NotContain(d =>
            d.Severity == DiagnosticSeverity.Warning &&
            d.Message.Contains("x:Null") &&
            d.Message.Contains("NOT supported"),
            "x:Null should be compatible with Avalonia");
    }

    #endregion
}
