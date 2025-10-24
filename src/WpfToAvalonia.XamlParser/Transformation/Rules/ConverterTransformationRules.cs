using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation.Rules;

/// <summary>
/// Base class for converter transformation rules.
/// Handles value converter references in XAML bindings.
/// Implements part of task 2.5.7.1.3: Transform value converters (XAML aspect)
/// </summary>
public abstract class ConverterTransformationRuleBase : PropertyTransformationRuleBase
{
    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        return property.HasMarkupExtension &&
               property.MarkupExtension?.Parameters.ContainsKey("Converter") == true;
    }
}

/// <summary>
/// Transforms converter references in XAML bindings.
/// Updates StaticResource references to converters with correct namespace handling.
/// </summary>
public sealed class ConverterReferenceTransformationRule : ConverterTransformationRuleBase
{
    public override string Name => "TransformConverterReference";

    public override int Priority => 75;

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        if (property.MarkupExtension == null) return property;

        var binding = property.MarkupExtension;

        if (binding.Parameters.TryGetValue("Converter", out var converterValue))
        {
            var converterStr = converterValue?.ToString() ?? "";

            // Check for built-in WPF converters that have Avalonia equivalents
            if (converterStr.Contains("BooleanToVisibilityConverter"))
            {
                context.RecordTransformation(
                    Name,
                    "Converter",
                    "BooleanToVisibilityConverter detected. " +
                    "Avalonia equivalent: BoolToVisibilityConverter (but behavior differs). " +
                    "Consider using IsVisible property directly instead of Visibility enum conversion.");

                context.RecordTransformation(
                    Name,
                    "ConverterVisibilityBehavior",
                    "WPF's BooleanToVisibilityConverter returns Visibility enum (Visible/Collapsed/Hidden). " +
                    "Avalonia uses IsVisible (bool) property. " +
                    "Consider binding to IsVisible directly: IsVisible=\"{Binding IsActive}\"");
            }

            // Check for StaticResource references
            if (converterStr.Contains("StaticResource") || converterStr.Contains("DynamicResource"))
            {
                context.RecordTransformation(
                    Name,
                    "Converter",
                    $"Converter uses resource reference: {converterStr}. " +
                    "Ensure converter class is updated for Avalonia (System.Windows.Data → Avalonia.Data.Converters).");
            }

            // Warn about converters needing update
            context.RecordTransformation(
                Name,
                "ConverterRequiresUpdate",
                $"Converter detected: {converterStr}. " +
                "Ensure converter implementation is migrated to Avalonia.Data.Converters namespace. " +
                "Update IValueConverter interface and handle nullable parameters.");
        }

        // Check for ConverterParameter
        if (binding.Parameters.ContainsKey("ConverterParameter"))
        {
            context.RecordTransformation(
                Name,
                "ConverterParameter",
                "ConverterParameter is supported in Avalonia - no change needed.");
        }

        // Check for ConverterCulture
        if (binding.Parameters.ContainsKey("ConverterCulture"))
        {
            context.RecordTransformation(
                Name,
                "ConverterCulture",
                "ConverterCulture parameter is available in Avalonia.");
        }

        return property;
    }
}

/// <summary>
/// Transforms MultiBinding converter references.
/// Handles IMultiValueConverter usage in XAML.
/// </summary>
public sealed class MultiConverterTransformationRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformMultiConverter";

    public override int Priority => 74;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        return property.HasMarkupExtension &&
               property.MarkupExtension?.ExtensionName == "MultiBinding" &&
               property.MarkupExtension?.Parameters.ContainsKey("Converter") == true;
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        if (property.MarkupExtension == null) return property;

        var multiBinding = property.MarkupExtension;

        if (multiBinding.Parameters.TryGetValue("Converter", out var converterValue))
        {
            context.RecordTransformation(
                Name,
                "MultiValueConverter",
                $"IMultiValueConverter detected: {converterValue}. " +
                "Supported in Avalonia 11+. " +
                "Update converter: System.Windows.Data.IMultiValueConverter → Avalonia.Data.Converters.IMultiValueConverter. " +
                "Update method signature: object?[] values instead of object[] values.");
        }

        return property;
    }
}

/// <summary>
/// Detects and warns about FallbackValue usage with converters.
/// </summary>
public sealed class ConverterFallbackValueRule : ConverterTransformationRuleBase
{
    public override string Name => "CheckConverterFallbackValue";

    public override int Priority => 73;

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        if (property.MarkupExtension == null) return property;

        var binding = property.MarkupExtension;

        if (binding.Parameters.ContainsKey("Converter") &&
            binding.Parameters.ContainsKey("FallbackValue"))
        {
            var fallbackValue = binding.Parameters["FallbackValue"];

            context.RecordTransformation(
                Name,
                "FallbackValue",
                $"FallbackValue with converter detected: {fallbackValue}. " +
                "FallbackValue is supported in Avalonia and works the same way.");
        }

        return property;
    }
}
