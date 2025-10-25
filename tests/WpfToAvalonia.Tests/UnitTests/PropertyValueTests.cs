using FluentAssertions;
using WpfToAvalonia.XamlParser.UnifiedAst;
using Xunit;

namespace WpfToAvalonia.Tests.UnitTests;

public class PropertyValueTests
{
    [Fact]
    public void FromString_CreatesStringValue()
    {
        // Arrange & Act
        var value = PropertyValue.FromString("Hello");

        // Assert
        value.Kind.Should().Be(PropertyValueKind.String);
        value.IsString.Should().BeTrue();
        value.AsString().Should().Be("Hello");
        value.ToString().Should().Contain("String: \"Hello\"");
    }

    [Fact]
    public void FromElement_CreatesElementValue()
    {
        // Arrange
        var element = new UnifiedXamlElement { TypeName = "Button" };

        // Act
        var value = PropertyValue.FromElement(element);

        // Assert
        value.Kind.Should().Be(PropertyValueKind.Element);
        value.IsElement.Should().BeTrue();
        value.AsElement().Should().BeSameAs(element);
        value.ToString().Should().Contain("Element: Button");
    }

    [Fact]
    public void FromMarkupExtension_CreatesMarkupExtensionValue()
    {
        // Arrange
        var extension = new UnifiedXamlMarkupExtension { ExtensionName = "Binding" };

        // Act
        var value = PropertyValue.FromMarkupExtension(extension);

        // Assert
        value.Kind.Should().Be(PropertyValueKind.MarkupExtension);
        value.IsMarkupExtension.Should().BeTrue();
        value.AsMarkupExtension().Should().BeSameAs(extension);
        value.ToString().Should().Contain("MarkupExtension: Binding");
    }

    [Fact]
    public void Null_CreatesNullValue()
    {
        // Act
        var value = PropertyValue.Null();

        // Assert
        value.Kind.Should().Be(PropertyValueKind.Null);
        value.IsNull.Should().BeTrue();
        value.ToString().Should().Contain("Null");
    }

    [Fact]
    public void AsString_ThrowsWhenNotString()
    {
        // Arrange
        var value = PropertyValue.Null();

        // Act & Assert
        var act = () => value.AsString();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Actual kind: Null*");
    }

    [Fact]
    public void AsElement_ThrowsWhenNotElement()
    {
        // Arrange
        var value = PropertyValue.FromString("Hello");

        // Act & Assert
        var act = () => value.AsElement();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Actual kind: String*");
    }

    [Fact]
    public void AsMarkupExtension_ThrowsWhenNotMarkupExtension()
    {
        // Arrange
        var element = new UnifiedXamlElement { TypeName = "Button" };
        var value = PropertyValue.FromElement(element);

        // Act & Assert
        var act = () => value.AsMarkupExtension();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Actual kind: Element*");
    }

    [Fact]
    public void TryGetString_ReturnsTrueForString()
    {
        // Arrange
        var value = PropertyValue.FromString("Test");

        // Act
        var result = value.TryGetString(out var str);

        // Assert
        result.Should().BeTrue();
        str.Should().Be("Test");
    }

    [Fact]
    public void TryGetString_ReturnsFalseForNonString()
    {
        // Arrange
        var value = PropertyValue.Null();

        // Act
        var result = value.TryGetString(out var str);

        // Assert
        result.Should().BeFalse();
        str.Should().BeEmpty();
    }

    [Fact]
    public void TryGetElement_ReturnsTrueForElement()
    {
        // Arrange
        var element = new UnifiedXamlElement { TypeName = "Button" };
        var value = PropertyValue.FromElement(element);

        // Act
        var result = value.TryGetElement(out var retrievedElement);

        // Assert
        result.Should().BeTrue();
        retrievedElement.Should().BeSameAs(element);
    }

    [Fact]
    public void TryGetMarkupExtension_ReturnsTrueForMarkupExtension()
    {
        // Arrange
        var extension = new UnifiedXamlMarkupExtension { ExtensionName = "Binding" };
        var value = PropertyValue.FromMarkupExtension(extension);

        // Act
        var result = value.TryGetMarkupExtension(out var retrievedExtension);

        // Assert
        result.Should().BeTrue();
        retrievedExtension.Should().BeSameAs(extension);
    }

    [Fact]
    public void Match_InvokesCorrectCallback()
    {
        // Arrange
        var value = PropertyValue.FromString("Test");
        var stringCalled = false;
        var elementCalled = false;
        var extensionCalled = false;
        var nullCalled = false;

        // Act
        var result = value.Match(
            onString: s => { stringCalled = true; return s; },
            onElement: e => { elementCalled = true; return e.TypeName; },
            onMarkupExtension: m => { extensionCalled = true; return m.ExtensionName; },
            onNull: () => { nullCalled = true; return "null"; }
        );

        // Assert
        stringCalled.Should().BeTrue();
        elementCalled.Should().BeFalse();
        extensionCalled.Should().BeFalse();
        nullCalled.Should().BeFalse();
        result.Should().Be("Test");
    }

    [Fact]
    public void Switch_InvokesCorrectAction()
    {
        // Arrange
        var element = new UnifiedXamlElement { TypeName = "Button" };
        var value = PropertyValue.FromElement(element);
        var stringCalled = false;
        var elementCalled = false;
        var extensionCalled = false;
        var nullCalled = false;

        // Act
        value.Switch(
            onString: s => stringCalled = true,
            onElement: e => elementCalled = true,
            onMarkupExtension: m => extensionCalled = true,
            onNull: () => nullCalled = true
        );

        // Assert
        stringCalled.Should().BeFalse();
        elementCalled.Should().BeTrue();
        extensionCalled.Should().BeFalse();
        nullCalled.Should().BeFalse();
    }

    [Fact]
    public void FromString_ThrowsOnNullArgument()
    {
        // Act & Assert
        var act = () => PropertyValue.FromString(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromElement_ThrowsOnNullArgument()
    {
        // Act & Assert
        var act = () => PropertyValue.FromElement(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromMarkupExtension_ThrowsOnNullArgument()
    {
        // Act & Assert
        var act = () => PropertyValue.FromMarkupExtension(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PropertyValue_IsRecord()
    {
        // Arrange
        var value1 = PropertyValue.FromString("Test");
        var value2 = PropertyValue.FromString("Test");

        // Assert
        // Records have value-based equality
        value1.Should().Be(value2);
        value1.GetHashCode().Should().Be(value2.GetHashCode());
    }
}
