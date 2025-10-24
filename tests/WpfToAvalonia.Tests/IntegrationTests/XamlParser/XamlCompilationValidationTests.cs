using FluentAssertions;
using WpfToAvalonia.XamlParser;
using Xunit;

namespace WpfToAvalonia.Tests.IntegrationTests.XamlParser;

/// <summary>
/// Validation tests to verify generated Avalonia XAML compiles.
/// Implements task 2.5.8.3.1: Verify generated Avalonia XAML compiles
/// </summary>
public class XamlCompilationValidationTests
{
    [Fact]
    public void Transform_SimpleWindow_ShouldProduceValidAvaloniaXaml()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 Title=""Test Window"" Width=""400"" Height=""300"">
    <Grid>
        <Button Content=""Click Me"" />
    </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("Conversion should succeed");
        result.OutputXaml.Should().NotBeNullOrEmpty("Output XAML should be generated");

        // Verify Avalonia namespace is present
        result.OutputXaml.Should().Contain("https://github.com/avaloniaui",
            "Output should contain Avalonia namespace");

        // Verify structure is preserved
        result.OutputXaml.Should().Contain("Window", "Window element should be preserved");
        result.OutputXaml.Should().Contain("Grid", "Grid element should be preserved");
        result.OutputXaml.Should().Contain("Button", "Button element should be preserved");

        // Verify it's well-formed XML
        var xmlValidation = ValidateXmlStructure(result.OutputXaml);
        xmlValidation.IsValid.Should().BeTrue(xmlValidation.ErrorMessage);
    }

    [Fact]
    public void Transform_WindowWithControls_ShouldHaveValidXamlStructure()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <StackPanel>
        <TextBlock Text=""Label"" />
        <TextBox />
        <CheckBox Content=""Option"" />
        <ComboBox />
    </StackPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        // Validate XML structure
        var xmlValidation = ValidateXmlStructure(result.OutputXaml);
        xmlValidation.IsValid.Should().BeTrue(xmlValidation.ErrorMessage);

        // Verify all controls are present
        result.OutputXaml.Should().Contain("StackPanel");
        result.OutputXaml.Should().Contain("TextBlock");
        result.OutputXaml.Should().Contain("TextBox");
        result.OutputXaml.Should().Contain("CheckBox");
        result.OutputXaml.Should().Contain("ComboBox");
    }

    [Fact]
    public void Transform_WindowWithResources_ShouldPreserveResourceStructure()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <SolidColorBrush x:Key=""MyBrush"" Color=""Blue"" />
        <Style x:Key=""MyButtonStyle"" TargetType=""Button"">
            <Setter Property=""Background"" Value=""Red"" />
        </Style>
    </Window.Resources>
    <Button Style=""{StaticResource MyButtonStyle}"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        // Validate XML structure
        var xmlValidation = ValidateXmlStructure(result.OutputXaml);
        xmlValidation.IsValid.Should().BeTrue(xmlValidation.ErrorMessage);

        // Verify resources section
        result.OutputXaml.Should().Contain("Resources", "Resources section should be preserved");
    }

    [Fact]
    public void Transform_WindowWithDataTemplate_ShouldPreserveTemplateStructure()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <ListBox>
        <ListBox.ItemTemplate>
            <DataTemplate>
                <StackPanel>
                    <TextBlock Text=""{Binding Name}"" />
                    <TextBlock Text=""{Binding Value}"" />
                </StackPanel>
            </DataTemplate>
        </ListBox.ItemTemplate>
    </ListBox>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        // Validate XML structure
        var xmlValidation = ValidateXmlStructure(result.OutputXaml);
        xmlValidation.IsValid.Should().BeTrue(xmlValidation.ErrorMessage);

        // Verify template structure
        result.OutputXaml.Should().Contain("DataTemplate", "DataTemplate should be preserved");
        result.OutputXaml.Should().Contain("Binding", "Binding expressions should be preserved");
    }

    [Fact]
    public void Transform_WindowWithBindings_ShouldPreserveBindingSyntax()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <StackPanel>
        <TextBox Text=""{Binding UserName, Mode=TwoWay}"" />
        <CheckBox IsChecked=""{Binding IsEnabled}"" />
        <Button Content=""Submit"" Command=""{Binding SubmitCommand}"" />
    </StackPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        // Validate XML structure
        var xmlValidation = ValidateXmlStructure(result.OutputXaml);
        xmlValidation.IsValid.Should().BeTrue(xmlValidation.ErrorMessage);

        // Verify binding syntax is valid
        result.OutputXaml.Should().Contain("Binding", "Binding expressions should be present");
    }

    [Fact]
    public void Transform_WindowWithGridLayout_ShouldPreserveGridDefinitions()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""*"" />
            <RowDefinition Height=""100"" />
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width=""200"" />
            <ColumnDefinition Width=""*"" />
        </Grid.ColumnDefinitions>
        <TextBlock Grid.Row=""0"" Grid.Column=""0"" Text=""Header"" />
        <TextBox Grid.Row=""1"" Grid.Column=""0"" Grid.ColumnSpan=""2"" />
    </Grid>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        // Validate XML structure
        var xmlValidation = ValidateXmlStructure(result.OutputXaml);
        xmlValidation.IsValid.Should().BeTrue(xmlValidation.ErrorMessage);

        // Verify grid definitions
        result.OutputXaml.Should().Contain("RowDefinition", "Row definitions should be preserved");
        result.OutputXaml.Should().Contain("ColumnDefinition", "Column definitions should be preserved");
        result.OutputXaml.Should().Contain("Grid.Row", "Grid.Row attached properties should be preserved");
        result.OutputXaml.Should().Contain("Grid.Column", "Grid.Column attached properties should be preserved");
    }

    [Fact]
    public void Transform_WindowWithStyles_ShouldProduceValidStyleSyntax()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Window.Resources>
        <Style x:Key=""HeaderStyle"" TargetType=""TextBlock"">
            <Setter Property=""FontSize"" Value=""20"" />
            <Setter Property=""FontWeight"" Value=""Bold"" />
            <Setter Property=""Foreground"" Value=""Blue"" />
        </Style>
    </Window.Resources>
    <TextBlock Style=""{StaticResource HeaderStyle}"" Text=""Title"" />
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        // Validate XML structure
        var xmlValidation = ValidateXmlStructure(result.OutputXaml);
        xmlValidation.IsValid.Should().BeTrue(xmlValidation.ErrorMessage);

        // Verify style syntax
        result.OutputXaml.Should().Contain("Style", "Style should be preserved");
        result.OutputXaml.Should().Contain("Setter", "Setters should be preserved");
    }

    [Fact]
    public void Transform_ComplexWindow_ShouldProduceValidCompleteXaml()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 Title=""Complex Window"" Width=""800"" Height=""600"">
    <Window.Resources>
        <Style x:Key=""ButtonStyle"" TargetType=""Button"">
            <Setter Property=""Margin"" Value=""5"" />
        </Style>
    </Window.Resources>
    <DockPanel>
        <Menu DockPanel.Dock=""Top"">
            <MenuItem Header=""File"" />
            <MenuItem Header=""Edit"" />
        </Menu>
        <StatusBar DockPanel.Dock=""Bottom"">
            <TextBlock Text=""Ready"" />
        </StatusBar>
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width=""200"" />
                <ColumnDefinition Width=""*"" />
            </Grid.ColumnDefinitions>
            <ListBox Grid.Column=""0"" />
            <TabControl Grid.Column=""1"">
                <TabItem Header=""Tab 1"">
                    <TextBlock Text=""Content 1"" />
                </TabItem>
            </TabControl>
        </Grid>
    </DockPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue("Complex window should convert successfully");

        // Validate XML structure
        var xmlValidation = ValidateXmlStructure(result.OutputXaml);
        xmlValidation.IsValid.Should().BeTrue(xmlValidation.ErrorMessage);

        // Verify all major components are present
        result.OutputXaml.Should().Contain("DockPanel");
        result.OutputXaml.Should().Contain("Menu");
        result.OutputXaml.Should().Contain("StatusBar");
        result.OutputXaml.Should().Contain("Grid");
        result.OutputXaml.Should().Contain("ListBox");
        result.OutputXaml.Should().Contain("TabControl");
    }

    [Fact]
    public void Transform_WindowWithAttachedProperties_ShouldPreserveAttachedProperties()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <DockPanel>
        <Menu DockPanel.Dock=""Top"" />
        <StatusBar DockPanel.Dock=""Bottom"" />
        <Canvas>
            <Rectangle Canvas.Left=""10"" Canvas.Top=""20"" Width=""100"" Height=""50"" />
            <Ellipse Canvas.Left=""150"" Canvas.Top=""100"" Width=""80"" Height=""80"" />
        </Canvas>
    </DockPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        // Validate XML structure
        var xmlValidation = ValidateXmlStructure(result.OutputXaml);
        xmlValidation.IsValid.Should().BeTrue(xmlValidation.ErrorMessage);

        // Verify attached properties
        result.OutputXaml.Should().Contain("DockPanel.Dock", "DockPanel.Dock should be preserved");
        result.OutputXaml.Should().Contain("Canvas.Left", "Canvas.Left should be preserved");
        result.OutputXaml.Should().Contain("Canvas.Top", "Canvas.Top should be preserved");
    }

    [Fact]
    public void Transform_WindowWithNamespaces_ShouldHandleNamespaceCorrectly()
    {
        // Arrange
        var converter = new WpfToAvaloniaConverter();
        var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                 xmlns:local=""clr-namespace:MyApp"">
    <StackPanel>
        <TextBlock Text=""Test"" />
    </StackPanel>
</Window>";

        // Act
        var result = converter.Convert(wpfXaml);

        // Assert
        result.Success.Should().BeTrue();

        // Validate XML structure
        var xmlValidation = ValidateXmlStructure(result.OutputXaml);
        xmlValidation.IsValid.Should().BeTrue(xmlValidation.ErrorMessage);
    }

    /// <summary>
    /// Helper method to validate XML structure
    /// </summary>
    private (bool IsValid, string ErrorMessage) ValidateXmlStructure(string xaml)
    {
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xaml);

            // Check that root element exists
            if (doc.Root == null)
            {
                return (false, "Root element is null");
            }

            // Check that root element has a namespace
            if (string.IsNullOrEmpty(doc.Root.Name.NamespaceName))
            {
                return (false, "Root element has no namespace");
            }

            return (true, string.Empty);
        }
        catch (System.Xml.XmlException ex)
        {
            return (false, $"XML parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"Unexpected error: {ex.Message}");
        }
    }
}
