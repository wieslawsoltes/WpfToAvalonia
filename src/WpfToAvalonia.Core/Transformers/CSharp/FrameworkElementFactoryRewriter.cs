using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Rewrites FrameworkElementFactory usage from WPF to Avalonia equivalents.
/// Implements task 2.5.2.1: Transform FrameworkElementFactory usage
/// </summary>
/// <remarks>
/// FrameworkElementFactory is used in WPF to programmatically create templates (ControlTemplate, DataTemplate).
/// Avalonia doesn't use FrameworkElementFactory - templates should be defined in XAML or using FuncDataTemplate/FuncControlTemplate.
///
/// This rewriter:
/// - Detects FrameworkElementFactory usage
/// - Warns users about the need to convert to XAML or Avalonia's template approach
/// - Provides guidance on using FuncDataTemplate&lt;T&gt; or defining templates in XAML
///
/// WPF Pattern:
/// var factory = new FrameworkElementFactory(typeof(TextBlock));
/// factory.SetValue(TextBlock.TextProperty, new Binding("Name"));
/// var template = new DataTemplate { VisualTree = factory };
///
/// Avalonia Alternatives:
/// 1. XAML (recommended):
///    &lt;DataTemplate DataType="{x:Type local:MyViewModel}"&gt;
///      &lt;TextBlock Text="{Binding Name}" /&gt;
///    &lt;/DataTemplate&gt;
///
/// 2. FuncDataTemplate (code):
///    new FuncDataTemplate&lt;MyViewModel&gt;((vm, ns) =&gt; new TextBlock { [!TextBlock.TextProperty] = vm[!MyViewModel.NameProperty] })
/// </remarks>
public sealed class FrameworkElementFactoryRewriter : WpfToAvaloniaRewriter
{
    private int _frameworkElementFactoryDetected;
    private int _dataTemplateCodeDetected;
    private int _controlTemplateCodeDetected;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrameworkElementFactoryRewriter"/> class.
    /// </summary>
    public FrameworkElementFactoryRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Gets the number of FrameworkElementFactory usages detected.
    /// </summary>
    public int FrameworkElementFactoryDetected => _frameworkElementFactoryDetected;

    /// <summary>
    /// Gets the number of DataTemplate code usages detected.
    /// </summary>
    public int DataTemplateCodeDetected => _dataTemplateCodeDetected;

    /// <summary>
    /// Gets the number of ControlTemplate code usages detected.
    /// </summary>
    public int ControlTemplateCodeDetected => _controlTemplateCodeDetected;

    /// <summary>
    /// Visits object creation expressions to detect FrameworkElementFactory instantiation.
    /// </summary>
    public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        var symbolInfo = TryGetSymbolInfo(node);
        if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IMethodSymbol methodSymbol)
        {
            var typeName = methodSymbol.ContainingType?.ToDisplayString();

            // Detect FrameworkElementFactory
            if (typeName == "System.Windows.FrameworkElementFactory")
            {
                _frameworkElementFactoryDetected++;

                // Try to extract the type being created
                string? elementType = null;
                if (node.ArgumentList?.Arguments.Count > 0)
                {
                    var firstArg = node.ArgumentList.Arguments[0].Expression;
                    if (firstArg is TypeOfExpressionSyntax typeOf)
                    {
                        elementType = typeOf.Type.ToString();
                    }
                }

                var elementTypeInfo = elementType != null ? $" for type '{elementType}'" : "";

                Diagnostics.AddWarning(
                    "FRAMEWORK_ELEMENT_FACTORY_NOT_SUPPORTED",
                    $"FrameworkElementFactory{elementTypeInfo} is not supported in Avalonia. " +
                    $"Alternatives: 1) Define templates in XAML (recommended), 2) Use FuncDataTemplate<T> or FuncControlTemplate, " +
                    $"3) Use ItemTemplate selector with different templates. " +
                    $"See: https://docs.avaloniaui.net/docs/templates/",
                    null);

                Diagnostics.AddInfo(
                    "FRAMEWORK_ELEMENT_FACTORY_XAML_ALTERNATIVE",
                    $"Recommended: Convert this template to XAML. Example:\n" +
                    $"<DataTemplate DataType=\"{{x:Type local:YourViewModel}}\">\n" +
                    $"  <{elementType ?? "YourControl"} Property=\"{{Binding YourProperty}}\" />\n" +
                    $"</DataTemplate>",
                    null);
            }
            // Detect DataTemplate with VisualTree assignment
            else if (typeName == "System.Windows.DataTemplate")
            {
                _dataTemplateCodeDetected++;

                Diagnostics.AddInfo(
                    "DATA_TEMPLATE_CODE_DETECTED",
                    "DataTemplate created in code. In Avalonia, prefer XAML templates or use FuncDataTemplate<T>. " +
                    "Example: new FuncDataTemplate<MyViewModel>((vm, ns) => new TextBlock { [!TextBlock.TextProperty] = vm[!MyViewModel.NameProperty] })",
                    null);
            }
            // Detect ControlTemplate creation
            else if (typeName == "System.Windows.Controls.ControlTemplate")
            {
                _controlTemplateCodeDetected++;

                Diagnostics.AddInfo(
                    "CONTROL_TEMPLATE_CODE_DETECTED",
                    "ControlTemplate created in code. In Avalonia, templates should be defined in XAML or use FuncControlTemplate. " +
                    "Define ControlTemplate in Styles.axaml and apply via ControlTheme.",
                    null);
            }
        }

        return base.VisitObjectCreationExpression(node);
    }

    /// <summary>
    /// Visits invocation expressions to detect FrameworkElementFactory method calls.
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

                // Detect FrameworkElementFactory methods
                if (containingType == "System.Windows.FrameworkElementFactory")
                {
                    if (methodName is "SetValue" or "SetBinding" or "AppendChild" or "SetResourceReference")
                    {
                        Diagnostics.AddInfo(
                            "FRAMEWORK_ELEMENT_FACTORY_METHOD",
                            $"FrameworkElementFactory.{methodName}() usage detected. This must be converted to XAML or Avalonia template code.",
                            null);
                    }
                }
            }
        }

        return base.VisitInvocationExpression(node);
    }

    /// <summary>
    /// Visits assignment expressions to detect VisualTree property assignment.
    /// </summary>
    public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        // Detect DataTemplate.VisualTree = factory pattern
        if (node.Left is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "VisualTree")
        {
            var symbolInfo = TryGetSymbolInfo(memberAccess);
            if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IPropertySymbol propertySymbol)
            {
                var containingType = propertySymbol.ContainingType?.ToDisplayString();

                if (containingType == "System.Windows.DataTemplate" ||
                    containingType == "System.Windows.Controls.ControlTemplate")
                {
                    Diagnostics.AddWarning(
                        "VISUAL_TREE_ASSIGNMENT_NOT_SUPPORTED",
                        $"Assignment to {containingType}.VisualTree is not supported in Avalonia. " +
                        $"Convert this template to XAML or use Avalonia's functional template approach.",
                        null);
                }
            }
        }

        return base.VisitAssignmentExpression(node);
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
