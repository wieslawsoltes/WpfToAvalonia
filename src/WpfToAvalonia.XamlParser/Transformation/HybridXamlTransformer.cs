using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation;

/// <summary>
/// Transformation mode that determines which AST layer to use.
/// </summary>
public enum TransformationMode
{
    /// <summary>
    /// Transform only at XML layer (fast, format-preserving).
    /// Best for simple attribute/element name changes.
    /// </summary>
    XmlOnly,

    /// <summary>
    /// Transform only at semantic layer (type-safe).
    /// Best for complex transformations requiring type information.
    /// </summary>
    SemanticOnly,

    /// <summary>
    /// Hybrid transformation using both layers.
    /// Uses XML layer where possible, semantic layer where needed.
    /// </summary>
    Hybrid
}

/// <summary>
/// Base class for hybrid XAML transformers that can operate on both XML and semantic layers.
/// This is the foundation for all WPF to Avalonia XAML transformations.
/// </summary>
public abstract class HybridXamlTransformer
{
    protected readonly DiagnosticCollector Diagnostics;
    protected readonly TransformationMode Mode;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridXamlTransformer"/> class.
    /// </summary>
    protected HybridXamlTransformer(DiagnosticCollector diagnostics, TransformationMode mode = TransformationMode.Hybrid)
    {
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        Mode = mode;
    }

    /// <summary>
    /// Gets the name of this transformer for diagnostic purposes.
    /// </summary>
    public abstract string TransformerName { get; }

    /// <summary>
    /// Determines whether this transformer can handle the given element.
    /// </summary>
    public abstract bool CanTransform(UnifiedXamlElement element);

    /// <summary>
    /// Transforms a UnifiedXamlDocument.
    /// </summary>
    public virtual UnifiedXamlDocument Transform(UnifiedXamlDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        Diagnostics.AddInfo(
            "TRANSFORMER_START",
            $"Starting {TransformerName} transformation",
            document.FilePath);

        try
        {
            // Validate document has required layers for the transformation mode
            ValidateDocumentLayers(document);

            // Transform the document
            if (document.Root != null)
            {
                TransformElement(document.Root);
            }

            // Validate the transformed document
            if (Mode != TransformationMode.XmlOnly)
            {
                ValidateTransformation(document);
            }

            Diagnostics.AddInfo(
                "TRANSFORMER_SUCCESS",
                $"{TransformerName} transformation completed successfully",
                document.FilePath);

            return document;
        }
        catch (Exception ex)
        {
            Diagnostics.AddError(
                "TRANSFORMER_FAILED",
                $"{TransformerName} transformation failed: {ex.Message}",
                document.FilePath);
            throw;
        }
    }

    /// <summary>
    /// Transforms a UnifiedXamlElement.
    /// This is the core transformation method that subclasses implement.
    /// </summary>
    protected abstract void TransformElement(UnifiedXamlElement element);

    /// <summary>
    /// Validates that the document has the required layers for the transformation mode.
    /// </summary>
    protected virtual void ValidateDocumentLayers(UnifiedXamlDocument document)
    {
        switch (Mode)
        {
            case TransformationMode.XmlOnly:
                if (document.XmlDocument == null)
                {
                    throw new InvalidOperationException(
                        $"{TransformerName} requires XML layer but document has no XmlDocument");
                }
                break;

            case TransformationMode.SemanticOnly:
                if (document.SemanticDocument == null)
                {
                    throw new InvalidOperationException(
                        $"{TransformerName} requires semantic layer but document has no SemanticDocument");
                }
                break;

            case TransformationMode.Hybrid:
                if (document.XmlDocument == null)
                {
                    Diagnostics.AddWarning(
                        "MISSING_XML_LAYER",
                        $"{TransformerName} operates in Hybrid mode but document has no XML layer",
                        document.FilePath);
                }
                if (document.SemanticDocument == null)
                {
                    Diagnostics.AddWarning(
                        "MISSING_SEMANTIC_LAYER",
                        $"{TransformerName} operates in Hybrid mode but document has no semantic layer",
                        document.FilePath);
                }
                break;
        }
    }

    /// <summary>
    /// Validates the transformed document.
    /// Checks both XML validity and semantic validity.
    /// </summary>
    protected virtual void ValidateTransformation(UnifiedXamlDocument document)
    {
        if (document.Root == null)
        {
            return;
        }

        var validationErrors = 0;

        // Validate all elements
        foreach (var element in document.Root.DescendantsAndSelf())
        {
            // Check if element has required layers
            if (element.XmlElement == null && Mode != TransformationMode.SemanticOnly)
            {
                Diagnostics.AddWarning(
                    "VALIDATION_MISSING_XML",
                    $"Element {element.TypeName} is missing XML layer after transformation",
                    element.Location.FilePath,
                    element.Location.Line,
                    element.Location.Column);
                validationErrors++;
            }

            // Check if transformation preserved essential information
            if (string.IsNullOrEmpty(element.TypeName))
            {
                Diagnostics.AddWarning(
                    "VALIDATION_MISSING_TYPE",
                    "Element is missing type name after transformation",
                    element.Location.FilePath,
                    element.Location.Line,
                    element.Location.Column);
                validationErrors++;
            }
        }

        if (validationErrors > 0)
        {
            Diagnostics.AddWarning(
                "VALIDATION_ISSUES",
                $"Found {validationErrors} validation issues after {TransformerName} transformation",
                document.FilePath);
        }
    }

    /// <summary>
    /// Determines the optimal transformation mode for a specific element.
    /// Subclasses can override this to make intelligent mode selection decisions.
    /// </summary>
    protected virtual TransformationMode DetermineTransformationMode(UnifiedXamlElement element)
    {
        // Default: use the configured mode
        return Mode;
    }

    /// <summary>
    /// Transforms at XML level only (fast, format-preserving).
    /// </summary>
    protected virtual void TransformXmlLayer(UnifiedXamlElement element)
    {
        // Default: no-op
        // Subclasses override to implement XML-level transformations
    }

    /// <summary>
    /// Transforms at semantic level (type-safe).
    /// </summary>
    protected virtual void TransformSemanticLayer(UnifiedXamlElement element)
    {
        // Default: no-op
        // Subclasses override to implement semantic-level transformations
    }

    /// <summary>
    /// Recursively transforms an element and all its children.
    /// </summary>
    protected void TransformElementRecursive(UnifiedXamlElement element)
    {
        if (!CanTransform(element))
        {
            // Still transform children even if we can't transform this element
            foreach (var child in element.Children.ToList())
            {
                TransformElementRecursive(child);
            }
            return;
        }

        // Transform this element
        TransformElement(element);

        // Transform children
        foreach (var child in element.Children.ToList())
        {
            TransformElementRecursive(child);
        }
    }

    /// <summary>
    /// Creates a diagnostic for a transformation.
    /// </summary>
    protected void AddTransformationDiagnostic(
        string code,
        string message,
        UnifiedXamlElement element,
        DiagnosticSeverity severity = DiagnosticSeverity.Info)
    {
        var diagnostic = new TransformationDiagnostic
        {
            Code = code,
            Message = message,
            Severity = severity,
            FilePath = element.Location.FilePath,
            Line = element.Location.Line,
            Column = element.Location.Column
        };

        element.Diagnostics.Add(diagnostic);

        switch (severity)
        {
            case DiagnosticSeverity.Error:
                Diagnostics.AddError(code, message, element.Location.FilePath, element.Location.Line, element.Location.Column);
                break;
            case DiagnosticSeverity.Warning:
                Diagnostics.AddWarning(code, message, element.Location.FilePath, element.Location.Line, element.Location.Column);
                break;
            case DiagnosticSeverity.Info:
                Diagnostics.AddInfo(code, message, element.Location.FilePath, element.Location.Line, element.Location.Column);
                break;
        }
    }
}

/// <summary>
/// Base class for transformers that can determine their mode based on element complexity.
/// </summary>
public abstract class AdaptiveHybridXamlTransformer : HybridXamlTransformer
{
    protected AdaptiveHybridXamlTransformer(DiagnosticCollector diagnostics)
        : base(diagnostics, TransformationMode.Hybrid)
    {
    }

    /// <summary>
    /// Determines whether the transformation needs semantic information.
    /// </summary>
    protected abstract bool RequiresSemanticInfo(UnifiedXamlElement element);

    protected override TransformationMode DetermineTransformationMode(UnifiedXamlElement element)
    {
        // Adaptive: use semantic layer only if needed
        return RequiresSemanticInfo(element)
            ? TransformationMode.SemanticOnly
            : TransformationMode.XmlOnly;
    }

    protected override void TransformElement(UnifiedXamlElement element)
    {
        var mode = DetermineTransformationMode(element);

        switch (mode)
        {
            case TransformationMode.XmlOnly:
                TransformXmlLayer(element);
                break;

            case TransformationMode.SemanticOnly:
                TransformSemanticLayer(element);
                break;

            case TransformationMode.Hybrid:
                TransformXmlLayer(element);
                TransformSemanticLayer(element);
                break;
        }
    }
}
