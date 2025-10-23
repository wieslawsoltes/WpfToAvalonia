using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Runtime.InteropServices;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Transformers.CSharp;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Tests.IntegrationTests;

/// <summary>
/// Integration tests for full C# transformation pipeline (Task 2.1.2.6).
/// These tests verify the complete transformation from WPF C# code to Avalonia C# code,
/// including using directives, type references, and dependency property transformations.
///
/// NOTE: These tests require WPF assemblies to be available, which are only present on Windows.
/// Tests will be skipped on non-Windows platforms.
/// </summary>
public class CSharpTransformationIntegrationTests
{
    private readonly IMappingRepository _mappingRepository;
    private readonly bool _skipTests;

    public CSharpTransformationIntegrationTests()
    {
        // Skip tests on non-Windows platforms - WPF assemblies are not available
        _skipTests = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // Create a mapping repository with the default mappings file
        var mappingsPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "WpfToAvalonia.Mappings", "Data", "core-mappings.json");

        var repository = new JsonMappingRepository(mappingsPath);

        // Load mappings synchronously for test setup
        repository.LoadAsync().GetAwaiter().GetResult();

        _mappingRepository = repository;
    }

    /// <summary>
    /// Normalizes C# code by removing extra whitespace for comparison.
    /// </summary>
    private static string NormalizeCode(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var normalized = root.NormalizeWhitespace().ToFullString();

        // Further normalize for comparison
        return System.Text.RegularExpressions.Regex.Replace(
            normalized.Trim(),
            @"\s+",
            " ");
    }

    /// <summary>
    /// Transforms WPF C# code through the full transformation pipeline.
    /// </summary>
    private string TransformCode(string wpfCode)
    {
        // Parse the WPF code
        var tree = CSharpSyntaxTree.ParseText(wpfCode);
        var compilation = CSharpCompilation.Create("TestCompilation")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);

        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = new DiagnosticCollector();
        var root = tree.GetRoot();

        // Step 1: Transform using directives
        var usingRewriter = new UsingDirectivesRewriter(
            semanticModel,
            diagnostics,
            _mappingRepository);
        root = usingRewriter.Visit(root);

        // Re-compile for updated semantic model
        tree = tree.WithRootAndOptions(root, tree.Options);
        compilation = compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.First(),
            tree);
        semanticModel = compilation.GetSemanticModel(tree);

        // Step 2: Transform type references
        var typeRewriter = new TypeReferenceRewriter(
            semanticModel,
            diagnostics,
            _mappingRepository);
        root = typeRewriter.Visit(root);

        // Re-compile for updated semantic model
        tree = tree.WithRootAndOptions(root, tree.Options);
        compilation = compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.First(),
            tree);
        semanticModel = compilation.GetSemanticModel(tree);

        // Step 3: Transform property access
        var propertyRewriter = new PropertyAccessRewriter(
            semanticModel,
            diagnostics,
            _mappingRepository);
        root = propertyRewriter.Visit(root);

        // Re-compile for updated semantic model
        tree = tree.WithRootAndOptions(root, tree.Options);
        compilation = compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.First(),
            tree);
        semanticModel = compilation.GetSemanticModel(tree);

        // Step 4: Transform dependency properties
        var dpRewriter = new DependencyPropertyRewriter(
            semanticModel,
            diagnostics,
            _mappingRepository);
        root = dpRewriter.Visit(root);

        return root.ToFullString();
    }

    [Fact]
    public void Transform_SimpleDependencyProperty_FullPipeline()
    {
        if (_skipTests) return;

        // Arrange
        var wpfCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestApp
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(""Title"", typeof(string), typeof(MyControl));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
    }
}";

        var expectedCode = @"
using Avalonia;
using Avalonia.Controls;

namespace TestApp
{
    public class MyControl : Control
    {
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<MyControl, string>(""Title"");

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert - verify key transformations
        result.Should().Contain("using Avalonia;", "using directives should be transformed");
        result.Should().Contain("using Avalonia.Controls;", "control namespaces should be transformed");
        result.Should().NotContain("using System.Windows", "WPF usings should be removed");

        result.Should().Contain("StyledProperty<string>", "DependencyProperty should be transformed to StyledProperty");
        result.Should().Contain("AvaloniaProperty.Register", "Register method should be Avalonia-style");
        result.Should().NotContain("DependencyProperty", "DependencyProperty references should be removed");

        // Verify the transformation produces syntactically valid code
        var transformedTree = CSharpSyntaxTree.ParseText(result);
        transformedTree.GetDiagnostics().Should().BeEmpty("transformed code should be syntactically valid");
    }

    [Fact]
    public void Transform_ReadOnlyDependencyProperty_FullPipeline()
    {
        if (_skipTests) return;

        // Arrange
        var wpfCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestApp
{
    public class MyControl : Control
    {
        private static readonly DependencyPropertyKey IsLoadedPropertyKey =
            DependencyProperty.RegisterReadOnly(""IsLoaded"", typeof(bool), typeof(MyControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsLoadedProperty =
            IsLoadedPropertyKey.DependencyProperty;

        public bool IsLoaded
        {
            get => (bool)GetValue(IsLoadedProperty);
            private set => SetValue(IsLoadedPropertyKey, value);
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("using Avalonia;", "using directives should be transformed");
        result.Should().NotContain("using System.Windows", "WPF usings should be removed");

        result.Should().Contain("DirectProperty", "read-only property should use DirectProperty");
        result.Should().Contain("RegisterDirect", "read-only property should use RegisterDirect");
        result.Should().NotContain("DependencyPropertyKey", "DependencyPropertyKey should be removed");

        // Verify syntax validity
        var transformedTree = CSharpSyntaxTree.ParseText(result);
        transformedTree.GetDiagnostics().Should().BeEmpty("transformed code should be syntactically valid");
    }

    [Fact]
    public void Transform_AttachedProperty_FullPipeline()
    {
        if (_skipTests) return;

        // Arrange
        var wpfCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestApp
{
    public class DockPanel : Panel
    {
        public static readonly DependencyProperty DockProperty =
            DependencyProperty.RegisterAttached(""Dock"", typeof(Dock), typeof(DockPanel));

        public static Dock GetDock(UIElement element) =>
            (Dock)element.GetValue(DockProperty);

        public static void SetDock(UIElement element, Dock value) =>
            element.SetValue(DockProperty, value);
    }

    public enum Dock
    {
        Left, Top, Right, Bottom, Fill
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("using Avalonia;", "using directives should be transformed");
        result.Should().NotContain("using System.Windows", "WPF usings should be removed");

        result.Should().Contain("StyledProperty<Dock>", "attached property should use StyledProperty");
        result.Should().Contain("RegisterAttached", "attached property should use RegisterAttached");
        result.Should().Contain("Control element", "UIElement should be transformed to Control");
        result.Should().NotContain("UIElement element", "UIElement type should not remain");

        // Verify syntax validity
        var transformedTree = CSharpSyntaxTree.ParseText(result);
        transformedTree.GetDiagnostics().Should().BeEmpty("transformed code should be syntactically valid");
    }

    [Fact]
    public void Transform_PropertyWithMetadata_FullPipeline()
    {
        if (_skipTests) return;

        // Arrange
        var wpfCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestApp
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register(""Count"", typeof(int), typeof(MyControl),
                new PropertyMetadata(42));

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("using Avalonia;", "using directives should be transformed");
        result.Should().Contain("StyledProperty<int>", "property type should be preserved");
        result.Should().Contain("defaultValue:", "default value should be preserved");
        result.Should().Contain("42", "default value should be correct");

        // Verify syntax validity
        var transformedTree = CSharpSyntaxTree.ParseText(result);
        transformedTree.GetDiagnostics().Should().BeEmpty("transformed code should be syntactically valid");
    }

    [Fact]
    public void Transform_PropertyWithCallback_FullPipeline()
    {
        if (_skipTests) return;

        // Arrange
        var wpfCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestApp
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(""Value"", typeof(double), typeof(MyControl),
                new PropertyMetadata(0.0, OnValueChanged));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MyControl)d;
            // Handle change
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("using Avalonia;", "using directives should be transformed");
        result.Should().Contain("StyledProperty<double>", "property type should be preserved");
        result.Should().Contain("notifying:", "callback should be transformed to notifying");
        result.Should().Contain("AvaloniaObject", "DependencyObject should be transformed to AvaloniaObject");
        result.Should().NotContain("DependencyObject d", "DependencyObject parameter should be transformed");

        // Verify syntax validity
        var transformedTree = CSharpSyntaxTree.ParseText(result);
        transformedTree.GetDiagnostics().Should().BeEmpty("transformed code should be syntactically valid");
    }

    [Fact]
    public void Transform_MultipleProperties_FullPipeline()
    {
        if (_skipTests) return;

        // Arrange
        var wpfCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestApp
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register(""Name"", typeof(string), typeof(MyControl));

        public static readonly DependencyProperty AgeProperty =
            DependencyProperty.Register(""Age"", typeof(int), typeof(MyControl),
                new PropertyMetadata(0));

        public string Name
        {
            get => (string)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        public int Age
        {
            get => (int)GetValue(AgeProperty);
            set => SetValue(AgeProperty, value);
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("using Avalonia;", "using directives should be transformed");
        result.Should().NotContain("using System.Windows", "WPF usings should be removed");

        // Verify both properties are transformed
        result.Should().Contain("StyledProperty<string> NameProperty", "first property should be transformed");
        result.Should().Contain("StyledProperty<int> AgeProperty", "second property should be transformed");
        result.Should().Contain("AvaloniaProperty.Register<MyControl, string>(\"Name\")", "Name registration should be correct");
        result.Should().Contain("AvaloniaProperty.Register<MyControl, int>(\"Age\"", "Age registration should be correct");

        // Verify syntax validity
        var transformedTree = CSharpSyntaxTree.ParseText(result);
        transformedTree.GetDiagnostics().Should().BeEmpty("transformed code should be syntactically valid");
    }

    [Fact]
    public void Transform_ComplexControl_WithMultipleFeatures_FullPipeline()
    {
        if (_skipTests) return;

        // Arrange
        var wpfCode = @"
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TestApp
{
    public class CustomButton : Button
    {
        public static readonly DependencyProperty IsHighlightedProperty =
            DependencyProperty.Register(""IsHighlighted"", typeof(bool), typeof(CustomButton),
                new PropertyMetadata(false, OnIsHighlightedChanged));

        public static readonly DependencyProperty HighlightBrushProperty =
            DependencyProperty.Register(""HighlightBrush"", typeof(Brush), typeof(CustomButton));

        private static void OnIsHighlightedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (CustomButton)d;
            button.UpdateVisualState();
        }

        public bool IsHighlighted
        {
            get => (bool)GetValue(IsHighlightedProperty);
            set => SetValue(IsHighlightedProperty, value);
        }

        public Brush HighlightBrush
        {
            get => (Brush)GetValue(HighlightBrushProperty);
            set => SetValue(HighlightBrushProperty, value);
        }

        private void UpdateVisualState()
        {
            // Update visual state
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("using Avalonia;", "Avalonia using should be present");
        result.Should().Contain("using Avalonia.Controls;", "Avalonia.Controls using should be present");
        result.Should().Contain("using Avalonia.Media;", "Avalonia.Media using should be present for Brush");
        result.Should().NotContain("using System.Windows", "WPF usings should be removed");

        result.Should().Contain("StyledProperty<bool>", "bool property should be correct");
        result.Should().Contain("StyledProperty<IBrush>", "Brush should be transformed to IBrush");
        result.Should().Contain("AvaloniaObject", "DependencyObject should be transformed");
        result.Should().Contain("CustomButton)d", "cast should remain valid");

        // Verify syntax validity
        var transformedTree = CSharpSyntaxTree.ParseText(result);
        transformedTree.GetDiagnostics().Should().BeEmpty("transformed code should be syntactically valid");
    }
}
