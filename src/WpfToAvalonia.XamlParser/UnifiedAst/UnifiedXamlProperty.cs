using System.Xml.Linq;
using WpfToAvalonia.XamlParser.TypeSystem;

namespace WpfToAvalonia.XamlParser.UnifiedAst;

/// <summary>
/// Represents a XAML property or attribute in the unified AST.
/// Examples: Text="Hello", Visibility="Visible", &lt;Button.Content&gt;...&lt;/Button.Content&gt;
/// </summary>
public sealed class UnifiedXamlProperty : UnifiedXamlNode
{
    // === XML Layer ===

    /// <summary>
    /// Gets or sets the underlying XAttribute (for attribute syntax).
    /// </summary>
    public XAttribute? XmlAttribute { get; set; }

    /// <summary>
    /// Gets or sets the underlying XElement (for property element syntax).
    /// </summary>
    public XElement? XmlPropertyElement { get; set; }

    // === XamlX Semantic Layer ===

    /// <summary>
    /// Gets or sets the XamlX property assignment node.
    /// </summary>
    public object? SemanticProperty { get; set; }

    /// <summary>
    /// Gets or sets the resolved property information from XamlX.
    /// </summary>
    public IXamlProperty? PropertyInfo { get; set; }

    /// <summary>
    /// Gets or sets the resolved type of this property.
    /// Example: System.String, System.Boolean, System.Windows.Visibility
    /// </summary>
    public IXamlType? PropertyType { get; set; }

    // === Unified Structure ===

    /// <summary>
    /// Gets or sets the property name.
    /// Example: "Text", "Content", "Visibility"
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property value.
    /// Can be a string, UnifiedXamlElement, UnifiedXamlMarkupExtension, or other value.
    /// </summary>
    [Obsolete("Use ValueTyped for type-safe access. This property will be removed in v2.0.")]
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the strongly-typed property value.
    /// This is the preferred way to access property values with compile-time type safety.
    /// </summary>
    public PropertyValue? ValueTyped { get; set; }

    /// <summary>
    /// Gets or sets the kind of property.
    /// </summary>
    public PropertyKind Kind { get; set; }

    // === Attached Properties ===

    /// <summary>
    /// Gets or sets the owner type for attached properties.
    /// Example: For Grid.Row, this would be "Grid"
    /// </summary>
    public string? AttachedOwnerType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is an attached property.
    /// </summary>
    public bool IsAttached => !string.IsNullOrEmpty(AttachedOwnerType);

    // === Markup Extensions ===

    /// <summary>
    /// Gets or sets the markup extension if this property uses one.
    /// Example: {Binding Path}, {StaticResource MyBrush}
    /// </summary>
    public UnifiedXamlMarkupExtension? MarkupExtension { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this property uses a markup extension.
    /// </summary>
    public bool HasMarkupExtension => MarkupExtension != null;

    // === Type Conversion ===

    /// <summary>
    /// Gets or sets the type converter used for this property's value.
    /// </summary>
    public string? TypeConverterName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether type conversion is required.
    /// </summary>
    public bool RequiresTypeConversion { get; set; }

    // === Helper Methods ===

    /// <summary>
    /// Gets the string value representation.
    /// </summary>
    public string? GetStringValue()
    {
        if (Value is string str)
        {
            return str;
        }

        if (XmlAttribute != null)
        {
            return XmlAttribute.Value;
        }

        return Value?.ToString();
    }

    /// <summary>
    /// Gets the full property name including attached owner.
    /// Example: "Grid.Row" for attached property, "Text" for regular property.
    /// </summary>
    public string GetFullPropertyName()
    {
        if (IsAttached)
        {
            return $"{AttachedOwnerType}.{PropertyName}";
        }
        return PropertyName;
    }

    /// <summary>
    /// Parses an attached property name.
    /// Example: "Grid.Row" → AttachedOwnerType="Grid", PropertyName="Row"
    /// </summary>
    public static (string? ownerType, string propertyName) ParseAttachedProperty(string fullName)
    {
        var dotIndex = fullName.IndexOf('.');
        if (dotIndex > 0 && dotIndex < fullName.Length - 1)
        {
            return (fullName.Substring(0, dotIndex), fullName.Substring(dotIndex + 1));
        }
        return (null, fullName);
    }

    public override string ToString()
    {
        var result = GetFullPropertyName();
        if (Value != null)
        {
            result += $"=\"{Value}\"";
        }
        return result;
    }
}

/// <summary>
/// Defines the kind of XAML property.
/// </summary>
public enum PropertyKind
{
    /// <summary>
    /// Attribute syntax: &lt;Button Text="Hello" /&gt;
    /// </summary>
    Attribute,

    /// <summary>
    /// Property element syntax: &lt;Button&gt;&lt;Button.Content&gt;...&lt;/Button.Content&gt;&lt;/Button&gt;
    /// </summary>
    PropertyElement,

    /// <summary>
    /// Attached property: Grid.Row="0"
    /// </summary>
    AttachedProperty,

    /// <summary>
    /// Content property (implicit): &lt;Button&gt;Click Me&lt;/Button&gt; sets Content property
    /// </summary>
    ContentProperty
}

// IXamlProperty interface is now defined in TypeSystem/IXamlTypeSystem.cs
