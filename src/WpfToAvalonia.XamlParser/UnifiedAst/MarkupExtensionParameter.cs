namespace WpfToAvalonia.XamlParser.UnifiedAst;

/// <summary>
/// Represents a strongly-typed markup extension parameter value.
/// This discriminated union replaces Dictionary&lt;string, object?&gt; with compile-time type safety.
/// </summary>
public sealed record MarkupExtensionParameter
{
    private MarkupExtensionParameter(object? content, ParameterValueKind kind)
    {
        Content = content;
        Kind = kind;
    }

    /// <summary>
    /// Gets the underlying parameter value.
    /// </summary>
    internal object? Content { get; }

    /// <summary>
    /// Gets the kind of parameter value.
    /// </summary>
    public ParameterValueKind Kind { get; }

    // === Factory Methods ===

    /// <summary>
    /// Creates a string parameter value.
    /// </summary>
    public static MarkupExtensionParameter FromString(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return new MarkupExtensionParameter(value, ParameterValueKind.String);
    }

    /// <summary>
    /// Creates a nested markup extension parameter value.
    /// </summary>
    public static MarkupExtensionParameter FromExtension(UnifiedXamlMarkupExtension extension)
    {
        if (extension == null)
            throw new ArgumentNullException(nameof(extension));

        return new MarkupExtensionParameter(extension, ParameterValueKind.NestedExtension);
    }

    /// <summary>
    /// Creates a relative source parameter value.
    /// </summary>
    public static MarkupExtensionParameter FromRelativeSource(RelativeSourceExpression expression)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));

        return new MarkupExtensionParameter(expression, ParameterValueKind.RelativeSource);
    }

    /// <summary>
    /// Creates a type reference parameter value.
    /// </summary>
    public static MarkupExtensionParameter FromType(QualifiedTypeName typeName)
    {
        if (typeName == null)
            throw new ArgumentNullException(nameof(typeName));

        return new MarkupExtensionParameter(typeName, ParameterValueKind.Type);
    }

    /// <summary>
    /// Creates a numeric parameter value.
    /// </summary>
    public static MarkupExtensionParameter FromNumber(double value)
    {
        return new MarkupExtensionParameter(value, ParameterValueKind.Number);
    }

    /// <summary>
    /// Creates a boolean parameter value.
    /// </summary>
    public static MarkupExtensionParameter FromBoolean(bool value)
    {
        return new MarkupExtensionParameter(value, ParameterValueKind.Boolean);
    }

    /// <summary>
    /// Creates a null parameter value.
    /// </summary>
    public static MarkupExtensionParameter Null()
    {
        return new MarkupExtensionParameter(null, ParameterValueKind.Null);
    }

    // === Type-Safe Accessors ===

    /// <summary>
    /// Gets the string value. Throws if not a string.
    /// </summary>
    public string AsString()
    {
        if (Kind != ParameterValueKind.String)
            throw new InvalidOperationException($"Parameter is not a string. Actual kind: {Kind}");

        return (string)Content!;
    }

    /// <summary>
    /// Gets the nested extension value. Throws if not an extension.
    /// </summary>
    public UnifiedXamlMarkupExtension AsExtension()
    {
        if (Kind != ParameterValueKind.NestedExtension)
            throw new InvalidOperationException($"Parameter is not a nested extension. Actual kind: {Kind}");

        return (UnifiedXamlMarkupExtension)Content!;
    }

    /// <summary>
    /// Gets the relative source value. Throws if not a relative source.
    /// </summary>
    public RelativeSourceExpression AsRelativeSource()
    {
        if (Kind != ParameterValueKind.RelativeSource)
            throw new InvalidOperationException($"Parameter is not a relative source. Actual kind: {Kind}");

        return (RelativeSourceExpression)Content!;
    }

    /// <summary>
    /// Gets the type reference value. Throws if not a type.
    /// </summary>
    public QualifiedTypeName AsType()
    {
        if (Kind != ParameterValueKind.Type)
            throw new InvalidOperationException($"Parameter is not a type reference. Actual kind: {Kind}");

        return (QualifiedTypeName)Content!;
    }

    /// <summary>
    /// Gets the numeric value. Throws if not a number.
    /// </summary>
    public double AsNumber()
    {
        if (Kind != ParameterValueKind.Number)
            throw new InvalidOperationException($"Parameter is not a number. Actual kind: {Kind}");

        return (double)Content!;
    }

    /// <summary>
    /// Gets the boolean value. Throws if not a boolean.
    /// </summary>
    public bool AsBoolean()
    {
        if (Kind != ParameterValueKind.Boolean)
            throw new InvalidOperationException($"Parameter is not a boolean. Actual kind: {Kind}");

        return (bool)Content!;
    }

    // === Try-Pattern Support ===

    /// <summary>
    /// Tries to get the string value.
    /// </summary>
    public bool TryGetString(out string value)
    {
        if (Kind == ParameterValueKind.String)
        {
            value = (string)Content!;
            return true;
        }

        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Tries to get the nested extension value.
    /// </summary>
    public bool TryGetExtension(out UnifiedXamlMarkupExtension extension)
    {
        if (Kind == ParameterValueKind.NestedExtension)
        {
            extension = (UnifiedXamlMarkupExtension)Content!;
            return true;
        }

        extension = null!;
        return false;
    }

    /// <summary>
    /// Tries to get the relative source value.
    /// </summary>
    public bool TryGetRelativeSource(out RelativeSourceExpression expression)
    {
        if (Kind == ParameterValueKind.RelativeSource)
        {
            expression = (RelativeSourceExpression)Content!;
            return true;
        }

        expression = null!;
        return false;
    }

    /// <summary>
    /// Tries to get the type reference value.
    /// </summary>
    public bool TryGetType(out QualifiedTypeName typeName)
    {
        if (Kind == ParameterValueKind.Type)
        {
            typeName = (QualifiedTypeName)Content!;
            return true;
        }

        typeName = null!;
        return false;
    }

    /// <summary>
    /// Tries to get the numeric value.
    /// </summary>
    public bool TryGetNumber(out double value)
    {
        if (Kind == ParameterValueKind.Number)
        {
            value = (double)Content!;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Tries to get the boolean value.
    /// </summary>
    public bool TryGetBoolean(out bool value)
    {
        if (Kind == ParameterValueKind.Boolean)
        {
            value = (bool)Content!;
            return true;
        }

        value = false;
        return false;
    }

    // === Pattern Matching ===

    /// <summary>
    /// Pattern matches on the parameter value kind and returns a result.
    /// </summary>
    public TResult Match<TResult>(
        Func<string, TResult> onString,
        Func<UnifiedXamlMarkupExtension, TResult> onExtension,
        Func<RelativeSourceExpression, TResult> onRelativeSource,
        Func<QualifiedTypeName, TResult> onType,
        Func<double, TResult> onNumber,
        Func<bool, TResult> onBoolean,
        Func<TResult> onNull)
    {
        return Kind switch
        {
            ParameterValueKind.String => onString((string)Content!),
            ParameterValueKind.NestedExtension => onExtension((UnifiedXamlMarkupExtension)Content!),
            ParameterValueKind.RelativeSource => onRelativeSource((RelativeSourceExpression)Content!),
            ParameterValueKind.Type => onType((QualifiedTypeName)Content!),
            ParameterValueKind.Number => onNumber((double)Content!),
            ParameterValueKind.Boolean => onBoolean((bool)Content!),
            ParameterValueKind.Null => onNull(),
            _ => throw new InvalidOperationException($"Unknown parameter kind: {Kind}")
        };
    }

    /// <summary>
    /// Pattern matches on the parameter value kind and executes an action.
    /// </summary>
    public void Switch(
        Action<string>? onString = null,
        Action<UnifiedXamlMarkupExtension>? onExtension = null,
        Action<RelativeSourceExpression>? onRelativeSource = null,
        Action<QualifiedTypeName>? onType = null,
        Action<double>? onNumber = null,
        Action<bool>? onBoolean = null,
        Action? onNull = null)
    {
        switch (Kind)
        {
            case ParameterValueKind.String:
                onString?.Invoke((string)Content!);
                break;
            case ParameterValueKind.NestedExtension:
                onExtension?.Invoke((UnifiedXamlMarkupExtension)Content!);
                break;
            case ParameterValueKind.RelativeSource:
                onRelativeSource?.Invoke((RelativeSourceExpression)Content!);
                break;
            case ParameterValueKind.Type:
                onType?.Invoke((QualifiedTypeName)Content!);
                break;
            case ParameterValueKind.Number:
                onNumber?.Invoke((double)Content!);
                break;
            case ParameterValueKind.Boolean:
                onBoolean?.Invoke((bool)Content!);
                break;
            case ParameterValueKind.Null:
                onNull?.Invoke();
                break;
        }
    }

    // === Helper Properties ===

    /// <summary>
    /// Gets a value indicating whether this parameter is null.
    /// </summary>
    public bool IsNull => Kind == ParameterValueKind.Null;

    /// <summary>
    /// Gets a value indicating whether this parameter is a string.
    /// </summary>
    public bool IsString => Kind == ParameterValueKind.String;

    /// <summary>
    /// Gets a value indicating whether this parameter is a nested extension.
    /// </summary>
    public bool IsExtension => Kind == ParameterValueKind.NestedExtension;

    /// <summary>
    /// Gets a value indicating whether this parameter is a relative source.
    /// </summary>
    public bool IsRelativeSource => Kind == ParameterValueKind.RelativeSource;

    /// <summary>
    /// Gets a value indicating whether this parameter is a type reference.
    /// </summary>
    public bool IsType => Kind == ParameterValueKind.Type;

    /// <summary>
    /// Gets a value indicating whether this parameter is a number.
    /// </summary>
    public bool IsNumber => Kind == ParameterValueKind.Number;

    /// <summary>
    /// Gets a value indicating whether this parameter is a boolean.
    /// </summary>
    public bool IsBoolean => Kind == ParameterValueKind.Boolean;

    /// <summary>
    /// Gets a string representation for debugging.
    /// </summary>
    public override string ToString()
    {
        return Kind switch
        {
            ParameterValueKind.String => $"String: \"{Content}\"",
            ParameterValueKind.NestedExtension => $"Extension: {Content}",
            ParameterValueKind.RelativeSource => $"RelativeSource: {Content}",
            ParameterValueKind.Type => $"Type: {Content}",
            ParameterValueKind.Number => $"Number: {Content}",
            ParameterValueKind.Boolean => $"Boolean: {Content}",
            ParameterValueKind.Null => "Null",
            _ => $"Unknown: {Content}"
        };
    }
}

/// <summary>
/// Defines the possible kinds of markup extension parameter values.
/// </summary>
public enum ParameterValueKind
{
    /// <summary>
    /// String literal value.
    /// </summary>
    String,

    /// <summary>
    /// Nested markup extension.
    /// </summary>
    NestedExtension,

    /// <summary>
    /// RelativeSource binding expression.
    /// </summary>
    RelativeSource,

    /// <summary>
    /// Type reference (x:Type).
    /// </summary>
    Type,

    /// <summary>
    /// Numeric value.
    /// </summary>
    Number,

    /// <summary>
    /// Boolean value.
    /// </summary>
    Boolean,

    /// <summary>
    /// Null value.
    /// </summary>
    Null
}
