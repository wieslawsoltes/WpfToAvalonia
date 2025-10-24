using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Rewrites threading model code from WPF to Avalonia equivalents.
/// Implements task 2.5.3.3: Update threading model code
/// </summary>
/// <remarks>
/// WPF and Avalonia have different threading models:
///
/// WPF Threading:
/// - Dispatcher thread (UI thread)
/// - DispatcherObject base class with thread affinity checking
/// - Dispatcher.Invoke/BeginInvoke for cross-thread UI updates
/// - DispatcherPriority for operation scheduling
/// - Application.Current.Dispatcher
///
/// Avalonia Threading:
/// - UI thread (Dispatcher thread)
/// - No DispatcherObject base class (thread checking is less strict)
/// - Dispatcher.UIThread.Post/InvokeAsync for cross-thread UI updates
/// - DispatcherPriority enum exists but with different values
/// - Avalonia.Threading.Dispatcher.UIThread (static access)
///
/// Key Differences:
/// 1. Dispatcher.Invoke → Dispatcher.UIThread.InvokeAsync (async by default)
/// 2. Dispatcher.BeginInvoke → Dispatcher.UIThread.Post
/// 3. Application.Current.Dispatcher → Dispatcher.UIThread
/// 4. CheckAccess/VerifyAccess → CheckAccess still exists on controls
/// 5. Thread affinity is less strict in Avalonia
/// </remarks>
public sealed class ThreadingModelRewriter : WpfToAvaloniaRewriter
{
    private int _dispatcherInvokeCalls;
    private int _dispatcherBeginInvokeCalls;
    private int _checkAccessCalls;
    private int _verifyAccessCalls;
    private int _dispatcherPropertyAccess;
    private int _dispatcherObjectInheritance;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadingModelRewriter"/> class.
    /// </summary>
    public ThreadingModelRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Gets the number of Dispatcher.Invoke calls detected.
    /// </summary>
    public int DispatcherInvokeCalls => _dispatcherInvokeCalls;

    /// <summary>
    /// Gets the number of Dispatcher.BeginInvoke calls detected.
    /// </summary>
    public int DispatcherBeginInvokeCalls => _dispatcherBeginInvokeCalls;

    /// <summary>
    /// Gets the number of CheckAccess calls detected.
    /// </summary>
    public int CheckAccessCalls => _checkAccessCalls;

    /// <summary>
    /// Gets the number of VerifyAccess calls detected.
    /// </summary>
    public int VerifyAccessCalls => _verifyAccessCalls;

    /// <summary>
    /// Gets the number of Dispatcher property accesses detected.
    /// </summary>
    public int DispatcherPropertyAccess => _dispatcherPropertyAccess;

    /// <summary>
    /// Gets the number of DispatcherObject inheritance detected.
    /// </summary>
    public int DispatcherObjectInheritance => _dispatcherObjectInheritance;

    /// <summary>
    /// Visits invocation expressions to detect dispatcher method calls.
    /// </summary>
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;
            var symbolInfo = TryGetSymbolInfo(memberAccess);

            if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IMethodSymbol methodSymbol)
            {
                var containingType = methodSymbol.ContainingType?.ToDisplayString();

                // Detect Dispatcher.Invoke
                if (methodName == "Invoke" && containingType == "System.Windows.Threading.Dispatcher")
                {
                    _dispatcherInvokeCalls++;

                    Diagnostics.AddWarning(
                        "DISPATCHER_INVOKE_NEEDS_UPDATE",
                        $"Dispatcher.Invoke() needs to be updated for Avalonia. " +
                        $"Change to: await Dispatcher.UIThread.InvokeAsync(() => {{ /* your code */ }}); " +
                        $"Note: Avalonia's InvokeAsync is async by default. " +
                        $"Replace: dispatcher.Invoke(action) → await Dispatcher.UIThread.InvokeAsync(action) " +
                        $"Also update: Application.Current.Dispatcher.Invoke → Dispatcher.UIThread.InvokeAsync",
                        null);
                }
                // Detect Dispatcher.BeginInvoke
                else if (methodName == "BeginInvoke" && containingType == "System.Windows.Threading.Dispatcher")
                {
                    _dispatcherBeginInvokeCalls++;

                    Diagnostics.AddWarning(
                        "DISPATCHER_BEGIN_INVOKE_NEEDS_UPDATE",
                        $"Dispatcher.BeginInvoke() needs to be updated for Avalonia. " +
                        $"Change to: Dispatcher.UIThread.Post(() => {{ /* your code */ }}); " +
                        $"Or use: await Dispatcher.UIThread.InvokeAsync(() => {{ /* your code */ }}, DispatcherPriority.Background); " +
                        $"Replace: dispatcher.BeginInvoke(action) → Dispatcher.UIThread.Post(action)",
                        null);
                }
                // Detect CheckAccess
                else if (methodName == "CheckAccess")
                {
                    _checkAccessCalls++;

                    if (containingType == "System.Windows.Threading.Dispatcher")
                    {
                        Diagnostics.AddInfo(
                            "CHECK_ACCESS_DISPATCHER",
                            $"Dispatcher.CheckAccess() → Use Dispatcher.UIThread.CheckAccess() in Avalonia. " +
                            $"Pattern: if (!Dispatcher.UIThread.CheckAccess()) {{ await Dispatcher.UIThread.InvokeAsync(...); }}",
                            null);
                    }
                    else if (containingType == "System.Windows.Threading.DispatcherObject")
                    {
                        Diagnostics.AddInfo(
                            "CHECK_ACCESS_CONTROL",
                            $"CheckAccess() on UI element is available in Avalonia. " +
                            $"Syntax remains similar but consider using Dispatcher.UIThread.CheckAccess() for consistency.",
                            null);
                    }
                }
                // Detect VerifyAccess
                else if (methodName == "VerifyAccess" &&
                        (containingType == "System.Windows.Threading.Dispatcher" ||
                         containingType == "System.Windows.Threading.DispatcherObject"))
                {
                    _verifyAccessCalls++;

                    Diagnostics.AddWarning(
                        "VERIFY_ACCESS_NOT_SUPPORTED",
                        $"VerifyAccess() is not available in Avalonia. " +
                        $"Replace with CheckAccess(): " +
                        $"if (!Dispatcher.UIThread.CheckAccess()) throw new InvalidOperationException(\"Wrong thread\");",
                        null);
                }
            }
        }

        return base.VisitInvocationExpression(node);
    }

    /// <summary>
    /// Visits member access expressions to detect Dispatcher property access.
    /// </summary>
    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (node.Name.Identifier.Text == "Dispatcher")
        {
            var symbolInfo = TryGetSymbolInfo(node);
            if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IPropertySymbol propertySymbol)
            {
                var containingType = propertySymbol.ContainingType?.ToDisplayString();

                // Detect Application.Current.Dispatcher
                if (containingType == "System.Windows.Application")
                {
                    _dispatcherPropertyAccess++;

                    Diagnostics.AddInfo(
                        "DISPATCHER_PROPERTY_APPLICATION",
                        $"Application.Current.Dispatcher needs to be updated for Avalonia. " +
                        $"Change to: Dispatcher.UIThread (static property in Avalonia.Threading). " +
                        $"Replace: Application.Current.Dispatcher.Invoke(...) → await Dispatcher.UIThread.InvokeAsync(...)",
                        null);
                }
                // Detect DispatcherObject.Dispatcher
                else if (containingType == "System.Windows.Threading.DispatcherObject" ||
                        containingType == "System.Windows.DependencyObject")
                {
                    _dispatcherPropertyAccess++;

                    Diagnostics.AddInfo(
                        "DISPATCHER_PROPERTY_OBJECT",
                        $"Dispatcher property on UI element. In Avalonia, use Dispatcher.UIThread instead. " +
                        $"Replace: this.Dispatcher.Invoke(...) → await Dispatcher.UIThread.InvokeAsync(...)",
                        null);
                }
            }
        }
        // Detect DispatcherPriority enum usage
        else if (node.Name.Identifier.Text.Contains("DispatcherPriority"))
        {
            Diagnostics.AddInfo(
                "DISPATCHER_PRIORITY_CHECK",
                $"DispatcherPriority exists in Avalonia but with different values. " +
                $"WPF priorities: Inactive, SystemIdle, ApplicationIdle, ContextIdle, Background, Input, Loaded, Render, DataBind, Normal, Send. " +
                $"Avalonia priorities: MinValue, SystemIdle, ApplicationIdle, ContextIdle, Background, Input, Loaded, Render, DataBind, Normal, Send, MaxValue. " +
                $"Most common priorities work the same.",
                null);
        }

        return base.VisitMemberAccessExpression(node);
    }

    /// <summary>
    /// Visits class declarations to detect DispatcherObject inheritance.
    /// </summary>
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (node.BaseList != null)
        {
            foreach (var baseType in node.BaseList.Types)
            {
                var baseTypeName = baseType.Type.ToString();

                if (baseTypeName == "DispatcherObject" || baseTypeName == "System.Windows.Threading.DispatcherObject")
                {
                    _dispatcherObjectInheritance++;

                    var className = node.Identifier.Text;

                    Diagnostics.AddWarning(
                        "DISPATCHER_OBJECT_NOT_SUPPORTED",
                        $"Class '{className}' inherits from DispatcherObject, which doesn't exist in Avalonia. " +
                        $"Avalonia doesn't enforce thread affinity at the base class level. Options:\n" +
                        $"1. Remove DispatcherObject inheritance (most cases)\n" +
                        $"2. Use AvaloniaObject if you need dependency properties\n" +
                        $"3. Implement manual thread checking if needed: Dispatcher.UIThread.CheckAccess()",
                        null);
                }
            }
        }

        return base.VisitClassDeclaration(node);
    }

    /// <summary>
    /// Tries to get symbol info, catching exceptions from stale semantic models.
    /// </summary>
    private SymbolInfo? TryGetSymbolInfo(SyntaxNode node)
    {
        try
        {
            return SemanticModel.GetSymbolInfo(node);
        }
        catch
        {
            // Node might not exist in semantic model after previous transformations
            return null;
        }
    }
}
