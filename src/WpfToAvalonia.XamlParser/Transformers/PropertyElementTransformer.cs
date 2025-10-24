using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformers;

/// <summary>
/// Transforms WPF property element syntax to Avalonia property element syntax.
/// </summary>
/// <remarks>
/// Property element syntax (e.g., &lt;Button.Content&gt;...&lt;/Button.Content&gt;) mostly works the same in Avalonia.
/// This transformer validates and transforms property elements where needed, particularly for:
/// - Collection properties (Items, Children, Resources)
/// - Complex property values (Brushes, Transforms, Templates)
/// - Attached property elements
/// - Content properties
/// </remarks>
public class PropertyElementTransformer : IXamlTransformer
{
    public string Name => "PropertyElementTransformer";
    public int Priority => 36; // Run after attached properties, before events

    private static readonly Dictionary<string, string> PropertyElementMappings = new()
    {
        // Most property elements are the same, but some have differences

        // Style and Template related
        { "Style.Triggers", "Style.Triggers" }, // Note: Triggers work differently in Avalonia
        { "ControlTemplate.Triggers", "ControlTemplate.Triggers" }, // Note: Triggers work differently
        { "DataTemplate.Triggers", "DataTemplate.Triggers" }, // Note: Not supported in Avalonia

        // Resources
        { "FrameworkElement.Resources", "Control.Resources" }, // Different base class
        { "Window.Resources", "Window.Resources" }, // Same
        { "UserControl.Resources", "UserControl.Resources" }, // Same
        { "Application.Resources", "Application.Resources" }, // Same

        // Content properties
        { "ContentControl.Content", "ContentControl.Content" }, // Same
        { "HeaderedContentControl.Header", "HeaderedContentControl.Header" }, // Same

        // Collection properties
        { "ItemsControl.Items", "ItemsControl.Items" }, // Same
        { "Panel.Children", "Panel.Children" }, // Same
        { "Grid.RowDefinitions", "Grid.RowDefinitions" }, // Same
        { "Grid.ColumnDefinitions", "Grid.ColumnDefinitions" }, // Same

        // Brushes and drawing
        { "Shape.Fill", "Shape.Fill" }, // Same but note gradient brush differences
        { "Shape.Stroke", "Shape.Stroke" }, // Same
        { "Control.Background", "Control.Background" }, // Same
        { "Control.Foreground", "Control.Foreground" }, // Same
        { "Border.Background", "Border.Background" }, // Same

        // Transforms
        { "UIElement.RenderTransform", "Visual.RenderTransform" }, // Different base class
        { "UIElement.LayoutTransform", "Control.LayoutTransform" }, // Different support in Avalonia

        // Effects
        { "UIElement.Effect", "Visual.Effect" }, // Limited effect support in Avalonia
    };

    private static readonly HashSet<string> UnsupportedPropertyElements = new()
    {
        "DataTemplate.Triggers", // Not supported in Avalonia
        "FrameworkElement.Triggers", // Not supported in Avalonia (use Styles)
        "UIElement.CommandBindings", // Different command handling
        "UIElement.InputBindings", // Different input handling
    };

    private static readonly Dictionary<string, string> PropertyElementNotes = new()
    {
        { "Style.Triggers", "Style triggers work differently in Avalonia. Use Style selectors and setters instead." },
        { "ControlTemplate.Triggers", "Template triggers not supported. Use ControlTheme with styles." },
        { "DataTemplate.Triggers", "DataTemplate triggers not supported. Use data binding or styles." },
        { "UIElement.LayoutTransform", "LayoutTransform has limited support. Consider using RenderTransform." },
        { "UIElement.Effect", "Effects have limited support. Only OpacityMask and some basic effects." },
        { "Grid.RowDefinitions", "Same syntax, but Avalonia uses different sizing algorithm." },
        { "Grid.ColumnDefinitions", "Same syntax, but Avalonia uses different sizing algorithm." },
    };

    public void Transform(UnifiedXamlDocument document, TransformationContext context)
    {
        if (document.Root == null)
        {
            context.Diagnostics.AddWarning(
                "PROPERTY_ELEMENT_TRANSFORM_NO_ROOT",
                "Document has no root element",
                null);
            return;
        }

        context.Diagnostics.AddInfo(
            "PROPERTY_ELEMENT_TRANSFORM_START",
            "Starting property element transformation",
            null);

        // Transform all elements
        TransformElementPropertyElements(document.Root, context);

        foreach (var descendant in document.Root.Descendants())
        {
            TransformElementPropertyElements(descendant, context);
        }

        context.Diagnostics.AddInfo(
            "PROPERTY_ELEMENT_TRANSFORM_COMPLETE",
            $"Property element transformation complete",
            null);
    }

    private void TransformElementPropertyElements(UnifiedXamlElement element, TransformationContext context)
    {
        // Find all property elements (properties with Kind = PropertyElement)
        var propertyElements = element.Properties
            .Where(p => p.Kind == PropertyKind.PropertyElement)
            .ToList();

        foreach (var propertyElement in propertyElements)
        {
            TransformPropertyElement(propertyElement, element, context);
        }
    }

    private void TransformPropertyElement(UnifiedXamlProperty propertyElement, UnifiedXamlElement element, TransformationContext context)
    {
        var fullPropertyName = propertyElement.PropertyName;

        // For attached properties, the full name includes the owner type
        var qualifiedName = propertyElement.AttachedOwnerType != null
            ? $"{propertyElement.AttachedOwnerType}.{fullPropertyName}"
            : $"{element.TypeName}.{fullPropertyName}";

        // Check if this property element is unsupported
        if (UnsupportedPropertyElements.Contains(qualifiedName))
        {
            context.Diagnostics.AddWarning(
                "PROPERTY_ELEMENT_UNSUPPORTED",
                $"Property element '{qualifiedName}' is not supported in Avalonia. Consider removing or finding an alternative.",
                null);
            context.Statistics.WarningsGenerated++;
            propertyElement.SetMetadata("Unsupported", $"No Avalonia support for {qualifiedName}");
            return;
        }

        // Check if we have a mapping for this property element
        if (PropertyElementMappings.TryGetValue(qualifiedName, out var avaloniaPropertyName))
        {
            if (qualifiedName != avaloniaPropertyName)
            {
                // Property element name changed
                var newPropertyName = avaloniaPropertyName.Split('.').Last();
                var newOwnerType = avaloniaPropertyName.Contains('.')
                    ? avaloniaPropertyName.Substring(0, avaloniaPropertyName.LastIndexOf('.'))
                    : element.TypeName;

                context.Diagnostics.AddInfo(
                    "PROPERTY_ELEMENT_MAPPED",
                    $"Transforming property element: {qualifiedName} → {avaloniaPropertyName}",
                    null);

                propertyElement.PropertyName = newPropertyName;
                if (propertyElement.AttachedOwnerType != null)
                {
                    propertyElement.AttachedOwnerType = newOwnerType;
                }

                context.Statistics.PropertiesTransformed++;
            }
            else
            {
                // Property element is compatible
                context.Diagnostics.AddInfo(
                    "PROPERTY_ELEMENT_COMPATIBLE",
                    $"Property element '{qualifiedName}' is compatible with Avalonia",
                    null);
            }

            // Add notes if applicable
            if (PropertyElementNotes.TryGetValue(qualifiedName, out var note))
            {
                context.Diagnostics.AddWarning(
                    "PROPERTY_ELEMENT_NOTE",
                    $"{qualifiedName}: {note}",
                    null);
            }
        }
        else
        {
            // Unknown property element - might be custom or from user code
            context.Diagnostics.AddInfo(
                "PROPERTY_ELEMENT_UNKNOWN",
                $"Property element '{qualifiedName}' is not in the known list. Verify compatibility with Avalonia.",
                null);
        }

        // Validate property element content
        ValidatePropertyElementContent(propertyElement, element, context);

        context.Statistics.IncrementCount($"PropertyElement:{qualifiedName}");
    }

    private void ValidatePropertyElementContent(UnifiedXamlProperty propertyElement, UnifiedXamlElement element, TransformationContext context)
    {
        // Check if the property element contains complex content
        if (propertyElement.Value is UnifiedXamlElement childElement)
        {
            var childTypeName = childElement.TypeName;

            // Special validation for certain content types
            if (childTypeName == "Trigger" || childTypeName == "DataTrigger" || childTypeName == "EventTrigger")
            {
                context.Diagnostics.AddWarning(
                    "PROPERTY_ELEMENT_TRIGGER",
                    $"Trigger found in {element.TypeName}.{propertyElement.PropertyName}. Triggers are not supported in Avalonia. Use styles or bindings instead.",
                    null);
                context.Statistics.WarningsGenerated++;
            }
            else if (childTypeName == "Storyboard" || childTypeName == "BeginStoryboard")
            {
                context.Diagnostics.AddWarning(
                    "PROPERTY_ELEMENT_STORYBOARD",
                    $"Storyboard found in {element.TypeName}.{propertyElement.PropertyName}. Use Avalonia's animation system (Transitions, Keyframe animations) instead.",
                    null);
                context.Statistics.WarningsGenerated++;
            }
            else if (childTypeName.EndsWith("Effect"))
            {
                context.Diagnostics.AddWarning(
                    "PROPERTY_ELEMENT_EFFECT",
                    $"Effect '{childTypeName}' found in {element.TypeName}.{propertyElement.PropertyName}. Avalonia has limited effect support. Only basic effects are available.",
                    null);
            }
        }
    }
}
