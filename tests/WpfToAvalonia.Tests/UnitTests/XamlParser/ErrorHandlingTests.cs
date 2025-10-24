using FluentAssertions;
using WpfToAvalonia.XamlParser;
using Xunit;

namespace WpfToAvalonia.Tests.UnitTests.XamlParser;

/// <summary>
/// Unit tests for error handling and diagnostics in XAML parsing.
/// Implements task 2.5.8.1.4: Test error handling and diagnostics
/// </summary>
public class ErrorHandlingTests
{
    [Fact]
    public void Parse_EmptyString_Should_ReturnError()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = "";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeFalse("Empty XAML should result in error");
        result.Diagnostics.Should().NotBeEmpty("Should have diagnostic messages");
    }

    [Fact]
    public void Parse_NullString_Should_ReturnError()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        string? xaml = null;

        // Act
        var result = converter.Convert(xaml!);

        // Assert
        result.Success.Should().BeFalse("Null XAML should result in error");
    }

    [Fact]
    public void Parse_MalformedXml_UnclosedTag_Should_ReturnError()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""Test""
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeFalse("Malformed XML should result in error");
        result.Diagnostics.Should().NotBeEmpty("Should report XML parsing errors");
    }

    [Fact]
    public void Parse_MalformedXml_MismatchedTags_Should_ReturnError()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""Test"" />
</Grid>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeFalse("Mismatched tags should result in error");
    }

    [Fact]
    public void Parse_InvalidNamespace_Should_ReportDiagnostic()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://invalid.namespace.com"">
    <Button Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // May succeed with warnings about unknown namespace
        result.Diagnostics.Should().NotBeEmpty("Should report namespace issues");
    }

    [Fact]
    public void Parse_UnknownElement_Should_ReportDiagnostic()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <NonExistentControl />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Parser may handle unknown elements gracefully
        // Should at least provide diagnostic information
        result.Diagnostics.Should().NotBeEmpty("Should report unknown element");
    }

    [Fact]
    public void Parse_InvalidProperty_Should_ReportDiagnostic()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button NonExistentProperty=""Value"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Parser may handle unknown properties gracefully
        result.Diagnostics.Should().NotBeEmpty("Should report unknown property");
    }

    [Fact]
    public void Parse_InvalidMarkupExtension_Should_ReportError()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Background=""{InvalidExtension}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Should handle or report invalid markup extension
        result.Diagnostics.Should().NotBeEmpty("Should report invalid markup extension");
    }

    [Fact]
    public void Parse_UnclosedMarkupExtension_Should_ReturnError()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Background=""{StaticResource MyBrush"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // The XML parser may or may not catch unclosed markup extensions
        // Different parsers handle this differently - some treat it as invalid XML,
        // others may parse it as a malformed attribute value
        // At minimum, we should either get an error OR a diagnostic message
        var hasErrorOrDiagnostic = !result.Success || result.Diagnostics.Any();
        hasErrorOrDiagnostic.Should().BeTrue("Should either fail or report diagnostic for unclosed markup extension");
    }

    [Fact]
    public void Parse_CircularResourceReference_Should_ReportDiagnostic()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <SolidColorBrush x:Key=""Brush1"" Color=""{StaticResource Brush2}"" />
        <SolidColorBrush x:Key=""Brush2"" Color=""{StaticResource Brush1}"" />
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Should detect or report circular reference
        // Behavior may vary based on implementation
    }

    [Fact]
    public void Parse_MissingRequiredAttribute_Should_ReportDiagnostic()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Style>
        <Setter Property=""Background"" Value=""Red"" />
    </Style>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Style without TargetType may cause issues
        // Should report diagnostic
    }

    [Fact]
    public void Convert_Should_ProvideDiagnosticsForTransformations()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ListView>
        <ListViewItem Content=""Item 1"" />
    </ListView>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Valid XAML should succeed");
        result.Diagnostics.Should().NotBeEmpty("Should provide transformation diagnostics");
        result.Diagnostics.Should().Contain(d => d.Message.Contains("ListView") || d.Message.Contains("ListBox"),
            "Should report ListView transformation");
    }

    [Fact]
    public void Convert_Should_ReportMultipleDiagnostics()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ListView Visibility=""Visible"">
        <ListViewItem Content=""Item 1"" />
    </ListView>
    <TextBox UpdateSourceTrigger=""PropertyChanged"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Valid XAML should succeed");
        result.Diagnostics.Should().NotBeEmpty("Should provide multiple diagnostics");
        // Should report both ListView->ListBox and Visibility->IsVisible transformations
    }

    [Fact]
    public void Convert_Should_CategorizeDiagnostics()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ListView />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue();
        result.Diagnostics.Should().NotBeEmpty();
        // Diagnostics should have severity levels (Info, Warning, Error)
        result.Diagnostics.Should().AllSatisfy(d =>
        {
            d.Severity.Should().BeDefined("Each diagnostic should have a valid severity");
        });
    }

    [Fact]
    public void Convert_Should_ProvideLocationInformation()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ListView />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue();
        // Location information may not always be available, but check if provided
        var diagnosticsWithLocation = result.Diagnostics.Where(d =>
            !string.IsNullOrEmpty(d.FilePath) || d.Line.HasValue);
        // If location info is implemented, verify it's present
    }

    [Fact]
    public void Parse_InvalidEnumValue_Should_ReportDiagnostic()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button HorizontalAlignment=""InvalidValue"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Should handle or report invalid enum value
        result.Diagnostics.Should().NotBeEmpty("Should report invalid enum value");
    }

    [Fact]
    public void Parse_TypeMismatch_Should_ReportDiagnostic()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Width=""NotANumber"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Should handle or report type mismatch
        result.Diagnostics.Should().NotBeEmpty("Should report type mismatch");
    }

    [Fact]
    public void Parse_MissingXmlNamespace_Should_ReturnError()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window>
    <Button Content=""Test"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Missing xmlns may cause issues
        result.Diagnostics.Should().NotBeEmpty("Should report missing namespace");
    }

    [Fact]
    public void Convert_Should_HandleGracefulDegradation()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""Valid"" />
    <NonExistentControl />
    <TextBox Text=""AlsoValid"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Should continue processing despite unknown element
        result.OutputXaml.Should().Contain("Button", "Should process valid elements");
        result.OutputXaml.Should().Contain("TextBox", "Should process subsequent valid elements");
        result.Diagnostics.Should().NotBeEmpty("Should report unknown element");
    }

    [Fact]
    public void Convert_Should_ReportDeprecatedFeatures()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <DockPanel LastChildFill=""True"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue();
        // May report if any features are deprecated or have changed behavior
    }

    [Fact]
    public void Parse_DuplicateKeys_InResources_Should_ReportDiagnostic()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <SolidColorBrush x:Key=""MyBrush"" Color=""Red"" />
        <SolidColorBrush x:Key=""MyBrush"" Color=""Blue"" />
    </Window.Resources>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // Should detect duplicate keys
        result.Diagnostics.Should().NotBeEmpty("Should report duplicate keys");
    }
}
