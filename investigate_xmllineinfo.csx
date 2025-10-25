#!/usr/bin/env dotnet-script

using System;
using System.Xml;
using System.Xml.Linq;

// Simple XML with known character positions
var source = "<Window>\n    <Grid />\n</Window>";

Console.WriteLine("Source string with positions:");
for (int i = 0; i < source.Length; i++)
{
    var ch = source[i];
    var display = ch == '\n' ? "\\n" : ch.ToString();
    Console.WriteLine($"  Position {i,2}: '{display}'");
}

var doc = XDocument.Parse(source, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

var windowElement = doc.Root!;
var gridElement = doc.Descendants().First(e => e.Name.LocalName == "Grid");

var windowLineInfo = (IXmlLineInfo)windowElement;
var gridLineInfo = (IXmlLineInfo)gridElement;

Console.WriteLine($"\nWindow IXmlLineInfo: Line {windowLineInfo.LineNumber}, Column {windowLineInfo.LinePosition}");
Console.WriteLine($"Grid IXmlLineInfo: Line {gridLineInfo.LineNumber}, Column {gridLineInfo.LinePosition}");

// Expected: Window at position 0 (the '<' char)
// Expected: Grid at position 14 (the '<' char after newline and 4 spaces)

Console.WriteLine($"\nExpected Window at position 0: source[0] = '{source[0]}'");
Console.WriteLine($"Expected Grid at position 14: source[14] = '{source[14]}'");

// Now let's compute line start positions
var lineStartPositions = new List<int> { 0 }; // Line 1 starts at position 0

for (int i = 0; i < source.Length; i++)
{
    if (source[i] == '\n')
    {
        lineStartPositions.Add(i + 1); // Next line starts after the newline
    }
}

Console.WriteLine($"\nLine start positions:");
for (int i = 0; i < lineStartPositions.Count; i++)
{
    Console.WriteLine($"  Line {i + 1} starts at position {lineStartPositions[i]}");
}

// Now calculate character positions using IXmlLineInfo
int windowCharPos = lineStartPositions[windowLineInfo.LineNumber - 1] + windowLineInfo.LinePosition - 1;
int gridCharPos = lineStartPositions[gridLineInfo.LineNumber - 1] + gridLineInfo.LinePosition - 1;

Console.WriteLine($"\nCalculated Window character position: {windowCharPos} (character: '{source[windowCharPos]}')");
Console.WriteLine($"Calculated Grid character position: {gridCharPos} (character: '{source[gridCharPos]}')");
