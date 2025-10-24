using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Rewrites TemplatePart attributes from WPF to Avalonia equivalents.
/// Implements task 2.5.2.2: Update template part attributes
/// </summary>
/// <remarks>
/// TemplatePart attributes are used to declare named parts in control templates.
/// Both WPF and Avalonia support this pattern, but with some differences:
///
/// WPF:
/// [TemplatePart(Name = "PART_ContentHost", Type = typeof(ScrollViewer))]
/// public class MyCustomControl : Control
/// {
///     public override void OnApplyTemplate()
///     {
///         var part = GetTemplateChild("PART_ContentHost") as ScrollViewer;
///     }
/// }
///
/// Avalonia:
/// [TemplatePart(Name = "PART_ContentHost", Type = typeof(ScrollViewer))]  // Same attribute
/// public class MyCustomControl : TemplatedControl
/// {
///     protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
///     {
///         var part = e.NameScope.Find<ScrollViewer>("PART_ContentHost");  // Different lookup
///     }
/// }
///
/// Key differences:
/// - Base class: Control (WPF) vs TemplatedControl (Avalonia)
/// - OnApplyTemplate signature: void (WPF) vs TemplateAppliedEventArgs parameter (Avalonia)
/// - Template part lookup: GetTemplateChild (WPF) vs NameScope.Find (Avalonia)
/// </remarks>
public sealed class TemplatePartAttributeRewriter : WpfToAvaloniaRewriter
{
    private int _templatePartAttributesFound;
    private int _getTemplateChildCalls;
    private int _onApplyTemplateOverrides;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplatePartAttributeRewriter"/> class.
    /// </summary>
    public TemplatePartAttributeRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Gets the number of TemplatePart attributes found.
    /// </summary>
    public int TemplatePartAttributesFound => _templatePartAttributesFound;

    /// <summary>
    /// Gets the number of GetTemplateChild calls found.
    /// </summary>
    public int GetTemplateChildCalls => _getTemplateChildCalls;

    /// <summary>
    /// Gets the number of OnApplyTemplate overrides found.
    /// </summary>
    public int OnApplyTemplateOverrides => _onApplyTemplateOverrides;

    /// <summary>
    /// Visits attribute lists to detect TemplatePart attributes.
    /// </summary>
    public override SyntaxNode? VisitAttributeList(AttributeListSyntax node)
    {
        foreach (var attribute in node.Attributes)
        {
            var symbolInfo = TryGetSymbolInfo(attribute);
            if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IMethodSymbol methodSymbol)
            {
                var attributeTypeName = methodSymbol.ContainingType?.ToDisplayString();

                if (attributeTypeName == "System.Windows.TemplatePartAttribute")
                {
                    _templatePartAttributesFound++;

                    // Extract Name and Type parameters
                    string? partName = null;
                    string? partType = null;

                    if (attribute.ArgumentList != null)
                    {
                        foreach (var arg in attribute.ArgumentList.Arguments)
                        {
                            var paramName = arg.NameEquals?.Name.Identifier.Text;
                            var value = arg.Expression.ToString();

                            if (paramName == "Name")
                            {
                                partName = value.Trim('"');
                            }
                            else if (paramName == "Type")
                            {
                                // Extract type from typeof(...)
                                if (value.StartsWith("typeof(") && value.EndsWith(")"))
                                {
                                    partType = value.Substring(7, value.Length - 8);
                                }
                            }
                        }
                    }

                    Diagnostics.AddInfo(
                        "TEMPLATE_PART_ATTRIBUTE_COMPATIBLE",
                        $"TemplatePart attribute found (Name='{partName}', Type={partType}). " +
                        $"The attribute syntax is compatible with Avalonia, but GetTemplateChild() calls need to be updated to e.NameScope.Find<T>(). " +
                        $"Also ensure OnApplyTemplate signature uses TemplateAppliedEventArgs parameter.",
                        null);
                }
            }
        }

        return base.VisitAttributeList(node);
    }

    /// <summary>
    /// Visits invocation expressions to detect GetTemplateChild calls.
    /// </summary>
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;

            if (methodName == "GetTemplateChild")
            {
                var symbolInfo = TryGetSymbolInfo(memberAccess);
                if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IMethodSymbol methodSymbol)
                {
                    var containingType = methodSymbol.ContainingType?.ToDisplayString();

                    if (containingType == "System.Windows.FrameworkElement" ||
                        containingType == "System.Windows.Controls.Control")
                    {
                        _getTemplateChildCalls++;

                        // Try to extract the part name
                        string? partName = null;
                        if (node.ArgumentList.Arguments.Count > 0)
                        {
                            partName = node.ArgumentList.Arguments[0].Expression.ToString().Trim('"');
                        }

                        Diagnostics.AddWarning(
                            "GET_TEMPLATE_CHILD_NEEDS_UPDATE",
                            $"GetTemplateChild(\"{partName}\") needs to be updated for Avalonia. " +
                            $"Change to: e.NameScope.Find<TargetType>(\"{partName}\") or e.NameScope.Get<TargetType>(\"{partName}\") " +
                            $"(where 'e' is the TemplateAppliedEventArgs parameter in OnApplyTemplate). " +
                            $"Note: Find returns null if not found, Get throws exception.",
                            null);
                    }
                }
            }
        }

        return base.VisitInvocationExpression(node);
    }

    /// <summary>
    /// Visits method declarations to detect OnApplyTemplate overrides.
    /// </summary>
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (node.Identifier.Text == "OnApplyTemplate")
        {
            // Check if this is overriding a base method
            if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword)))
            {
                _onApplyTemplateOverrides++;

                // Check the signature
                var hasCorrectSignature = node.ParameterList.Parameters.Count == 1 &&
                                         node.ParameterList.Parameters[0].Type?.ToString().Contains("TemplateAppliedEventArgs") == true;

                if (!hasCorrectSignature)
                {
                    var currentSignature = node.ParameterList.ToString();

                    Diagnostics.AddWarning(
                        "ON_APPLY_TEMPLATE_SIGNATURE_CHANGE",
                        $"OnApplyTemplate{currentSignature} signature needs to be updated for Avalonia. " +
                        $"Change to: protected override void OnApplyTemplate(TemplateAppliedEventArgs e). " +
                        $"Use e.NameScope.Find<T>(\"name\") instead of GetTemplateChild(\"name\"). " +
                        $"Call base.OnApplyTemplate(e) instead of base.OnApplyTemplate().",
                        null);
                }
                else
                {
                    Diagnostics.AddInfo(
                        "ON_APPLY_TEMPLATE_CORRECT_SIGNATURE",
                        "OnApplyTemplate has correct Avalonia signature with TemplateAppliedEventArgs parameter.",
                        null);
                }
            }
        }

        return base.VisitMethodDeclaration(node);
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
