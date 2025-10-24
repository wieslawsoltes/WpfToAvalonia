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
    private readonly Dictionary<string, TypeMapping> _syntaxBasedMappings;

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
        _syntaxBasedMappings = InitializeSyntaxBasedMappings();
    }

    /// <summary>
    /// Creates a dictionary of simple type name to type mapping for syntax-based fallback.
    /// This allows transformation to work even when semantic model cannot resolve types.
    /// </summary>
    private Dictionary<string, TypeMapping> InitializeSyntaxBasedMappings()
    {
        var mappings = new Dictionary<string, TypeMapping>(StringComparer.Ordinal);

        foreach (var mapping in MappingRepository.GetAllTypeMappings())
        {
            // Use simple type name as key (e.g., "DependencyObject" not "System.Windows.DependencyObject")
            if (!string.IsNullOrEmpty(mapping.SimpleTypeName))
            {
                // Avoid duplicates - first mapping wins
                if (!mappings.ContainsKey(mapping.SimpleTypeName))
                {
                    mappings[mapping.SimpleTypeName] = mapping;
                }
            }
        }

        return mappings;
    }

    /// <summary>
    /// Visits an identifier name and transforms WPF type references.
    /// </summary>
    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        // Try semantic-based transformation first
        SymbolInfo symbolInfo;
        ITypeSymbol? typeSymbol = null;
        bool useSemanticAnalysis = false;

        try
        {
            symbolInfo = SemanticModel.GetSymbolInfo(node);
            typeSymbol = symbolInfo.Symbol as ITypeSymbol;

            if (typeSymbol != null && IsWpfType(typeSymbol))
            {
                useSemanticAnalysis = true;
                var fullTypeName = GetFullTypeName(typeSymbol).Replace("global::", "");
                var mapping = MappingRepository.FindTypeMapping(fullTypeName);

                if (mapping != null)
                {
                    return TransformTypeReference(node, mapping);
                }

                // No mapping found, report warning
                var lineSpan = node.GetLocation().GetLineSpan();
                Diagnostics.AddWarning(
                    DiagnosticCodes.TypeMappingNotFound,
                    $"No mapping found for WPF type: {fullTypeName}",
                    lineSpan.Path,
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1);
            }
        }
        catch
        {
            // Semantic analysis failed - fall back to syntax-based transformation
        }

        // If semantic analysis didn't work or type wasn't resolved, try syntax-based fallback
        if (!useSemanticAnalysis)
        {
            var syntaxResult = TrySyntaxBasedTransformation(node);
            if (syntaxResult != null)
            {
                return syntaxResult;
            }
        }

        return base.VisitIdentifierName(node);
    }

    /// <summary>
    /// Attempts to transform a type reference using syntax-based matching.
    /// This fallback is used when semantic analysis cannot resolve the type.
    /// </summary>
    private SyntaxNode? TrySyntaxBasedTransformation(IdentifierNameSyntax node)
    {
        var typeName = node.Identifier.Text;

        // Look up the type name in our syntax-based mappings
        if (_syntaxBasedMappings.TryGetValue(typeName, out var mapping))
        {
            return TransformTypeReference(node, mapping);
        }

        return null;
    }

    /// <summary>
    /// Transforms a type reference node using the provided mapping.
    /// </summary>
    private SyntaxNode TransformTypeReference(IdentifierNameSyntax node, TypeMapping mapping)
    {
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
