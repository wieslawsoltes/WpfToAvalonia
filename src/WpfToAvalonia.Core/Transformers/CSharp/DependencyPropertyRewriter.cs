using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Rewrites WPF DependencyProperty declarations to Avalonia StyledProperty or DirectProperty.
/// </summary>
public sealed class DependencyPropertyRewriter : WpfToAvaloniaRewriter
{
    private int _dependencyPropertiesConverted;

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyPropertyRewriter"/> class.
    /// </summary>
    /// <param name="semanticModel">The semantic model for the syntax tree.</param>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <param name="mappingRepository">The mapping repository.</param>
    public DependencyPropertyRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Visits a field declaration and transforms DependencyProperty to StyledProperty.
    /// </summary>
    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        // Check if this is a DependencyProperty field
        var variableDeclaration = node.Declaration;
        var typeSyntax = variableDeclaration.Type;

        var typeInfo = SemanticModel.GetTypeInfo(typeSyntax);
        var typeSymbol = typeInfo.Type;

        if (typeSymbol == null)
        {
            return base.VisitFieldDeclaration(node);
        }

        var fullTypeName = GetFullTypeName(typeSymbol).Replace("global::", "");

        // Check if this is a DependencyProperty
        if (!fullTypeName.Contains("System.Windows.DependencyProperty"))
        {
            return base.VisitFieldDeclaration(node);
        }

        // Transform DependencyProperty to StyledProperty
        // This is a simplified transformation - in reality, we'd need to analyze the
        // Register call to determine if it should be StyledProperty or DirectProperty

        var lineSpan = node.GetLocation().GetLineSpan();

        Diagnostics.AddWarning(
            DiagnosticCodes.DependencyPropertyRequiresManualReview,
            "DependencyProperty found. Manual conversion to StyledProperty or DirectProperty required. " +
            "Analyze the property implementation to determine the correct Avalonia property type.",
            lineSpan.Path,
            lineSpan.StartLinePosition.Line + 1,
            lineSpan.StartLinePosition.Character + 1);

        _dependencyPropertiesConverted++;

        // For now, we'll keep the field as-is and flag it for manual review
        // A more complete implementation would:
        // 1. Detect if it's a regular or attached property
        // 2. Transform DependencyProperty.Register to AvaloniaProperty.Register
        // 3. Update the field type from DependencyProperty to StyledProperty<T> or DirectProperty<TOwner, TValue>
        // 4. Transform property metadata and callbacks

        return base.VisitFieldDeclaration(node);
    }

    /// <summary>
    /// Visits an invocation expression to detect DependencyProperty.Register calls.
    /// </summary>
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var symbolInfo = SemanticModel.GetSymbolInfo(node);
        var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

        if (methodSymbol == null)
        {
            return base.VisitInvocationExpression(node);
        }

        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
        {
            return base.VisitInvocationExpression(node);
        }

        var fullTypeName = GetFullTypeName(containingType).Replace("global::", "");

        // Check for DependencyProperty.Register calls
        if (fullTypeName.Contains("System.Windows.DependencyProperty") &&
            methodSymbol.Name == "Register")
        {
            var lineSpan = node.GetLocation().GetLineSpan();

            Diagnostics.AddInfo(
                DiagnosticCodes.DependencyPropertyFound,
                $"Found DependencyProperty.Register call. This needs to be converted to AvaloniaProperty.Register.",
                lineSpan.Path,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1);

            // Flag for manual review
            Diagnostics.AddWarning(
                DiagnosticCodes.DependencyPropertyRequiresManualReview,
                "DependencyProperty.Register requires manual conversion to AvaloniaProperty.Register<T>. " +
                "Review the property type, default value, and property changed callbacks.",
                lineSpan.Path,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1);
        }

        // Check for RegisterAttached calls
        if (fullTypeName.Contains("System.Windows.DependencyProperty") &&
            methodSymbol.Name == "RegisterAttached")
        {
            var lineSpan = node.GetLocation().GetLineSpan();

            Diagnostics.AddInfo(
                DiagnosticCodes.AttachedPropertyFound,
                "Found DependencyProperty.RegisterAttached call. This needs to be converted to AvaloniaProperty.RegisterAttached.",
                lineSpan.Path,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1);
        }

        return base.VisitInvocationExpression(node);
    }

    /// <summary>
    /// Visits a property declaration to detect CLR property wrappers for DependencyProperty.
    /// </summary>
    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        // Check if this property uses GetValue/SetValue (typical DependencyProperty pattern)
        var accessors = node.AccessorList?.Accessors;
        if (accessors == null)
        {
            return base.VisitPropertyDeclaration(node);
        }

        foreach (var accessor in accessors)
        {
            if (accessor.Body == null && accessor.ExpressionBody == null)
            {
                continue;
            }

            var descendantNodes = accessor.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invocation in descendantNodes)
            {
                var symbolInfo = SemanticModel.GetSymbolInfo(invocation);
                var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

                if (methodSymbol != null &&
                    (methodSymbol.Name == "GetValue" || methodSymbol.Name == "SetValue"))
                {
                    var lineSpan = node.GetLocation().GetLineSpan();

                    Diagnostics.AddInfo(
                        DiagnosticCodes.DependencyPropertyFound,
                        $"Property '{node.Identifier.Text}' uses GetValue/SetValue pattern. " +
                        "This is a DependencyProperty CLR wrapper that needs conversion.",
                        lineSpan.Path,
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.StartLinePosition.Character + 1);

                    break;
                }
            }
        }

        return base.VisitPropertyDeclaration(node);
    }

    /// <summary>
    /// Gets the count of dependency properties that were found and flagged for conversion.
    /// </summary>
    public int DependencyPropertiesConverted => _dependencyPropertiesConverted;
}
