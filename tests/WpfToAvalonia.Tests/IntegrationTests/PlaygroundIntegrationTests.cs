using FluentAssertions;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Services;
using WpfToAvalonia.Mappings;
using WpfToAvalonia.XamlParser;
using Xunit;

namespace WpfToAvalonia.Tests.IntegrationTests;

/// <summary>
/// Integration tests for Playground demo code.
/// Ensures the exact sample code shown in the Playground works correctly.
/// Prevents regressions when modifying conversion pipeline.
///
/// These tests use the same converters that the Playground uses:
/// - XAML: WpfToAvaloniaConverter
/// - C#: CSharpConverterService with JsonMappingRepository
/// </summary>
public class PlaygroundIntegrationTests
{
    // Sample XAML from Playground (exact copy from MainWindowViewModel.cs lines 307-337)
    private const string PlaygroundSampleXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        Title=""Sample WPF Window"" Height=""450"" Width=""800"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Setter Property=""Background"" Value=""White"" />
            <Setter Property=""Padding"" Value=""10,5"" />
            <Style.Triggers>
                <Trigger Property=""IsMouseOver"" Value=""True"">
                    <Setter Property=""Background"" Value=""LightBlue"" />
                </Trigger>
                <Trigger Property=""IsPressed"" Value=""True"">
                    <Setter Property=""Background"" Value=""DarkBlue"" />
                    <Setter Property=""Foreground"" Value=""White"" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
    <Grid>
        <StackPanel Margin=""20"" VerticalAlignment=""Center"">
            <TextBlock Text=""WPF to Avalonia Converter""
                       FontSize=""24""
                       FontWeight=""Bold""
                       HorizontalAlignment=""Center""
                       Margin=""0,0,0,20"" />
            <Button Content=""Click Me"" HorizontalAlignment=""Center"" />
            <TextBox Text=""Sample Text"" Margin=""0,10,0,0"" />
            <CheckBox Content=""Enable Feature"" Margin=""0,10,0,0"" />
        </StackPanel>
    </Grid>
</Window>";

    // Sample C# from Playground (exact copy from MainWindowViewModel.cs lines 339-373)
    private const string PlaygroundSampleCSharp = @"using System.Windows;
using System.Windows.Controls;

namespace SampleWpfApp
{
    public class MyControl : Control
    {
        // Simple DependencyProperty
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(""Title"", typeof(string), typeof(MyControl));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        // DependencyProperty with metadata
        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register(""Count"", typeof(int), typeof(MyControl),
                new PropertyMetadata(0, OnCountChanged));

        private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MyControl)d;
            // Handle count change
        }

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }
    }
}";

    private static CSharpConverterService CreateCSharpConverter()
    {
        // Use the same mapping repository path as other integration tests
        var mappingsPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "WpfToAvalonia.Mappings", "Data", "core-mappings.json");

        var repository = new JsonMappingRepository(mappingsPath);
        repository.LoadAsync().GetAwaiter().GetResult();

        return new CSharpConverterService(repository);
    }

    [Fact]
    public void PlaygroundXamlDemo_ShouldConvertWithoutCrashing()
    {
        // Arrange - Use same converter as Playground (WpfToAvaloniaConverter)
        var converter = new WpfToAvaloniaConverter();

        // Act
        var result = converter.Convert(PlaygroundSampleXaml);

        // Assert
        result.Should().NotBeNull("Converter should return a result");
        result.OutputXaml.Should().NotBeNullOrEmpty("Should produce output XAML");

        // Verify it's valid Avalonia XAML
        result.OutputXaml.Should().Contain("https://github.com/avaloniaui",
            "Should convert to Avalonia namespace");

        // Verify core elements are preserved
        result.OutputXaml.Should().Contain("Window", "Should preserve Window element");
        result.OutputXaml.Should().Contain("Button", "Should preserve Button element");
        result.OutputXaml.Should().Contain("TextBox", "Should preserve TextBox element");
        result.OutputXaml.Should().Contain("CheckBox", "Should preserve CheckBox element");

        // Log diagnostics for debugging
        var errors = result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
        var warnings = result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

        // Should succeed or at least not have errors
        if (!result.Success)
        {
            var errorMessages = string.Join(", ", result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.Message));
            Assert.Fail($"XAML conversion had {errors} errors: {errorMessages}");
        }
    }

    [Fact]
    public void PlaygroundCSharpDemo_ShouldConvertWithoutCrashing()
    {
        // Arrange - Use same converter as Playground (CSharpConverterService)
        var converter = CreateCSharpConverter();
        var diagnostics = new DiagnosticCollector();

        // Act
        string output = string.Empty;
        Exception? exception = null;
        try
        {
            output = converter.Convert(PlaygroundSampleCSharp, diagnostics);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Assert
        exception.Should().BeNull("C# conversion should not throw exception");
        output.Should().NotBeNullOrEmpty("Should produce output C# code");

        // Verify structure is preserved (basic smoke test)
        output.Should().Contain("namespace", "Should preserve namespace structure");
        output.Should().Contain("class", "Should preserve class declaration");

        // Verify transformations occurred
        output.Should().NotBe(PlaygroundSampleCSharp, "Should perform transformations");
        output.Should().Contain("using Avalonia", "Should transform System.Windows to Avalonia");
        output.Should().Contain("StyledProperty", "Should transform DependencyProperty to StyledProperty");

        // Verify callback signature transformation
        output.Should().Contain("AvaloniaObject d", "Should transform DependencyObject to AvaloniaObject in callback");
        output.Should().Contain("AvaloniaPropertyChangedEventArgs e", "Should transform DependencyPropertyChangedEventArgs to AvaloniaPropertyChangedEventArgs");

        // Log diagnostics
        var errors = diagnostics.ErrorCount;
        var warnings = diagnostics.WarningCount;

        // Should not have critical errors
        if (errors > 0)
        {
            var errorMessages = string.Join(", ", diagnostics.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.Message));
            Assert.Fail($"C# conversion had {errors} errors: {errorMessages}");
        }
    }

    [Fact]
    public void PlaygroundXamlDemo_MultipleConversions_ShouldBeConsistent()
    {
        // This test ensures the XAML converter produces consistent results
        // Important for Playground user experience

        // Arrange
        var converter = new WpfToAvaloniaConverter();

        // Act - Convert same XAML twice
        var result1 = converter.Convert(PlaygroundSampleXaml);
        var result2 = converter.Convert(PlaygroundSampleXaml);

        // Assert - Results should be consistent
        result1.OutputXaml.Should().Be(result2.OutputXaml,
            "Multiple conversions of same XAML should produce identical output");
    }

    [Fact]
    public void PlaygroundCSharpDemo_MultipleConversions_ShouldBeConsistent()
    {
        // This test ensures the C# converter produces consistent results
        // Important for Playground user experience

        // Arrange
        var converter = CreateCSharpConverter();
        var diagnostics1 = new DiagnosticCollector();
        var diagnostics2 = new DiagnosticCollector();

        // Act - Convert same C# code twice
        var output1 = converter.Convert(PlaygroundSampleCSharp, diagnostics1);
        var output2 = converter.Convert(PlaygroundSampleCSharp, diagnostics2);

        // Assert - Results should be consistent
        output1.Should().Be(output2,
            "Multiple conversions of same C# code should produce identical output");
    }

    [Fact]
    public void PlaygroundDemo_EmptyXaml_ShouldHandleGracefully()
    {
        // Test edge case that users might encounter in Playground

        // Arrange
        var converter = new WpfToAvaloniaConverter();

        // Act
        var result = converter.Convert(string.Empty);

        // Assert
        result.Should().NotBeNull("Should return result even for empty input");
        result.Success.Should().BeFalse("Empty input should not succeed");
        result.Diagnostics.Should().NotBeEmpty("Should report error for empty input");
    }

    [Fact]
    public void PlaygroundDemo_EmptyCSharp_ShouldHandleGracefully()
    {
        // Test edge case that users might encounter in Playground

        // Arrange
        var converter = CreateCSharpConverter();
        var diagnostics = new DiagnosticCollector();

        // Act
        var output = converter.Convert(string.Empty, diagnostics);

        // Assert
        output.Should().BeEmpty("Empty input should return empty output");
        diagnostics.Diagnostics.Should().BeEmpty("Should not generate diagnostics for empty input");
    }
}
