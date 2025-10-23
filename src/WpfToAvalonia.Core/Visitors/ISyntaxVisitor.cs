using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace WpfToAvalonia.Core.Visitors;

/// <summary>
/// Defines a visitor for traversing and transforming C# syntax trees.
/// </summary>
public interface ISyntaxVisitor
{
    /// <summary>
    /// Visits a syntax node and returns a potentially transformed node.
    /// </summary>
    /// <param name="node">The syntax node to visit.</param>
    /// <param name="semanticModel">The semantic model for the syntax tree.</param>
    /// <returns>The transformed syntax node, or the original if no transformation occurred.</returns>
    SyntaxNode? Visit(SyntaxNode node, SemanticModel semanticModel);
}

/// <summary>
/// Base class for syntax rewriters that transform WPF code to Avalonia.
/// </summary>
public abstract class WpfToAvaloniaRewriter : CSharpSyntaxRewriter
{
    /// <summary>
    /// Gets the semantic model for the current syntax tree.
    /// </summary>
    protected SemanticModel SemanticModel { get; }

    /// <summary>
    /// Gets the diagnostic collector for reporting issues.
    /// </summary>
    protected Core.Diagnostics.DiagnosticCollector Diagnostics { get; }

    /// <summary>
    /// Gets the mapping repository for looking up WPF to Avalonia mappings.
    /// </summary>
    protected Mappings.IMappingRepository MappingRepository { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfToAvaloniaRewriter"/> class.
    /// </summary>
    /// <param name="semanticModel">The semantic model for the syntax tree.</param>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <param name="mappingRepository">The mapping repository.</param>
    protected WpfToAvaloniaRewriter(
        SemanticModel semanticModel,
        Core.Diagnostics.DiagnosticCollector diagnostics,
        Mappings.IMappingRepository mappingRepository)
        : base(visitIntoStructuredTrivia: false)
    {
        SemanticModel = semanticModel;
        Diagnostics = diagnostics;
        MappingRepository = mappingRepository;
    }

    /// <summary>
    /// Determines whether a type is a WPF type based on its namespace.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <returns>True if the type is from a WPF namespace, otherwise false.</returns>
    protected bool IsWpfType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            return false;

        var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();
        if (string.IsNullOrEmpty(namespaceName))
            return false;

        return namespaceName.StartsWith("System.Windows", StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the fully qualified name of a type.
    /// </summary>
    /// <param name="typeSymbol">The type symbol.</param>
    /// <returns>The fully qualified type name.</returns>
    protected string GetFullTypeName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}
