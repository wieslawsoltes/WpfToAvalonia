using WpfToAvalonia.XamlParser.UnifiedAst;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.Transformation;
using WpfToAvalonia.XamlParser.Transformation.Rules;

namespace WpfToAvalonia.XamlParser.Transformers;

/// <summary>
/// Orchestrates the execution of XAML transformers in priority order.
/// </summary>
/// <remarks>
/// The pipeline executes transformers in ascending priority order:
/// - Priority 10: Namespace transformation
/// - Priority 20: Type transformation
/// - Priority 30: Property transformation
/// - Priority 40+: Additional transformations (bindings, styles, resources, etc.)
/// </remarks>
public class TransformationPipeline
{
    private readonly List<IXamlTransformer> _transformers;
    private readonly WpfToAvaloniaMapping _mappingProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransformationPipeline"/> class.
    /// </summary>
    /// <param name="mappingProvider">The WPF to Avalonia mapping provider.</param>
    public TransformationPipeline(WpfToAvaloniaMapping? mappingProvider = null)
    {
        _transformers = new List<IXamlTransformer>();
        _mappingProvider = mappingProvider ?? new WpfToAvaloniaMapping();
    }

    /// <summary>
    /// Adds a transformer to the pipeline.
    /// </summary>
    /// <param name="transformer">The transformer to add.</param>
    public void AddTransformer(IXamlTransformer transformer)
    {
        _transformers.Add(transformer);
    }

    /// <summary>
    /// Adds multiple transformers to the pipeline.
    /// </summary>
    /// <param name="transformers">The transformers to add.</param>
    public void AddTransformers(params IXamlTransformer[] transformers)
    {
        _transformers.AddRange(transformers);
    }

    /// <summary>
    /// Removes all transformers from the pipeline.
    /// </summary>
    public void ClearTransformers()
    {
        _transformers.Clear();
    }

    /// <summary>
    /// Executes all transformers on the document in priority order.
    /// </summary>
    /// <param name="document">The document to transform.</param>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <returns>The transformation context with statistics and diagnostics.</returns>
    public TransformationContext Transform(UnifiedXamlDocument document, DiagnosticCollector diagnostics)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (diagnostics == null)
        {
            throw new ArgumentNullException(nameof(diagnostics));
        }

        diagnostics.AddInfo(
            "PIPELINE_START",
            $"Starting transformation pipeline with {_transformers.Count} transformers",
            null);

        // Create transformation context
        var context = new TransformationContext(diagnostics, _mappingProvider);

        // Sort transformers by priority (ascending order)
        var sortedTransformers = _transformers
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Name)
            .ToList();

        diagnostics.AddInfo(
            "PIPELINE_ORDER",
            $"Transformer execution order: {string.Join(", ", sortedTransformers.Select(t => $"{t.Name}(Priority={t.Priority})"))}",
            null);

        // Execute each transformer
        foreach (var transformer in sortedTransformers)
        {
            try
            {
                diagnostics.AddInfo(
                    "TRANSFORMER_START",
                    $"Executing transformer: {transformer.Name} (Priority={transformer.Priority})",
                    null);

                var startTime = DateTime.UtcNow;
                transformer.Transform(document, context);
                var duration = DateTime.UtcNow - startTime;

                diagnostics.AddInfo(
                    "TRANSFORMER_COMPLETE",
                    $"Completed transformer: {transformer.Name} in {duration.TotalMilliseconds:F2}ms",
                    null);
            }
            catch (Exception ex)
            {
                diagnostics.AddError(
                    "TRANSFORMER_ERROR",
                    $"Transformer '{transformer.Name}' failed: {ex.Message}",
                    null);

                // Continue with next transformer or stop based on configuration
                // For now, we'll continue to allow other transformers to run
            }
        }

        diagnostics.AddInfo(
            "PIPELINE_COMPLETE",
            $"Transformation pipeline complete. Elements: {context.Statistics.ElementsTransformed}, " +
            $"Properties: {context.Statistics.PropertiesTransformed}, " +
            $"Namespaces: {context.Statistics.NamespacesTransformed}, " +
            $"Warnings: {context.Statistics.WarningsGenerated}",
            null);

        return context;
    }

    /// <summary>
    /// Creates a default transformation pipeline with standard transformers.
    /// </summary>
    /// <param name="mappingProvider">Optional custom mapping provider.</param>
    /// <returns>A pipeline with standard WPF to Avalonia transformers.</returns>
    public static TransformationPipeline CreateDefault(WpfToAvaloniaMapping? mappingProvider = null)
    {
        var pipeline = new TransformationPipeline(mappingProvider);

        // Add core transformers in priority order
        pipeline.AddTransformers(
            new NamespaceTransformer(),      // Priority 10
            new TypeTransformer(),           // Priority 20
            new PropertyTransformer()        // Priority 30
        );

        // Add binding transformers (Priority 40)
        pipeline.AddTransformer(new RuleBasedTransformer("BindingTransformations", 40, new ITransformationRule[]
        {
            new BasicBindingTransformationRule(),
            new ElementNameBindingTransformationRule(),
            new RelativeSourceBindingTransformationRule(),
            new BindingPathTransformationRule(),
            new MultiBindingTransformationRule()
        }));

        // Add resource transformer (Priority 45)
        pipeline.AddTransformer(new ResourceTransformer());

        // Add style transformers (Priority 50)
        pipeline.AddTransformer(new RuleBasedTransformer("StyleTransformations", 50, new ITransformationRule[]
        {
            new TriggerToStyleSelectorTransformer(),
            new DataTriggerToBindingTransformer(),
            new EventTriggerToAnimationTransformer(),
            new MultiTriggerTransformer(),
            new StyleTriggersRestructuringRule(),
            new ConvertedTriggerCleanupRule(),
            new StyleToControlThemeTransformer()
        }));

        // Add template transformer (Priority 55)
        pipeline.AddTransformer(new TemplateTransformer());

        // Add control transformers (Priority 60)
        pipeline.AddTransformer(new RuleBasedTransformer("ControlTransformations", 60, new ITransformationRule[]
        {
            new TextBlockTransformationRule(),
            new ButtonTransformationRule(),
            new TextBoxTransformationRule(),
            new CheckBoxTransformationRule(),
            new RadioButtonTransformationRule(),
            new ComboBoxTransformationRule()
        }));

        return pipeline;
    }

    /// <summary>
    /// Gets all registered transformers sorted by priority.
    /// </summary>
    /// <returns>Transformers in execution order.</returns>
    public IReadOnlyList<IXamlTransformer> GetTransformers()
    {
        return _transformers
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Name)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets the count of registered transformers.
    /// </summary>
    public int TransformerCount => _transformers.Count;
}
