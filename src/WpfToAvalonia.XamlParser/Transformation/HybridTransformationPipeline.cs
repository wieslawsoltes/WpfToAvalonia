using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation;

/// <summary>
/// Pipeline that orchestrates multiple hybrid XAML transformers.
/// Applies transformations in sequence while maintaining document consistency.
/// </summary>
public sealed class HybridTransformationPipeline
{
    private readonly DiagnosticCollector _diagnostics;
    private readonly List<HybridXamlTransformer> _transformers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridTransformationPipeline"/> class.
    /// </summary>
    public HybridTransformationPipeline(DiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    /// <summary>
    /// Adds a transformer to the pipeline.
    /// Transformers are executed in the order they are added.
    /// </summary>
    public HybridTransformationPipeline AddTransformer(HybridXamlTransformer transformer)
    {
        if (transformer == null)
        {
            throw new ArgumentNullException(nameof(transformer));
        }

        _transformers.Add(transformer);
        return this;
    }

    /// <summary>
    /// Adds multiple transformers to the pipeline.
    /// </summary>
    public HybridTransformationPipeline AddTransformers(params HybridXamlTransformer[] transformers)
    {
        foreach (var transformer in transformers)
        {
            AddTransformer(transformer);
        }
        return this;
    }

    /// <summary>
    /// Adds multiple transformers to the pipeline.
    /// </summary>
    public HybridTransformationPipeline AddTransformers(IEnumerable<HybridXamlTransformer> transformers)
    {
        foreach (var transformer in transformers)
        {
            AddTransformer(transformer);
        }
        return this;
    }

    /// <summary>
    /// Executes all transformers in the pipeline on the document.
    /// </summary>
    public UnifiedXamlDocument Transform(UnifiedXamlDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (_transformers.Count == 0)
        {
            _diagnostics.AddWarning(
                "PIPELINE_EMPTY",
                "Transformation pipeline has no transformers",
                document.FilePath);
            return document;
        }

        _diagnostics.AddInfo(
            "PIPELINE_START",
            $"Starting transformation pipeline with {_transformers.Count} transformers",
            document.FilePath);

        var startTime = DateTime.UtcNow;
        var transformedDocument = document;

        // Execute each transformer in sequence
        for (int i = 0; i < _transformers.Count; i++)
        {
            var transformer = _transformers[i];

            _diagnostics.AddInfo(
                "PIPELINE_TRANSFORMER",
                $"Executing transformer {i + 1}/{_transformers.Count}: {transformer.TransformerName}",
                document.FilePath);

            try
            {
                transformedDocument = transformer.Transform(transformedDocument);

                // Validate document integrity after each transformation
                ValidateDocumentIntegrity(transformedDocument, transformer.TransformerName);
            }
            catch (Exception ex)
            {
                _diagnostics.AddError(
                    "PIPELINE_TRANSFORMER_FAILED",
                    $"Transformer {transformer.TransformerName} failed: {ex.Message}",
                    document.FilePath);

                // Continue with remaining transformers or abort?
                // For now, we'll abort the pipeline on error
                throw new InvalidOperationException(
                    $"Transformation pipeline aborted at transformer {transformer.TransformerName}",
                    ex);
            }
        }

        var elapsed = DateTime.UtcNow - startTime;

        _diagnostics.AddInfo(
            "PIPELINE_COMPLETE",
            $"Transformation pipeline completed successfully in {elapsed.TotalMilliseconds:F2}ms",
            document.FilePath);

        return transformedDocument;
    }

    /// <summary>
    /// Validates that the document maintains structural integrity after transformation.
    /// </summary>
    private void ValidateDocumentIntegrity(UnifiedXamlDocument document, string transformerName)
    {
        if (document.Root == null)
        {
            _diagnostics.AddError(
                "PIPELINE_INTEGRITY_NO_ROOT",
                $"Document lost its root element after {transformerName} transformation",
                document.FilePath);
            return;
        }

        // Check for circular references in the tree
        var visited = new HashSet<UnifiedXamlElement>();
        ValidateNoCycles(document.Root, visited, transformerName);

        // Check that parent-child relationships are bidirectional
        ValidateParentChildConsistency(document.Root, null, transformerName);
    }

    /// <summary>
    /// Validates that there are no cycles in the element tree.
    /// </summary>
    private void ValidateNoCycles(
        UnifiedXamlElement element,
        HashSet<UnifiedXamlElement> visited,
        string transformerName)
    {
        if (visited.Contains(element))
        {
            _diagnostics.AddError(
                "PIPELINE_INTEGRITY_CYCLE",
                $"Circular reference detected in element tree after {transformerName} transformation",
                element.Location.FilePath,
                element.Location.Line,
                element.Location.Column);
            return;
        }

        visited.Add(element);

        foreach (var child in element.Children)
        {
            ValidateNoCycles(child, visited, transformerName);
        }

        visited.Remove(element);
    }

    /// <summary>
    /// Validates that parent-child relationships are consistent.
    /// </summary>
    private void ValidateParentChildConsistency(
        UnifiedXamlElement element,
        UnifiedXamlElement? expectedParent,
        string transformerName)
    {
        if (element.Parent != expectedParent)
        {
            _diagnostics.AddWarning(
                "PIPELINE_INTEGRITY_PARENT_MISMATCH",
                $"Element parent mismatch detected after {transformerName} transformation",
                element.Location.FilePath,
                element.Location.Line,
                element.Location.Column);
        }

        foreach (var child in element.Children)
        {
            ValidateParentChildConsistency(child, element, transformerName);
        }
    }

    /// <summary>
    /// Gets the number of transformers in the pipeline.
    /// </summary>
    public int TransformerCount => _transformers.Count;

    /// <summary>
    /// Clears all transformers from the pipeline.
    /// </summary>
    public void Clear()
    {
        _transformers.Clear();
    }

    /// <summary>
    /// Removes a transformer from the pipeline.
    /// </summary>
    public bool RemoveTransformer(HybridXamlTransformer transformer)
    {
        return _transformers.Remove(transformer);
    }

    /// <summary>
    /// Gets all transformers in the pipeline.
    /// </summary>
    public IReadOnlyList<HybridXamlTransformer> GetTransformers()
    {
        return _transformers.AsReadOnly();
    }
}

/// <summary>
/// Builder for creating transformation pipelines with fluent API.
/// </summary>
public sealed class HybridTransformationPipelineBuilder
{
    private readonly DiagnosticCollector _diagnostics;
    private readonly List<HybridXamlTransformer> _transformers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridTransformationPipelineBuilder"/> class.
    /// </summary>
    public HybridTransformationPipelineBuilder(DiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    /// <summary>
    /// Adds a transformer to the pipeline.
    /// </summary>
    public HybridTransformationPipelineBuilder AddTransformer(HybridXamlTransformer transformer)
    {
        _transformers.Add(transformer ?? throw new ArgumentNullException(nameof(transformer)));
        return this;
    }

    /// <summary>
    /// Adds a transformer to the pipeline if the condition is true.
    /// </summary>
    public HybridTransformationPipelineBuilder AddTransformerIf(
        bool condition,
        HybridXamlTransformer transformer)
    {
        if (condition)
        {
            AddTransformer(transformer);
        }
        return this;
    }

    /// <summary>
    /// Adds a transformer to the pipeline if the condition is true.
    /// </summary>
    public HybridTransformationPipelineBuilder AddTransformerIf(
        bool condition,
        Func<HybridXamlTransformer> transformerFactory)
    {
        if (condition)
        {
            AddTransformer(transformerFactory());
        }
        return this;
    }

    /// <summary>
    /// Adds multiple transformers to the pipeline.
    /// </summary>
    public HybridTransformationPipelineBuilder AddTransformers(params HybridXamlTransformer[] transformers)
    {
        foreach (var transformer in transformers)
        {
            AddTransformer(transformer);
        }
        return this;
    }

    /// <summary>
    /// Adds multiple transformers to the pipeline.
    /// </summary>
    public HybridTransformationPipelineBuilder AddTransformers(IEnumerable<HybridXamlTransformer> transformers)
    {
        foreach (var transformer in transformers)
        {
            AddTransformer(transformer);
        }
        return this;
    }

    /// <summary>
    /// Builds the transformation pipeline.
    /// </summary>
    public HybridTransformationPipeline Build()
    {
        var pipeline = new HybridTransformationPipeline(_diagnostics);
        pipeline.AddTransformers(_transformers);
        return pipeline;
    }

    /// <summary>
    /// Builds and executes the transformation pipeline on a document.
    /// </summary>
    public UnifiedXamlDocument BuildAndTransform(UnifiedXamlDocument document)
    {
        var pipeline = Build();
        return pipeline.Transform(document);
    }
}
