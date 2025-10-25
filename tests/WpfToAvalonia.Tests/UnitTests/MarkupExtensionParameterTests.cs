using FluentAssertions;
using WpfToAvalonia.XamlParser.UnifiedAst;
using Xunit;

namespace WpfToAvalonia.Tests.UnitTests;

public class MarkupExtensionParameterTests
{
    [Fact]
    public void FromString_CreatesStringParameter()
    {
        // Act
        var param = MarkupExtensionParameter.FromString("Test");

        // Assert
        param.Kind.Should().Be(ParameterValueKind.String);
        param.IsString.Should().BeTrue();
        param.AsString().Should().Be("Test");
        param.ToString().Should().Contain("String: \"Test\"");
    }

    [Fact]
    public void FromExtension_CreatesExtensionParameter()
    {
        // Arrange
        var extension = new UnifiedXamlMarkupExtension { ExtensionName = "Binding" };

        // Act
        var param = MarkupExtensionParameter.FromExtension(extension);

        // Assert
        param.Kind.Should().Be(ParameterValueKind.NestedExtension);
        param.IsExtension.Should().BeTrue();
        param.AsExtension().Should().BeSameAs(extension);
    }

    [Fact]
    public void FromRelativeSource_CreatesRelativeSourceParameter()
    {
        // Arrange
        var relativeSource = new RelativeSourceExpression
        {
            Mode = RelativeSourceMode.FindAncestor,
            AncestorLevel = 2
        };

        // Act
        var param = MarkupExtensionParameter.FromRelativeSource(relativeSource);

        // Assert
        param.Kind.Should().Be(ParameterValueKind.RelativeSource);
        param.IsRelativeSource.Should().BeTrue();
        param.AsRelativeSource().Should().Be(relativeSource);
    }

    [Fact]
    public void FromType_CreatesTypeParameter()
    {
        // Arrange
        var typeName = new QualifiedTypeName("Button");

        // Act
        var param = MarkupExtensionParameter.FromType(typeName);

        // Assert
        param.Kind.Should().Be(ParameterValueKind.Type);
        param.IsType.Should().BeTrue();
        param.AsType().Should().Be(typeName);
    }

    [Fact]
    public void FromNumber_CreatesNumberParameter()
    {
        // Act
        var param = MarkupExtensionParameter.FromNumber(42.5);

        // Assert
        param.Kind.Should().Be(ParameterValueKind.Number);
        param.IsNumber.Should().BeTrue();
        param.AsNumber().Should().Be(42.5);
    }

    [Fact]
    public void FromBoolean_CreatesBooleanParameter()
    {
        // Act
        var param = MarkupExtensionParameter.FromBoolean(true);

        // Assert
        param.Kind.Should().Be(ParameterValueKind.Boolean);
        param.IsBoolean.Should().BeTrue();
        param.AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Null_CreatesNullParameter()
    {
        // Act
        var param = MarkupExtensionParameter.Null();

        // Assert
        param.Kind.Should().Be(ParameterValueKind.Null);
        param.IsNull.Should().BeTrue();
    }

    [Fact]
    public void AsString_ThrowsWhenNotString()
    {
        // Arrange
        var param = MarkupExtensionParameter.FromNumber(42);

        // Act & Assert
        var act = () => param.AsString();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a string*");
    }

    [Fact]
    public void AsExtension_ThrowsWhenNotExtension()
    {
        // Arrange
        var param = MarkupExtensionParameter.FromString("Test");

        // Act & Assert
        var act = () => param.AsExtension();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a nested extension*");
    }

    [Fact]
    public void AsRelativeSource_ThrowsWhenNotRelativeSource()
    {
        // Arrange
        var param = MarkupExtensionParameter.FromString("Test");

        // Act & Assert
        var act = () => param.AsRelativeSource();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a relative source*");
    }

    [Fact]
    public void AsType_ThrowsWhenNotType()
    {
        // Arrange
        var param = MarkupExtensionParameter.FromString("Test");

        // Act & Assert
        var act = () => param.AsType();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a type reference*");
    }

    [Fact]
    public void AsNumber_ThrowsWhenNotNumber()
    {
        // Arrange
        var param = MarkupExtensionParameter.FromString("Test");

        // Act & Assert
        var act = () => param.AsNumber();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a number*");
    }

    [Fact]
    public void AsBoolean_ThrowsWhenNotBoolean()
    {
        // Arrange
        var param = MarkupExtensionParameter.FromString("Test");

        // Act & Assert
        var act = () => param.AsBoolean();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a boolean*");
    }

    [Fact]
    public void TryGetString_ReturnsTrueForString()
    {
        // Arrange
        var param = MarkupExtensionParameter.FromString("Test");

        // Act
        var result = param.TryGetString(out var value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be("Test");
    }

    [Fact]
    public void TryGetString_ReturnsFalseForNonString()
    {
        // Arrange
        var param = MarkupExtensionParameter.FromNumber(42);

        // Act
        var result = param.TryGetString(out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeEmpty();
    }

    [Fact]
    public void TryGetExtension_ReturnsTrueForExtension()
    {
        // Arrange
        var extension = new UnifiedXamlMarkupExtension { ExtensionName = "Binding" };
        var param = MarkupExtensionParameter.FromExtension(extension);

        // Act
        var result = param.TryGetExtension(out var value);

        // Assert
        result.Should().BeTrue();
        value.Should().BeSameAs(extension);
    }

    [Fact]
    public void TryGetRelativeSource_ReturnsTrueForRelativeSource()
    {
        // Arrange
        var relativeSource = new RelativeSourceExpression { Mode = RelativeSourceMode.Self };
        var param = MarkupExtensionParameter.FromRelativeSource(relativeSource);

        // Act
        var result = param.TryGetRelativeSource(out var value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(relativeSource);
    }

    [Fact]
    public void TryGetType_ReturnsTrueForType()
    {
        // Arrange
        var typeName = new QualifiedTypeName("Button");
        var param = MarkupExtensionParameter.FromType(typeName);

        // Act
        var result = param.TryGetType(out var value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(typeName);
    }

    [Fact]
    public void TryGetNumber_ReturnsTrueForNumber()
    {
        // Arrange
        var param = MarkupExtensionParameter.FromNumber(42.5);

        // Act
        var result = param.TryGetNumber(out var value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(42.5);
    }

    [Fact]
    public void TryGetBoolean_ReturnsTrueForBoolean()
    {
        // Arrange
        var param = MarkupExtensionParameter.FromBoolean(true);

        // Act
        var result = param.TryGetBoolean(out var value);

        // Assert
        result.Should().BeTrue();
        value.Should().BeTrue();
    }

    [Fact]
    public void Match_InvokesCorrectCallback()
    {
        // Arrange
        var param = MarkupExtensionParameter.FromString("Test");
        var stringCalled = false;

        // Act
        var result = param.Match(
            onString: s => { stringCalled = true; return s; },
            onExtension: e => "extension",
            onRelativeSource: r => "relative",
            onType: t => "type",
            onNumber: n => n.ToString(),
            onBoolean: b => b.ToString(),
            onNull: () => "null"
        );

        // Assert
        stringCalled.Should().BeTrue();
        result.Should().Be("Test");
    }

    [Fact]
    public void Switch_InvokesCorrectAction()
    {
        // Arrange
        var param = MarkupExtensionParameter.FromNumber(42);
        var numberCalled = false;

        // Act
        param.Switch(
            onString: s => Assert.Fail("Should not call onString"),
            onNumber: n => numberCalled = true
        );

        // Assert
        numberCalled.Should().BeTrue();
    }

    [Fact]
    public void FromString_ThrowsOnNullArgument()
    {
        // Act & Assert
        var act = () => MarkupExtensionParameter.FromString(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromExtension_ThrowsOnNullArgument()
    {
        // Act & Assert
        var act = () => MarkupExtensionParameter.FromExtension(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromRelativeSource_ThrowsOnNullArgument()
    {
        // Act & Assert
        var act = () => MarkupExtensionParameter.FromRelativeSource(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromType_ThrowsOnNullArgument()
    {
        // Act & Assert
        var act = () => MarkupExtensionParameter.FromType(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RecordEquality_WithSameValues_AreEqual()
    {
        // Arrange
        var param1 = MarkupExtensionParameter.FromString("Test");
        var param2 = MarkupExtensionParameter.FromString("Test");

        // Assert
        param1.Should().Be(param2);
        param1.GetHashCode().Should().Be(param2.GetHashCode());
    }

    [Fact]
    public void RecordEquality_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var param1 = MarkupExtensionParameter.FromString("Test1");
        var param2 = MarkupExtensionParameter.FromString("Test2");

        // Assert
        param1.Should().NotBe(param2);
    }
}
