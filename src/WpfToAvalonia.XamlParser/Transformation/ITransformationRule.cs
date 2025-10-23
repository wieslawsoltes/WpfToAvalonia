using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation;

/// <summary>
/// Represents a transformation rule that can transform XAML nodes.
/// </summary>
public interface ITransformationRule
{
    /// <summary>
    /// Gets the name of this transformation rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the priority of this rule. Higher priority rules run first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Determines if this rule can transform the given node.
    /// </summary>
    bool CanTransform(UnifiedXamlNode node);

    /// <summary>
    /// Transforms the given node.
    /// Returns the transformed node, or null if the node should be removed.
    /// </summary>
    UnifiedXamlNode? Transform(UnifiedXamlNode node, TransformationContext context);
}

/// <summary>
/// Specialized transformation rule for XAML elements.
/// </summary>
public interface IElementTransformationRule : ITransformationRule
{
    /// <summary>
    /// Determines if this rule can transform the given element.
    /// </summary>
    bool CanTransformElement(UnifiedXamlElement element);

    /// <summary>
    /// Transforms the given element.
    /// </summary>
    UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context);
}

/// <summary>
/// Specialized transformation rule for XAML properties.
/// </summary>
public interface IPropertyTransformationRule : ITransformationRule
{
    /// <summary>
    /// Determines if this rule can transform the given property.
    /// </summary>
    bool CanTransformProperty(UnifiedXamlProperty property);

    /// <summary>
    /// Transforms the given property.
    /// </summary>
    UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context);
}

/// <summary>
/// Specialized transformation rule for markup extensions.
/// </summary>
public interface IMarkupExtensionTransformationRule : ITransformationRule
{
    /// <summary>
    /// Determines if this rule can transform the given markup extension.
    /// </summary>
    bool CanTransformMarkupExtension(UnifiedXamlMarkupExtension markupExtension);

    /// <summary>
    /// Transforms the given markup extension.
    /// </summary>
    UnifiedXamlMarkupExtension? TransformMarkupExtension(UnifiedXamlMarkupExtension markupExtension, TransformationContext context);
}

/// <summary>
/// Context information for transformations.
/// </summary>
public sealed class TransformationContext
{
    /// <summary>
    /// Gets the document being transformed.
    /// </summary>
    public UnifiedXamlDocument Document { get; }

    /// <summary>
    /// Gets the current element being processed (for property transformations).
    /// </summary>
    public UnifiedXamlElement? CurrentElement { get; set; }

    /// <summary>
    /// Gets the transformation options.
    /// </summary>
    public TransformationOptions Options { get; }

    /// <summary>
    /// Gets the transformation statistics.
    /// </summary>
    public TransformationStatistics Statistics { get; } = new();

    /// <summary>
    /// Gets custom metadata for the transformation.
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

    public TransformationContext(UnifiedXamlDocument document, TransformationOptions options)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Records a transformation.
    /// </summary>
    public void RecordTransformation(string ruleName, string nodeType, string details)
    {
        Statistics.RecordTransformation(ruleName, nodeType, details);
    }
}

/// <summary>
/// Options for XAML transformation.
/// </summary>
public sealed class TransformationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to preserve comments.
    /// </summary>
    public bool PreserveComments { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to preserve formatting.
    /// </summary>
    public bool PreserveFormatting { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to add transformation comments.
    /// </summary>
    public bool AddTransformationComments { get; set; } = false;

    /// <summary>
    /// Gets or sets the target Avalonia version.
    /// </summary>
    public string TargetAvaloniaVersion { get; set; } = "11.0";

    /// <summary>
    /// Gets or sets a value indicating whether to use compiled bindings.
    /// </summary>
    public bool UseCompiledBindings { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to use Avalonia-specific binding syntax.
    /// When true, converts ElementName bindings to # syntax (e.g., #ElementName.Property).
    /// </summary>
    public bool UseAvaloniaBindingSyntax { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to transform resource dictionaries.
    /// </summary>
    public bool TransformResourceDictionaries { get; set; } = true;

    /// <summary>
    /// Gets or sets custom transformation settings.
    /// </summary>
    public Dictionary<string, object> CustomSettings { get; } = new();
}

/// <summary>
/// Statistics for transformation operations.
/// </summary>
public sealed class TransformationStatistics
{
    private readonly List<TransformationRecord> _records = new();

    /// <summary>
    /// Gets the total number of transformations.
    /// </summary>
    public int TotalTransformations => _records.Count;

    /// <summary>
    /// Gets all transformation records.
    /// </summary>
    public IReadOnlyList<TransformationRecord> Records => _records;

    /// <summary>
    /// Records a transformation.
    /// </summary>
    public void RecordTransformation(string ruleName, string nodeType, string details)
    {
        _records.Add(new TransformationRecord
        {
            RuleName = ruleName,
            NodeType = nodeType,
            Details = details,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Gets transformations grouped by rule name.
    /// </summary>
    public Dictionary<string, int> GetTransformationsByRule()
    {
        return _records
            .GroupBy(r => r.RuleName)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Gets transformations grouped by node type.
    /// </summary>
    public Dictionary<string, int> GetTransformationsByNodeType()
    {
        return _records
            .GroupBy(r => r.NodeType)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}

/// <summary>
/// Record of a single transformation.
/// </summary>
public sealed class TransformationRecord
{
    public string RuleName { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
