namespace WpfToAvalonia.XamlParser.UnifiedAst;

/// <summary>
/// Represents a strongly-typed property value with compile-time safety.
/// This discriminated union replaces the unsafe object? Value pattern.
/// </summary>
public sealed record PropertyValue
{
    private PropertyValue(object? content, PropertyValueKind kind)
    {
        Content = content;
        Kind = kind;
    }

    /// <summary>
    /// Gets the underlying content object.
    /// </summary>
    public object? Content { get; }

    /// <summary>
    /// Gets the kind of value stored.
    /// </summary>
    public PropertyValueKind Kind { get; }

    // Factory methods for type safety

    /// <summary>
    /// Creates a PropertyValue from a string.
    /// </summary>
    public static PropertyValue FromString(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        return new PropertyValue(value, PropertyValueKind.String);
    }

    /// <summary>
    /// Creates a PropertyValue from a nested element.
    /// </summary>
    public static PropertyValue FromElement(UnifiedXamlElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        return new PropertyValue(element, PropertyValueKind.Element);
    }

    /// <summary>
    /// Creates a PropertyValue from a markup extension.
    /// </summary>
    public static PropertyValue FromMarkupExtension(UnifiedXamlMarkupExtension extension)
    {
        if (extension == null)
            throw new ArgumentNullException(nameof(extension));
        return new PropertyValue(extension, PropertyValueKind.MarkupExtension);
    }

    /// <summary>
    /// Creates a null PropertyValue.
    /// </summary>
    public static PropertyValue Null()
        => new PropertyValue(null, PropertyValueKind.Null);

    // Type-safe accessors with helpful error messages

    /// <summary>
    /// Gets the value as a string.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the value is not a string.</exception>
    public string AsString()
    {
        if (Kind != PropertyValueKind.String)
            throw new InvalidOperationException($"Cannot access value as String. Actual kind: {Kind}");
        return (string)Content!;
    }

    /// <summary>
    /// Gets the value as a UnifiedXamlElement.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the value is not an element.</exception>
    public UnifiedXamlElement AsElement()
    {
        if (Kind != PropertyValueKind.Element)
            throw new InvalidOperationException($"Cannot access value as Element. Actual kind: {Kind}");
        return (UnifiedXamlElement)Content!;
    }

    /// <summary>
    /// Gets the value as a UnifiedXamlMarkupExtension.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the value is not a markup extension.</exception>
    public UnifiedXamlMarkupExtension AsMarkupExtension()
    {
        if (Kind != PropertyValueKind.MarkupExtension)
            throw new InvalidOperationException($"Cannot access value as MarkupExtension. Actual kind: {Kind}");
        return (UnifiedXamlMarkupExtension)Content!;
    }

    /// <summary>
    /// Checks if this value is null.
    /// </summary>
    public bool IsNull => Kind == PropertyValueKind.Null;

    /// <summary>
    /// Checks if this value is a string.
    /// </summary>
    public bool IsString => Kind == PropertyValueKind.String;

    /// <summary>
    /// Checks if this value is an element.
    /// </summary>
    public bool IsElement => Kind == PropertyValueKind.Element;

    /// <summary>
    /// Checks if this value is a markup extension.
    /// </summary>
    public bool IsMarkupExtension => Kind == PropertyValueKind.MarkupExtension;

    // Pattern matching support

    /// <summary>
    /// Pattern matches on the value kind and returns a result.
    /// </summary>
    public TResult Match<TResult>(
        Func<string, TResult> onString,
        Func<UnifiedXamlElement, TResult> onElement,
        Func<UnifiedXamlMarkupExtension, TResult> onMarkupExtension,
        Func<TResult> onNull)
    {
        if (onString == null) throw new ArgumentNullException(nameof(onString));
        if (onElement == null) throw new ArgumentNullException(nameof(onElement));
        if (onMarkupExtension == null) throw new ArgumentNullException(nameof(onMarkupExtension));
        if (onNull == null) throw new ArgumentNullException(nameof(onNull));

        return Kind switch
        {
            PropertyValueKind.String => onString((string)Content!),
            PropertyValueKind.Element => onElement((UnifiedXamlElement)Content!),
            PropertyValueKind.MarkupExtension => onMarkupExtension((UnifiedXamlMarkupExtension)Content!),
            PropertyValueKind.Null => onNull(),
            _ => throw new InvalidOperationException($"Unknown value kind: {Kind}")
        };
    }

    /// <summary>
    /// Pattern matches on the value kind and executes an action.
    /// </summary>
    public void Switch(
        Action<string>? onString = null,
        Action<UnifiedXamlElement>? onElement = null,
        Action<UnifiedXamlMarkupExtension>? onMarkupExtension = null,
        Action? onNull = null)
    {
        switch (Kind)
        {
            case PropertyValueKind.String:
                onString?.Invoke((string)Content!);
                break;
            case PropertyValueKind.Element:
                onElement?.Invoke((UnifiedXamlElement)Content!);
                break;
            case PropertyValueKind.MarkupExtension:
                onMarkupExtension?.Invoke((UnifiedXamlMarkupExtension)Content!);
                break;
            case PropertyValueKind.Null:
                onNull?.Invoke();
                break;
            default:
                throw new InvalidOperationException($"Unknown value kind: {Kind}");
        }
    }

    /// <summary>
    /// Tries to get the value as a string.
    /// </summary>
    public bool TryGetString(out string value)
    {
        if (Kind == PropertyValueKind.String)
        {
            value = (string)Content!;
            return true;
        }
        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Tries to get the value as an element.
    /// </summary>
    public bool TryGetElement(out UnifiedXamlElement element)
    {
        if (Kind == PropertyValueKind.Element)
        {
            element = (UnifiedXamlElement)Content!;
            return true;
        }
        element = null!;
        return false;
    }

    /// <summary>
    /// Tries to get the value as a markup extension.
    /// </summary>
    public bool TryGetMarkupExtension(out UnifiedXamlMarkupExtension extension)
    {
        if (Kind == PropertyValueKind.MarkupExtension)
        {
            extension = (UnifiedXamlMarkupExtension)Content!;
            return true;
        }
        extension = null!;
        return false;
    }

    /// <summary>
    /// Gets a string representation of the value for debugging.
    /// </summary>
    public override string ToString()
    {
        return Kind switch
        {
            PropertyValueKind.String => $"String: \"{Content}\"",
            PropertyValueKind.Element => $"Element: {((UnifiedXamlElement)Content!).TypeName}",
            PropertyValueKind.MarkupExtension => $"MarkupExtension: {((UnifiedXamlMarkupExtension)Content!).ExtensionName}",
            PropertyValueKind.Null => "Null",
            _ => $"Unknown: {Kind}"
        };
    }
}

/// <summary>
/// Defines the kind of value stored in a PropertyValue.
/// </summary>
public enum PropertyValueKind
{
    /// <summary>
    /// The value is a string literal.
    /// </summary>
    String,

    /// <summary>
    /// The value is a nested XAML element.
    /// </summary>
    Element,

    /// <summary>
    /// The value is a markup extension (Binding, StaticResource, etc.).
    /// </summary>
    MarkupExtension,

    /// <summary>
    /// The value is null or not set.
    /// </summary>
    Null
}
