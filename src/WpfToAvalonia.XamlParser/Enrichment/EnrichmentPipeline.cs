using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.TypeSystem;
using WpfToAvalonia.XamlParser.UnifiedAst;
using WpfToAvalonia.XamlParser.Visitors;

namespace WpfToAvalonia.XamlParser.Enrichment;

// IEnricher interface is defined in TypeResolutionEnricher.cs

/// <summary>
/// Coordinates multiple enrichment passes over the Unified AST.
/// Each pass adds semantic information from different sources (type system, resources, bindings, etc.).
/// </summary>
public sealed class EnrichmentPipeline
{
    private readonly IXamlTypeResolver _typeResolver;
    private readonly DiagnosticCollector _diagnostics;
    private readonly List<IEnricher> _enrichers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EnrichmentPipeline"/> class.
    /// </summary>
    public EnrichmentPipeline(IXamlTypeResolver typeResolver, DiagnosticCollector diagnostics)
    {
        _typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

        // Register default enrichers
        RegisterDefaultEnrichers();
    }

    /// <summary>
    /// Registers the default enrichers.
    /// </summary>
    private void RegisterDefaultEnrichers()
    {
        // Phase 1: Type resolution (must run first)
        _enrichers.Add(new TypeResolutionEnricher(_typeResolver));

        // Phase 2: Resource resolution (depends on type resolution)
        _enrichers.Add(new ResourceResolutionEnricher(_typeResolver));

        // Phase 3: Binding analysis (depends on type resolution)
        _enrichers.Add(new BindingAnalysisEnricher(_typeResolver));
    }

    /// <summary>
    /// Adds a custom enricher to the pipeline.
    /// </summary>
    public void AddEnricher(IEnricher enricher)
    {
        _enrichers.Add(enricher);
    }

    /// <summary>
    /// Runs the enrichment pipeline on a XAML document.
    /// </summary>
    public void Enrich(UnifiedXamlDocument document)
    {
        if (document.Root == null)
        {
            return;
        }

        // Run each enricher in sequence
        foreach (var enricher in _enrichers)
        {
            try
            {
                enricher.Enrich(document);
            }
            catch (Exception ex)
            {
                _diagnostics.AddError(
                    "ENRICHMENT_ERROR",
                    $"Enricher '{enricher.GetType().Name}' failed: {ex.Message}",
                    document.FilePath);
            }
        }

        // Mark document as analyzed
        document.SetMetadata("Enriched", true);
    }
}

/// <summary>
/// Enricher that resolves resource references.
/// </summary>
public sealed class ResourceResolutionEnricher : UnifiedXamlVisitorBase, IEnricher
{
    private readonly IXamlTypeResolver _typeResolver;
    private UnifiedXamlDocument? _currentDocument;

    public ResourceResolutionEnricher(IXamlTypeResolver typeResolver)
    {
        _typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
    }

    public void Enrich(UnifiedXamlDocument document)
    {
        _currentDocument = document;
        VisitDocument(document);
    }

    public override void VisitProperty(UnifiedXamlProperty property)
    {
        // Check if this property uses a resource reference
        if (property.MarkupExtension != null)
        {
            var extensionType = property.MarkupExtension.GetExtensionType();
            if (extensionType == MarkupExtensionType.StaticResource ||
                extensionType == MarkupExtensionType.DynamicResource)
            {
                var resourceKey = property.MarkupExtension.Resource?.ResourceKey;
                if (!string.IsNullOrEmpty(resourceKey) && _currentDocument != null)
                {
                    // Try to resolve the resource
                    if (_currentDocument.Resources.TryGetResource(resourceKey, out var resource))
                    {
                        if (property.MarkupExtension.Resource != null)
                        {
                            property.MarkupExtension.Resource.ResolvedResource = resource;
                        }
                    }
                    else
                    {
                        property.MarkupExtension.AddDiagnostic(
                            "RESOURCE_NOT_FOUND",
                            $"Resource with key '{resourceKey}' not found",
                            DiagnosticSeverity.Warning);
                    }
                }
            }
        }

        base.VisitProperty(property);
    }
}

/// <summary>
/// Enricher that analyzes binding expressions.
/// </summary>
public sealed class BindingAnalysisEnricher : UnifiedXamlVisitorBase, IEnricher
{
    private readonly IXamlTypeResolver _typeResolver;

    public BindingAnalysisEnricher(IXamlTypeResolver typeResolver)
    {
        _typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
    }

    public void Enrich(UnifiedXamlDocument document)
    {
        VisitDocument(document);
    }

    public override void VisitProperty(UnifiedXamlProperty property)
    {
        // Analyze binding expressions
        if (property.MarkupExtension != null)
        {
            var extensionType = property.MarkupExtension.GetExtensionType();
            if (extensionType == MarkupExtensionType.Binding && property.MarkupExtension.Binding != null)
            {
                var binding = property.MarkupExtension.Binding;

                // Validate binding properties
                if (string.IsNullOrEmpty(binding.Path) &&
                    string.IsNullOrEmpty(binding.ElementName) &&
                    binding.RelativeSource == null &&
                    binding.Source == null)
                {
                    property.MarkupExtension.AddDiagnostic(
                        "BINDING_NO_SOURCE",
                        "Binding has no source (Path, ElementName, RelativeSource, or Source)",
                        DiagnosticSeverity.Warning);
                }

                // Check for ElementName bindings
                if (!string.IsNullOrEmpty(binding.ElementName))
                {
                    // Try to find the referenced element
                    var document = GetDocument(property);
                    if (document != null)
                    {
                        var targetElement = document.FindElementByName(binding.ElementName);
                        if (targetElement == null)
                        {
                            property.MarkupExtension.AddDiagnostic(
                                "BINDING_ELEMENT_NOT_FOUND",
                                $"Element with name '{binding.ElementName}' not found",
                                DiagnosticSeverity.Warning);
                        }
                    }
                }
            }
        }

        base.VisitProperty(property);
    }

    private UnifiedXamlDocument? GetDocument(UnifiedXamlNode node)
    {
        // Walk up the tree to find the document
        var current = node;
        while (current != null)
        {
            if (current is UnifiedXamlElement element && element.Parent == null)
            {
                // This is the root element, but we need the document
                // In practice, we'd need a reference back to the document
                return null;
            }
            current = current.Parent;
        }
        return null;
    }
}
