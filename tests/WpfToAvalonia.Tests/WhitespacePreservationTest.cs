using FluentAssertions;
using WpfToAvalonia.XamlParser;
using Xunit;
using Xunit.Abstractions;

namespace WpfToAvalonia.Tests;

public class WhitespacePreservationTest
{
    private readonly ITestOutputHelper _output;

    public WhitespacePreservationTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ShouldPreserveWhitespaceWithPreserveFormattingOption()
    {
        // Arrange
        var input = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        Title=""Sample"" Height=""450"" Width=""800"">
    <Grid>
        <StackPanel Margin=""20"">
            <TextBlock Text=""Hello""
                       FontSize=""24""
                       Margin=""0,0,0,20"" />
            <Button Content=""Click"" />
        </StackPanel>
    </Grid>
</Window>";

        var converter = new WpfToAvaloniaConverter();
        var options = new ConversionOptions
        {
            PreserveFormatting = true,
            PreserveComments = true,
            AddTransformationComments = false
        };

        // Act
        var result = converter.Convert(input, null, options);

        // Assert
        result.Success.Should().BeTrue();
        result.OutputXaml.Should().NotBeNullOrEmpty();

        _output.WriteLine("=== INPUT ===");
        _output.WriteLine(input);
        _output.WriteLine("");
        _output.WriteLine("=== OUTPUT ===");
        _output.WriteLine(result.OutputXaml);
        _output.WriteLine("");
        _output.WriteLine("=== OUTPUT (with visible whitespace) ===");
        _output.WriteLine(result.OutputXaml!.Replace(" ", "·").Replace("\n", "↵\n").Replace("\r", "⏎"));
        _output.WriteLine("");

        // Check that indentation is preserved
        var outputLines = result.OutputXaml!.Split('\n');
        var inputLines = input.Split('\n');

        // Basic structural check: should have similar line count (within reason)
        Math.Abs(outputLines.Length - inputLines.Length).Should().BeLessThan(5,
            "Output should have similar structure to input");

        // Check that Grid is indented properly (no extra blank lines)
        result.OutputXaml.Should().Contain(">\n    <Grid>",
            "Grid should follow Window with single newline and 4 spaces indentation");

        // Check that StackPanel is indented properly (no extra blank lines)
        result.OutputXaml.Should().Contain(">\n        <StackPanel",
            "StackPanel should follow Grid with single newline and 8 spaces indentation");
    }

    [Fact]
    public void ShouldShowFormattingInformation()
    {
        // Arrange
        var input = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Grid>
        <Button Content=""Click"" />
    </Grid>
</Window>";

        var converter = new WpfToAvaloniaConverter();

        // Act
        var result = converter.Convert(input);

        // Assert
        result.Success.Should().BeTrue();
        result.Document.Should().NotBeNull();
        result.Document!.Root.Should().NotBeNull();

        _output.WriteLine("=== ROOT FORMATTING ===");
        _output.WriteLine($"Leading: '{result.Document.Root!.Formatting.LeadingWhitespace}'");
        _output.WriteLine($"Trailing: '{result.Document.Root.Formatting.TrailingWhitespace}'");
        _output.WriteLine($"Inner: '{result.Document.Root.Formatting.InnerWhitespace}'");

        if (result.Document.Root.Children.Count > 0)
        {
            var grid = result.Document.Root.Children[0];
            _output.WriteLine("");
            _output.WriteLine("=== GRID FORMATTING ===");
            _output.WriteLine($"Leading: '{grid.Formatting.LeadingWhitespace}'");
            _output.WriteLine($"Trailing: '{grid.Formatting.TrailingWhitespace}'");
            _output.WriteLine($"Inner: '{grid.Formatting.InnerWhitespace}'");

            if (grid.Children.Count > 0)
            {
                var button = grid.Children[0];
                _output.WriteLine("");
                _output.WriteLine("=== BUTTON FORMATTING ===");
                _output.WriteLine($"Leading: '{button.Formatting.LeadingWhitespace}'");
                _output.WriteLine($"Trailing: '{button.Formatting.TrailingWhitespace}'");
                _output.WriteLine($"Inner: '{button.Formatting.InnerWhitespace}'");
            }
        }
    }
}
