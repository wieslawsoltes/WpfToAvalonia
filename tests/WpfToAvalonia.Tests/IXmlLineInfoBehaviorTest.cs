using System.Xml;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace WpfToAvalonia.Tests;

/// <summary>
/// Test to understand how IXmlLineInfo reports positions in XML files.
/// </summary>
public class IXmlLineInfoBehaviorTest
{
    private readonly ITestOutputHelper _output;

    public IXmlLineInfoBehaviorTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void UnderstandIXmlLineInfoPositions()
    {
        // Simple XML with known character positions
        var source = "<Window>\n    <Grid />\n</Window>";

        // Print the source with character positions
        _output.WriteLine("Source string with positions:");
        for (int i = 0; i < source.Length; i++)
        {
            var ch = source[i];
            var display = ch == '\n' ? "\\n" : ch.ToString();
            _output.WriteLine($"  Position {i,2}: '{display}'");
        }

        var doc = XDocument.Parse(source, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

        var windowElement = doc.Root!;
        var gridElement = doc.Descendants().First(e => e.Name.LocalName == "Grid");

        var windowLineInfo = (IXmlLineInfo)windowElement;
        var gridLineInfo = (IXmlLineInfo)gridElement;

        _output.WriteLine($"\nWindow IXmlLineInfo: Line {windowLineInfo.LineNumber}, Column {windowLineInfo.LinePosition}");
        _output.WriteLine($"Grid IXmlLineInfo: Line {gridLineInfo.LineNumber}, Column {gridLineInfo.LinePosition}");

        // Expected: Window at position 0 (the '<' char)
        // Expected: Grid at position 14 (the '<' char after newline and 4 spaces)

        _output.WriteLine($"\nExpected Window at position 0: source[0] = '{source[0]}'");
        _output.WriteLine($"Expected Grid at position 14: source[14] = '{source[14]}'");
    }
}
