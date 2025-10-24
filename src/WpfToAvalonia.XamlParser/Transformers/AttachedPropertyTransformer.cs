using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformers;

/// <summary>
/// Transforms WPF attached properties to Avalonia attached properties.
/// </summary>
/// <remarks>
/// Most attached properties work the same way in Avalonia, but some need transformation:
/// - Grid.Row, Grid.Column, Grid.RowSpan, Grid.ColumnSpan (same)
/// - Canvas.Left, Canvas.Top, Canvas.Right, Canvas.Bottom (same)
/// - DockPanel.Dock (same)
/// - ScrollViewer attached properties (most are same, some differences)
/// - Panel.ZIndex → same but note: Avalonia uses different layering semantics
/// </remarks>
public class AttachedPropertyTransformer : IPropertyTransformer
{
    public string Name => "AttachedPropertyTransformer";
    public int Priority => 35; // Run after type transformation, before property transformation

    private static readonly Dictionary<string, string> AttachedPropertyMappings = new()
    {
        // Most attached properties have same syntax, but we track known ones for validation
        // Grid attached properties (same in Avalonia)
        { "Grid.Row", "Grid.Row" },
        { "Grid.Column", "Grid.Column" },
        { "Grid.RowSpan", "Grid.RowSpan" },
        { "Grid.ColumnSpan", "Grid.ColumnSpan" },
        { "Grid.IsSharedSizeScope", "Grid.IsSharedSizeScope" },

        // Canvas attached properties (same in Avalonia)
        { "Canvas.Left", "Canvas.Left" },
        { "Canvas.Top", "Canvas.Top" },
        { "Canvas.Right", "Canvas.Right" },
        { "Canvas.Bottom", "Canvas.Bottom" },

        // DockPanel attached properties (same in Avalonia)
        { "DockPanel.Dock", "DockPanel.Dock" },

        // Panel attached properties
        { "Panel.ZIndex", "Panel.ZIndex" }, // Same but different semantics

        // ScrollViewer attached properties (mostly same)
        { "ScrollViewer.HorizontalScrollBarVisibility", "ScrollViewer.HorizontalScrollBarVisibility" },
        { "ScrollViewer.VerticalScrollBarVisibility", "ScrollViewer.VerticalScrollBarVisibility" },
        { "ScrollViewer.CanContentScroll", "ScrollViewer.CanContentScroll" },

        // ToolTipService attached properties
        { "ToolTipService.ShowDuration", "ToolTip.ShowDelay" }, // Different in Avalonia
        { "ToolTipService.InitialShowDelay", "ToolTip.ShowDelay" },

        // KeyboardNavigation attached properties (different in Avalonia)
        { "KeyboardNavigation.TabNavigation", "KeyboardNavigation.TabNavigation" },
        { "KeyboardNavigation.DirectionalNavigation", "KeyboardNavigation.DirectionalNavigation" },
    };

    private static readonly HashSet<string> UnsupportedAttachedProperties = new()
    {
        "TextOptions.TextFormattingMode", // No direct equivalent
        "TextOptions.TextRenderingMode", // No direct equivalent
        "RenderOptions.BitmapScalingMode", // Use RenderOptions in Avalonia
        "VirtualizingStackPanel.IsVirtualizing", // Avalonia uses different virtualization
        "VirtualizingStackPanel.VirtualizationMode", // Different approach in Avalonia
    };

    public void Transform(UnifiedXamlDocument document, TransformationContext context)
    {
        if (document.Root == null)
        {
            context.Diagnostics.AddWarning(
                "ATTACHED_PROPERTY_TRANSFORM_NO_ROOT",
                "Document has no root element",
                null);
            return;
        }

        context.Diagnostics.AddInfo(
            "ATTACHED_PROPERTY_TRANSFORM_START",
            "Starting attached property transformation",
            null);

        // Transform all elements
        TransformElementAttachedProperties(document.Root, context);

        foreach (var descendant in document.Root.Descendants())
        {
            TransformElementAttachedProperties(descendant, context);
        }

        context.Diagnostics.AddInfo(
            "ATTACHED_PROPERTY_TRANSFORM_COMPLETE",
            $"Attached property transformation complete",
            null);
    }

    private void TransformElementAttachedProperties(UnifiedXamlElement element, TransformationContext context)
    {
        // Find all attached properties (properties with a dot in the name)
        var attachedProperties = element.Properties
            .Where(p => p.PropertyName.Contains('.'))
            .ToList();

        foreach (var property in attachedProperties)
        {
            if (ShouldTransform(property, element, context))
            {
                TransformProperty(property, element, context);
            }
        }
    }

    public bool ShouldTransform(UnifiedXamlProperty property, UnifiedXamlElement element, TransformationContext context)
    {
        // Only transform if it's an attached property (has a dot)
        return property.PropertyName.Contains('.');
    }

    public void TransformProperty(UnifiedXamlProperty property, UnifiedXamlElement element, TransformationContext context)
    {
        var propertyName = property.PropertyName;

        // Check if it's an unsupported attached property
        if (UnsupportedAttachedProperties.Contains(propertyName))
        {
            context.Diagnostics.AddWarning(
                "ATTACHED_PROPERTY_UNSUPPORTED",
                $"Attached property '{propertyName}' is not supported in Avalonia. Consider removing or finding an alternative.",
                null);
            context.Statistics.WarningsGenerated++;
            property.SetMetadata("Unsupported", $"No Avalonia equivalent for {propertyName}");
            return;
        }

        // Check if we have a mapping for this attached property
        if (AttachedPropertyMappings.TryGetValue(propertyName, out var avaloniaPropertyName))
        {
            if (propertyName != avaloniaPropertyName)
            {
                // Property name changed
                context.Diagnostics.AddInfo(
                    "ATTACHED_PROPERTY_MAPPED",
                    $"Transforming attached property: {propertyName} → {avaloniaPropertyName} on {element.TypeName}",
                    null);
                property.PropertyName = avaloniaPropertyName;
                context.Statistics.PropertiesTransformed++;
            }
            else
            {
                // Property is compatible (same name)
                context.Diagnostics.AddInfo(
                    "ATTACHED_PROPERTY_COMPATIBLE",
                    $"Attached property '{propertyName}' is compatible with Avalonia on {element.TypeName}",
                    null);
            }

            // Add special warnings for properties with different semantics
            if (propertyName == "Panel.ZIndex")
            {
                context.Diagnostics.AddWarning(
                    "PANEL_ZINDEX_SEMANTICS",
                    "Panel.ZIndex works differently in Avalonia. In WPF, higher values render on top. In Avalonia, ZIndex affects rendering order within the same parent only.",
                    null);
            }
        }
        else if (IsCustomAttachedProperty(propertyName))
        {
            // Custom attached property from user code or third-party library
            context.Diagnostics.AddWarning(
                "ATTACHED_PROPERTY_CUSTOM",
                $"Custom attached property '{propertyName}' detected on {element.TypeName}. Ensure the attached property is defined in your Avalonia code.",
                null);
        }
        else
        {
            // Unknown attached property - might be from WPF-specific control
            context.Diagnostics.AddWarning(
                "ATTACHED_PROPERTY_UNKNOWN",
                $"Unknown attached property '{propertyName}' on {element.TypeName}. Verify if this has an Avalonia equivalent.",
                null);
            context.Statistics.WarningsGenerated++;
        }

        context.Statistics.IncrementCount($"AttachedProperty:{propertyName}");
    }

    private bool IsCustomAttachedProperty(string propertyName)
    {
        // Custom attached properties typically come from user namespaces or third-party libraries
        // We can detect them by checking if they're not in the standard WPF namespaces
        var ownerType = propertyName.Split('.')[0];

        // Standard WPF controls that define attached properties
        var standardWpfOwners = new HashSet<string>
        {
            "Grid", "Canvas", "DockPanel", "Panel", "ScrollViewer",
            "ToolTipService", "KeyboardNavigation", "TextOptions",
            "RenderOptions", "VirtualizingStackPanel", "Validation",
            "ContextMenuService", "FocusManager"
        };

        return !standardWpfOwners.Contains(ownerType);
    }
}
