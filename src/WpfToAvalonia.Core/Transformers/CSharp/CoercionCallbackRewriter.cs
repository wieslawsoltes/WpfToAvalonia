using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Rewrites dependency property coercion callbacks from WPF to Avalonia equivalents.
/// Implements task 2.5.3.1: Handle coercion callbacks
/// </summary>
/// <remarks>
/// Coercion callbacks are used in WPF to validate and adjust property values when they are set.
/// WPF uses CoerceValueCallback in PropertyMetadata, while Avalonia uses different validation approaches.
///
/// WPF Pattern:
/// public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
///     "Value", typeof(double), typeof(MyControl),
///     new PropertyMetadata(0.0, OnValueChanged, CoerceValue));
///
/// private static object CoerceValue(DependencyObject d, object baseValue)
/// {
///     var value = (double)baseValue;
///     return Math.Max(0, Math.Min(100, value)); // Clamp to 0-100
/// }
///
/// Avalonia Alternatives:
/// 1. Use validation in property setter:
///    public double Value
///    {
///        get => GetValue(ValueProperty);
///        set => SetValue(ValueProperty, Math.Max(0, Math.Min(100, value)));
///    }
///
/// 2. Use StyledProperty with coerce parameter:
///    public static readonly StyledProperty<double> ValueProperty =
///        AvaloniaProperty.Register<MyControl, double>(
///            nameof(Value),
///            defaultValue: 0.0,
///            coerce: (o, v) => Math.Max(0, Math.Min(100, v)));
///
/// 3. Use validation callback in property metadata:
///    Validate = value => value >= 0 && value <= 100
/// </remarks>
public sealed class CoercionCallbackRewriter : WpfToAvaloniaRewriter
{
    private int _coercionCallbacksDetected;
    private int _propertyMetadataWithCoercion;
    private int _coerceValueMethodCalls;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoercionCallbackRewriter"/> class.
    /// </summary>
    public CoercionCallbackRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Gets the number of coercion callbacks detected.
    /// </summary>
    public int CoercionCallbacksDetected => _coercionCallbacksDetected;

    /// <summary>
    /// Gets the number of PropertyMetadata instances with coercion detected.
    /// </summary>
    public int PropertyMetadataWithCoercion => _propertyMetadataWithCoercion;

    /// <summary>
    /// Gets the number of CoerceValue method calls detected.
    /// </summary>
    public int CoerceValueMethodCalls => _coerceValueMethodCalls;

    /// <summary>
    /// Visits object creation expressions to detect PropertyMetadata with coercion callbacks.
    /// </summary>
    public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        var symbolInfo = TryGetSymbolInfo(node);
        if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IMethodSymbol methodSymbol)
        {
            var typeName = methodSymbol.ContainingType?.ToDisplayString();

            // Detect PropertyMetadata or FrameworkPropertyMetadata with coercion callback
            if (typeName == "System.Windows.PropertyMetadata" ||
                typeName == "System.Windows.FrameworkPropertyMetadata" ||
                typeName == "System.Windows.UIPropertyMetadata")
            {
                // Check if this metadata has a coercion callback (3rd or 4th parameter)
                if (node.ArgumentList?.Arguments.Count >= 3)
                {
                    var args = node.ArgumentList.Arguments;

                    // Common patterns:
                    // new PropertyMetadata(defaultValue, propertyChangedCallback, coerceValueCallback)
                    // new FrameworkPropertyMetadata(defaultValue, flags, propertyChangedCallback, coerceValueCallback)

                    bool hasCoercion = false;
                    string? coercionCallbackName = null;

                    // Check for coercion in 3rd parameter position
                    if (args.Count == 3)
                    {
                        var thirdArg = args[2].Expression;
                        if (IsCoercionCallback(thirdArg))
                        {
                            hasCoercion = true;
                            coercionCallbackName = GetCallbackName(thirdArg);
                        }
                    }
                    // Check for coercion in 4th parameter position (FrameworkPropertyMetadata)
                    else if (args.Count >= 4)
                    {
                        var fourthArg = args[3].Expression;
                        if (IsCoercionCallback(fourthArg))
                        {
                            hasCoercion = true;
                            coercionCallbackName = GetCallbackName(fourthArg);
                        }
                    }

                    if (hasCoercion)
                    {
                        _propertyMetadataWithCoercion++;

                        Diagnostics.AddWarning(
                            "COERCION_CALLBACK_NOT_DIRECTLY_SUPPORTED",
                            $"PropertyMetadata with coercion callback '{coercionCallbackName}' detected. " +
                            $"Avalonia doesn't use the same pattern. Alternatives:\n" +
                            $"1. Use StyledProperty.Register with 'coerce' parameter: " +
                            $"coerce: (owner, value) => /* your coercion logic */\n" +
                            $"2. Add validation in property setter\n" +
                            $"3. Use validate parameter: validate: value => /* validation logic */\n" +
                            $"Convert CoerceValueCallback to match Avalonia's signature.",
                            null);
                    }
                }
            }
        }

        return base.VisitObjectCreationExpression(node);
    }

    /// <summary>
    /// Visits method declarations to detect coercion callback methods.
    /// </summary>
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Detect methods that look like coercion callbacks
        // Signature: static object MethodName(DependencyObject d, object baseValue)
        if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) &&
            node.ReturnType.ToString() == "object" &&
            node.ParameterList.Parameters.Count == 2)
        {
            var param1Type = node.ParameterList.Parameters[0].Type?.ToString();
            var param2Type = node.ParameterList.Parameters[1].Type?.ToString();

            if (param1Type == "DependencyObject" && param2Type == "object")
            {
                var methodName = node.Identifier.Text;

                // Common naming patterns for coercion callbacks
                if (methodName.StartsWith("Coerce") || methodName.Contains("Coercion") || methodName.EndsWith("Coerce"))
                {
                    _coercionCallbacksDetected++;

                    Diagnostics.AddWarning(
                        "COERCION_CALLBACK_METHOD",
                        $"Coercion callback method '{methodName}' detected. " +
                        $"Convert to Avalonia's coerce callback signature: " +
                        $"Func<TOwner, TValue, TValue> where parameters are (owner, value) and return is coerced value. " +
                        $"Remove DependencyObject parameter and change from object to specific types.",
                        null);

                    Diagnostics.AddInfo(
                        "COERCION_CALLBACK_EXAMPLE",
                        $"Example conversion for {methodName}:\n" +
                        $"// WPF:\n" +
                        $"private static object {methodName}(DependencyObject d, object baseValue)\n" +
                        $"{{\n" +
                        $"    var value = (TValue)baseValue;\n" +
                        $"    return /* coerced value */;\n" +
                        $"}}\n\n" +
                        $"// Avalonia:\n" +
                        $"private static TValue {methodName}(TOwner owner, TValue value)\n" +
                        $"{{\n" +
                        $"    return /* coerced value */;\n" +
                        $"}}",
                        null);
                }
            }
        }

        return base.VisitMethodDeclaration(node);
    }

    /// <summary>
    /// Visits invocation expressions to detect CoerceValue method calls.
    /// </summary>
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;

            if (methodName == "CoerceValue")
            {
                var symbolInfo = TryGetSymbolInfo(memberAccess);
                if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IMethodSymbol methodSymbol)
                {
                    var containingType = methodSymbol.ContainingType?.ToDisplayString();

                    if (containingType == "System.Windows.DependencyObject")
                    {
                        _coerceValueMethodCalls++;

                        // Extract property being coerced
                        string? propertyName = null;
                        if (node.ArgumentList.Arguments.Count > 0)
                        {
                            propertyName = node.ArgumentList.Arguments[0].Expression.ToString();
                        }

                        Diagnostics.AddWarning(
                            "COERCE_VALUE_NOT_SUPPORTED",
                            $"DependencyObject.CoerceValue({propertyName}) is not supported in Avalonia. " +
                            $"Avalonia's property system handles coercion automatically during property registration. " +
                            $"If you need to manually re-coerce a value, you can use: " +
                            $"SetValue(property, GetValue(property)) to trigger the coerce callback, " +
                            $"or implement custom logic in the property setter.",
                            null);
                    }
                }
            }
        }

        return base.VisitInvocationExpression(node);
    }

    /// <summary>
    /// Checks if an expression is a coercion callback.
    /// </summary>
    private bool IsCoercionCallback(ExpressionSyntax expression)
    {
        // Check for null (no callback)
        if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.NullLiteralExpression))
        {
            return false;
        }

        // Check for method references
        if (expression is IdentifierNameSyntax identifier)
        {
            var name = identifier.Identifier.Text;
            return name.Contains("Coerce") || name.Contains("Coercion");
        }

        // Check for lambda expressions
        if (expression is SimpleLambdaExpressionSyntax || expression is ParenthesizedLambdaExpressionSyntax)
        {
            return true; // Assume lambdas in this position are coercion callbacks
        }

        return false;
    }

    /// <summary>
    /// Gets the name of a callback from an expression.
    /// </summary>
    private string? GetCallbackName(ExpressionSyntax expression)
    {
        if (expression is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.Text;
        }
        else if (expression is SimpleLambdaExpressionSyntax || expression is ParenthesizedLambdaExpressionSyntax)
        {
            return "inline lambda";
        }
        return "unknown";
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
