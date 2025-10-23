using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Rewrites using directives from WPF namespaces to Avalonia namespaces.
/// </summary>
public sealed class UsingDirectivesRewriter : WpfToAvaloniaRewriter
{
    private readonly HashSet<string> _addedNamespaces = new();
    private readonly HashSet<string> _removedNamespaces = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="UsingDirectivesRewriter"/> class.
    /// </summary>
    /// <param name="semanticModel">The semantic model for the syntax tree.</param>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <param name="mappingRepository">The mapping repository.</param>
    public UsingDirectivesRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Visits a using directive and transforms WPF namespaces to Avalonia.
    /// </summary>
    public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
    {
        // Handle using alias directives separately (e.g., using WpfButton = System.Windows.Controls.Button;)
        if (node.Alias != null)
        {
            return base.VisitUsingDirective(node);
        }

        var namespaceName = node.Name?.ToString();
        if (string.IsNullOrEmpty(namespaceName))
        {
            return base.VisitUsingDirective(node);
        }

        // Check if this is a WPF namespace that needs to be transformed
        var mapping = MappingRepository.FindNamespaceMapping(namespaceName);
        if (mapping == null)
        {
            // Not a WPF namespace, keep as is
            return base.VisitUsingDirective(node);
        }

        // Transform the namespace
        var avaloniaNamespaceName = mapping.AvaloniaNamespace;
        var newName = SyntaxFactory.ParseName(avaloniaNamespaceName)
            .WithTriviaFrom(node.Name!);

        var newUsing = node.WithName(newName);

        _removedNamespaces.Add(namespaceName);
        _addedNamespaces.Add(avaloniaNamespaceName);

        // Report the transformation
        var lineSpan = node.GetLocation().GetLineSpan();
        Diagnostics.AddInfo(
            DiagnosticCodes.NamespaceTransformed,
            $"Transformed namespace: {namespaceName} → {avaloniaNamespaceName}",
            lineSpan.Path,
            lineSpan.StartLinePosition.Line + 1,
            lineSpan.StartLinePosition.Character + 1);

        if (mapping.RequiresManualReview)
        {
            Diagnostics.AddWarning(
                DiagnosticCodes.NamespaceTransformed,
                $"Namespace transformation requires manual review: {mapping.Notes}",
                lineSpan.Path,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1);
        }

        return newUsing;
    }

    /// <summary>
    /// Gets the set of added namespaces during transformation.
    /// </summary>
    public IReadOnlySet<string> AddedNamespaces => _addedNamespaces;

    /// <summary>
    /// Gets the set of removed namespaces during transformation.
    /// </summary>
    public IReadOnlySet<string> RemovedNamespaces => _removedNamespaces;
}
