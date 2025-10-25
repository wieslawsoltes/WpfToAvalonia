using FluentAssertions;
using WpfToAvalonia.XamlParser.UnifiedAst;
using Xunit;

namespace WpfToAvalonia.Tests.UnitTests;

public class RelativeSourceExpressionTests
{
    [Fact]
    public void DefaultConstructor_CreatesSelfMode()
    {
        // Act
        var expression = new RelativeSourceExpression();

        // Assert
        expression.Mode.Should().Be(RelativeSourceMode.Self);
        expression.AncestorType.Should().BeNull();
        expression.AncestorLevel.Should().Be(1);
    }

    [Fact]
    public void Parse_WithSelf_CreatesSelfMode()
    {
        // Act
        var expression = RelativeSourceExpression.Parse("Self");

        // Assert
        expression.Mode.Should().Be(RelativeSourceMode.Self);
        expression.AncestorType.Should().BeNull();
    }

    [Fact]
    public void Parse_WithFindAncestor_CreatesFindAncestorMode()
    {
        // Act
        var expression = RelativeSourceExpression.Parse("FindAncestor");

        // Assert
        expression.Mode.Should().Be(RelativeSourceMode.FindAncestor);
    }

    [Fact]
    public void Parse_WithTemplatedParent_CreatesTemplatedParentMode()
    {
        // Act
        var expression = RelativeSourceExpression.Parse("TemplatedParent");

        // Assert
        expression.Mode.Should().Be(RelativeSourceMode.TemplatedParent);
    }

    [Fact]
    public void Parse_WithPreviousData_CreatesPreviousDataMode()
    {
        // Act
        var expression = RelativeSourceExpression.Parse("PreviousData");

        // Assert
        expression.Mode.Should().Be(RelativeSourceMode.PreviousData);
    }

    [Fact]
    public void Parse_WithAncestorType_ExtractsTypeName()
    {
        // Act
        var expression = RelativeSourceExpression.Parse(
            "Mode=FindAncestor, AncestorType=ItemsControl");

        // Assert
        expression.Mode.Should().Be(RelativeSourceMode.FindAncestor);
        expression.AncestorType.Should().NotBeNull();
        expression.AncestorType!.LocalName.Should().Be("ItemsControl");
    }

    [Fact]
    public void Parse_WithXTypeAncestorType_ExtractsTypeName()
    {
        // Act
        var expression = RelativeSourceExpression.Parse(
            "Mode=FindAncestor, AncestorType={x:Type ItemsControl}");

        // Assert
        expression.Mode.Should().Be(RelativeSourceMode.FindAncestor);
        expression.AncestorType.Should().NotBeNull();
        expression.AncestorType!.LocalName.Should().Be("ItemsControl");
    }

    [Fact]
    public void Parse_WithAncestorLevel_ExtractsLevel()
    {
        // Act
        var expression = RelativeSourceExpression.Parse(
            "Mode=FindAncestor, AncestorType=ItemsControl, AncestorLevel=3");

        // Assert
        expression.Mode.Should().Be(RelativeSourceMode.FindAncestor);
        expression.AncestorLevel.Should().Be(3);
    }

    [Fact]
    public void Parse_WithPrefixedTypeName_ResolvesPrefix()
    {
        // Arrange
        var prefixes = new Dictionary<string, string>
        {
            ["local"] = "clr-namespace:MyApp.Controls"
        };

        // Act
        var expression = RelativeSourceExpression.Parse(
            "Mode=FindAncestor, AncestorType=local:MyControl",
            prefixes);

        // Assert
        expression.AncestorType.Should().NotBeNull();
        expression.AncestorType!.LocalName.Should().Be("MyControl");
        expression.AncestorType!.Namespace.Should().Be("clr-namespace:MyApp.Controls");
    }

    [Fact]
    public void Parse_CaseInsensitive_Succeeds()
    {
        // Act
        var expression = RelativeSourceExpression.Parse("findancestor");

        // Assert
        expression.Mode.Should().Be(RelativeSourceMode.FindAncestor);
    }

    [Fact]
    public void Parse_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => RelativeSourceExpression.Parse("");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void Parse_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => RelativeSourceExpression.Parse("   ");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Fact]
    public void TryParse_WithValidInput_ReturnsTrue()
    {
        // Act
        var success = RelativeSourceExpression.TryParse(
            "Mode=FindAncestor, AncestorType=ItemsControl",
            out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().NotBeNull();
        result.Mode.Should().Be(RelativeSourceMode.FindAncestor);
    }

    [Fact]
    public void TryParse_WithInvalidInput_ReturnsFalse()
    {
        // Act
        var success = RelativeSourceExpression.TryParse("", out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void RecordWithSyntax_CreatesNewInstance()
    {
        // Arrange
        var original = new RelativeSourceExpression
        {
            Mode = RelativeSourceMode.FindAncestor,
            AncestorLevel = 1
        };

        // Act
        var modified = original with { AncestorLevel = 2 };

        // Assert
        original.AncestorLevel.Should().Be(1);
        modified.AncestorLevel.Should().Be(2);
        modified.Mode.Should().Be(RelativeSourceMode.FindAncestor);
    }

    [Fact]
    public void RecordEquality_WithSameValues_AreEqual()
    {
        // Arrange
        var expr1 = new RelativeSourceExpression
        {
            Mode = RelativeSourceMode.FindAncestor,
            AncestorLevel = 2
        };
        var expr2 = new RelativeSourceExpression
        {
            Mode = RelativeSourceMode.FindAncestor,
            AncestorLevel = 2
        };

        // Assert
        expr1.Should().Be(expr2);
        expr1.GetHashCode().Should().Be(expr2.GetHashCode());
    }

    [Fact]
    public void RecordEquality_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var expr1 = new RelativeSourceExpression { Mode = RelativeSourceMode.Self };
        var expr2 = new RelativeSourceExpression { Mode = RelativeSourceMode.FindAncestor };

        // Assert
        expr1.Should().NotBe(expr2);
    }
}
