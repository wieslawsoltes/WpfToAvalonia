using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformers;

/// <summary>
/// Transforms WPF properties to Avalonia properties.
/// </summary>
/// <remarks>
/// Handles property name changes and value conversions:
/// - Visibility → IsVisible (with value conversion: Collapsed/Hidden → False, Visible → True)
/// - HorizontalContentAlignment/VerticalContentAlignment → same name (Avalonia uses same names)
/// </remarks>
public class PropertyTransformer : IPropertyTransformer
{
    public string Name => "PropertyTransformer";
    public int Priority => 30; // Run after type transformation

    public void Transform(UnifiedXamlDocument document, TransformationContext context)
    {
        if (document.Root == null)
        {
            context.Diagnostics.AddWarning(
                "PROPERTY_TRANSFORM_NO_ROOT",
                "Document has no root element",
                null);
            return;
        }

        context.Diagnostics.AddInfo(
            "PROPERTY_TRANSFORM_START",
            "Starting property transformation",
            null);

        // Transform all elements
        TransformElementProperties(document.Root, context);

        foreach (var descendant in document.Root.Descendants())
        {
            TransformElementProperties(descendant, context);
        }

        context.Diagnostics.AddInfo(
            "PROPERTY_TRANSFORM_COMPLETE",
            $"Property transformation complete: {context.Statistics.PropertiesTransformed} properties transformed",
            null);
    }

    private void TransformElementProperties(UnifiedXamlElement element, TransformationContext context)
    {
        // Transform each property
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
        // Check if we have a mapping for this property
        return context.MappingProvider.TryGetPropertyMapping(property.PropertyName, out _);
    }

    public void TransformProperty(UnifiedXamlProperty property, UnifiedXamlElement element, TransformationContext context)
    {
        var originalPropertyName = property.PropertyName;

        if (!context.MappingProvider.TryGetPropertyMapping(originalPropertyName, out var mapping) || mapping == null)
        {
            return;
        }

        context.Diagnostics.AddInfo(
            "PROPERTY_MAPPED",
            $"Transforming property: {originalPropertyName} → {mapping.AvaloniaPropertyName} on {element.TypeName}",
            null);

        // Transform based on mapping type
        switch (mapping.MappingType)
        {
            case PropertyMappingType.NameOnly:
                TransformNameOnly(property, mapping, context);
                break;

            case PropertyMappingType.NameAndValueChange:
                TransformNameAndValue(property, mapping, element, context);
                break;

            case PropertyMappingType.Removed:
                TransformRemoved(property, element, context);
                break;

            case PropertyMappingType.Custom:
                TransformCustom(property, mapping, element, context);
                break;
        }

        context.Statistics.PropertiesTransformed++;
        context.Statistics.IncrementCount($"Property:{originalPropertyName}");
    }

    private void TransformNameOnly(UnifiedXamlProperty property, PropertyMapping mapping, TransformationContext context)
    {
        property.PropertyName = mapping.AvaloniaPropertyName;
    }

    private void TransformNameAndValue(UnifiedXamlProperty property, PropertyMapping mapping, UnifiedXamlElement element, TransformationContext context)
    {
        var originalName = property.PropertyName;
        var originalValue = property.Value;

        // Change property name
        property.PropertyName = mapping.AvaloniaPropertyName;

        // Transform value based on property
        if (originalName == "Visibility")
        {
            TransformVisibilityValue(property, context);
        }
        else if (mapping.ValueConverter != null && originalValue is string strValue)
        {
            // Use custom value converter if provided
            property.Value = mapping.ValueConverter(strValue);
        }

        context.Diagnostics.AddInfo(
            "PROPERTY_VALUE_TRANSFORMED",
            $"Transformed {originalName}=\"{originalValue}\" → {property.PropertyName}=\"{property.Value}\" on {element.TypeName}",
            null);
    }

    private void TransformVisibilityValue(UnifiedXamlProperty property, TransformationContext context)
    {
        if (property.Value == null)
            return;

        var originalValue = property.Value;
        var newValue = originalValue switch
        {
            "Visible" => "True",
            "Collapsed" => "False",
            "Hidden" => "False", // Both Hidden and Collapsed map to False
            _ => originalValue // Keep as-is if it's a binding or unknown value
        };

        property.Value = newValue;

        if (originalValue is string str && string.Equals(str, "Hidden", StringComparison.Ordinal))
        {
            context.Diagnostics.AddWarning(
                "VISIBILITY_HIDDEN_TO_FALSE",
                "WPF Visibility.Hidden mapped to IsVisible=False. Note: Avalonia doesn't distinguish between Hidden and Collapsed.",
                null);
            context.Statistics.WarningsGenerated++;
        }
    }

    private void TransformRemoved(UnifiedXamlProperty property, UnifiedXamlElement element, TransformationContext context)
    {
        context.Diagnostics.AddWarning(
            "PROPERTY_REMOVED",
            $"Property '{property.PropertyName}' removed from {element.TypeName} (no Avalonia equivalent)",
            null);

        // Mark for removal (actual removal happens during serialization)
        property.State = TransformationState.Transformed;
        property.SetMetadata("Removed", $"No Avalonia equivalent for {property.PropertyName}");

        context.Statistics.WarningsGenerated++;
    }

    private void TransformCustom(UnifiedXamlProperty property, PropertyMapping mapping, UnifiedXamlElement element, TransformationContext context)
    {
        context.Diagnostics.AddWarning(
            "PROPERTY_CUSTOM_TRANSFORM_NEEDED",
            $"Property '{property.PropertyName}' on {element.TypeName} requires custom transformation logic",
            null);

        // Custom transformations would be implemented here based on specific needs
        context.Statistics.WarningsGenerated++;
    }
}
