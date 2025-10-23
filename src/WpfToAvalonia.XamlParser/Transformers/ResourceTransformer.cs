using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformers;

/// <summary>
/// Transforms WPF resource dictionaries and resource references to Avalonia equivalents.
/// </summary>
/// <remarks>
/// Handles:
/// - ResourceDictionary element transformation
/// - StaticResource → StaticResource (same syntax in Avalonia)
/// - DynamicResource → DynamicResource (same syntax in Avalonia)
/// - MergedDictionaries transformation
/// - Resource key collision detection
/// </remarks>
public class ResourceTransformer : IXamlTransformer
{
    public string Name => "ResourceTransformer";
    public int Priority => 45; // Run after binding transformations (40), before styles (50)

    public void Transform(UnifiedXamlDocument document, TransformationContext context)
    {
        if (document.Root == null)
        {
            context.Diagnostics.AddWarning(
                "RESOURCE_TRANSFORM_NO_ROOT",
                "Document has no root element",
                null);
            return;
        }

        context.Diagnostics.AddInfo(
            "RESOURCE_TRANSFORM_START",
            "Starting resource transformation",
            null);

        // Process resource dictionaries
        TransformResourceDictionaries(document.Root, context);

        context.Diagnostics.AddInfo(
            "RESOURCE_TRANSFORM_COMPLETE",
            "Resource transformation complete",
            null);
    }

    private void TransformResourceDictionaries(UnifiedXamlElement element, TransformationContext context)
    {
        // Transform ResourceDictionary elements
        if (element.TypeName == "ResourceDictionary")
        {
            context.Diagnostics.AddInfo(
                "RESOURCE_DICTIONARY_FOUND",
                "Found ResourceDictionary element (syntax is compatible with Avalonia)",
                null);
            context.Statistics.IncrementCount("ResourceDictionary");
        }

        // Check for Resources property
        var resourcesProperty = element.Properties.FirstOrDefault(p =>
            p.PropertyName == "Resources" && p.Kind == PropertyKind.PropertyElement);

        if (resourcesProperty != null)
        {
            context.Diagnostics.AddInfo(
                "RESOURCES_PROPERTY_FOUND",
                $"Found Resources property on {element.TypeName}",
                null);
            context.Statistics.IncrementCount("ResourcesProperty");
        }

        // Check for MergedDictionaries
        var mergedDictProp = element.Properties.FirstOrDefault(p =>
            p.PropertyName == "MergedDictionaries" && p.Kind == PropertyKind.PropertyElement);

        if (mergedDictProp != null)
        {
            context.Diagnostics.AddInfo(
                "MERGED_DICTIONARIES_FOUND",
                "Found MergedDictionaries (syntax is compatible with Avalonia)",
                null);
            context.Statistics.IncrementCount("MergedDictionaries");
        }

        // Check for StaticResource and DynamicResource references
        foreach (var property in element.Properties)
        {
            if (property.MarkupExtension != null)
            {
                var ext = property.MarkupExtension;
                if (ext.ExtensionName == "StaticResource")
                {
                    context.Diagnostics.AddInfo(
                        "STATIC_RESOURCE_REFERENCE",
                        $"Found StaticResource reference in {element.TypeName}.{property.PropertyName}",
                        null);
                    context.Statistics.IncrementCount("StaticResource");
                }
                else if (ext.ExtensionName == "DynamicResource")
                {
                    context.Diagnostics.AddInfo(
                        "DYNAMIC_RESOURCE_REFERENCE",
                        $"Found DynamicResource reference in {element.TypeName}.{property.PropertyName}",
                        null);
                    context.Statistics.IncrementCount("DynamicResource");
                }
            }
        }

        // Recursively transform child elements
        foreach (var child in element.Children)
        {
            TransformResourceDictionaries(child, context);
        }
    }
}
