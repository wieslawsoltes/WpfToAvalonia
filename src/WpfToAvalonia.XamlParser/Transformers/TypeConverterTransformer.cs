using WpfToAvalonia.XamlParser.UnifiedAst;
using System.Text.RegularExpressions;

namespace WpfToAvalonia.XamlParser.Transformers;

/// <summary>
/// Transforms WPF type converter syntax to Avalonia type converter syntax.
/// </summary>
/// <remarks>
/// Handles differences in type converters between WPF and Avalonia:
/// - Color values: Same syntax (named colors, #RGB, #RRGGBB, #AARRGGBB)
/// - Brush values: Mostly same, but some brush types differ
/// - Thickness values: Same (uniform or "left,top,right,bottom")
/// - GridLength values: Different (Auto, *, 1.5*, 100 in WPF vs Auto, *, 1.5*, 100 in Avalonia)
/// - Geometry values: PathGeometry syntax differences
/// - Duration values: Different format
/// - FontWeight values: Different (numeric in WPF vs named in Avalonia)
/// </remarks>
public class TypeConverterTransformer : IPropertyTransformer
{
    public string Name => "TypeConverterTransformer";
    public int Priority => 42; // Run after property transformations

    private static readonly Dictionary<string, string> ColorNameMappings = new()
    {
        // Most color names are the same, but verify edge cases
        // Avalonia supports standard web color names
    };

    private static readonly Dictionary<string, string> FontWeightMappings = new()
    {
        // WPF numeric values to Avalonia named values
        { "100", "Thin" },
        { "200", "ExtraLight" },
        { "300", "Light" },
        { "400", "Normal" },
        { "500", "Medium" },
        { "600", "SemiBold" },
        { "700", "Bold" },
        { "800", "ExtraBold" },
        { "900", "Black" },
        { "950", "ExtraBlack" },
    };

    public void Transform(UnifiedXamlDocument document, TransformationContext context)
    {
        if (document.Root == null)
        {
            context.Diagnostics.AddWarning(
                "TYPE_CONVERTER_TRANSFORM_NO_ROOT",
                "Document has no root element",
                null);
            return;
        }

        context.Diagnostics.AddInfo(
            "TYPE_CONVERTER_TRANSFORM_START",
            "Starting type converter transformation",
            null);

        // Transform all elements
        TransformElementTypeConverters(document.Root, context);

        foreach (var descendant in document.Root.Descendants())
        {
            TransformElementTypeConverters(descendant, context);
        }

        context.Diagnostics.AddInfo(
            "TYPE_CONVERTER_TRANSFORM_COMPLETE",
            $"Type converter transformation complete",
            null);
    }

    private void TransformElementTypeConverters(UnifiedXamlElement element, TransformationContext context)
    {
        foreach (var property in element.Properties)
        {
            if (ShouldTransform(property, element, context))
            {
                TransformProperty(property, element, context);
            }
        }
    }

    public bool ShouldTransform(UnifiedXamlProperty property, UnifiedXamlElement element, TransformationContext context)
    {
        // Only transform properties that likely use type converters
        var propertyName = property.PropertyName;

        return propertyName == "FontWeight" ||
               propertyName.Contains("Color") ||
               propertyName.Contains("Brush") ||
               propertyName == "Background" ||
               propertyName == "Foreground" ||
               propertyName == "Fill" ||
               propertyName == "Stroke" ||
               propertyName == "Duration" ||
               propertyName.Contains("Geometry") ||
               (property.Value is string strValue && !string.IsNullOrWhiteSpace(strValue));
    }

    public void TransformProperty(UnifiedXamlProperty property, UnifiedXamlElement element, TransformationContext context)
    {
        if (property.Value is not string stringValue || string.IsNullOrWhiteSpace(stringValue))
        {
            return;
        }

        var propertyName = property.PropertyName;
        var originalValue = stringValue;
        var transformedValue = stringValue;
        bool wasTransformed = false;

        // Transform based on property name/type
        if (propertyName == "FontWeight")
        {
            transformedValue = TransformFontWeight(stringValue, out wasTransformed);
        }
        else if (propertyName == "Duration")
        {
            transformedValue = TransformDuration(stringValue, out wasTransformed);
        }
        else if (propertyName.Contains("Color") ||
                 propertyName.Contains("Brush") ||
                 propertyName == "Background" ||
                 propertyName == "Foreground" ||
                 propertyName == "Fill" ||
                 propertyName == "Stroke")
        {
            transformedValue = TransformColorOrBrush(stringValue, out wasTransformed);
        }
        else if (propertyName.Contains("Geometry") || propertyName == "Data")
        {
            ValidateGeometry(stringValue, element, context);
        }

        if (wasTransformed && transformedValue != originalValue)
        {
            property.Value = transformedValue;
            context.Diagnostics.AddInfo(
                "TYPE_CONVERTER_TRANSFORMED",
                $"Transformed {element.TypeName}.{propertyName}: '{originalValue}' → '{transformedValue}'",
                null);
            context.Statistics.PropertiesTransformed++;
        }

        context.Statistics.IncrementCount($"TypeConverter:{propertyName}");
    }

    private string TransformFontWeight(string value, out bool wasTransformed)
    {
        wasTransformed = false;
        var trimmed = value.Trim();

        // Check if it's a numeric value that needs conversion
        if (FontWeightMappings.TryGetValue(trimmed, out var namedValue))
        {
            wasTransformed = true;
            return namedValue;
        }

        // Already a named value or not recognized
        return value;
    }

    private string TransformDuration(string value, out bool wasTransformed)
    {
        wasTransformed = false;
        var trimmed = value.Trim();

        // WPF Duration format: "0:0:2" (hours:minutes:seconds) or "Automatic", "Forever"
        // Avalonia Duration format: Similar but might have differences

        if (trimmed == "Automatic" || trimmed == "Forever")
        {
            // These are the same
            return value;
        }

        // TimeSpan format is mostly compatible
        // Just validate it looks correct
        if (Regex.IsMatch(trimmed, @"^\d+:\d+:\d+(\.\d+)?$"))
        {
            return value;
        }

        return value;
    }

    private string TransformColorOrBrush(string value, out bool wasTransformed)
    {
        wasTransformed = false;
        var trimmed = value.Trim();

        // Color formats that are the same:
        // - Named colors: "Red", "Blue", etc.
        // - Hex colors: "#FF0000", "#FFFF0000"
        // - RGB: Not typically used in XAML strings

        // Check for hex color format
        if (trimmed.StartsWith("#"))
        {
            // Validate hex color format
            if (Regex.IsMatch(trimmed, @"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$"))
            {
                // Valid hex color - same in both WPF and Avalonia
                return value;
            }
        }

        // Check for named colors (mostly compatible)
        // Avalonia supports standard web color names
        if (Regex.IsMatch(trimmed, @"^[A-Za-z]+$"))
        {
            // Likely a named color - mostly compatible
            return value;
        }

        // Check for special brush keywords
        if (trimmed == "Transparent")
        {
            return value; // Same in both
        }

        return value;
    }

    private void ValidateGeometry(string value, UnifiedXamlElement element, TransformationContext context)
    {
        // PathGeometry syntax is mostly the same, but some edge cases differ
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        // Check for complex path geometry
        if (value.Contains("M") || value.Contains("L") || value.Contains("C") || value.Contains("Z"))
        {
            context.Diagnostics.AddInfo(
                "GEOMETRY_VALIDATION",
                $"Path geometry found in {element.TypeName}.Data. Verify that complex path syntax is compatible with Avalonia.",
                null);
        }

        // Check for arc geometry (known to have differences)
        if (value.Contains("A"))
        {
            context.Diagnostics.AddWarning(
                "GEOMETRY_ARC_WARNING",
                $"Arc geometry found in {element.TypeName}.Data. Arc syntax may differ between WPF and Avalonia. Verify rendering.",
                null);
        }
    }
}
