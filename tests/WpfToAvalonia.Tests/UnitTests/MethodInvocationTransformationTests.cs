using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Transformers.CSharp;

namespace WpfToAvalonia.Tests.UnitTests;

/// <summary>
/// Tests for method invocation transformation from WPF to Avalonia.
/// </summary>
public class MethodInvocationTransformationTests
{
    [Fact]
    public void TransformDispatcherInvoke_ToDispatcherUIThreadPost()
    {
        // Arrange
        var wpfCode = @"
using System;
using System.Windows.Threading;

namespace TestApp
{
    public class MyClass
    {
        private Dispatcher _dispatcher;

        public void DoWork()
        {
            _dispatcher.Invoke(() => Console.WriteLine(""Hello""));
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("_dispatcher.UIThread.Post");
        result.Should().NotContain("_dispatcher.Invoke");
    }

    [Fact]
    public void TransformDispatcherBeginInvoke_ToDispatcherUIThreadPost()
    {
        // Arrange
        var wpfCode = @"
using System;
using System.Windows.Threading;

namespace TestApp
{
    public class MyClass
    {
        private Dispatcher _dispatcher;

        public void DoWork()
        {
            _dispatcher.BeginInvoke(() => Console.WriteLine(""Hello""));
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("_dispatcher.UIThread.Post");
        result.Should().NotContain("_dispatcher.BeginInvoke");
    }

    [Fact]
    public void TransformDispatcherInvokeAsync_ToDispatcherUIThreadInvokeAsync()
    {
        // Arrange
        var wpfCode = @"
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TestApp
{
    public class MyClass
    {
        private Dispatcher _dispatcher;

        public async Task DoWorkAsync()
        {
            await _dispatcher.InvokeAsync(() => Console.WriteLine(""Hello""));
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("_dispatcher.UIThread.InvokeAsync");
    }

    [Fact]
    public void TransformDispatcherCheckAccess_ToDispatcherUIThreadCheckAccess()
    {
        // Arrange
        var wpfCode = @"
using System.Windows.Threading;

namespace TestApp
{
    public class MyClass
    {
        private Dispatcher _dispatcher;

        public bool IsOnUIThread()
        {
            return _dispatcher.CheckAccess();
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("_dispatcher.UIThread.CheckAccess");
    }

    [Fact]
    public void TransformVisualTreeHelperGetParent_ToGetVisualParent()
    {
        // Arrange
        var wpfCode = @"
using System.Windows;
using System.Windows.Media;

namespace TestApp
{
    public class MyClass
    {
        public DependencyObject GetParent(DependencyObject element)
        {
            return VisualTreeHelper.GetParent(element);
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("element.GetVisualParent");
        result.Should().NotContain("VisualTreeHelper.GetParent");
    }

    [Fact]
    public void TransformVisualTreeHelperGetChild_ToGetVisualChildrenElementAt()
    {
        // Arrange
        var wpfCode = @"
using System.Windows;
using System.Windows.Media;

namespace TestApp
{
    public class MyClass
    {
        public DependencyObject GetChild(DependencyObject element, int index)
        {
            return VisualTreeHelper.GetChild(element, index);
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("element.GetVisualChildren().ElementAt(index)");
        result.Should().NotContain("VisualTreeHelper.GetChild");
    }

    [Fact]
    public void TransformVisualTreeHelperGetChildrenCount_ToGetVisualChildrenCount()
    {
        // Arrange
        var wpfCode = @"
using System.Windows;
using System.Windows.Media;

namespace TestApp
{
    public class MyClass
    {
        public int GetChildCount(DependencyObject element)
        {
            return VisualTreeHelper.GetChildrenCount(element);
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("element.GetVisualChildren().Count()");
        result.Should().NotContain("VisualTreeHelper.GetChildrenCount");
    }

    [Fact]
    public void TransformLogicalTreeHelperGetParent_ToGetLogicalParent()
    {
        // Arrange
        var wpfCode = @"
using System.Windows;

namespace TestApp
{
    public class MyClass
    {
        public DependencyObject GetLogicalParent(DependencyObject element)
        {
            return LogicalTreeHelper.GetParent(element);
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("element.GetLogicalParent");
        result.Should().NotContain("LogicalTreeHelper.GetParent");
    }

    [Fact]
    public void TransformLogicalTreeHelperGetChildren_ToGetLogicalChildren()
    {
        // Arrange
        var wpfCode = @"
using System.Collections;
using System.Windows;

namespace TestApp
{
    public class MyClass
    {
        public IEnumerable GetLogicalChildren(DependencyObject element)
        {
            return LogicalTreeHelper.GetChildren(element);
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("element.GetLogicalChildren");
        result.Should().NotContain("LogicalTreeHelper.GetChildren");
    }

    [Fact]
    public void TransformKeyboardFocus_ToElementFocus()
    {
        // Arrange
        var wpfCode = @"
using System.Windows;
using System.Windows.Input;

namespace TestApp
{
    public class MyClass
    {
        public void SetFocus(UIElement element)
        {
            Keyboard.Focus(element);
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("element.Focus");
        result.Should().NotContain("Keyboard.Focus");
    }

    [Fact]
    public void TransformMultipleDispatcherMethods_InSameFile()
    {
        // Arrange
        var wpfCode = @"
using System;
using System.Windows.Threading;

namespace TestApp
{
    public class MyClass
    {
        private Dispatcher _dispatcher;

        public void Method1()
        {
            _dispatcher.Invoke(() => Console.WriteLine(""Method1""));
        }

        public void Method2()
        {
            _dispatcher.BeginInvoke(() => Console.WriteLine(""Method2""));
        }

        public bool Method3()
        {
            return _dispatcher.CheckAccess();
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("_dispatcher.UIThread.Post");
        result.Should().Contain("_dispatcher.UIThread.CheckAccess");
        result.Should().NotContain("_dispatcher.Invoke(() =>");
        result.Should().NotContain("_dispatcher.BeginInvoke");
    }

    [Fact]
    public void TransformNestedMethodCalls_HandlesCorrectly()
    {
        // Arrange
        var wpfCode = @"
using System.Windows;
using System.Windows.Media;

namespace TestApp
{
    public class MyClass
    {
        public DependencyObject GetGrandparent(DependencyObject element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            return VisualTreeHelper.GetParent(parent);
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("element.GetVisualParent");
        result.Should().Contain("parent.GetVisualParent");
        result.Should().NotContain("VisualTreeHelper.GetParent");
    }

    [Fact]
    public void PreserveNonWpfMethodInvocations()
    {
        // Arrange
        var wpfCode = @"
using System;

namespace TestApp
{
    public class MyClass
    {
        public void CustomMethod()
        {
            Console.WriteLine(""Hello"");
            var result = Math.Max(1, 2);
        }
    }
}";

        // Act
        var result = TransformCode(wpfCode);

        // Assert
        result.Should().Contain("Console.WriteLine");
        result.Should().Contain("Math.Max");
    }

    private string TransformCode(string wpfCode)
    {
        var tree = CSharpSyntaxTree.ParseText(wpfCode);
        var compilation = CSharpCompilation.Create("test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(tree);

        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = new DiagnosticCollector();
        var transformer = new MethodInvocationTransformer(diagnostics, semanticModel);

        var root = tree.GetRoot();
        var newRoot = transformer.Visit(root);

        return newRoot?.ToFullString() ?? string.Empty;
    }
}
