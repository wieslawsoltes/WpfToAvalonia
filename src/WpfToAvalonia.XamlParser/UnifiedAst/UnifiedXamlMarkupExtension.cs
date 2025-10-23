using WpfToAvalonia.XamlParser.TypeSystem;

namespace WpfToAvalonia.XamlParser.UnifiedAst;

/// <summary>
/// Represents a XAML markup extension in the unified AST.
/// Examples: {Binding Path}, {StaticResource MyBrush}, {x:Type Button}
/// </summary>
public sealed class UnifiedXamlMarkupExtension : UnifiedXamlNode
{
    // === XamlX Semantic Layer ===
    // Markup extensions are primarily handled by the semantic layer

    /// <summary>
    /// Gets or sets the XamlX semantic extension node.
    /// </summary>
    public object? SemanticExtension { get; set; }

    /// <summary>
    /// Gets or sets the XamlX semantic node (alias for SemanticExtension).
    /// Used by semantic enrichment.
    /// </summary>
    public new object? SemanticNode
    {
        get => SemanticExtension;
        set => SemanticExtension = value;
    }

    // === Unified Structure ===

    /// <summary>
    /// Gets or sets the extension name without the "Extension" suffix.
    /// Examples: "Binding", "StaticResource", "DynamicResource", "x:Type"
    /// </summary>
    public string ExtensionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the parameters/properties of the markup extension.
    /// Key = parameter name, Value = parameter value
    /// </summary>
    public Dictionary<string, object?> Parameters { get; } = new();

    /// <summary>
    /// Gets or sets the positional argument (for extensions with a default property).
    /// Example: {StaticResource MyBrush} has positional argument "MyBrush"
    /// </summary>
    public object? PositionalArgument { get; set; }

    // === Specific Extension Types ===

    /// <summary>
    /// Gets or sets binding-specific information (if this is a Binding extension).
    /// </summary>
    public BindingExpression? Binding { get; set; }

    /// <summary>
    /// Gets or sets resource reference information (if this is a StaticResource/DynamicResource).
    /// </summary>
    public ResourceReference? Resource { get; set; }

    /// <summary>
    /// Gets or sets type reference information (if this is an x:Type extension).
    /// </summary>
    public TypeReference? Type { get; set; }

    /// <summary>
    /// Gets or sets static member reference (if this is an x:Static extension).
    /// </summary>
    public StaticReference? Static { get; set; }

    // === Helper Methods ===

    /// <summary>
    /// Gets a parameter value by name.
    /// </summary>
    public T? GetParameter<T>(string name, T? defaultValue = default)
    {
        if (Parameters.TryGetValue(name, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// Sets a parameter value.
    /// </summary>
    public void SetParameter(string name, object? value)
    {
        Parameters[name] = value;
    }

    /// <summary>
    /// Determines the extension type.
    /// </summary>
    public MarkupExtensionType GetExtensionType()
    {
        return ExtensionName switch
        {
            "Binding" => MarkupExtensionType.Binding,
            "StaticResource" => MarkupExtensionType.StaticResource,
            "DynamicResource" => MarkupExtensionType.DynamicResource,
            "TemplateBinding" => MarkupExtensionType.TemplateBinding,
            "RelativeSource" => MarkupExtensionType.RelativeSource,
            "x:Type" or "Type" => MarkupExtensionType.Type,
            "x:Static" or "Static" => MarkupExtensionType.Static,
            "x:Null" or "Null" => MarkupExtensionType.Null,
            _ => MarkupExtensionType.Custom
        };
    }

    public override string ToString()
    {
        if (PositionalArgument != null)
        {
            return $"{{{ExtensionName} {PositionalArgument}}}";
        }

        if (Parameters.Count > 0)
        {
            var paramStr = string.Join(", ", Parameters.Select(p => $"{p.Key}={p.Value}"));
            return $"{{{ExtensionName} {paramStr}}}";
        }

        return $"{{{ExtensionName}}}";
    }
}

/// <summary>
/// Represents a binding expression.
/// </summary>
public sealed class BindingExpression
{
    /// <summary>
    /// Gets or sets the binding path.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the binding mode.
    /// </summary>
    public string? Mode { get; set; }

    /// <summary>
    /// Gets or sets the update source trigger.
    /// </summary>
    public string? UpdateSourceTrigger { get; set; }

    /// <summary>
    /// Gets or sets the converter.
    /// </summary>
    public string? Converter { get; set; }

    /// <summary>
    /// Gets or sets the converter parameter.
    /// </summary>
    public object? ConverterParameter { get; set; }

    /// <summary>
    /// Gets or sets the string format.
    /// </summary>
    public string? StringFormat { get; set; }

    /// <summary>
    /// Gets or sets the element name for ElementName bindings.
    /// </summary>
    public string? ElementName { get; set; }

    /// <summary>
    /// Gets or sets the relative source.
    /// </summary>
    public RelativeSourceExpression? RelativeSource { get; set; }

    /// <summary>
    /// Gets or sets the source object.
    /// </summary>
    public object? Source { get; set; }
}

/// <summary>
/// Represents a relative source binding.
/// </summary>
public sealed class RelativeSourceExpression
{
    /// <summary>
    /// Gets or sets the relative source mode.
    /// </summary>
    public string? Mode { get; set; }

    /// <summary>
    /// Gets or sets the ancestor type.
    /// </summary>
    public string? AncestorType { get; set; }

    /// <summary>
    /// Gets or sets the ancestor level.
    /// </summary>
    public int? AncestorLevel { get; set; }
}

/// <summary>
/// Represents a resource reference.
/// </summary>
public sealed class ResourceReference
{
    /// <summary>
    /// Gets or sets the resource key.
    /// </summary>
    public string? ResourceKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a dynamic resource.
    /// </summary>
    public bool IsDynamic { get; set; }

    /// <summary>
    /// Gets or sets the resolved resource value (if available).
    /// </summary>
    public object? ResolvedResource { get; set; }
}

/// <summary>
/// Represents a type reference (x:Type).
/// </summary>
public sealed class TypeReference
{
    /// <summary>
    /// Gets or sets the type name.
    /// </summary>
    public string? TypeName { get; set; }

    /// <summary>
    /// Gets or sets the resolved type.
    /// </summary>
    public IXamlType? ResolvedType { get; set; }
}

/// <summary>
/// Represents a static member reference (x:Static).
/// </summary>
public sealed class StaticReference
{
    /// <summary>
    /// Gets or sets the member name.
    /// </summary>
    public string? MemberName { get; set; }

    /// <summary>
    /// Gets or sets the owner type.
    /// </summary>
    public string? OwnerType { get; set; }

    /// <summary>
    /// Gets or sets the resolved value.
    /// </summary>
    public object? ResolvedValue { get; set; }
}

/// <summary>
/// Defines the type of markup extension.
/// </summary>
public enum MarkupExtensionType
{
    Binding,
    StaticResource,
    DynamicResource,
    TemplateBinding,
    RelativeSource,
    Type,
    Static,
    Null,
    Custom
}
