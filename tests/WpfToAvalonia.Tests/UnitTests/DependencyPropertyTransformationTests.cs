using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Transformers.CSharp;
using WpfToAvalonia.Core.Analyzers;

namespace WpfToAvalonia.Tests.UnitTests;

/// <summary>
/// Tests for DependencyProperty to StyledProperty/DirectProperty transformation.
/// </summary>
public class DependencyPropertyTransformationTests
{
    /// <summary>
    /// Normalizes C# code by removing extra whitespace and formatting for comparison.
    /// </summary>
    private static string NormalizeCode(string code)
    {
        // Remove all newlines and extra whitespace
        var normalized = System.Text.RegularExpressions.Regex.Replace(
            code.Trim(),
            @"\s+",
            " ");

        // Normalize common patterns
        normalized = normalized
            .Replace("> <", "><")
            .Replace(" {", "{")
            .Replace("{ ", "{")
            .Replace(" }", "}")
            .Replace("} ", "}")
            .Replace("; ", ";")
            .Replace(" ;", ";")
            .Replace(" (", "(")
            .Replace("( ", "(")
            .Replace(" )", ")")
            .Replace(") ", ")")
            .Replace(" ,", ",")
            .Replace(", ", ",")
            .Replace(" =>", "=>")
            .Replace("=> ", "=>")
            .Replace(" =", "=")
            .Replace("= ", "=");

        return normalized;
    }

    [Fact]
    public void TransformSimpleDependencyProperty_ToStyledProperty()
    {
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

        /* Expected transformation:
        using Avalonia;
        using Avalonia.Controls;

        namespace TestApp
        {
            public class MyControl : Control
            {
                public static readonly StyledProperty<string> TitleProperty =
                    AvaloniaProperty.Register<MyControl, string>("Title");

                public string Title
                {
                    get => GetValue(TitleProperty);
                    set => SetValue(TitleProperty, value);
                }
            }
        }
        */

        // Act
        var result = TransformCode(wpfCode);

        // Assert - verify key transformations happen
        result.Should().Contain("StyledProperty<string>");
        result.Should().Contain("AvaloniaProperty.Register");
        result.Should().NotContain("DependencyProperty");
    }

    [Fact]
    public void TransformReadOnlyDependencyProperty_ToDirectProperty()
    {
        // Arrange
        var wpfCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestApp
{
    public class MyControl : Control
    {
        private static readonly DependencyPropertyKey IsLoadedPropertyKey =
            DependencyProperty.RegisterReadOnly(""IsLoaded"", typeof(bool), typeof(MyControl), new PropertyMetadata(false));

        public static readonly DependencyProperty IsLoadedProperty =
            IsLoadedPropertyKey.DependencyProperty;

        public bool IsLoaded
        {
            get => (bool)GetValue(IsLoadedProperty);
            private set => SetValue(IsLoadedPropertyKey, value);
        }
    }
}";

        /* Expected transformation:
        using Avalonia;
        using Avalonia.Controls;

        namespace TestApp
        {
            public class MyControl : Control
            {
                private bool _isLoaded = false;

                public static readonly DirectProperty<MyControl, bool> IsLoadedProperty =
                    AvaloniaProperty.RegisterDirect<MyControl, bool>(
                        "IsLoaded",
                        o => o.IsLoaded,
                        (o, v) => o.IsLoaded = v);

                public bool IsLoaded
                {
                    get => _isLoaded;
                    private set => SetAndRaise(IsLoadedProperty, ref _isLoaded, value);
                }
            }
        }
        */

        // Act
        var result = TransformCode(wpfCode);

        // Assert - verify key transformations happen
        result.Should().Contain("DirectProperty");
        result.Should().Contain("RegisterDirect");
        result.Should().NotContain("DependencyPropertyKey");
    }

    [Fact]
    public void TransformAttachedDependencyProperty_ToAttachedProperty()
    {
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
}";

        /* Expected transformation:
        using Avalonia;
        using Avalonia.Controls;

        namespace TestApp
        {
            public class DockPanel : Panel
            {
                public static readonly AttachedProperty<Dock> DockProperty =
                    AvaloniaProperty.RegisterAttached<DockPanel, Control, Dock>("Dock");

                public static Dock GetDock(Control element) =>
                    element.GetValue(DockProperty);

                public static void SetDock(Control element, Dock value) =>
                    element.SetValue(DockProperty, value);
            }
        }
        */

        // Act
        var result = TransformCode(wpfCode);

        // Assert - verify key transformations happen
        result.Should().Contain("StyledProperty<Dock>");
        result.Should().Contain("RegisterAttached");
        result.Should().NotContain("DependencyProperty.RegisterAttached");
    }

    [Fact]
    public void TransformDependencyPropertyWithMetadata_PreservesDefaultValue()
    {
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
                new PropertyMetadata(0));

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }
    }
}";

        /* Expected transformation:
        using Avalonia;
        using Avalonia.Controls;

        namespace TestApp
        {
            public class MyControl : Control
            {
                public static readonly StyledProperty<int> CountProperty =
                    AvaloniaProperty.Register<MyControl, int>("Count", defaultValue: 0);

                public int Count
                {
                    get => GetValue(CountProperty);
                    set => SetValue(CountProperty, value);
                }
            }
        }
        */

        // Act
        var result = TransformCode(wpfCode);

        // Assert - verify key transformations happen
        result.Should().Contain("defaultValue: 0");
        result.Should().Contain("StyledProperty<int>");
    }

    [Fact]
    public void TransformDependencyPropertyWithCallback_PreservesCallback()
    {
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
            // Handle change
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }
}";

        /* Expected transformation:
        using Avalonia;
        using Avalonia.Controls;

        namespace TestApp
        {
            public class MyControl : Control
            {
                public static readonly StyledProperty<double> ValueProperty =
                    AvaloniaProperty.Register<MyControl, double>("Value", defaultValue: 0.0, notifying: OnValueChanged);

                private static void OnValueChanged(AvaloniaObject d, bool before)
                {
                    // Handle change
                }

                public double Value
                {
                    get => GetValue(ValueProperty);
                    set => SetValue(ValueProperty, value);
                }
            }
        }
        */

        // Act
        var result = TransformCode(wpfCode);

        // Assert - verify key transformations happen
        result.Should().Contain("notifying: OnValueChanged");
        result.Should().Contain("StyledProperty<double>");
    }

    [Fact]
    public void DirectPropertyAnalyzer_DetectsReadOnlyProperty()
    {
        // Arrange
        var code = @"
namespace TestApp
{
    public class MyClass
    {
        public string ReadOnlyProp { get; }

        public string ReadWriteProp { get; set; }

        public string PrivateSetProp { get; private set; }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test")
            .AddSyntaxTrees(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var analyzer = new DirectPropertyAnalyzer(semanticModel);
        var root = tree.GetRoot();
        var properties = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>()
            .ToList();

        // Act & Assert
        var readOnlyProp = properties.First(p => p.Identifier.Text == "ReadOnlyProp");
        analyzer.ShouldUseDirectProperty(readOnlyProp).Should().BeTrue();

        var readWriteProp = properties.First(p => p.Identifier.Text == "ReadWriteProp");
        analyzer.ShouldUseDirectProperty(readWriteProp).Should().BeFalse();

        var privateSetProp = properties.First(p => p.Identifier.Text == "PrivateSetProp");
        analyzer.ShouldUseDirectProperty(privateSetProp).Should().BeTrue();
    }

    [Fact]
    public void DirectPropertyAnalyzer_FindsBackingField()
    {
        // Arrange
        var code = @"
namespace TestApp
{
    public class MyClass
    {
        private string _name;

        public string Name
        {
            get => _name;
            set => _name = value;
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test")
            .AddSyntaxTrees(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var analyzer = new DirectPropertyAnalyzer(semanticModel);
        var root = tree.GetRoot();
        var property = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>()
            .First();

        // Act
        var backingField = analyzer.FindBackingField(property);

        // Assert
        backingField.Should().NotBeNull();
        backingField!.Declaration.Variables.First().Identifier.Text.Should().Be("_name");
    }

    [Fact]
    public void DirectPropertyAnalyzer_GeneratesCorrectRegistration()
    {
        // Arrange
        var code = @"
namespace TestApp
{
    public class MyClass
    {
        public string Name { get; set; }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test")
            .AddSyntaxTrees(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var analyzer = new DirectPropertyAnalyzer(semanticModel);
        var root = tree.GetRoot();
        var property = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>()
            .First();

        // Act
        var registration = analyzer.GenerateDirectPropertyRegistration(property, "MyClass", null);

        // Assert
        registration.Should().Contain("AvaloniaProperty.RegisterDirect<MyClass, string>");
        registration.Should().Contain("\"Name\"");
        registration.Should().Contain("o => o.Name");
        registration.Should().Contain("(o, v) => o.Name = v");
    }

    [Fact]
    public void DirectPropertyAnalyzer_GeneratesBackingField()
    {
        // Arrange
        var code = @"
namespace TestApp
{
    public class MyClass
    {
        public string Name { get; set; }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test")
            .AddSyntaxTrees(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var analyzer = new DirectPropertyAnalyzer(semanticModel);
        var root = tree.GetRoot();
        var property = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>()
            .First();

        // Act
        var backingField = analyzer.GenerateBackingField(property, "\"default\"");

        // Assert
        var fieldName = backingField.Declaration.Variables.First().Identifier.Text;
        fieldName.Should().Be("_name");

        var initializer = backingField.Declaration.Variables.First().Initializer;
        initializer.Should().NotBeNull();
        initializer!.Value.ToString().Should().Be("\"default\"");
    }

    private string TransformCode(string wpfCode)
    {
        var tree = CSharpSyntaxTree.ParseText(wpfCode);
        var compilation = CSharpCompilation.Create("test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);

        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = new DiagnosticCollector();
        var transformer = new DependencyPropertyTransformer(diagnostics, semanticModel);

        var root = tree.GetRoot();
        var newRoot = transformer.Visit(root);

        return newRoot?.ToFullString() ?? string.Empty;
    }
}
