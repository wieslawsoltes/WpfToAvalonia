using System.Xml;
using System.Xml.Linq;
using WpfToAvalonia.Core;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser;
using Xunit;
using Xunit.Abstractions;

namespace WpfToAvalonia.Tests;

public class WhitespaceExtractionDiagnosticTest
{
    private readonly ITestOutputHelper _output;

    public WhitespaceExtractionDiagnosticTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DiagnoseWhitespaceExtraction()
    {
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

        var diagnostics = new DiagnosticCollector();
        var parser = new UnifiedXamlParser(diagnostics);
        var document = parser.Parse(input, "test.xaml");

        _output.WriteLine("=== Document Root ===");
        if (document.Root != null)
        {
            _output.WriteLine($"Root TypeName: {document.Root.TypeName}");
            _output.WriteLine($"Root LeadingWhitespace: '{document.Root.Formatting.LeadingWhitespace?.Replace("\n", "\\n").Replace(" ", "·")}'");
            _output.WriteLine($"Root SourceStartPosition: {document.Root.Formatting.SourceStartPosition}");
            _output.WriteLine($"Root LineNumber: {document.Root.Formatting.LineNumber}");
            _output.WriteLine($"Root ColumnNumber: {document.Root.Formatting.ColumnNumber}");

            _output.WriteLine("\n=== First Child (Grid) ===");
            if (document.Root.Children.Count > 0)
            {
                var grid = document.Root.Children[0];
                _output.WriteLine($"Grid TypeName: {grid.TypeName}");
                _output.WriteLine($"Grid LeadingWhitespace: '{grid.Formatting.LeadingWhitespace?.Replace("\n", "\\n").Replace(" ", "·")}'");
                _output.WriteLine($"Grid SourceStartPosition: {grid.Formatting.SourceStartPosition}");
                _output.WriteLine($"Grid LineNumber: {grid.Formatting.LineNumber}");
                _output.WriteLine($"Grid ColumnNumber: {grid.Formatting.ColumnNumber}");

                _output.WriteLine("\n=== Second Child (StackPanel) ===");
                if (grid.Children.Count > 0)
                {
                    var stackPanel = grid.Children[0];
                    _output.WriteLine($"StackPanel TypeName: {stackPanel.TypeName}");
                    _output.WriteLine($"StackPanel LeadingWhitespace: '{stackPanel.Formatting.LeadingWhitespace?.Replace("\n", "\\n").Replace(" ", "·")}'");
                    _output.WriteLine($"StackPanel SourceStartPosition: {stackPanel.Formatting.SourceStartPosition}");
                    _output.WriteLine($"StackPanel LineNumber: {stackPanel.Formatting.LineNumber}");
                    _output.WriteLine($"StackPanel ColumnNumber: {stackPanel.Formatting.ColumnNumber}");
                }
            }
        }
    }
}
