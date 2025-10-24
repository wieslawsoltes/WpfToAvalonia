using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Pipeline;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Transforms a C# file from WPF to Avalonia.
/// </summary>
public sealed class CSharpFileTransformer : ITransformer
{
    /// <inheritdoc />
    public string Name => "C# File Transformer";

    /// <inheritdoc />
    public int Priority => 100;

    /// <inheritdoc />
    public bool CanTransform(TransformationContext context)
    {
        return context.Project != null;
    }

    /// <inheritdoc />
    public async Task TransformAsync(TransformationContext context, CancellationToken cancellationToken = default)
    {
        if (context.Project == null)
        {
            return;
        }

        var project = context.Project;
        var updatedDocuments = new List<Document>();

        foreach (var document in project.Documents)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Only process C# files
            if (document.FilePath == null || !document.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var transformedDocument = await TransformDocumentAsync(document, context, cancellationToken);
            if (transformedDocument != null && transformedDocument != document)
            {
                updatedDocuments.Add(transformedDocument);
                context.TransformedFiles.Add(document.FilePath);
            }
        }

        // Apply all document changes to the project
        if (updatedDocuments.Count > 0 && !context.IsDryRun)
        {
            var solution = project.Solution;
            foreach (var updatedDoc in updatedDocuments)
            {
                solution = updatedDoc.Project.Solution;
            }

            context.Solution = solution;
        }
    }

    private async Task<Document?> TransformDocumentAsync(
        Document document,
        TransformationContext context,
        CancellationToken cancellationToken)
    {
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
        if (syntaxRoot == null)
        {
            return null;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel == null)
        {
            return null;
        }

        var originalRoot = syntaxRoot;

        // Apply using directive transformation
        var usingRewriter = new UsingDirectivesRewriter(
            semanticModel,
            context.Diagnostics,
            context.MappingRepository);

        syntaxRoot = usingRewriter.Visit(syntaxRoot);

        // Re-get semantic model if syntax changed
        if (syntaxRoot != originalRoot)
        {
            document = document.WithSyntaxRoot(syntaxRoot);
            semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
            {
                return document;
            }
        }

        // Apply type reference transformation
        var typeRewriter = new TypeReferenceRewriter(
            semanticModel,
            context.Diagnostics,
            context.MappingRepository);

        syntaxRoot = typeRewriter.Visit(syntaxRoot);

        // Re-get semantic model for subsequent transformations
        document = document.WithSyntaxRoot(syntaxRoot);
        semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel == null)
        {
            return document;
        }

        // Apply property access transformation
        var propertyRewriter = new PropertyAccessRewriter(
            semanticModel,
            context.Diagnostics,
            context.MappingRepository);

        syntaxRoot = propertyRewriter.Visit(syntaxRoot);

        // Apply event handler transformation
        document = document.WithSyntaxRoot(syntaxRoot);
        semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel != null)
        {
            var eventRewriter = new EventHandlerRewriter(
                semanticModel,
                context.Diagnostics,
                context.MappingRepository);

            syntaxRoot = eventRewriter.Visit(syntaxRoot);
        }

        // Apply dependency property analysis
        document = document.WithSyntaxRoot(syntaxRoot);
        semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel != null)
        {
            var dpRewriter = new DependencyPropertyRewriter(
                semanticModel,
                context.Diagnostics,
                context.MappingRepository);

            syntaxRoot = dpRewriter.Visit(syntaxRoot);
        }

        // Apply resource access transformation (Task 2.5.1)
        document = document.WithSyntaxRoot(syntaxRoot);
        semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel != null)
        {
            var resourceRewriter = new ResourceAccessRewriter(
                semanticModel,
                context.Diagnostics,
                context.MappingRepository);

            syntaxRoot = resourceRewriter.Visit(syntaxRoot);
        }

        // Apply FrameworkElementFactory transformation (Task 2.5.2.1)
        document = document.WithSyntaxRoot(syntaxRoot);
        semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel != null)
        {
            var frameworkElementFactoryRewriter = new FrameworkElementFactoryRewriter(
                semanticModel,
                context.Diagnostics,
                context.MappingRepository);

            syntaxRoot = frameworkElementFactoryRewriter.Visit(syntaxRoot);
        }

        // Apply template part attribute transformation (Task 2.5.2.2)
        document = document.WithSyntaxRoot(syntaxRoot);
        semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel != null)
        {
            var templatePartRewriter = new TemplatePartAttributeRewriter(
                semanticModel,
                context.Diagnostics,
                context.MappingRepository);

            syntaxRoot = templatePartRewriter.Visit(syntaxRoot);
        }

        // Apply Visual State Manager transformation (Task 2.5.2.3)
        document = document.WithSyntaxRoot(syntaxRoot);
        semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel != null)
        {
            var vsmRewriter = new VisualStateManagerRewriter(
                semanticModel,
                context.Diagnostics,
                context.MappingRepository);

            syntaxRoot = vsmRewriter.Visit(syntaxRoot);
        }

        // Apply coercion callback transformation (Task 2.5.3.1)
        document = document.WithSyntaxRoot(syntaxRoot);
        semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel != null)
        {
            var coercionRewriter = new CoercionCallbackRewriter(
                semanticModel,
                context.Diagnostics,
                context.MappingRepository);

            syntaxRoot = coercionRewriter.Visit(syntaxRoot);
        }

        // Apply Freezable transformation (Task 2.5.3.2)
        document = document.WithSyntaxRoot(syntaxRoot);
        semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel != null)
        {
            var freezableRewriter = new FreezableRewriter(
                semanticModel,
                context.Diagnostics,
                context.MappingRepository);

            syntaxRoot = freezableRewriter.Visit(syntaxRoot);
        }

        // Apply threading model transformation (Task 2.5.3.3)
        document = document.WithSyntaxRoot(syntaxRoot);
        semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel != null)
        {
            var threadingRewriter = new ThreadingModelRewriter(
                semanticModel,
                context.Diagnostics,
                context.MappingRepository);

            syntaxRoot = threadingRewriter.Visit(syntaxRoot);
        }

        // Apply WPF attribute transformation (Task 2.5.3.4)
        document = document.WithSyntaxRoot(syntaxRoot);
        semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel != null)
        {
            var attributeRewriter = new WpfAttributeRewriter(
                semanticModel,
                context.Diagnostics,
                context.MappingRepository);

            syntaxRoot = attributeRewriter.Visit(syntaxRoot);
        }

        // Format the document if configured
        if (context.Configuration.PreserveFormatting)
        {
            syntaxRoot = syntaxRoot.NormalizeWhitespace();
        }

        return document.WithSyntaxRoot(syntaxRoot);
    }
}
