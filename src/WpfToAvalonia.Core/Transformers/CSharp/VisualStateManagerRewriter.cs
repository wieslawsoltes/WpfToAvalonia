using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Rewrites Visual State Manager code from WPF to Avalonia equivalents.
/// Implements task 2.5.2.3: Convert visual state manager code
/// </summary>
/// <remarks>
/// Visual State Manager (VSM) is used to manage visual states of controls.
/// WPF and Avalonia have different approaches:
///
/// WPF Visual State Manager:
/// - VisualStateManager.GoToState(control, "StateName", useTransitions)
/// - VisualStateGroup with multiple VisualState elements
/// - Storyboard-based state animations
/// - Defined in ControlTemplate.VisualStateManager
///
/// Avalonia Approach:
/// - Pseudoclasses (:pointerover, :pressed, :disabled)
/// - Style selectors (^:pointerover, ^:pressed)
/// - Transitions for animated state changes
/// - More declarative, less code-based state management
///
/// Migration Strategy:
/// 1. Simple states → Convert to pseudoclasses
/// 2. Complex states → Use custom pseudoclasses or classes
/// 3. State transitions → Use Avalonia Transitions
/// 4. VisualStateManager.GoToState() → Set pseudoclasses or classes
///
/// Example WPF:
/// VisualStateManager.GoToState(this, "MouseOver", true);
///
/// Example Avalonia:
/// PseudoClasses.Set(":hover", true);  // For built-in pseudoclasses
/// // or
/// Classes.Add("mouseover");  // For custom state classes
/// </remarks>
public sealed class VisualStateManagerRewriter : WpfToAvaloniaRewriter
{
    private int _goToStateCalls;
    private int _visualStateGroupUsage;
    private int _getCurrentStateAccess;
    private int _visualStateManagerAttachedProperties;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualStateManagerRewriter"/> class.
    /// </summary>
    public VisualStateManagerRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Gets the number of VisualStateManager.GoToState calls found.
    /// </summary>
    public int GoToStateCalls => _goToStateCalls;

    /// <summary>
    /// Gets the number of VisualStateGroup usages found.
    /// </summary>
    public int VisualStateGroupUsage => _visualStateGroupUsage;

    /// <summary>
    /// Gets the number of GetCurrentState access found.
    /// </summary>
    public int GetCurrentStateAccess => _getCurrentStateAccess;

    /// <summary>
    /// Gets the number of VisualStateManager attached property usages found.
    /// </summary>
    public int VisualStateManagerAttachedProperties => _visualStateManagerAttachedProperties;

    /// <summary>
    /// Visits invocation expressions to detect VisualStateManager method calls.
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

                // Detect VisualStateManager.GoToState
                if (containingType == "System.Windows.VisualStateManager" && methodName == "GoToState")
                {
                    _goToStateCalls++;

                    // Extract state name
                    string? stateName = null;
                    if (node.ArgumentList.Arguments.Count >= 2)
                    {
                        stateName = node.ArgumentList.Arguments[1].Expression.ToString().Trim('"');
                    }

                    var stateNameLower = stateName?.ToLower() ?? "unknown";
                    Diagnostics.AddWarning(
                        "VISUAL_STATE_MANAGER_NOT_SUPPORTED",
                        $"VisualStateManager.GoToState(\"{stateName}\") is not directly supported in Avalonia. " +
                        $"Alternatives:\n" +
                        $"1. Use pseudoclasses: PseudoClasses.Set(\":{stateNameLower}\", true) " +
                        $"   and define styles with ^:{stateNameLower} selector\n" +
                        $"2. Use CSS-like classes: Classes.Add(\"{stateName}\") " +
                        $"   and define styles with .{stateName} selector\n" +
                        $"3. Define state transitions using <Transitions> in XAML\n" +
                        $"4. For common states like 'pressed', 'pointerover', Avalonia provides built-in pseudoclasses",
                        null);

                    // Check for common state names and provide specific guidance
                    if (stateName != null)
                    {
                        var stateGuidance = GetStateConversionGuidance(stateName);
                        if (!string.IsNullOrEmpty(stateGuidance))
                        {
                            Diagnostics.AddInfo(
                                "VISUAL_STATE_CONVERSION_GUIDANCE",
                                stateGuidance,
                                null);
                        }
                    }
                }
                // Detect VisualStateManager.GetVisualStateGroups
                else if (containingType == "System.Windows.VisualStateManager" && methodName == "GetVisualStateGroups")
                {
                    _visualStateGroupUsage++;

                    Diagnostics.AddWarning(
                        "VISUAL_STATE_GROUPS_NOT_SUPPORTED",
                        "VisualStateManager.GetVisualStateGroups is not supported in Avalonia. " +
                        "Visual state groups should be replaced with Avalonia's style-based approach using pseudoclasses and selectors.",
                        null);
                }
                // Detect GetCurrentState
                else if (containingType == "System.Windows.VisualStateManager" && methodName == "GetCurrentState")
                {
                    _getCurrentStateAccess++;

                    Diagnostics.AddWarning(
                        "GET_CURRENT_STATE_NOT_SUPPORTED",
                        "VisualStateManager.GetCurrentState is not supported in Avalonia. " +
                        "Track state using custom properties or check PseudoClasses/Classes collections.",
                        null);
                }
            }
        }

        return base.VisitInvocationExpression(node);
    }

    /// <summary>
    /// Visits object creation expressions to detect VisualStateGroup and VisualState creation.
    /// </summary>
    public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        var symbolInfo = TryGetSymbolInfo(node);
        if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IMethodSymbol methodSymbol)
        {
            var typeName = methodSymbol.ContainingType?.ToDisplayString();

            if (typeName == "System.Windows.VisualStateGroup")
            {
                _visualStateGroupUsage++;

                Diagnostics.AddWarning(
                    "VISUAL_STATE_GROUP_NOT_SUPPORTED",
                    "VisualStateGroup creation in code is not supported in Avalonia. " +
                    "Use XAML styles with pseudoclass selectors or CSS-like class selectors instead.",
                    null);
            }
            else if (typeName == "System.Windows.VisualState")
            {
                Diagnostics.AddWarning(
                    "VISUAL_STATE_NOT_SUPPORTED",
                    "VisualState creation in code is not supported in Avalonia. " +
                    "Define state-based styling in XAML using Styles with pseudoclass/class selectors.",
                    null);
            }
        }

        return base.VisitObjectCreationExpression(node);
    }

    /// <summary>
    /// Visits member access expressions to detect VisualStateManager attached property access.
    /// </summary>
    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var symbolInfo = TryGetSymbolInfo(node);
        if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IPropertySymbol propertySymbol)
        {
            var containingType = propertySymbol.ContainingType?.ToDisplayString();

            // Detect VisualStateManager.VisualStateGroups attached property
            if (containingType == "System.Windows.VisualStateManager")
            {
                var propertyName = node.Name.Identifier.Text;

                if (propertyName is "VisualStateGroupsProperty" or "CustomVisualStateManagerProperty")
                {
                    _visualStateManagerAttachedProperties++;

                    Diagnostics.AddWarning(
                        "VISUAL_STATE_MANAGER_ATTACHED_PROPERTY",
                        $"VisualStateManager.{propertyName} attached property is not supported in Avalonia. " +
                        $"Convert to Avalonia's style-based state management system.",
                        null);
                }
            }
        }

        return base.VisitMemberAccessExpression(node);
    }

    /// <summary>
    /// Provides conversion guidance for common visual state names.
    /// </summary>
    private string? GetStateConversionGuidance(string stateName)
    {
        return stateName.ToLowerInvariant() switch
        {
            "normal" => "For 'Normal' state: This is the default state. No pseudoclass needed. Define base styles.",
            "mouseover" or "hover" => "For 'MouseOver' state: Use ^:pointerover pseudoclass in XAML styles.",
            "pressed" => "For 'Pressed' state: Use ^:pressed pseudoclass in XAML styles.",
            "disabled" => "For 'Disabled' state: Use ^:disabled pseudoclass or bind IsEnabled property.",
            "focused" => "For 'Focused' state: Use ^:focus pseudoclass in XAML styles.",
            "checked" or "selected" => "For 'Checked/Selected' state: Use ^:checked or ^:selected pseudoclass.",
            "unchecked" => "For 'Unchecked' state: Use ^:unchecked pseudoclass.",
            "indeterminate" => "For 'Indeterminate' state: Use ^:indeterminate pseudoclass.",
            "readonly" => "For 'ReadOnly' state: Create custom pseudoclass or use Classes collection.",
            "invalid" or "error" => "For 'Invalid/Error' state: Create custom pseudoclass for validation state.",
            _ => null
        };
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
