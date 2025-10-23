using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation.Rules;

/// <summary>
/// Transforms Visibility property from WPF to Avalonia.
/// WPF: Visible, Hidden, Collapsed
/// Avalonia: Visible, Hidden, Collapsed (same!)
/// </summary>
public sealed class VisibilityPropertyRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformVisibilityToIsVisible";

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        // Only transform if it's a simple value (not a binding)
        return property.PropertyName == "Visibility" && !property.HasMarkupExtension;
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        var value = property.GetStringValue();
        if (value != null)
        {
            // In Avalonia, we typically use IsVisible (bool) instead of Visibility enum
            // Visible → true, Hidden/Collapsed → false
            property.PropertyName = "IsVisible";
            property.Value = (value == "Visible").ToString().ToLower();

            context.RecordTransformation(Name, "Property", $"Visibility='{value}' → IsVisible='{property.Value}'");
        }

        return property;
    }
}

/// <summary>
/// Transforms Focusable property (WPF) to Focusable (Avalonia).
/// Values are the same (bool).
/// </summary>
public sealed class FocusablePropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "Focusable";
    protected override string AvaloniaPropertyName => "Focusable";
}

/// <summary>
/// Transforms IsEnabled property (compatible between WPF and Avalonia).
/// </summary>
public sealed class IsEnabledPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "IsEnabled";
    protected override string AvaloniaPropertyName => "IsEnabled";
}

/// <summary>
/// Transforms ToolTip property.
/// </summary>
public sealed class ToolTipPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "ToolTip";
    protected override string AvaloniaPropertyName => "ToolTip.Tip";
}

/// <summary>
/// Transforms ContextMenu property.
/// </summary>
public sealed class ContextMenuPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "ContextMenu";
    protected override string AvaloniaPropertyName => "ContextMenu";
}

/// <summary>
/// Transforms Cursor property.
/// WPF uses different cursor names than Avalonia in some cases.
/// </summary>
public sealed class CursorPropertyRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformCursor";

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        return property.PropertyName == "Cursor";
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        var value = property.GetStringValue();
        if (value != null)
        {
            // Map WPF cursor names to Avalonia cursor names
            property.Value = value switch
            {
                "Hand" => "Hand",
                "Arrow" => "Arrow",
                "Cross" => "Cross",
                "Help" => "Help",
                "IBeam" => "IBeam",
                "No" => "No",
                "Wait" => "Wait",
                "SizeAll" => "SizeAll",
                "SizeNESW" => "TopLeftCorner", // Approximate
                "SizeNS" => "SizeNorthSouth",
                "SizeNWSE" => "TopRightCorner", // Approximate
                "SizeWE" => "SizeWestEast",
                "UpArrow" => "UpArrow",
                _ => value // Keep original if no mapping
            };

            context.RecordTransformation(Name, "Property", $"Cursor='{value}'");
        }

        return property;
    }
}

/// <summary>
/// Transforms FontFamily property.
/// </summary>
public sealed class FontFamilyPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "FontFamily";
    protected override string AvaloniaPropertyName => "FontFamily";
}

/// <summary>
/// Transforms FontSize property.
/// </summary>
public sealed class FontSizePropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "FontSize";
    protected override string AvaloniaPropertyName => "FontSize";
}

/// <summary>
/// Transforms FontWeight property.
/// </summary>
public sealed class FontWeightPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "FontWeight";
    protected override string AvaloniaPropertyName => "FontWeight";
}

/// <summary>
/// Transforms FontStyle property.
/// </summary>
public sealed class FontStylePropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "FontStyle";
    protected override string AvaloniaPropertyName => "FontStyle";
}

/// <summary>
/// Transforms Foreground property.
/// </summary>
public sealed class ForegroundPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "Foreground";
    protected override string AvaloniaPropertyName => "Foreground";
}

/// <summary>
/// Transforms Background property.
/// </summary>
public sealed class BackgroundPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "Background";
    protected override string AvaloniaPropertyName => "Background";
}

/// <summary>
/// Transforms BorderBrush property.
/// </summary>
public sealed class BorderBrushPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "BorderBrush";
    protected override string AvaloniaPropertyName => "BorderBrush";
}

/// <summary>
/// Transforms BorderThickness property.
/// </summary>
public sealed class BorderThicknessPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "BorderThickness";
    protected override string AvaloniaPropertyName => "BorderThickness";
}

/// <summary>
/// Transforms Padding property.
/// </summary>
public sealed class PaddingPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "Padding";
    protected override string AvaloniaPropertyName => "Padding";
}

/// <summary>
/// Transforms Margin property.
/// </summary>
public sealed class MarginPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "Margin";
    protected override string AvaloniaPropertyName => "Margin";
}

/// <summary>
/// Transforms HorizontalAlignment property.
/// </summary>
public sealed class HorizontalAlignmentPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "HorizontalAlignment";
    protected override string AvaloniaPropertyName => "HorizontalAlignment";
}

/// <summary>
/// Transforms VerticalAlignment property.
/// </summary>
public sealed class VerticalAlignmentPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "VerticalAlignment";
    protected override string AvaloniaPropertyName => "VerticalAlignment";
}

/// <summary>
/// Transforms Width property.
/// </summary>
public sealed class WidthPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "Width";
    protected override string AvaloniaPropertyName => "Width";
}

/// <summary>
/// Transforms Height property.
/// </summary>
public sealed class HeightPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "Height";
    protected override string AvaloniaPropertyName => "Height";
}

/// <summary>
/// Transforms MinWidth property.
/// </summary>
public sealed class MinWidthPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "MinWidth";
    protected override string AvaloniaPropertyName => "MinWidth";
}

/// <summary>
/// Transforms MinHeight property.
/// </summary>
public sealed class MinHeightPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "MinHeight";
    protected override string AvaloniaPropertyName => "MinHeight";
}

/// <summary>
/// Transforms MaxWidth property.
/// </summary>
public sealed class MaxWidthPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "MaxWidth";
    protected override string AvaloniaPropertyName => "MaxWidth";
}

/// <summary>
/// Transforms MaxHeight property.
/// </summary>
public sealed class MaxHeightPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "MaxHeight";
    protected override string AvaloniaPropertyName => "MaxHeight";
}

/// <summary>
/// Transforms Opacity property.
/// </summary>
public sealed class OpacityPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "Opacity";
    protected override string AvaloniaPropertyName => "Opacity";
}

/// <summary>
/// Transforms RenderTransform property.
/// </summary>
public sealed class RenderTransformPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "RenderTransform";
    protected override string AvaloniaPropertyName => "RenderTransform";
}

/// <summary>
/// Transforms RenderTransformOrigin property.
/// </summary>
public sealed class RenderTransformOriginPropertyRule : PropertyRenameRule
{
    protected override string WpfPropertyName => "RenderTransformOrigin";
    protected override string AvaloniaPropertyName => "RenderTransformOrigin";
}
