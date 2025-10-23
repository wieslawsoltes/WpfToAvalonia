using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Rewrites property access from WPF properties to Avalonia properties.
/// </summary>
public sealed class PropertyAccessRewriter : WpfToAvaloniaRewriter
{
    private int _propertyReferencesChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyAccessRewriter"/> class.
    /// </summary>
    /// <param name="semanticModel">The semantic model for the syntax tree.</param>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <param name="mappingRepository">The mapping repository.</param>
    public PropertyAccessRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Visits a member access expression and transforms WPF property access.
    /// </summary>
    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        SymbolInfo symbolInfo;
        try
        {
            symbolInfo = SemanticModel.GetSymbolInfo(node);
        }
        catch
        {
            // Node might not exist in semantic model after previous transformations
            return base.VisitMemberAccessExpression(node);
        }

        var propertySymbol = symbolInfo.Symbol as IPropertySymbol;

        if (propertySymbol == null)
        {
            return base.VisitMemberAccessExpression(node);
        }

        // Get the property name
        var propertyName = propertySymbol.Name;
        var ownerTypeName = propertySymbol.ContainingType?.ToDisplayString();

        // Look up property mapping
        var mapping = MappingRepository.FindPropertyMapping(propertyName, ownerTypeName);

        if (mapping == null)
        {
            // Try without owner type (general mapping)
            mapping = MappingRepository.FindPropertyMapping(propertyName);
        }

        if (mapping == null)
        {
            return base.VisitMemberAccessExpression(node);
        }

        // Transform the property name
        var newName = SyntaxFactory.IdentifierName(mapping.AvaloniaPropertyName)
            .WithTriviaFrom(node.Name);

        var newMemberAccess = node.WithName((SimpleNameSyntax)newName);

        _propertyReferencesChanged++;

        // Report the transformation
        var lineSpan = node.GetLocation().GetLineSpan();
        Diagnostics.AddInfo(
            DiagnosticCodes.PropertyTransformed,
            $"Transformed property: {propertyName} → {mapping.AvaloniaPropertyName}",
            lineSpan.Path,
            lineSpan.StartLinePosition.Line + 1,
            lineSpan.StartLinePosition.Character + 1);

        if (mapping.TypeChanged)
        {
            Diagnostics.AddWarning(
                DiagnosticCodes.PropertyTypeChanged,
                $"Property type changed: {mapping.WpfPropertyType} → {mapping.AvaloniaPropertyType}. " +
                $"Value conversion may be needed: {mapping.ValueConversionRule}",
                lineSpan.Path,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1);
        }

        if (mapping.RequiresManualReview)
        {
            Diagnostics.AddWarning(
                DiagnosticCodes.PropertyRequiresManualReview,
                $"Property transformation requires manual review: {mapping.Notes}",
                lineSpan.Path,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1);
        }

        return newMemberAccess;
    }

    /// <summary>
    /// Visits an assignment expression to handle property value conversions.
    /// </summary>
    public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        // Check if we're assigning to a property that has a type change
        if (node.Left is MemberAccessExpressionSyntax memberAccess)
        {
            var symbolInfo = SemanticModel.GetSymbolInfo(memberAccess);
            var propertySymbol = symbolInfo.Symbol as IPropertySymbol;

            if (propertySymbol != null)
            {
                var propertyName = propertySymbol.Name;
                var ownerTypeName = propertySymbol.ContainingType?.ToDisplayString();

                var mapping = MappingRepository.FindPropertyMapping(propertyName, ownerTypeName);
                if (mapping == null)
                {
                    mapping = MappingRepository.FindPropertyMapping(propertyName);
                }

                if (mapping != null && mapping.TypeChanged)
                {
                    // Special handling for Visibility -> IsVisible conversion
                    if (propertyName == "Visibility" && mapping.AvaloniaPropertyName == "IsVisible")
                    {
                        var lineSpan = node.GetLocation().GetLineSpan();
                        Diagnostics.AddWarning(
                            DiagnosticCodes.PropertyValueConversionNeeded,
                            "Visibility enum assignment needs conversion to bool for IsVisible. " +
                            "Example: Visibility.Visible → true, Visibility.Collapsed → false",
                            lineSpan.Path,
                            lineSpan.StartLinePosition.Line + 1,
                            lineSpan.StartLinePosition.Character + 1);
                    }
                }
            }
        }

        return base.VisitAssignmentExpression(node);
    }

    /// <summary>
    /// Gets the count of property references that were changed.
    /// </summary>
    public int PropertyReferencesChanged => _propertyReferencesChanged;
}
