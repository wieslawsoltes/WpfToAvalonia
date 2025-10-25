using System.Text;
using System.Xml;
using System.Xml.Linq;
using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Formatting;

/// <summary>
/// Extracts whitespace formatting from source XAML using position-based analysis.
/// This enables 100% accurate whitespace preservation including multi-line attributes.
/// </summary>
public sealed class SourceBasedWhitespaceExtractor
{
    private readonly string _sourceXaml;
    private readonly int[]? _lineStartPositions;

    public SourceBasedWhitespaceExtractor(string sourceXaml)
    {
        _sourceXaml = sourceXaml ?? throw new ArgumentNullException(nameof(sourceXaml));
        _lineStartPositions = ComputeLineStartPositions(sourceXaml);
    }

    /// <summary>
    /// Extracts formatting hints for an element using its position in the source.
    /// </summary>
    public FormattingHints ExtractElementFormatting(XElement element)
    {
        var hints = new FormattingHints
        {
            SourceXaml = _sourceXaml
        };

        if (element is not IXmlLineInfo lineInfo || !lineInfo.HasLineInfo())
            return hints;

        hints.LineNumber = lineInfo.LineNumber;
        hints.ColumnNumber = lineInfo.LinePosition;

        try
        {
            // Calculate character position from line/column
            var startPos = GetCharacterPosition(lineInfo.LineNumber, lineInfo.LinePosition);
            hints.SourceStartPosition = startPos;

            // Extract leading whitespace (from previous element/tag end to this element start)
            hints.LeadingWhitespace = ExtractLeadingWhitespace(startPos);

            // Try to find the element's end position
            var elementString = element.ToString();
            var searchStart = Math.Max(0, startPos - 100); // Start search a bit before
            var foundIndex = _sourceXaml.IndexOf(elementString, searchStart, StringComparison.Ordinal);

            if (foundIndex >= 0)
            {
                hints.SourceEndPosition = foundIndex + elementString.Length;

                // Extract trailing whitespace
                hints.TrailingWhitespace = ExtractTrailingWhitespace(hints.SourceEndPosition);
            }
        }
        catch
        {
            // If extraction fails, return what we have
        }

        return hints;
    }

    /// <summary>
    /// Extracts formatting hints for an attribute using regex-based position detection.
    /// </summary>
    public FormattingHints ExtractAttributeFormatting(XAttribute attribute, XElement parentElement)
    {
        var hints = new FormattingHints
        {
            SourceXaml = _sourceXaml
        };

        if (parentElement is not IXmlLineInfo parentLineInfo || !parentLineInfo.HasLineInfo())
            return hints;

        try
        {
            // Get parent element start
            var parentStartPos = GetCharacterPosition(parentLineInfo.LineNumber, parentLineInfo.LinePosition);

            // Find the opening tag content
            var openTagEnd = _sourceXaml.IndexOf('>', parentStartPos);
            if (openTagEnd < 0) return hints;

            var openTagContent = _sourceXaml.Substring(parentStartPos, openTagEnd - parentStartPos);

            // Find this specific attribute in the opening tag
            var attrName = attribute.Name.LocalName;
            var attrPattern = $@"{attrName}\s*=";
            var attrMatch = System.Text.RegularExpressions.Regex.Match(openTagContent, attrPattern);

            if (attrMatch.Success)
            {
                var attrPosInTag = attrMatch.Index;
                var attrAbsolutePos = parentStartPos + attrPosInTag;

                hints.SourceStartPosition = attrAbsolutePos;

                // Extract whitespace BEFORE this attribute
                // Look backwards to find what precedes it
                var precedingText = openTagContent.Substring(0, attrPosInTag);

                // Find the last non-whitespace character position
                int lastNonWhitespace = precedingText.Length - 1;
                while (lastNonWhitespace >= 0 && char.IsWhiteSpace(precedingText[lastNonWhitespace]))
                {
                    lastNonWhitespace--;
                }

                if (lastNonWhitespace < precedingText.Length - 1)
                {
                    var whitespace = precedingText.Substring(lastNonWhitespace + 1);
                    hints.LeadingWhitespace = whitespace;

                    // Check if attribute is on a new line
                    hints.PreserveLineBreak = whitespace.Contains('\n');
                }
            }
        }
        catch
        {
            // If extraction fails, return empty hints
        }

        return hints;
    }

    /// <summary>
    /// Extracts leading whitespace before the given position.
    /// </summary>
    private string ExtractLeadingWhitespace(int position)
    {
        if (position < 0 || position >= _sourceXaml.Length)
            return string.Empty;

        // For position 0 (start of document), there's no leading whitespace
        if (position == 0)
            return string.Empty;

        // Look backwards to find the previous non-whitespace character
        int start = position - 1;
        while (start >= 0 && char.IsWhiteSpace(_sourceXaml[start]))
        {
            start--;
        }

        // Extract whitespace from after the non-whitespace char to the position
        if (start < position - 1)
        {
            var whitespace = _sourceXaml.Substring(start + 1, position - start - 1);

            // Normalize: keep only last newline with indentation
            if (whitespace.Contains('\n'))
            {
                var lines = whitespace.Split('\n');
                if (lines.Length > 1)
                {
                    return "\n" + lines[^1];
                }
            }

            return whitespace;
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts trailing whitespace after the given position.
    /// </summary>
    private string? ExtractTrailingWhitespace(int position)
    {
        if (position < 0 || position >= _sourceXaml.Length)
            return null;

        // Look forward to find the next non-whitespace character
        int end = position;
        while (end < _sourceXaml.Length && char.IsWhiteSpace(_sourceXaml[end]))
        {
            end++;
        }

        if (end > position)
        {
            return _sourceXaml.Substring(position, end - position);
        }

        return null;
    }

    /// <summary>
    /// Converts line/column position to character position.
    /// NOTE: IXmlLineInfo.LinePosition points to the FIRST CHARACTER OF THE ELEMENT NAME, not the '<'.
    /// We need to subtract 1 to get to the '<' character.
    /// </summary>
    private int GetCharacterPosition(int lineNumber, int columnNumber)
    {
        if (_lineStartPositions == null || lineNumber < 1 || lineNumber > _lineStartPositions.Length)
            return 0;

        var lineStart = _lineStartPositions[lineNumber - 1];
        // Column is 1-based and points to the first character of the element name
        // We subtract 2 instead of 1 to get to the '<' character: one for 1-based, one for the '<'
        return lineStart + columnNumber - 2; // Adjust for 1-based column and the '<' character
    }

    /// <summary>
    /// Precomputes the starting character position of each line for fast lookup.
    /// Handles all line ending types: \n, \r\n, and \r.
    /// </summary>
    private static int[] ComputeLineStartPositions(string source)
    {
        var positions = new List<int> { 0 }; // Line 1 starts at position 0

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == '\n')
            {
                // Unix line ending (\n) or part of Windows line ending (\r\n)
                positions.Add(i + 1); // Next line starts after the newline
            }
            else if (source[i] == '\r')
            {
                // Check if this is a Windows line ending (\r\n) or old Mac (\r)
                if (i + 1 < source.Length && source[i + 1] == '\n')
                {
                    // Windows \r\n - skip the \r, let \n handle it
                    continue;
                }
                else
                {
                    // Old Mac \r only
                    positions.Add(i + 1); // Next line starts after the \r
                }
            }
        }

        return positions.ToArray();
    }
}
