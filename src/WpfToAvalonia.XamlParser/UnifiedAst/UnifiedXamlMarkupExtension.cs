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
    [Obsolete("Use TypedParameters for type-safe access. This property will be removed in v2.0.")]
    public Dictionary<string, object?> Parameters { get; } = new();

    /// <summary>
    /// Gets the strongly-typed parameters/properties of the markup extension.
    /// This is the preferred way to access parameters with compile-time type safety.
    /// </summary>
    public Dictionary<string, MarkupExtensionParameter> TypedParameters { get; } = new();

    /// <summary>
    /// Gets or sets the positional argument (for extensions with a default property).
    /// Example: {StaticResource MyBrush} has positional argument "MyBrush"
    /// </summary>
    [Obsolete("Use TypedPositionalArgument for type-safe access. This property will be removed in v2.0.")]
    public object? PositionalArgument { get; set; }

    /// <summary>
    /// Gets or sets the strongly-typed positional argument.
    /// This is the preferred way to access the positional argument with compile-time type safety.
    /// </summary>
    public MarkupExtensionParameter? TypedPositionalArgument { get; set; }

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
    [Obsolete("Use TypedParameters dictionary directly for type-safe access. This method will be removed in v2.0.")]
    public T? GetParameter<T>(string name, T? defaultValue = default)
    {
        #pragma warning disable CS0618
        if (Parameters.TryGetValue(name, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        #pragma warning restore CS0618
        return defaultValue;
    }

    /// <summary>
    /// Sets a parameter value.
    /// </summary>
    [Obsolete("Use TypedParameters dictionary directly for type-safe access. This method will be removed in v2.0.")]
    public void SetParameter(string name, object? value)
    {
        #pragma warning disable CS0618
        Parameters[name] = value;
        #pragma warning restore CS0618
    }

    /// <summary>
    /// Gets the RelativeSource parameter if this is a binding with RelativeSource.
    /// </summary>
    public RelativeSourceExpression? GetRelativeSource()
    {
        if (TypedParameters.TryGetValue("RelativeSource", out var param) &&
            param.Kind == ParameterValueKind.RelativeSource)
        {
            return param.AsRelativeSource();
        }
        return null;
    }

    /// <summary>
    /// Gets the Path parameter as a string if available.
    /// </summary>
    public string? GetPath()
    {
        if (TypedParameters.TryGetValue("Path", out var param) &&
            param.Kind == ParameterValueKind.String)
        {
            return param.AsString();
        }
        return null;
    }

    /// <summary>
    /// Gets the Mode parameter as a string if available.
    /// </summary>
    public string? GetMode()
    {
        if (TypedParameters.TryGetValue("Mode", out var param) &&
            param.Kind == ParameterValueKind.String)
        {
            return param.AsString();
        }
        return null;
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
/// Represents a relative source binding with structured type information.
/// Replaces string-based AncestorType with QualifiedTypeName for type safety.
/// </summary>
public sealed record RelativeSourceExpression
{
    /// <summary>
    /// Gets the relative source mode.
    /// </summary>
    public RelativeSourceMode Mode { get; init; } = RelativeSourceMode.Self;

    /// <summary>
    /// Gets the ancestor type (for FindAncestor mode).
    /// </summary>
    public QualifiedTypeName? AncestorType { get; init; }

    /// <summary>
    /// Gets the ancestor level (for FindAncestor mode).
    /// </summary>
    public int AncestorLevel { get; init; } = 1;

    /// <summary>
    /// Parses a RelativeSource string into a structured expression.
    /// This replaces regex-based parsing with proper structured parsing.
    /// </summary>
    public static RelativeSourceExpression Parse(string value, IDictionary<string, string>? namespacePrefixes = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("RelativeSource value cannot be empty", nameof(value));

        // Simple parsing - can be enhanced as needed
        var mode = RelativeSourceMode.Self;
        QualifiedTypeName? ancestorType = null;
        var ancestorLevel = 1;

        // Parse mode
        if (value.Contains("FindAncestor", StringComparison.OrdinalIgnoreCase))
        {
            mode = RelativeSourceMode.FindAncestor;
        }
        else if (value.Contains("TemplatedParent", StringComparison.OrdinalIgnoreCase))
        {
            mode = RelativeSourceMode.TemplatedParent;
        }
        else if (value.Contains("PreviousData", StringComparison.OrdinalIgnoreCase))
        {
            mode = RelativeSourceMode.PreviousData;
        }

        // Parse AncestorType if FindAncestor mode
        if (mode == RelativeSourceMode.FindAncestor)
        {
            // Look for AncestorType=TypeName or AncestorType={x:Type TypeName}
            var ancestorTypeMatch = System.Text.RegularExpressions.Regex.Match(
                value,
                @"AncestorType\s*=\s*(?:\{x:Type\s+)?([a-zA-Z0-9_:]+)");

            if (ancestorTypeMatch.Success)
            {
                var typeName = ancestorTypeMatch.Groups[1].Value;
                ancestorType = QualifiedTypeName.Parse(typeName, namespacePrefixes);
            }

            // Parse AncestorLevel
            var levelMatch = System.Text.RegularExpressions.Regex.Match(
                value,
                @"AncestorLevel\s*=\s*(\d+)");

            if (levelMatch.Success && int.TryParse(levelMatch.Groups[1].Value, out var level))
            {
                ancestorLevel = level;
            }
        }

        return new RelativeSourceExpression
        {
            Mode = mode,
            AncestorType = ancestorType,
            AncestorLevel = ancestorLevel
        };
    }

    /// <summary>
    /// Tries to parse a RelativeSource string.
    /// </summary>
    public static bool TryParse(string value, out RelativeSourceExpression result, IDictionary<string, string>? namespacePrefixes = null)
    {
        try
        {
            result = Parse(value, namespacePrefixes);
            return true;
        }
        catch
        {
            result = null!;
            return false;
        }
    }
}

/// <summary>
/// Defines the RelativeSource binding mode.
/// </summary>
public enum RelativeSourceMode
{
    /// <summary>
    /// Binds to the element itself.
    /// </summary>
    Self,

    /// <summary>
    /// Binds to an ancestor of a specific type.
    /// </summary>
    FindAncestor,

    /// <summary>
    /// Binds to the previous data item.
    /// </summary>
    PreviousData,

    /// <summary>
    /// Binds to the templated parent.
    /// </summary>
    TemplatedParent
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
