using WpfToAvalonia.XamlParser.UnifiedAst;
using WpfToAvalonia.Core.Diagnostics;

namespace WpfToAvalonia.XamlParser.Transformers;

/// <summary>
/// Base interface for XAML transformers that convert WPF XAML to Avalonia XAML.
/// </summary>
public interface IXamlTransformer
{
    /// <summary>
    /// Gets the name of this transformer.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the priority of this transformer (lower values run first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Transforms a unified XAML document.
    /// </summary>
    /// <param name="document">The document to transform.</param>
    /// <param name="context">The transformation context.</param>
    void Transform(UnifiedXamlDocument document, TransformationContext context);
}

/// <summary>
/// Base interface for element-level transformers.
/// </summary>
public interface IElementTransformer : IXamlTransformer
{
    /// <summary>
    /// Determines if this transformer should process the given element.
    /// </summary>
    /// <param name="element">The element to check.</param>
    /// <param name="context">The transformation context.</param>
    /// <returns>True if the transformer should process this element.</returns>
    bool ShouldTransform(UnifiedXamlElement element, TransformationContext context);

    /// <summary>
    /// Transforms a single XAML element.
    /// </summary>
    /// <param name="element">The element to transform.</param>
    /// <param name="context">The transformation context.</param>
    void TransformElement(UnifiedXamlElement element, TransformationContext context);
}

/// <summary>
/// Base interface for property-level transformers.
/// </summary>
public interface IPropertyTransformer : IXamlTransformer
{
    /// <summary>
    /// Determines if this transformer should process the given property.
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <param name="element">The parent element.</param>
    /// <param name="context">The transformation context.</param>
    /// <returns>True if the transformer should process this property.</returns>
    bool ShouldTransform(UnifiedXamlProperty property, UnifiedXamlElement element, TransformationContext context);

    /// <summary>
    /// Transforms a single XAML property.
    /// </summary>
    /// <param name="property">The property to transform.</param>
    /// <param name="element">The parent element.</param>
    /// <param name="context">The transformation context.</param>
    void TransformProperty(UnifiedXamlProperty property, UnifiedXamlElement element, TransformationContext context);
}

/// <summary>
/// Transformation context that provides shared state and services during transformation.
/// </summary>
public class TransformationContext
{
    /// <summary>
    /// Gets the diagnostic collector.
    /// </summary>
    public DiagnosticCollector Diagnostics { get; }

    /// <summary>
    /// Gets the mapping provider.
    /// </summary>
    public WpfToAvaloniaMapping MappingProvider { get; }

    /// <summary>
    /// Gets or sets custom metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

    /// <summary>
    /// Gets the transformation statistics.
    /// </summary>
    public TransformationStatistics Statistics { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TransformationContext"/> class.
    /// </summary>
    public TransformationContext(DiagnosticCollector diagnostics, WpfToAvaloniaMapping mappingProvider)
    {
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        MappingProvider = mappingProvider ?? throw new ArgumentNullException(nameof(mappingProvider));
    }

    /// <summary>
    /// Gets metadata value or default.
    /// </summary>
    public T? GetMetadata<T>(string key, T? defaultValue = default)
    {
        if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// Sets metadata value.
    /// </summary>
    public void SetMetadata(string key, object value)
    {
        Metadata[key] = value;
    }
}

/// <summary>
/// Statistics collected during transformation.
/// </summary>
public class TransformationStatistics
{
    /// <summary>
    /// Gets or sets the number of elements transformed.
    /// </summary>
    public int ElementsTransformed { get; set; }

    /// <summary>
    /// Gets or sets the number of properties transformed.
    /// </summary>
    public int PropertiesTransformed { get; set; }

    /// <summary>
    /// Gets or sets the number of namespaces transformed.
    /// </summary>
    public int NamespacesTransformed { get; set; }

    /// <summary>
    /// Gets or sets the number of warnings generated.
    /// </summary>
    public int WarningsGenerated { get; set; }

    /// <summary>
    /// Gets the transformation type counts.
    /// </summary>
    public Dictionary<string, int> TransformationCounts { get; } = new();

    /// <summary>
    /// Increments a transformation count.
    /// </summary>
    public void IncrementCount(string transformationType)
    {
        if (!TransformationCounts.TryGetValue(transformationType, out var count))
        {
            count = 0;
        }
        TransformationCounts[transformationType] = count + 1;
    }
}

/// <summary>
/// WPF to Avalonia mapping provider.
/// This is a placeholder - will be populated with actual mappings.
/// </summary>
public class WpfToAvaloniaMapping
{
    /// <summary>
    /// Gets namespace mappings (WPF namespace → Avalonia namespace).
    /// </summary>
    public Dictionary<string, string> NamespaceMappings { get; } = new()
    {
        { "http://schemas.microsoft.com/winfx/2006/xaml/presentation", "https://github.com/avaloniaui" },
        { "http://schemas.microsoft.com/winfx/2006/xaml", "http://schemas.microsoft.com/winfx/2006/xaml" } // x: namespace stays the same
    };

    /// <summary>
    /// Gets type mappings (WPF type → Avalonia type).
    /// </summary>
    public Dictionary<string, string> TypeMappings { get; } = new()
    {
        // Most controls map 1:1, but some need special handling
        { "Window", "Window" },
        { "UserControl", "UserControl" },
        { "Button", "Button" },
        { "TextBox", "TextBox" },
        { "TextBlock", "TextBlock" },
        { "StackPanel", "StackPanel" },
        { "Grid", "Grid" },
        { "Border", "Border" },
        { "ListView", "ListBox" }, // Avalonia doesn't have ListView
        { "DataGrid", "DataGrid" },
        { "ComboBox", "ComboBox" },
        { "CheckBox", "CheckBox" },
        { "RadioButton", "RadioButton" }
    };

    /// <summary>
    /// Gets property mappings (WPF property → Avalonia property).
    /// </summary>
    public Dictionary<string, PropertyMapping> PropertyMappings { get; } = new()
    {
        { "Visibility", new PropertyMapping("IsVisible", PropertyMappingType.NameAndValueChange) },
        { "HorizontalContentAlignment", new PropertyMapping("HorizontalContentAlignment", PropertyMappingType.NameOnly) },
        { "VerticalContentAlignment", new PropertyMapping("VerticalContentAlignment", PropertyMappingType.NameOnly) }
    };

    /// <summary>
    /// Tries to get the Avalonia namespace for a WPF namespace.
    /// </summary>
    public bool TryGetNamespaceMapping(string wpfNamespace, out string? avaloniaNamespace)
    {
        return NamespaceMappings.TryGetValue(wpfNamespace, out avaloniaNamespace);
    }

    /// <summary>
    /// Tries to get the Avalonia type for a WPF type.
    /// </summary>
    public bool TryGetTypeMapping(string wpfType, out string? avaloniaType)
    {
        return TypeMappings.TryGetValue(wpfType, out avaloniaType);
    }

    /// <summary>
    /// Tries to get the property mapping for a WPF property.
    /// </summary>
    public bool TryGetPropertyMapping(string wpfProperty, out PropertyMapping? mapping)
    {
        return PropertyMappings.TryGetValue(wpfProperty, out mapping);
    }
}

/// <summary>
/// Property mapping information.
/// </summary>
public class PropertyMapping
{
    /// <summary>
    /// Gets the Avalonia property name.
    /// </summary>
    public string AvaloniaPropertyName { get; }

    /// <summary>
    /// Gets the mapping type.
    /// </summary>
    public PropertyMappingType MappingType { get; }

    /// <summary>
    /// Gets or sets the value converter function.
    /// </summary>
    public Func<string, string>? ValueConverter { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyMapping"/> class.
    /// </summary>
    public PropertyMapping(string avaloniaPropertyName, PropertyMappingType mappingType)
    {
        AvaloniaPropertyName = avaloniaPropertyName;
        MappingType = mappingType;
    }
}

/// <summary>
/// Property mapping types.
/// </summary>
public enum PropertyMappingType
{
    /// <summary>
    /// Only the property name changes.
    /// </summary>
    NameOnly,

    /// <summary>
    /// Both name and value format change.
    /// </summary>
    NameAndValueChange,

    /// <summary>
    /// Property is removed (no Avalonia equivalent).
    /// </summary>
    Removed,

    /// <summary>
    /// Custom transformation required.
    /// </summary>
    Custom
}
