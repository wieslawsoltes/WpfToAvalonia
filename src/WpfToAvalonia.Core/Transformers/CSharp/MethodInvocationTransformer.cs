using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Transforms WPF method invocations to Avalonia equivalents.
/// </summary>
public sealed class MethodInvocationTransformer : CSharpSyntaxRewriter
{
    private readonly DiagnosticCollector _diagnostics;
    private readonly SemanticModel _semanticModel;

    // Mapping of WPF methods to Avalonia equivalents
    private static readonly Dictionary<string, string> MethodMappings = new()
    {
        // Dispatcher methods
        { "Dispatcher.Invoke", "Dispatcher.UIThread.Post" },
        { "Dispatcher.BeginInvoke", "Dispatcher.UIThread.Post" },
        { "Dispatcher.InvokeAsync", "Dispatcher.UIThread.InvokeAsync" },
        { "Dispatcher.CheckAccess", "Dispatcher.UIThread.CheckAccess" },

        // Visual Tree methods
        { "VisualTreeHelper.GetParent", "Visual.GetVisualParent" },
        { "VisualTreeHelper.GetChild", "Visual.GetVisualChildren" },
        { "VisualTreeHelper.GetChildrenCount", "Visual.GetVisualChildren().Count()" },
        { "VisualTreeHelper.HitTest", "Visual.InputHitTest" },

        // Logical Tree methods
        { "LogicalTreeHelper.GetParent", "Logical.GetLogicalParent" },
        { "LogicalTreeHelper.GetChildren", "Logical.GetLogicalChildren" },
        { "LogicalTreeHelper.FindLogicalNode", "Logical.GetLogicalDescendants" },

        // Focus management
        { "FocusManager.SetFocusedElement", "FocusManager.SetFocusedElement" },
        { "FocusManager.GetFocusedElement", "FocusManager.GetFocusedElement" },
        { "Keyboard.Focus", "Focus" },

        // Routed commands
        { "ApplicationCommands.Close", "ApplicationCommands.Close" },
        { "ApplicationCommands.Copy", "ApplicationCommands.Copy" },
        { "ApplicationCommands.Cut", "ApplicationCommands.Cut" },
        { "ApplicationCommands.Paste", "ApplicationCommands.Paste" },
    };

    public MethodInvocationTransformer(DiagnosticCollector diagnostics, SemanticModel semanticModel)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        // Try to transform method invocation on the original node (before visiting children)
        // We need to use the original node for semantic analysis
        var transformedNode = TransformMethodInvocation(node);
        if (transformedNode != null && transformedNode != node)
        {
            // Return the transformed node directly without revisiting
            // The transformed node is already complete and doesn't need semantic analysis
            return transformedNode;
        }

        // No transformation, visit children normally
        return base.VisitInvocationExpression(node);
    }

    private InvocationExpressionSyntax? TransformMethodInvocation(InvocationExpressionSyntax node)
    {
        var memberAccess = node.Expression as MemberAccessExpressionSyntax;
        if (memberAccess == null)
        {
            return node;
        }

        // Get the full method name
        var methodName = GetFullMethodName(memberAccess);

        // Get type information for the expression
        var symbolInfo = _semanticModel.GetSymbolInfo(memberAccess.Expression);
        var typeInfo = _semanticModel.GetTypeInfo(memberAccess.Expression);
        var typeName = typeInfo.Type?.ToDisplayString() ?? "";

        // Transform Dispatcher methods
        if (IsDispatcherMethod(methodName, typeName))
        {
            return TransformDispatcherMethod(node, memberAccess, methodName);
        }

        // Transform VisualTreeHelper methods
        if (IsVisualTreeHelperMethod(methodName))
        {
            return TransformVisualTreeHelperMethod(node, memberAccess, methodName);
        }

        // Transform LogicalTreeHelper methods
        if (IsLogicalTreeHelperMethod(methodName))
        {
            return TransformLogicalTreeHelperMethod(node, memberAccess, methodName);
        }

        // Transform Focus methods
        if (IsFocusMethod(methodName))
        {
            return TransformFocusMethod(node, memberAccess, methodName);
        }

        // Transform routed command methods
        if (IsRoutedCommandMethod(methodName))
        {
            return TransformRoutedCommandMethod(node, memberAccess, methodName);
        }

        return node;
    }

    private InvocationExpressionSyntax TransformDispatcherMethod(
        InvocationExpressionSyntax node,
        MemberAccessExpressionSyntax memberAccess,
        string methodName)
    {
        // WPF: Dispatcher.Invoke(() => { ... })
        // Avalonia: Dispatcher.UIThread.Post(() => { ... })

        // WPF: Dispatcher.BeginInvoke(() => { ... })
        // Avalonia: Dispatcher.UIThread.Post(() => { ... })

        // WPF: await Dispatcher.InvokeAsync(() => { ... })
        // Avalonia: await Dispatcher.UIThread.InvokeAsync(() => { ... })

        // Visit the base expression to handle nested invocations
        var baseExpression = (ExpressionSyntax)Visit(memberAccess.Expression)!;
        var methodIdentifier = memberAccess.Name;

        InvocationExpressionSyntax newInvocation;

        if (methodName.Contains("Invoke") || methodName.Contains("BeginInvoke"))
        {
            // Transform to Dispatcher.UIThread.Post or InvokeAsync
            var useInvokeAsync = methodName.Contains("InvokeAsync");
            var newMethodName = useInvokeAsync ? "InvokeAsync" : "Post";

            var dispatcherUIThread = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                baseExpression,
                SyntaxFactory.IdentifierName("UIThread"));

            var newMemberAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                dispatcherUIThread,
                SyntaxFactory.IdentifierName(newMethodName));

            // Filter arguments FIRST (before visiting), then visit the filtered ones
            var args = node.ArgumentList.Arguments;
            var filteredArgs = FilterDispatcherPriorityArguments(args);

            // Now visit the filtered arguments
            var visitedArgs = filteredArgs.Select(arg =>
                SyntaxFactory.Argument(
                    arg.NameColon,
                    arg.RefKindKeyword,
                    (ExpressionSyntax)Visit(arg.Expression)!));

            newInvocation = SyntaxFactory.InvocationExpression(
                newMemberAccess,
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(visitedArgs)));

            _diagnostics.AddInfo(
                "DISPATCHER_METHOD_TRANSFORMED",
                $"Transformed Dispatcher method '{methodName}' to '{newMethodName}'",
                node.GetLocation().GetLineSpan().Path,
                node.GetLocation().GetLineSpan().StartLinePosition.Line);
        }
        else if (methodName.Contains("CheckAccess"))
        {
            // Dispatcher.CheckAccess() -> Dispatcher.UIThread.CheckAccess()
            var dispatcherUIThread = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                baseExpression,
                SyntaxFactory.IdentifierName("UIThread"));

            var newMemberAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                dispatcherUIThread,
                methodIdentifier);

            newInvocation = node.WithExpression(newMemberAccess);

            _diagnostics.AddInfo(
                "DISPATCHER_METHOD_TRANSFORMED",
                $"Transformed Dispatcher.CheckAccess to Dispatcher.UIThread.CheckAccess",
                node.GetLocation().GetLineSpan().Path,
                node.GetLocation().GetLineSpan().StartLinePosition.Line);
        }
        else
        {
            return node;
        }

        return newInvocation;
    }

    private InvocationExpressionSyntax TransformVisualTreeHelperMethod(
        InvocationExpressionSyntax node,
        MemberAccessExpressionSyntax memberAccess,
        string methodName)
    {
        // WPF: VisualTreeHelper.GetParent(element)
        // Avalonia: element.GetVisualParent()

        // WPF: VisualTreeHelper.GetChild(element, index)
        // Avalonia: element.GetVisualChildren().ElementAt(index)

        // WPF: VisualTreeHelper.GetChildrenCount(element)
        // Avalonia: element.GetVisualChildren().Count()

        var args = node.ArgumentList.Arguments;
        if (args.Count == 0)
        {
            return node;
        }

        // Visit the argument expressions to transform any nested invocations
        var visitedArgs = SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList(args.Select(arg =>
                SyntaxFactory.Argument(
                    arg.NameColon,
                    arg.RefKindKeyword,
                    (ExpressionSyntax)Visit(arg.Expression)!))));

        var targetElement = visitedArgs.Arguments[0].Expression;

        if (methodName.Contains("GetParent"))
        {
            // Transform to element.GetVisualParent()
            var newMemberAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                targetElement,
                SyntaxFactory.IdentifierName("GetVisualParent"));

            var newInvocation = SyntaxFactory.InvocationExpression(
                newMemberAccess,
                SyntaxFactory.ArgumentList());

            _diagnostics.AddInfo(
                "VISUAL_TREE_METHOD_TRANSFORMED",
                "Transformed VisualTreeHelper.GetParent to GetVisualParent",
                node.GetLocation().GetLineSpan().Path,
                node.GetLocation().GetLineSpan().StartLinePosition.Line);

            return newInvocation;
        }
        else if (methodName.Contains("GetChildrenCount"))
        {
            // Transform to element.GetVisualChildren().Count()
            // Check this BEFORE "GetChild" because "GetChildrenCount" contains "GetChild"
            var getVisualChildren = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                targetElement,
                SyntaxFactory.IdentifierName("GetVisualChildren"));

            var getVisualChildrenInvocation = SyntaxFactory.InvocationExpression(
                getVisualChildren,
                SyntaxFactory.ArgumentList());

            var count = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                getVisualChildrenInvocation,
                SyntaxFactory.IdentifierName("Count"));

            var newInvocation = SyntaxFactory.InvocationExpression(
                count,
                SyntaxFactory.ArgumentList());

            _diagnostics.AddInfo(
                "VISUAL_TREE_METHOD_TRANSFORMED",
                "Transformed VisualTreeHelper.GetChildrenCount to GetVisualChildren().Count()",
                node.GetLocation().GetLineSpan().Path,
                node.GetLocation().GetLineSpan().StartLinePosition.Line);

            return newInvocation;
        }
        else if (methodName.Contains("GetChild"))
        {
            // Transform to element.GetVisualChildren().ElementAt(index)
            if (visitedArgs.Arguments.Count < 2)
            {
                _diagnostics.AddWarning(
                    "VISUAL_TREE_INCOMPLETE",
                    "VisualTreeHelper.GetChild requires 2 arguments",
                    node.GetLocation().GetLineSpan().Path);
                return node;
            }

            var indexArg = visitedArgs.Arguments[1].Expression;

            var getVisualChildren = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                targetElement,
                SyntaxFactory.IdentifierName("GetVisualChildren"));

            var getVisualChildrenInvocation = SyntaxFactory.InvocationExpression(
                getVisualChildren,
                SyntaxFactory.ArgumentList());

            var elementAt = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                getVisualChildrenInvocation,
                SyntaxFactory.IdentifierName("ElementAt"));

            var newInvocation = SyntaxFactory.InvocationExpression(
                elementAt,
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(indexArg))));

            _diagnostics.AddInfo(
                "VISUAL_TREE_METHOD_TRANSFORMED",
                "Transformed VisualTreeHelper.GetChild to GetVisualChildren().ElementAt()",
                node.GetLocation().GetLineSpan().Path,
                node.GetLocation().GetLineSpan().StartLinePosition.Line);

            return newInvocation;
        }

        return node;
    }

    private InvocationExpressionSyntax TransformLogicalTreeHelperMethod(
        InvocationExpressionSyntax node,
        MemberAccessExpressionSyntax memberAccess,
        string methodName)
    {
        // WPF: LogicalTreeHelper.GetParent(element)
        // Avalonia: element.GetLogicalParent()

        // WPF: LogicalTreeHelper.GetChildren(element)
        // Avalonia: element.GetLogicalChildren()

        var args = node.ArgumentList.Arguments;
        if (args.Count == 0)
        {
            return node;
        }

        // Visit the argument expressions to transform any nested invocations
        var visitedArgs = SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList(args.Select(arg =>
                SyntaxFactory.Argument(
                    arg.NameColon,
                    arg.RefKindKeyword,
                    (ExpressionSyntax)Visit(arg.Expression)!))));

        var targetElement = visitedArgs.Arguments[0].Expression;

        if (methodName.Contains("GetParent"))
        {
            var newMemberAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                targetElement,
                SyntaxFactory.IdentifierName("GetLogicalParent"));

            var newInvocation = SyntaxFactory.InvocationExpression(
                newMemberAccess,
                SyntaxFactory.ArgumentList());

            _diagnostics.AddInfo(
                "LOGICAL_TREE_METHOD_TRANSFORMED",
                "Transformed LogicalTreeHelper.GetParent to GetLogicalParent",
                node.GetLocation().GetLineSpan().Path,
                node.GetLocation().GetLineSpan().StartLinePosition.Line);

            return newInvocation;
        }
        else if (methodName.Contains("GetChildren"))
        {
            var newMemberAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                targetElement,
                SyntaxFactory.IdentifierName("GetLogicalChildren"));

            var newInvocation = SyntaxFactory.InvocationExpression(
                newMemberAccess,
                SyntaxFactory.ArgumentList());

            _diagnostics.AddInfo(
                "LOGICAL_TREE_METHOD_TRANSFORMED",
                "Transformed LogicalTreeHelper.GetChildren to GetLogicalChildren",
                node.GetLocation().GetLineSpan().Path,
                node.GetLocation().GetLineSpan().StartLinePosition.Line);

            return newInvocation;
        }

        return node;
    }

    private InvocationExpressionSyntax TransformFocusMethod(
        InvocationExpressionSyntax node,
        MemberAccessExpressionSyntax memberAccess,
        string methodName)
    {
        // WPF: Keyboard.Focus(element)
        // Avalonia: element.Focus()

        if (methodName.Contains("Keyboard.Focus"))
        {
            var args = node.ArgumentList.Arguments;
            if (args.Count > 0)
            {
                // Visit the argument expression to transform any nested invocations
                var visitedArg = (ExpressionSyntax)Visit(args[0].Expression)!;

                var newMemberAccess = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    visitedArg,
                    SyntaxFactory.IdentifierName("Focus"));

                var newInvocation = SyntaxFactory.InvocationExpression(
                    newMemberAccess,
                    SyntaxFactory.ArgumentList());

                _diagnostics.AddInfo(
                    "FOCUS_METHOD_TRANSFORMED",
                    "Transformed Keyboard.Focus to element.Focus",
                    node.GetLocation().GetLineSpan().Path,
                    node.GetLocation().GetLineSpan().StartLinePosition.Line);

                return newInvocation;
            }
        }

        return node;
    }

    private InvocationExpressionSyntax TransformRoutedCommandMethod(
        InvocationExpressionSyntax node,
        MemberAccessExpressionSyntax memberAccess,
        string methodName)
    {
        // Most routed commands have similar names in Avalonia
        // Just add a diagnostic for manual review
        _diagnostics.AddInfo(
            "ROUTED_COMMAND_REVIEW",
            $"Routed command '{methodName}' may need manual review for Avalonia compatibility",
            node.GetLocation().GetLineSpan().Path,
            node.GetLocation().GetLineSpan().StartLinePosition.Line);

        return node;
    }

    private SeparatedSyntaxList<ArgumentSyntax> FilterDispatcherPriorityArguments(
        SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        // Remove DispatcherPriority arguments as Avalonia doesn't have them
        var filtered = arguments.Where(arg =>
        {
            var argType = _semanticModel.GetTypeInfo(arg.Expression).Type;
            return argType?.Name != "DispatcherPriority";
        }).ToList();

        if (filtered.Count < arguments.Count)
        {
            _diagnostics.AddInfo(
                "DISPATCHER_PRIORITY_REMOVED",
                "Removed DispatcherPriority parameter (not supported in Avalonia)",
                arguments.First().GetLocation().GetLineSpan().Path);
        }

        return SyntaxFactory.SeparatedList(filtered);
    }

    private string GetFullMethodName(MemberAccessExpressionSyntax memberAccess)
    {
        var parts = new List<string>();

        var current = memberAccess;
        while (current != null)
        {
            parts.Insert(0, current.Name.Identifier.Text);

            if (current.Expression is MemberAccessExpressionSyntax nestedMemberAccess)
            {
                current = nestedMemberAccess;
            }
            else if (current.Expression is IdentifierNameSyntax identifier)
            {
                parts.Insert(0, identifier.Identifier.Text);
                break;
            }
            else
            {
                break;
            }
        }

        return string.Join(".", parts);
    }

    private bool IsDispatcherMethod(string methodName, string typeName)
    {
        // Check if the type is Dispatcher (from System.Windows.Threading)
        bool isDispatcherType = typeName.Contains("Dispatcher") || typeName.Contains("System.Windows.Threading");

        // Check if method name is a Dispatcher method
        bool isDispatcherMethodName = methodName.EndsWith(".Invoke") ||
                                      methodName.EndsWith(".BeginInvoke") ||
                                      methodName.EndsWith(".InvokeAsync") ||
                                      methodName.EndsWith(".CheckAccess");

        return isDispatcherType && isDispatcherMethodName;
    }

    private bool IsVisualTreeHelperMethod(string methodName)
    {
        return methodName.Contains("VisualTreeHelper");
    }

    private bool IsLogicalTreeHelperMethod(string methodName)
    {
        return methodName.Contains("LogicalTreeHelper");
    }

    private bool IsFocusMethod(string methodName)
    {
        return methodName.Contains("Keyboard.Focus");
    }

    private bool IsRoutedCommandMethod(string methodName)
    {
        return methodName.Contains("ApplicationCommands") ||
               methodName.Contains("NavigationCommands") ||
               methodName.Contains("ComponentCommands") ||
               methodName.Contains("MediaCommands");
    }
}
