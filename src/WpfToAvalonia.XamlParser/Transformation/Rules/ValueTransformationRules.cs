using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation.Rules;

/// <summary>
/// Transforms color specifications from WPF to Avalonia format.
/// WPF and Avalonia both support standard color formats, but some edge cases differ.
/// </summary>
public sealed class ColorValueTransformationRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformColorValues";

    public override int Priority => 10;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        // Apply to properties that are likely colors
        var colorPropertyNames = new[]
        {
            "Foreground", "Background", "BorderBrush", "Fill", "Stroke",
            "Color", "Background", "OpacityMask"
        };

        return colorPropertyNames.Contains(property.PropertyName) &&
               !property.HasMarkupExtension &&
               property.GetStringValue() != null;
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        var value = property.GetStringValue();
        if (value == null) return property;

        // WPF and Avalonia both support:
        // - Named colors: "Red", "Blue", etc.
        // - Hex colors: "#FF0000", "#FFFF0000"
        // - rgb() and rgba() functions

        // Most color values work as-is, but we normalize format
        // Remove any WPF-specific prefixes if they exist
        var normalizedValue = value.Trim();

        // Check for SystemColors which may need transformation
        if (normalizedValue.StartsWith("SystemColors."))
        {
            // SystemColors may have different names in Avalonia
            var colorName = normalizedValue.Substring("SystemColors.".Length);
            // For now, just remove the prefix and use the color name
            property.Value = colorName;

            context.RecordTransformation(
                Name,
                "Property",
                $"Transformed SystemColor: {value} → {colorName}");
        }

        return property;
    }
}

/// <summary>
/// Transforms Thickness, Margin, Padding values from WPF to Avalonia.
/// Both use the same format, but this rule ensures consistency.
/// </summary>
public sealed class ThicknessValueTransformationRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformThicknessValues";

    public override int Priority => 10;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        var thicknessProperties = new[]
        {
            "Margin", "Padding", "BorderThickness"
        };

        return thicknessProperties.Contains(property.PropertyName) &&
               !property.HasMarkupExtension &&
               property.GetStringValue() != null;
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        var value = property.GetStringValue();
        if (value == null) return property;

        // Thickness format is the same in WPF and Avalonia:
        // - "5" (uniform)
        // - "5,10" (horizontal, vertical)
        // - "5,10,5,10" (left, top, right, bottom)

        // Just validate and normalize whitespace
        var normalized = value.Trim().Replace(" ", "");

        if (normalized != value)
        {
            property.Value = normalized;
            context.RecordTransformation(
                Name,
                "Property",
                $"Normalized thickness value: '{value}' → '{normalized}'");
        }

        return property;
    }
}

/// <summary>
/// Transforms StaticResource and DynamicResource references.
/// </summary>
public sealed class ResourceReferenceTransformationRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformResourceReferences";

    public override int Priority => 50; // Higher priority to run before other transformations

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        if (!property.HasMarkupExtension) return false;

        var extensionName = property.MarkupExtension?.ExtensionName;
        return extensionName == "StaticResource" || extensionName == "DynamicResource";
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        if (property.MarkupExtension == null) return property;

        var extensionName = property.MarkupExtension.ExtensionName;

        // In Avalonia, StaticResource and DynamicResource work the same way as WPF
        // But we may need to update resource keys if they reference WPF-specific resources
        var resourceKeyObj = property.MarkupExtension.PositionalArgument;

        if (resourceKeyObj is string resourceKey)
        {
            // Check for WPF system resource keys that need transformation
            var transformedKey = TransformSystemResourceKey(resourceKey);

            if (!string.Equals(transformedKey, resourceKey, StringComparison.Ordinal))
            {
                property.MarkupExtension.PositionalArgument = transformedKey;

                context.RecordTransformation(
                    Name,
                    "MarkupExtension",
                    $"Transformed resource key: {{{extensionName} {resourceKey}}} → {{{extensionName} {transformedKey}}}");
            }
        }

        return property;
    }

    private string TransformSystemResourceKey(string resourceKey)
    {
        // WPF system resource keys to Avalonia equivalents
        var systemResourceMappings = new Dictionary<string, string>
        {
            // Brushes
            { "SystemColors.ControlBrush", "SystemControlForegroundBaseHighBrush" },
            { "SystemColors.WindowBrush", "SystemControlPageBackgroundChromeLowBrush" },

            // Fonts
            { "SystemFonts.MessageFontFamily", "SystemFontFamily" },
            { "SystemFonts.MessageFontSize", "SystemFontSize" },

            // Common theme resources
            { "AccentColorBrush", "SystemAccentColor" }
        };

        return systemResourceMappings.TryGetValue(resourceKey, out var avaloniaKey)
            ? avaloniaKey
            : resourceKey;
    }
}

/// <summary>
/// Transforms GridLength values (Auto, *, specific values).
/// WPF and Avalonia use the same format.
/// </summary>
public sealed class GridLengthValueTransformationRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformGridLengthValues";

    public override int Priority => 10;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        // Apply to Width/Height on grid definitions
        // Note: Without parent context, we apply to all Width/Height properties
        return (property.PropertyName == "Width" || property.PropertyName == "Height") &&
               !property.HasMarkupExtension &&
               property.GetStringValue() != null;
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        var value = property.GetStringValue();
        if (value == null) return property;

        // GridLength values are the same in WPF and Avalonia:
        // - "Auto"
        // - "*" or "2*" (star sizing)
        // - "100" (absolute pixels)

        // Just validate format
        var normalized = value.Trim();

        if (normalized != value)
        {
            property.Value = normalized;
            context.RecordTransformation(
                Name,
                "Property",
                $"Normalized GridLength value: '{value}' → '{normalized}'");
        }

        return property;
    }
}

/// <summary>
/// Transforms Geometry/Path data strings.
/// WPF and Avalonia use the same path syntax.
/// </summary>
public sealed class GeometryValueTransformationRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformGeometryValues";

    public override int Priority => 10;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        return property.PropertyName == "Data" &&
               !property.HasMarkupExtension &&
               property.GetStringValue() != null;
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        // Geometry markup is the same in WPF and Avalonia
        // No transformation needed, just validate
        return property;
    }
}

/// <summary>
/// Transforms Duration values for animations.
/// </summary>
public sealed class DurationValueTransformationRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformDurationValues";

    public override int Priority => 10;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        return property.PropertyName == "Duration" &&
               !property.HasMarkupExtension &&
               property.GetStringValue() != null;
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        var value = property.GetStringValue();
        if (value == null) return property;

        // Duration format in WPF: "0:0:2" (hours:minutes:seconds)
        // Avalonia uses TimeSpan format which is similar
        // Both support:
        // - "0:0:2" (2 seconds)
        // - "0:0:0.5" (0.5 seconds)

        // Values are compatible, no transformation needed
        return property;
    }
}

/// <summary>
/// Transforms CornerRadius values.
/// </summary>
public sealed class CornerRadiusValueTransformationRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformCornerRadiusValues";

    public override int Priority => 10;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        return property.PropertyName == "CornerRadius" &&
               !property.HasMarkupExtension &&
               property.GetStringValue() != null;
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        var value = property.GetStringValue();
        if (value == null) return property;

        // CornerRadius format is the same in WPF and Avalonia:
        // - "5" (uniform)
        // - "5,10,15,20" (topLeft, topRight, bottomRight, bottomLeft)

        // Just validate and normalize
        var normalized = value.Trim().Replace(" ", "");

        if (normalized != value)
        {
            property.Value = normalized;
            context.RecordTransformation(
                Name,
                "Property",
                $"Normalized CornerRadius value: '{value}' → '{normalized}'");
        }

        return property;
    }
}
