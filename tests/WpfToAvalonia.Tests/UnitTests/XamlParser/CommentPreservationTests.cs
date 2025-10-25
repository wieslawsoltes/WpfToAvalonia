using FluentAssertions;
using System.Xml.Linq;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser;
using WpfToAvalonia.XamlParser.Converters;
using WpfToAvalonia.XamlParser.Serialization;
using WpfToAvalonia.XamlParser.UnifiedAst;
using Xunit;

namespace WpfToAvalonia.Tests.UnitTests.XamlParser;

/// <summary>
/// Unit tests for comment preservation in XAML transformation.
/// Implements Phase 3 Issue 3.1: Comment/Processing Instruction Support
/// </summary>
public class CommentPreservationTests
{
    [Fact]
    public void Convert_DocumentLevelLeadingComments_ShouldBePreserved()
    {
        // Arrange
        var xaml = @"<!-- This is a leading comment -->
<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
</Window>";

        var xDocument = XDocument.Parse(xaml, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        var diagnostics = new DiagnosticCollector();
        var converter = new XmlToUnifiedConverter(diagnostics);

        // Act
        var document = converter.Convert(xDocument);

        // Assert
        document.LeadingComments.Should().HaveCount(1, "there should be one leading comment");
        document.LeadingComments[0].Text.Should().Be(" This is a leading comment ", "the comment text should be preserved");
    }

    [Fact]
    public void Convert_DocumentLevelTrailingComments_ShouldBePreserved()
    {
        // Arrange
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
</Window>
<!-- This is a trailing comment -->";

        var xDocument = XDocument.Parse(xaml, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        var diagnostics = new DiagnosticCollector();
        var converter = new XmlToUnifiedConverter(diagnostics);

        // Act
        var document = converter.Convert(xDocument);

        // Assert
        document.TrailingComments.Should().HaveCount(1, "there should be one trailing comment");
        document.TrailingComments[0].Text.Should().Be(" This is a trailing comment ", "the comment text should be preserved");
    }

    [Fact]
    public void Convert_ElementComments_ShouldBePreserved()
    {
        // Arrange
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <!-- This is a comment inside the window -->
    <Button />
</Window>";

        var xDocument = XDocument.Parse(xaml, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        var diagnostics = new DiagnosticCollector();
        var converter = new XmlToUnifiedConverter(diagnostics);

        // Act
        var document = converter.Convert(xDocument);

        // Assert
        document.Root.Should().NotBeNull();
        document.Root!.Comments.Should().HaveCount(1, "the root element should have one comment");
        document.Root.Comments[0].Text.Should().Be(" This is a comment inside the window ", "the comment text should be preserved");
    }

    [Fact]
    public void Convert_MultipleComments_ShouldAllBePreserved()
    {
        // Arrange
        var xaml = @"<!-- Comment 1 -->
<!-- Comment 2 -->
<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <!-- Comment 3 -->
    <!-- Comment 4 -->
</Window>
<!-- Comment 5 -->";

        var xDocument = XDocument.Parse(xaml, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        var diagnostics = new DiagnosticCollector();
        var converter = new XmlToUnifiedConverter(diagnostics);

        // Act
        var document = converter.Convert(xDocument);

        // Assert
        document.LeadingComments.Should().HaveCount(2, "there should be two leading comments");
        document.LeadingComments[0].Text.Should().Be(" Comment 1 ");
        document.LeadingComments[1].Text.Should().Be(" Comment 2 ");

        document.Root!.Comments.Should().HaveCount(2, "the root element should have two comments");
        document.Root.Comments[0].Text.Should().Be(" Comment 3 ");
        document.Root.Comments[1].Text.Should().Be(" Comment 4 ");

        document.TrailingComments.Should().HaveCount(1, "there should be one trailing comment");
        document.TrailingComments[0].Text.Should().Be(" Comment 5 ");
    }

    [Fact]
    public void Serialize_LeadingComments_ShouldBeOutput()
    {
        // Arrange
        var diagnostics = new DiagnosticCollector();
        var document = new UnifiedXamlDocument
        {
            Root = new UnifiedXamlElement
            {
                TypeReference = new QualifiedTypeName("Window", "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
            }
        };
        document.LeadingComments.Add(new UnifiedXamlComment
        {
            Text = " This is a leading comment ",
            Preserve = true
        });

        var serializer = new UnifiedAstSerializer(diagnostics, new SerializationOptions
        {
            PreserveComments = true,
            UseAvaloniaNamespaces = false
        });

        // Act
        var xDocument = serializer.Serialize(document);

        // Assert
        var comments = xDocument.Nodes().OfType<XComment>().ToList();
        comments.Should().HaveCountGreaterThan(0, "there should be at least one comment");

        var leadingComment = comments.FirstOrDefault(c => c.Value.Contains("This is a leading comment"));
        leadingComment.Should().NotBeNull("the leading comment should be present in output");
    }

    [Fact]
    public void Serialize_TrailingComments_ShouldBeOutput()
    {
        // Arrange
        var diagnostics = new DiagnosticCollector();
        var document = new UnifiedXamlDocument
        {
            Root = new UnifiedXamlElement
            {
                TypeReference = new QualifiedTypeName("Window", "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
            }
        };
        document.TrailingComments.Add(new UnifiedXamlComment
        {
            Text = " This is a trailing comment ",
            Preserve = true
        });

        var serializer = new UnifiedAstSerializer(diagnostics, new SerializationOptions
        {
            PreserveComments = true,
            UseAvaloniaNamespaces = false
        });

        // Act
        var xDocument = serializer.Serialize(document);

        // Assert
        var comments = xDocument.Nodes().OfType<XComment>().ToList();
        comments.Should().HaveCountGreaterThan(0, "there should be at least one comment");

        var trailingComment = comments.FirstOrDefault(c => c.Value.Contains("This is a trailing comment"));
        trailingComment.Should().NotBeNull("the trailing comment should be present in output");
    }

    [Fact]
    public void Serialize_ElementComments_ShouldBeOutput()
    {
        // Arrange
        var diagnostics = new DiagnosticCollector();
        var rootElement = new UnifiedXamlElement
        {
            TypeReference = new QualifiedTypeName("Window", "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
        };
        rootElement.Comments.Add(new UnifiedXamlComment
        {
            Text = " This is an element comment ",
            Preserve = true,
            Parent = rootElement
        });

        var document = new UnifiedXamlDocument
        {
            Root = rootElement
        };

        var serializer = new UnifiedAstSerializer(diagnostics, new SerializationOptions
        {
            PreserveComments = true,
            UseAvaloniaNamespaces = false
        });

        // Act
        var xDocument = serializer.Serialize(document);

        // Assert
        var rootXElement = xDocument.Root;
        rootXElement.Should().NotBeNull();

        var comments = rootXElement!.Nodes().OfType<XComment>().ToList();
        comments.Should().HaveCountGreaterThan(0, "the root element should have comments");

        var elementComment = comments.FirstOrDefault(c => c.Value.Contains("This is an element comment"));
        elementComment.Should().NotBeNull("the element comment should be present in output");
    }

    [Fact]
    public void Serialize_PreserveCommentsDisabled_ShouldNotOutputComments()
    {
        // Arrange
        var diagnostics = new DiagnosticCollector();
        var document = new UnifiedXamlDocument
        {
            Root = new UnifiedXamlElement
            {
                TypeReference = new QualifiedTypeName("Window", "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
            }
        };
        document.LeadingComments.Add(new UnifiedXamlComment
        {
            Text = " This comment should not appear ",
            Preserve = true
        });

        var serializer = new UnifiedAstSerializer(diagnostics, new SerializationOptions
        {
            PreserveComments = false, // Disabled
            UseAvaloniaNamespaces = false,
            IncludeDiagnosticComments = false
        });

        // Act
        var xDocument = serializer.Serialize(document);

        // Assert
        var comments = xDocument.Nodes().OfType<XComment>().ToList();
        comments.Should().BeEmpty("no comments should be output when PreserveComments is false");
    }

    [Fact]
    public void Serialize_CommentPreserveFalse_ShouldNotBeOutput()
    {
        // Arrange
        var diagnostics = new DiagnosticCollector();
        var document = new UnifiedXamlDocument
        {
            Root = new UnifiedXamlElement
            {
                TypeReference = new QualifiedTypeName("Window", "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
            }
        };
        document.LeadingComments.Add(new UnifiedXamlComment
        {
            Text = " This comment has Preserve=false ",
            Preserve = false // Comment should not be preserved
        });

        var serializer = new UnifiedAstSerializer(diagnostics, new SerializationOptions
        {
            PreserveComments = true,
            UseAvaloniaNamespaces = false,
            IncludeDiagnosticComments = false
        });

        // Act
        var xDocument = serializer.Serialize(document);

        // Assert
        var comments = xDocument.Nodes().OfType<XComment>().ToList();
        comments.Should().BeEmpty("comments with Preserve=false should not be output");
    }

    [Fact]
    public void RoundTrip_Comments_ShouldBePreservedThroughConversionAndSerialization()
    {
        // Arrange
        var originalXaml = @"<!-- Leading comment -->
<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <!-- Element comment -->
    <Button Content=""Click Me"" />
</Window>
<!-- Trailing comment -->";

        var xDocument = XDocument.Parse(originalXaml, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        var diagnostics = new DiagnosticCollector();
        var converter = new XmlToUnifiedConverter(diagnostics);
        var serializer = new UnifiedAstSerializer(diagnostics, new SerializationOptions
        {
            PreserveComments = true,
            UseAvaloniaNamespaces = false,
            IncludeDiagnosticComments = false
        });

        // Act
        var document = converter.Convert(xDocument);
        var outputXDocument = serializer.Serialize(document);

        // Assert
        document.LeadingComments.Should().HaveCount(1);
        document.TrailingComments.Should().HaveCount(1);
        document.Root!.Comments.Should().HaveCount(1);

        var outputXaml = outputXDocument.ToString();
        outputXaml.Should().Contain("Leading comment", "leading comment should be in output");
        outputXaml.Should().Contain("Element comment", "element comment should be in output");
        outputXaml.Should().Contain("Trailing comment", "trailing comment should be in output");
    }

    [Fact]
    public void VisitorPattern_ShouldVisitAllComments()
    {
        // Arrange
        var document = new UnifiedXamlDocument
        {
            Root = new UnifiedXamlElement
            {
                TypeReference = new QualifiedTypeName("Window", "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
            }
        };
        document.LeadingComments.Add(new UnifiedXamlComment { Text = " Leading " });
        document.Root.Comments.Add(new UnifiedXamlComment { Text = " Element " });
        document.TrailingComments.Add(new UnifiedXamlComment { Text = " Trailing " });

        var visitor = new CommentCollectorVisitor();

        // Act
        visitor.VisitDocument(document);

        // Assert
        visitor.CollectedComments.Should().HaveCount(3, "visitor should visit all comments");
        visitor.CollectedComments.Should().Contain(c => c.Text.Contains("Leading"));
        visitor.CollectedComments.Should().Contain(c => c.Text.Contains("Element"));
        visitor.CollectedComments.Should().Contain(c => c.Text.Contains("Trailing"));
    }

    /// <summary>
    /// Test helper visitor that collects all comments from the AST.
    /// </summary>
    private class CommentCollectorVisitor : WpfToAvalonia.XamlParser.Visitors.UnifiedXamlVisitorBase
    {
        public List<UnifiedXamlComment> CollectedComments { get; } = new();

        public override void VisitComment(UnifiedXamlComment comment)
        {
            CollectedComments.Add(comment);
        }
    }
}
