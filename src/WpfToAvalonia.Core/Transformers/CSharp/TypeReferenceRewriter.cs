using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Rewrites type references from WPF types to Avalonia types.
/// </summary>
public sealed class TypeReferenceRewriter : WpfToAvaloniaRewriter
{
    private int _typeReferencesChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeReferenceRewriter"/> class.
    /// </summary>
    /// <param name="semanticModel">The semantic model for the syntax tree.</param>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <param name="mappingRepository">The mapping repository.</param>
    public TypeReferenceRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Visits an identifier name and transforms WPF type references.
    /// </summary>
    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        SymbolInfo symbolInfo;
        try
        {
            symbolInfo = SemanticModel.GetSymbolInfo(node);
        }
        catch
        {
            // Node might not exist in semantic model after previous transformations
            return base.VisitIdentifierName(node);
        }

        var typeSymbol = symbolInfo.Symbol as ITypeSymbol;

        if (typeSymbol == null)
        {
            return base.VisitIdentifierName(node);
        }

        if (!IsWpfType(typeSymbol))
        {
            return base.VisitIdentifierName(node);
        }

        var fullTypeName = GetFullTypeName(typeSymbol).Replace("global::", "");
        var mapping = MappingRepository.FindTypeMapping(fullTypeName);

        if (mapping == null)
        {
            // No mapping found, report warning
            var lineSpan = node.GetLocation().GetLineSpan();
            Diagnostics.AddWarning(
                DiagnosticCodes.TypeMappingNotFound,
                $"No mapping found for WPF type: {fullTypeName}",
                lineSpan.Path,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1);

            return base.VisitIdentifierName(node);
        }

        // Get the simple type name for the Avalonia type
        var avaloniaTypeName = mapping.TypeNameChanged
            ? GetSimpleTypeName(mapping.AvaloniaTypeName)
            : node.Identifier.Text;

        var newIdentifier = SyntaxFactory.IdentifierName(avaloniaTypeName)
            .WithTriviaFrom(node);

        _typeReferencesChanged++;

        // Report the transformation
        var location = node.GetLocation().GetLineSpan();
        Diagnostics.AddInfo(
            DiagnosticCodes.TypeTransformed,
            $"Transformed type reference: {node.Identifier.Text} → {avaloniaTypeName}",
            location.Path,
            location.StartLinePosition.Line + 1,
            location.StartLinePosition.Character + 1);

        if (mapping.RequiresManualReview)
        {
            Diagnostics.AddWarning(
                DiagnosticCodes.TypeRequiresManualReview,
                $"Type transformation requires manual review: {mapping.Notes}",
                location.Path,
                location.StartLinePosition.Line + 1,
                location.StartLinePosition.Character + 1);
        }

        return newIdentifier;
    }

    /// <summary>
    /// Visits a qualified name and transforms WPF type references.
    /// </summary>
    public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
    {
        SymbolInfo symbolInfo;
        try
        {
            symbolInfo = SemanticModel.GetSymbolInfo(node);
        }
        catch
        {
            // Node might not exist in semantic model after previous transformations
            return base.VisitQualifiedName(node);
        }

        var typeSymbol = symbolInfo.Symbol as ITypeSymbol;

        if (typeSymbol == null)
        {
            return base.VisitQualifiedName(node);
        }

        if (!IsWpfType(typeSymbol))
        {
            return base.VisitQualifiedName(node);
        }

        var fullTypeName = GetFullTypeName(typeSymbol).Replace("global::", "");
        var mapping = MappingRepository.FindTypeMapping(fullTypeName);

        if (mapping == null)
        {
            return base.VisitQualifiedName(node);
        }

        // Parse the new qualified name
        var newQualifiedName = SyntaxFactory.ParseName(mapping.AvaloniaTypeName)
            .WithTriviaFrom(node);

        _typeReferencesChanged++;

        return newQualifiedName;
    }

    /// <summary>
    /// Gets the count of type references that were changed.
    /// </summary>
    public int TypeReferencesChanged => _typeReferencesChanged;

    private string GetSimpleTypeName(string fullTypeName)
    {
        var lastDotIndex = fullTypeName.LastIndexOf('.');
        return lastDotIndex >= 0 ? fullTypeName.Substring(lastDotIndex + 1) : fullTypeName;
    }
}
