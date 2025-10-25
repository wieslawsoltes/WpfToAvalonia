using FluentAssertions;
using WpfToAvalonia.XamlParser.UnifiedAst;
using WpfToAvalonia.XamlParser.TypeSystem;
using Xunit;

namespace WpfToAvalonia.Tests.UnitTests;

public class QualifiedTypeNameTests
{
    [Fact]
    public void Constructor_WithLocalName_CreatesInstance()
    {
        // Act
        var typeName = new QualifiedTypeName("Button");

        // Assert
        typeName.LocalName.Should().Be("Button");
        typeName.Namespace.Should().BeNull();
        typeName.ResolvedType.Should().BeNull();
        typeName.IsResolved.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithNamespace_SetsNamespace()
    {
        // Act
        var typeName = new QualifiedTypeName("Button", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");

        // Assert
        typeName.LocalName.Should().Be("Button");
        typeName.Namespace.Should().Be("http://schemas.microsoft.com/winfx/2006/xaml/presentation");
    }

    [Fact]
    public void Constructor_WithEmptyLocalName_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new QualifiedTypeName("");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Local name cannot be empty*");
    }

    [Fact]
    public void Constructor_WithWhitespaceLocalName_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new QualifiedTypeName("   ");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Local name cannot be empty*");
    }

    [Fact]
    public void FullName_WithNoNamespace_ReturnsLocalName()
    {
        // Arrange
        var typeName = new QualifiedTypeName("Button");

        // Act & Assert
        typeName.FullName.Should().Be("Button");
    }

    [Fact]
    public void FullName_WithClrNamespace_ReturnsFullTypeName()
    {
        // Arrange
        var typeName = new QualifiedTypeName("MyControl", "clr-namespace:MyApp.Controls;assembly=MyApp");

        // Act & Assert
        typeName.FullName.Should().Be("MyApp.Controls.MyControl");
    }

    [Fact]
    public void FullName_WithClrNamespaceNoAssembly_ReturnsFullTypeName()
    {
        // Arrange
        var typeName = new QualifiedTypeName("MyControl", "clr-namespace:MyApp.Controls");

        // Act & Assert
        typeName.FullName.Should().Be("MyApp.Controls.MyControl");
    }

    [Fact]
    public void FullName_WithXmlNamespace_ReturnsLocalName()
    {
        // Arrange
        var typeName = new QualifiedTypeName("Button", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");

        // Act & Assert
        typeName.FullName.Should().Be("Button");
    }

    [Fact]
    public void FullName_WithResolvedType_UsesResolvedTypeFullName()
    {
        // Arrange
        var mockType = new MockXamlType("System.Windows.Controls.Button");
        var typeName = new QualifiedTypeName("Button", resolvedType: mockType);

        // Act & Assert
        typeName.FullName.Should().Be("System.Windows.Controls.Button");
    }

    [Fact]
    public void IsResolved_WithResolvedType_ReturnsTrue()
    {
        // Arrange
        var mockType = new MockXamlType("System.Windows.Controls.Button");
        var typeName = new QualifiedTypeName("Button", resolvedType: mockType);

        // Act & Assert
        typeName.IsResolved.Should().BeTrue();
    }

    [Fact]
    public void WithResolvedType_UpdatesResolvedType()
    {
        // Arrange
        var typeName = new QualifiedTypeName("Button");
        var mockType = new MockXamlType("System.Windows.Controls.Button");

        // Act
        var updated = typeName.WithResolvedType(mockType);

        // Assert
        updated.LocalName.Should().Be("Button");
        updated.ResolvedType.Should().BeSameAs(mockType);
        updated.IsResolved.Should().BeTrue();
        updated.FullName.Should().Be("System.Windows.Controls.Button");
    }

    [Fact]
    public void WithResolvedType_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var typeName = new QualifiedTypeName("Button");

        // Act & Assert
        var act = () => typeName.WithResolvedType(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithNamespace_UpdatesNamespace()
    {
        // Arrange
        var typeName = new QualifiedTypeName("Button");

        // Act
        var updated = typeName.WithNamespace("clr-namespace:MyApp");

        // Assert
        updated.LocalName.Should().Be("Button");
        updated.Namespace.Should().Be("clr-namespace:MyApp");
    }

    [Fact]
    public void WithLocalName_UpdatesLocalName()
    {
        // Arrange
        var typeName = new QualifiedTypeName("Button", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");

        // Act
        var updated = typeName.WithLocalName("TextBox");

        // Assert
        updated.LocalName.Should().Be("TextBox");
        updated.Namespace.Should().Be("http://schemas.microsoft.com/winfx/2006/xaml/presentation");
    }

    [Fact]
    public void Parse_WithSimpleName_ReturnsLocalNameOnly()
    {
        // Act
        var typeName = QualifiedTypeName.Parse("Button");

        // Assert
        typeName.LocalName.Should().Be("Button");
        typeName.Namespace.Should().BeNull();
    }

    [Fact]
    public void Parse_WithPrefixedName_ReturnsLocalNameAndUnresolvedPrefix()
    {
        // Act
        var typeName = QualifiedTypeName.Parse("local:MyControl");

        // Assert
        typeName.LocalName.Should().Be("MyControl");
        typeName.Namespace.Should().Be("local:");
    }

    [Fact]
    public void Parse_WithPrefixAndMappings_ResolvesPrefixToNamespace()
    {
        // Arrange
        var prefixes = new Dictionary<string, string>
        {
            ["local"] = "clr-namespace:MyApp.Controls"
        };

        // Act
        var typeName = QualifiedTypeName.Parse("local:MyControl", prefixes);

        // Assert
        typeName.LocalName.Should().Be("MyControl");
        typeName.Namespace.Should().Be("clr-namespace:MyApp.Controls");
    }

    [Fact]
    public void Parse_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => QualifiedTypeName.Parse("");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Qualified name cannot be empty*");
    }

    [Fact]
    public void Parse_WithColonButNoLocalName_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => QualifiedTypeName.Parse("local:");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*local name is empty*");
    }

    [Fact]
    public void TryParse_WithValidName_ReturnsTrue()
    {
        // Act
        var success = QualifiedTypeName.TryParse("Button", out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().NotBeNull();
        result.LocalName.Should().Be("Button");
    }

    [Fact]
    public void TryParse_WithInvalidName_ReturnsFalse()
    {
        // Act
        var success = QualifiedTypeName.TryParse("", out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void Matches_WithSameLocalName_ReturnsTrue()
    {
        // Arrange
        var typeName = new QualifiedTypeName("Button");

        // Act & Assert
        typeName.Matches("Button").Should().BeTrue();
    }

    [Fact]
    public void Matches_WithDifferentLocalName_ReturnsFalse()
    {
        // Arrange
        var typeName = new QualifiedTypeName("Button");

        // Act & Assert
        typeName.Matches("TextBox").Should().BeFalse();
    }

    [Fact]
    public void Matches_WithSameLocalNameAndNamespace_ReturnsTrue()
    {
        // Arrange
        var typeName = new QualifiedTypeName("Button", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");

        // Act & Assert
        typeName.Matches("Button", "http://schemas.microsoft.com/winfx/2006/xaml/presentation").Should().BeTrue();
    }

    [Fact]
    public void Matches_WithSameLocalNameButDifferentNamespace_ReturnsFalse()
    {
        // Arrange
        var typeName = new QualifiedTypeName("Button", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");

        // Act & Assert
        typeName.Matches("Button", "https://github.com/avaloniaui").Should().BeFalse();
    }

    [Fact]
    public void MatchesFullName_WithMatchingFullName_ReturnsTrue()
    {
        // Arrange
        var typeName = new QualifiedTypeName("MyControl", "clr-namespace:MyApp.Controls");

        // Act & Assert
        typeName.MatchesFullName("MyApp.Controls.MyControl").Should().BeTrue();
    }

    [Fact]
    public void MatchesFullName_WithNonMatchingFullName_ReturnsFalse()
    {
        // Arrange
        var typeName = new QualifiedTypeName("MyControl", "clr-namespace:MyApp.Controls");

        // Act & Assert
        typeName.MatchesFullName("System.Windows.Controls.Button").Should().BeFalse();
    }

    [Fact]
    public void ToString_WithLocalNameOnly_ReturnsLocalName()
    {
        // Arrange
        var typeName = new QualifiedTypeName("Button");

        // Act & Assert
        typeName.ToString().Should().Be("Button");
    }

    [Fact]
    public void ToString_WithNamespace_ShowsNamespaceInBrackets()
    {
        // Arrange
        var typeName = new QualifiedTypeName("Button", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");

        // Act & Assert
        typeName.ToString().Should().Be("Button [http://schemas.microsoft.com/winfx/2006/xaml/presentation]");
    }

    [Fact]
    public void ToString_WithResolvedType_ShowsFullName()
    {
        // Arrange
        var mockType = new MockXamlType("System.Windows.Controls.Button");
        var typeName = new QualifiedTypeName("Button", resolvedType: mockType);

        // Act & Assert
        typeName.ToString().Should().Be("Button (System.Windows.Controls.Button)");
    }

    [Fact]
    public void ForWpfType_CreatesTypeWithWpfNamespace()
    {
        // Act
        var typeName = QualifiedTypeName.ForWpfType("Button");

        // Assert
        typeName.LocalName.Should().Be("Button");
        typeName.Namespace.Should().Be("http://schemas.microsoft.com/winfx/2006/xaml/presentation");
    }

    [Fact]
    public void ForAvaloniaType_CreatesTypeWithAvaloniaNamespace()
    {
        // Act
        var typeName = QualifiedTypeName.ForAvaloniaType("Button");

        // Assert
        typeName.LocalName.Should().Be("Button");
        typeName.Namespace.Should().Be("https://github.com/avaloniaui");
    }

    [Fact]
    public void ForXamlDirective_CreatesTypeWithXamlNamespace()
    {
        // Act
        var typeName = QualifiedTypeName.ForXamlDirective("Key");

        // Assert
        typeName.LocalName.Should().Be("Key");
        typeName.Namespace.Should().Be("http://schemas.microsoft.com/winfx/2006/xaml");
    }

    [Fact]
    public void FromClrType_WithNamespace_ParsesCorrectly()
    {
        // Act
        var typeName = QualifiedTypeName.FromClrType("System.Windows.Controls.Button");

        // Assert
        typeName.LocalName.Should().Be("Button");
        typeName.Namespace.Should().Be("clr-namespace:System.Windows.Controls");
    }

    [Fact]
    public void FromClrType_WithoutNamespace_UsesLocalNameOnly()
    {
        // Act
        var typeName = QualifiedTypeName.FromClrType("Button");

        // Assert
        typeName.LocalName.Should().Be("Button");
        typeName.Namespace.Should().BeNull();
    }

    [Fact]
    public void RecordEquality_WithSameValues_AreEqual()
    {
        // Arrange
        var typeName1 = new QualifiedTypeName("Button", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
        var typeName2 = new QualifiedTypeName("Button", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");

        // Assert
        typeName1.Should().Be(typeName2);
        typeName1.GetHashCode().Should().Be(typeName2.GetHashCode());
    }

    [Fact]
    public void RecordEquality_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var typeName1 = new QualifiedTypeName("Button");
        var typeName2 = new QualifiedTypeName("TextBox");

        // Assert
        typeName1.Should().NotBe(typeName2);
    }

    // Mock implementation for testing
    private class MockXamlType : IXamlType
    {
        public MockXamlType(string fullName)
        {
            FullName = fullName;
        }

        public object Id => FullName;
        public string FullName { get; }
        public string Name => FullName.Split('.').Last();
        public string? Namespace => string.Join(".", FullName.Split('.').SkipLast(1));
        public bool IsPublic => true;
        public IXamlAssembly? Assembly => null;
        public IReadOnlyList<IXamlProperty> Properties => Array.Empty<IXamlProperty>();
        public IXamlType? BaseType => null;
        public bool IsValueType => false;
        public bool IsEnum => false;
        public IReadOnlyList<IXamlType> Interfaces => Array.Empty<IXamlType>();
        public bool IsAssignableFrom(IXamlType type) => false;
    }
}
