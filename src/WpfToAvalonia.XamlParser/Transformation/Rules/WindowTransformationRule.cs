using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation.Rules;

/// <summary>
/// Transforms WPF Window to Avalonia Window.
/// </summary>
public sealed class WindowTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "Window";

    protected override void TransformElementProperties(UnifiedXamlElement element, TransformationContext context)
    {
        // Update XML namespace if present
        if (element.XmlNamespace != null)
        {
            var nsAttr = element.XmlElement?.Attribute("xmlns");
            if (nsAttr != null && nsAttr.Value.Contains("winfx"))
            {
                // Will be handled by namespace transformer
            }
        }

        // Transform specific Window properties
        foreach (var property in element.Properties.ToList())
        {
            // WindowStartupLocation → WindowStartupLocation (same name, different enum values)
            if (property.PropertyName == "WindowStartupLocation")
            {
                TransformWindowStartupLocation(property);
            }
            // WindowStyle → SystemDecorations
            else if (property.PropertyName == "WindowStyle")
            {
                TransformWindowStyle(element, property);
            }
            // ResizeMode → CanResize
            else if (property.PropertyName == "ResizeMode")
            {
                TransformResizeMode(element, property);
            }
        }
    }

    private void TransformWindowStartupLocation(UnifiedXamlProperty property)
    {
        // WPF: Manual, CenterScreen, CenterOwner
        // Avalonia: Manual, CenterScreen, CenterOwner
        // Same values, no transformation needed
    }

    private void TransformWindowStyle(UnifiedXamlElement element, UnifiedXamlProperty property)
    {
        // WPF WindowStyle: None, SingleBorderWindow, ThreeDBorderWindow, ToolWindow
        // Avalonia SystemDecorations: None, BorderOnly, Full
        property.PropertyName = "SystemDecorations";

        var value = property.GetStringValue();
        if (value != null)
        {
            property.Value = value switch
            {
                "None" => "None",
                "ToolWindow" => "BorderOnly",
                _ => "Full"
            };
        }
    }

    private void TransformResizeMode(UnifiedXamlElement element, UnifiedXamlProperty property)
    {
        // WPF ResizeMode: NoResize, CanMinimize, CanResize, CanResizeWithGrip
        // Avalonia: CanResize (bool)
        var value = property.GetStringValue();
        if (value != null)
        {
            property.PropertyName = "CanResize";
            property.Value = (value == "CanResize" || value == "CanResizeWithGrip").ToString();
        }
    }
}

/// <summary>
/// Transforms WPF UserControl to Avalonia UserControl.
/// </summary>
public sealed class UserControlTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "UserControl";
}

/// <summary>
/// Transforms WPF Page to Avalonia UserControl (closest equivalent).
/// </summary>
public sealed class PageTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "Page";
    protected override string AvaloniaTypeName => "UserControl";

    protected override void TransformElementProperties(UnifiedXamlElement element, TransformationContext context)
    {
        // Remove WPF-specific navigation properties
        element.Properties.RemoveAll(p =>
            p.PropertyName == "KeepAlive" ||
            p.PropertyName == "NavigationUIVisibility" ||
            p.PropertyName == "ShowsNavigationUI");
    }
}
