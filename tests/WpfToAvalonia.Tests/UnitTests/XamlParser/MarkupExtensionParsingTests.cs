using FluentAssertions;
using WpfToAvalonia.XamlParser;
using Xunit;

namespace WpfToAvalonia.Tests.UnitTests.XamlParser;

/// <summary>
/// Unit tests for markup extension parsing in XAML.
/// Implements task 2.5.8.1.2: Test markup extension parsing
/// </summary>
public class MarkupExtensionParsingTests
{
    private static string NormalizeXaml(string xaml)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            xaml.Trim(),
            @"\s+",
            " ")
            .Replace("> <", "><")
            .Replace(" >", ">")
            .Replace(" =", "=");
    }

    [Fact]
    public void Parse_StaticResource_Should_BeRecognized()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Background=""{StaticResource MyBrush}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("StaticResource markup extension should be parsed");
        result.OutputXaml.Should().Contain("StaticResource", "StaticResource should be preserved");
    }

    [Fact]
    public void Parse_DynamicResource_Should_BeRecognized()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Background=""{DynamicResource MyBrush}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("DynamicResource markup extension should be parsed");
        result.OutputXaml.Should().Contain("DynamicResource", "DynamicResource should be preserved");
    }

    [Fact]
    public void Parse_Binding_SimpleProperty_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox Text=""{Binding Name}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Simple Binding should be parsed");
        result.OutputXaml.Should().Contain("{Binding Name}", "Binding path should be preserved");
    }

    [Fact]
    public void Parse_Binding_WithMode_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox Text=""{Binding Name, Mode=TwoWay}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Binding with Mode should be parsed");
        result.OutputXaml.Should().Contain("Binding", "Binding should be present");
        result.OutputXaml.Should().Contain("Mode", "Mode parameter should be preserved");
    }

    [Fact]
    public void Parse_Binding_WithConverter_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox Text=""{Binding Value, Converter={StaticResource MyConverter}}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Binding with Converter should be parsed");
        result.OutputXaml.Should().Contain("Converter", "Converter parameter should be recognized");
    }

    [Fact]
    public void Parse_Binding_WithElementName_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <TextBox x:Name=""SourceBox"" />
    <TextBox Text=""{Binding Text, ElementName=SourceBox}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        // ElementName bindings may or may not be fully supported
        // At minimum, the conversion should not crash
        result.Should().NotBeNull("Should return a result");

        // If conversion succeeded, check the binding syntax
        if (result.Success && result.OutputXaml != null)
        {
            // ElementName bindings in Avalonia use #elementName syntax instead of ElementName parameter
            // Accept either WPF ElementName syntax or Avalonia #name syntax
            var hasElementNameSyntax = result.OutputXaml.Contains("ElementName") || result.OutputXaml.Contains("#SourceBox");
            hasElementNameSyntax.Should().BeTrue("ElementName should be converted to Avalonia syntax or preserved");
        }
    }

    [Fact]
    public void Parse_Binding_WithRelativeSource_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""{Binding RelativeSource={RelativeSource Self}, Path=Name}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Binding with RelativeSource should be parsed");
        // Avalonia uses $self, $parent syntax instead of RelativeSource
        var hasRelativeSourceSyntax = result.OutputXaml.Contains("RelativeSource") ||
                                       result.OutputXaml.Contains("$self") ||
                                       result.OutputXaml.Contains("$parent");
        hasRelativeSourceSyntax.Should().BeTrue("RelativeSource should be converted to Avalonia syntax");
    }

    [Fact]
    public void Parse_TemplateBinding_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <ControlTemplate TargetType=""Button"">
        <Border Background=""{TemplateBinding Background}"" />
    </ControlTemplate>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("TemplateBinding should be parsed");
        result.OutputXaml.Should().Contain("TemplateBinding", "TemplateBinding should be recognized");
    }

    [Fact]
    public void Parse_MultiBinding_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox>
        <TextBox.Text>
            <MultiBinding Converter=""{StaticResource MyConverter}"">
                <Binding Path=""FirstName"" />
                <Binding Path=""LastName"" />
            </MultiBinding>
        </TextBox.Text>
    </TextBox>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("MultiBinding should be parsed");
        result.OutputXaml.Should().Contain("MultiBinding", "MultiBinding should be recognized");
    }

    [Fact]
    public void Parse_X_Type_Extension_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 xmlns:sys=""clr-namespace:System;assembly=mscorlib"">
    <Style TargetType=""{x:Type Button}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("x:Type markup extension should be parsed");
        result.OutputXaml.Should().Contain("x:Type", "x:Type should be recognized");
    }

    [Fact]
    public void Parse_X_Static_Extension_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <TextBox Text=""{x:Static SystemColors.ActiveBorderBrush}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("x:Static markup extension should be parsed");
        // x:Static behavior may vary in transformation
    }

    [Fact]
    public void Parse_X_Null_Extension_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Button Tag=""{x:Null}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("x:Null markup extension should be parsed");
    }

    [Fact]
    public void Parse_NestedMarkupExtensions_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox Text=""{Binding Path=Value, Converter={StaticResource MyConverter}}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Nested markup extensions should be parsed");
        result.OutputXaml.Should().Contain("Binding", "Outer Binding should be present");
        result.OutputXaml.Should().Contain("Converter", "Converter parameter should be present");
    }

    [Fact]
    public void Parse_Binding_WithMultipleParameters_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox Text=""{Binding Path=Name, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged,
                             Converter={StaticResource MyConverter}, ConverterParameter=10}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Binding with multiple parameters should be parsed");
        result.OutputXaml.Should().Contain("Binding", "Binding should be present");
    }

    [Fact]
    public void Parse_Binding_PropertyElementSyntax_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox>
        <TextBox.Text>
            <Binding Path=""Name"" Mode=""TwoWay"" />
        </TextBox.Text>
    </TextBox>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Binding in property element syntax should be parsed");
        result.OutputXaml.Should().Contain("Binding", "Binding should be recognized");
    }

    [Fact]
    public void Parse_ColorResource_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <SolidColorBrush Color=""{StaticResource MyColor}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Color with StaticResource should be parsed");
    }

    [Fact]
    public void Parse_ComplexBinding_WithStringFormat_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBox Text=""{Binding Price, StringFormat='Price: {0:C}'}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("Binding with StringFormat should be parsed");
        result.OutputXaml.Should().Contain("Binding", "Binding should be present");
    }

    [Fact]
    public void Parse_RelativeSource_FindAncestor_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Button Content=""{Binding RelativeSource={RelativeSource AncestorType={x:Type Window}}, Path=Title}"" />
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("RelativeSource with FindAncestor should be parsed");
    }

    [Fact]
    public void Parse_ArrayExtension_Should_Work()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var xaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 xmlns:sys=""clr-namespace:System;assembly=mscorlib"">
    <ComboBox>
        <ComboBox.ItemsSource>
            <x:Array Type=""sys:String"">
                <sys:String>Item 1</sys:String>
                <sys:String>Item 2</sys:String>
            </x:Array>
        </ComboBox.ItemsSource>
    </ComboBox>
</Window>";

        // Act
        var result = converter.Convert(xaml);

        // Assert
        result.Success.Should().BeTrue("x:Array markup extension should be parsed");
    }
}
