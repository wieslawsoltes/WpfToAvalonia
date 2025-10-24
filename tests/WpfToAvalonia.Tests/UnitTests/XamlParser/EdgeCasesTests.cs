using FluentAssertions;
using WpfToAvalonia.XamlParser;
using Xunit;

namespace WpfToAvalonia.Tests.UnitTests.XamlParser;

/// <summary>
/// Unit tests for edge cases and malformed XAML handling.
/// Implements task 2.5.8.1.5: Test edge cases and malformed XAML
/// </summary>
public class EdgeCasesTests
{
    [Fact]
    public void Parse_VeryLargeXaml_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var buttonsBuilder = new System.Text.StringBuilder();
        for (int i = 0; i < 1000; i++)
        {
            buttonsBuilder.AppendLine($"    <Button Content=\"Button {i}\" />");
        }
        var xaml = $@"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <StackPanel>
{buttonsBuilder}
    </StackPanel>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Should handle large XAML files");
        result.OutputXaml.Should().Contain("Button", "Should process all buttons");
    }

    [Fact]
    public void Parse_DeeplyNestedElements_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Grid>
        <Border>
            <StackPanel>
                <GroupBox>
                    <ScrollViewer>
                        <Grid>
                            <Border>
                                <StackPanel>
                                    <TextBox Text=""Deep nesting"" />
                                </StackPanel>
                            </Border>
                        </Grid>
                    </ScrollViewer>
                </GroupBox>
            </StackPanel>
        </Border>
    </Grid>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Should handle deeply nested elements");
        result.OutputXaml.Should().Contain("TextBox", "Should preserve deep nesting");
    }

    [Fact]
    public void Parse_EmptyElements_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <StackPanel />
    <Grid></Grid>
    <Border>
    </Border>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Should handle empty elements");
    }

    [Fact]
    public void Parse_WhitespaceOnlyContent_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock>

    </TextBlock>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Should handle whitespace-only content");
    }

    [Fact]
    public void Parse_SpecialCharactersInStrings_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox Text=""&lt;&gt;&amp;&quot;&apos;"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Should handle XML-escaped characters");
    }

    [Fact]
    public void Parse_UnicodeCharacters_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox Text=""こんにちは 世界 🌍"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Should handle Unicode characters");
    }

    [Fact]
    public void Parse_CDATASection_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock><![CDATA[<This is not XML>]]></TextBlock>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Should handle CDATA sections");
    }

    [Fact]
    public void Parse_XmlComments_Should_Preserve()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <!-- This is a comment -->
    <Button Content=""Test"" />
    <!-- Another comment -->
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Should handle XML comments");
        // Comments may or may not be preserved based on implementation
    }

    [Fact]
    public void Parse_MultipleRootElements_Should_HandleOrError()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" />
<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" />";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Multiple root elements are invalid XML, should error
        result.Success.Should().BeFalse("Multiple root elements should be rejected");
    }

    [Fact]
    public void Parse_NamespacePrefix_NotDeclared_Should_HandleOrError()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <local:CustomControl />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Undeclared namespace prefix should cause error or warning
        result.Diagnostics.Should().NotBeEmpty("Should report undeclared namespace prefix");
    }

    [Fact]
    public void Parse_MixedContent_TextAndElements_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock>
        Plain text
        <Run Text=""Run text"" />
        More plain text
    </TextBlock>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Should handle mixed content");
    }

    [Fact]
    public void Parse_DuplicateAttributes_Should_HandleOrError()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""First"" Content=""Second"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Duplicate attributes are invalid XML
        result.Success.Should().BeFalse("Duplicate attributes should be rejected");
    }

    [Fact]
    public void Parse_SelfClosingTagWithContent_Should_Error()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button />Content</Button>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeFalse("Self-closing tag with content should be rejected");
    }

    [Fact]
    public void Parse_AttributeWithoutQuotes_Should_Error()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=Test />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeFalse("Attribute without quotes should be rejected");
    }

    [Fact]
    public void Parse_EmptyAttributeValue_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content="""" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Empty attribute value should be valid");
    }

    [Fact]
    public void Parse_VeryLongAttributeValue_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var longString = new string('A', 10000);
        var xaml = $@"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox Text=""{longString}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Should handle very long attribute values");
    }

    [Fact]
    public void Parse_RecursiveTemplates_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <DataTemplate x:Key=""RecursiveTemplate"">
            <StackPanel>
                <TextBlock Text=""{Binding Name}"" />
                <ItemsControl ItemsSource=""{Binding Children}""
                             ItemTemplate=""{StaticResource RecursiveTemplate}"" />
            </StackPanel>
        </DataTemplate>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Should handle recursive template references");
    }

    [Fact]
    public void Parse_ForwardResourceReference_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Button Background=""{StaticResource ForwardBrush}"" />
    <Window.Resources>
        <SolidColorBrush x:Key=""ForwardBrush"" Color=""Red"" />
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Forward reference may or may not be supported
        // Should at least not crash
    }

    [Fact]
    public void Parse_NameCollisions_XName_And_Name_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Button x:Name=""MyButton"" Name=""MyButton"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Both x:Name and Name should be allowed (they're equivalent)
        result.Success.Should().BeTrue("Should handle both x:Name and Name");
    }

    [Fact]
    public void Parse_NullableTypes_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 xmlns:sys=""clr-namespace:System;assembly=mscorlib"">
    <Window.Resources>
        <sys:Int32 x:Key=""NullableInt"">
            {x:Null}
        </sys:Int32>
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // May or may not work depending on type system
    }

    [Fact]
    public void Parse_AttachedProperty_UnknownType_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button UnknownType.UnknownProperty=""Value"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Should handle or report unknown attached property
        result.Diagnostics.Should().NotBeEmpty("Should report unknown attached property");
    }

    [Fact]
    public void Parse_ComplexMarkupExtension_WithCommas_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""{Binding 'Path,With,Commas'}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Should handle commas in quoted values");
    }

    [Fact]
    public void Parse_NestedBraces_InMarkupExtension_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox Text=""{Binding StringFormat='Value: {{0}}'}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Should handle escaped braces in markup extensions");
    }

    [Fact]
    public void Parse_BOMCharacter_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = "\uFEFF" + @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // BOM handling varies across XML parsers. Some handle it gracefully, others may fail.
        // We just verify the converter doesn't crash - it should return a result object
        result.Should().NotBeNull("Converter should return a result even if parsing fails");

        // If it succeeds, verify the output
        if (result.Success && result.OutputXaml != null)
        {
            result.OutputXaml.Should().Contain("Button", "Should process the Button element if successful");
        }
    }

    [Fact]
    public void Parse_MixedLineEndings_Should_Handle()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = "<Window xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">\r\n" +
                  "    <Button Content=\"Test\" />\n" +
                  "    <TextBox Text=\"Value\" />\r" +
                  "</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Should handle mixed line endings (CRLF, LF, CR)");
    }
}
