using System.Xml;
using System.Xml.Linq;
using FluentAssertions;
using WpfToAvalonia.XamlParser.Formatting;
using WpfToAvalonia.XamlParser.UnifiedAst;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace WpfToAvalonia.Tests;

public class SourceBasedWhitespaceExtractorTests
{
    private readonly ITestOutputHelper _output;

    public SourceBasedWhitespaceExtractorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GetCharacterPosition_ShouldHandleSimpleCase()
    {
        var source = "<Window>\n    <Grid />\n</Window>";
        var doc = XDocument.Parse(source, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

        // Check IXmlLineInfo behavior
        var windowElement = doc.Root!;
        var gridElement = doc.Descendants().First(e => e.Name.LocalName == "Grid");

        var windowLineInfo = (IXmlLineInfo)windowElement;
        var gridLineInfo = (IXmlLineInfo)gridElement;

        _output.WriteLine($"Window at Line {windowLineInfo.LineNumber}, Column {windowLineInfo.LinePosition}");
        _output.WriteLine($"Grid at Line {gridLineInfo.LineNumber}, Column {gridLineInfo.LinePosition}");
        _output.WriteLine($"Source character at position 0: '{source[0]}'");
        _output.WriteLine($"Source character at position 9 (after Window>): '{source[9]}'");
        _output.WriteLine($"Source character at position 14 (before Grid<): '{source[14]}'");

        var extractor = new SourceBasedWhitespaceExtractor(source);
        var hints = extractor.ExtractElementFormatting(windowElement);

        hints.Should().NotBeNull();
        _output.WriteLine($"Window SourceStartPosition: {hints.SourceStartPosition}");
        _output.WriteLine($"Window LeadingWhitespace: '{hints.LeadingWhitespace?.Replace("\n", "\\n")}'");

        var gridHints = extractor.ExtractElementFormatting(gridElement);
        _output.WriteLine($"Grid SourceStartPosition: {gridHints.SourceStartPosition}");
        _output.WriteLine($"Grid LeadingWhitespace: '{gridHints.LeadingWhitespace?.Replace("\n", "\\n").Replace(" ", "·")}'");
    }

    [Fact]
    public void ExtractElementFormatting_ShouldPreserveMultiLineAttributes()
    {
        var source = @"<Window xmlns=""http://test""
        Title=""Test""
        Height=""450"">
    <Grid />
</Window>";

        var extractor = new SourceBasedWhitespaceExtractor(source);
        var doc = XDocument.Parse(source, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

        // Grid element has no namespace, so just find it by local name
        var gridElement = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Grid");

        gridElement.Should().NotBeNull();

        var hints = extractor.ExtractElementFormatting(gridElement!);

        _output.WriteLine($"Grid LineNumber: {hints.LineNumber}");
        _output.WriteLine($"Grid ColumnNumber: {hints.ColumnNumber}");
        _output.WriteLine($"Grid SourceStartPosition: {hints.SourceStartPosition}");
        _output.WriteLine($"Grid LeadingWhitespace: '{hints.LeadingWhitespace?.Replace("\n", "\\n").Replace(" ", "·")}'");

        // Grid should have leading whitespace with newline and 4 spaces indentation
        hints.LeadingWhitespace.Should().NotBeNullOrEmpty();
        hints.LeadingWhitespace.Should().Contain("\n");
    }

    [Fact]
    public void ExtractAttributeFormatting_ShouldDetectMultiLineAttributes()
    {
        var source = @"<Window xmlns=""http://test""
        Title=""Test""
        Height=""450"">
</Window>";

        var extractor = new SourceBasedWhitespaceExtractor(source);
        var doc = XDocument.Parse(source, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        var windowElement = doc.Root;
        var titleAttr = windowElement?.Attribute("Title");

        titleAttr.Should().NotBeNull();
        windowElement.Should().NotBeNull();

        var hints = extractor.ExtractAttributeFormatting(titleAttr!, windowElement!);

        _output.WriteLine($"Title attribute LeadingWhitespace: '{hints.LeadingWhitespace?.Replace("\n", "\\n").Replace(" ", "·")}'");
        _output.WriteLine($"Title attribute PreserveLineBreak: {hints.PreserveLineBreak}");

        // Title attribute should be on a new line
        hints.PreserveLineBreak.Should().BeTrue();
        hints.LeadingWhitespace.Should().Contain("\n");
    }

    [Fact]
    public void PositionCalculation_ShouldHandleWindowsLineEndings()
    {
        var source = "<Window>\r\n    <Grid />\r\n</Window>";
        var extractor = new SourceBasedWhitespaceExtractor(source);

        var doc = XDocument.Parse(source, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        var gridElement = doc.Root?.Element("Grid");

        gridElement.Should().NotBeNull();

        var hints = extractor.ExtractElementFormatting(gridElement!);

        _output.WriteLine($"Grid LeadingWhitespace length: {hints.LeadingWhitespace?.Length}");
        _output.WriteLine($"Grid LeadingWhitespace: '{hints.LeadingWhitespace?.Replace("\r", "\\r").Replace("\n", "\\n").Replace(" ", "·")}'");

        hints.LeadingWhitespace.Should().NotBeNullOrEmpty();
    }

    private FormattingHints ParseAndExtract(string source, SourceBasedWhitespaceExtractor extractor)
    {
        var doc = XDocument.Parse(source, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
        return extractor.ExtractElementFormatting(doc.Root!);
    }
}
