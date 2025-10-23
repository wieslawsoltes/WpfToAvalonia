using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformers;

/// <summary>
/// Transforms WPF control types to Avalonia control types.
/// </summary>
/// <remarks>
/// Most controls map 1:1, but some need special handling:
/// - ListView → ListBox (Avalonia doesn't have ListView)
/// - Some properties may need adjustment after type change
/// </remarks>
public class TypeTransformer : IElementTransformer
{
    public string Name => "TypeTransformer";
    public int Priority => 20; // Run after namespace transformation

    public void Transform(UnifiedXamlDocument document, TransformationContext context)
    {
        if (document.Root == null)
        {
            context.Diagnostics.AddWarning(
                "TYPE_TRANSFORM_NO_ROOT",
                "Document has no root element",
                null);
            return;
        }

        context.Diagnostics.AddInfo(
            "TYPE_TRANSFORM_START",
            "Starting type transformation",
            null);

        // Transform root and all descendants
        TransformElementTree(document.Root, context);

        context.Diagnostics.AddInfo(
            "TYPE_TRANSFORM_COMPLETE",
            $"Type transformation complete: {context.Statistics.ElementsTransformed} elements transformed",
            null);
    }

    private void TransformElementTree(UnifiedXamlElement element, TransformationContext context)
    {
        // Transform this element if needed
        if (ShouldTransform(element, context))
        {
            TransformElement(element, context);
        }

        // Transform children
        foreach (var child in element.Children)
        {
            TransformElementTree(child, context);
        }
    }

    public bool ShouldTransform(UnifiedXamlElement element, TransformationContext context)
    {
        // Check if we have a mapping for this type
        return context.MappingProvider.TryGetTypeMapping(element.TypeName, out _);
    }

    public void TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        var originalType = element.TypeName;

        if (!context.MappingProvider.TryGetTypeMapping(originalType, out var avaloniaType))
        {
            return;
        }

        // Check if type actually changes
        if (originalType == avaloniaType)
        {
            // Type maps 1:1, no transformation needed
            return;
        }

        context.Diagnostics.AddInfo(
            "TYPE_MAPPED",
            $"Mapping type: {originalType} → {avaloniaType}",
            null);

        element.TypeName = avaloniaType!;
        context.Statistics.ElementsTransformed++;
        context.Statistics.IncrementCount($"Type:{originalType}→{avaloniaType}");

        // Add warning for types that need special attention
        if (originalType == "ListView")
        {
            context.Diagnostics.AddWarning(
                "TYPE_LISTVIEW_TO_LISTBOX",
                $"ListView converted to ListBox - review View configuration (GridView not supported in Avalonia)",
                null);
            context.Statistics.WarningsGenerated++;
        }

        // Mark element as transformed
        element.State = TransformationState.Transformed;
        element.SetMetadata("TypeTransformation", $"{originalType} → {avaloniaType}");
    }
}
