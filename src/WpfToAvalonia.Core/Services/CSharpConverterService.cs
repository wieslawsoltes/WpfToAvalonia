using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Transformers.CSharp;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Services;

/// <summary>
/// Service for converting C# code from WPF to Avalonia.
/// Encapsulates the full C# transformation pipeline.
/// </summary>
public sealed class CSharpConverterService
{
    private readonly IMappingRepository _mappingRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpConverterService"/> class.
    /// </summary>
    /// <param name="mappingRepository">The mapping repository for WPF to Avalonia mappings.</param>
    public CSharpConverterService(IMappingRepository mappingRepository)
    {
        _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
    }

    /// <summary>
    /// Converts C# code from WPF to Avalonia.
    /// </summary>
    /// <param name="sourceCode">The WPF C# source code to convert.</param>
    /// <param name="diagnostics">The diagnostic collector to gather transformation diagnostics.</param>
    /// <returns>The converted Avalonia C# code.</returns>
    public string Convert(string sourceCode, DiagnosticCollector diagnostics)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            return sourceCode;
        }

        ArgumentNullException.ThrowIfNull(diagnostics);

        try
        {
            // Parse the C# code
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var compilation = CSharpCompilation.Create("ConversionCompilation")
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(tree);

            var semanticModel = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            // Step 1: Transform using directives
            var usingRewriter = new UsingDirectivesRewriter(semanticModel, diagnostics, _mappingRepository);
            root = usingRewriter.Visit(root);

            // Re-compile for updated semantic model
            tree = tree.WithRootAndOptions(root, tree.Options);
            compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), tree);
            semanticModel = compilation.GetSemanticModel(tree);

            // Step 2: Transform type references
            var typeRewriter = new TypeReferenceRewriter(semanticModel, diagnostics, _mappingRepository);
            root = typeRewriter.Visit(root);

            // Re-compile for updated semantic model
            tree = tree.WithRootAndOptions(root, tree.Options);
            compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), tree);
            semanticModel = compilation.GetSemanticModel(tree);

            // Step 3: Transform property access
            var propertyRewriter = new PropertyAccessRewriter(semanticModel, diagnostics, _mappingRepository);
            root = propertyRewriter.Visit(root);

            // Re-compile for updated semantic model
            tree = tree.WithRootAndOptions(root, tree.Options);
            compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), tree);
            semanticModel = compilation.GetSemanticModel(tree);

            // Step 4: Transform dependency properties
            var dpRewriter = new DependencyPropertyRewriter(semanticModel, diagnostics, _mappingRepository);
            root = dpRewriter.Visit(root);

            return root.ToFullString();
        }
        catch (Exception ex)
        {
            diagnostics.AddError(
                "CSHARP_CONVERSION_ERROR",
                $"C# conversion failed: {ex.Message}",
                null);
            return sourceCode; // Return original on error
        }
    }

    /// <summary>
    /// Converts C# code from WPF to Avalonia asynchronously.
    /// </summary>
    /// <param name="sourceCode">The WPF C# source code to convert.</param>
    /// <param name="diagnostics">The diagnostic collector to gather transformation diagnostics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The converted Avalonia C# code.</returns>
    public Task<string> ConvertAsync(
        string sourceCode,
        DiagnosticCollector diagnostics,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Convert(sourceCode, diagnostics), cancellationToken);
    }
}
