using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation.Rules;

/// <summary>
/// Transforms WPF TextBlock to Avalonia TextBlock.
/// </summary>
public sealed class TextBlockTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "TextBlock";

    protected override void TransformElementProperties(UnifiedXamlElement element, TransformationContext context)
    {
        // TextWrapping enum values are the same in WPF and Avalonia
        // TextTrimming enum values are the same in WPF and Avalonia
        // Most properties are compatible
    }
}

/// <summary>
/// Transforms WPF Button to Avalonia Button.
/// </summary>
public sealed class ButtonTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "Button";
}

/// <summary>
/// Transforms WPF TextBox to Avalonia TextBox.
/// </summary>
public sealed class TextBoxTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "TextBox";

    protected override void TransformElementProperties(UnifiedXamlElement element, TransformationContext context)
    {
        // AcceptsReturn, AcceptsTab are compatible
        // MaxLength is compatible
        // TextWrapping is compatible
    }
}

/// <summary>
/// Transforms WPF CheckBox to Avalonia CheckBox.
/// </summary>
public sealed class CheckBoxTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "CheckBox";
}

/// <summary>
/// Transforms WPF RadioButton to Avalonia RadioButton.
/// </summary>
public sealed class RadioButtonTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "RadioButton";
}

/// <summary>
/// Transforms WPF ComboBox to Avalonia ComboBox.
/// </summary>
public sealed class ComboBoxTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "ComboBox";
}

/// <summary>
/// Transforms WPF ListBox to Avalonia ListBox.
/// </summary>
public sealed class ListBoxTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "ListBox";
}

/// <summary>
/// Transforms WPF ListView to Avalonia ListBox (closest equivalent).
/// </summary>
public sealed class ListViewTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "ListView";
    protected override string AvaloniaTypeName => "ListBox";

    protected override void TransformElementProperties(UnifiedXamlElement element, TransformationContext context)
    {
        // Remove View property (GridView not supported in Avalonia)
        element.Properties.RemoveAll(p => p.PropertyName == "View");

        // Add diagnostic if GridView was used
        var viewProperty = element.Properties.FirstOrDefault(p => p.PropertyName == "View");
        if (viewProperty != null)
        {
            element.AddDiagnostic(
                "LISTVIEW_GRIDVIEW",
                "ListView.View (GridView) is not supported in Avalonia. Consider using DataGrid instead.",
                Core.Diagnostics.DiagnosticSeverity.Warning);
        }
    }
}

/// <summary>
/// Transforms WPF ListViewItem to Avalonia ListBoxItem.
/// </summary>
public sealed class ListViewItemTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "ListViewItem";
    protected override string AvaloniaTypeName => "ListBoxItem";
}

/// <summary>
/// Transforms WPF DataGrid to Avalonia DataGrid.
/// </summary>
public sealed class DataGridTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "DataGrid";
}

/// <summary>
/// Transforms WPF TreeView to Avalonia TreeView.
/// </summary>
public sealed class TreeViewTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "TreeView";
}

/// <summary>
/// Transforms WPF ProgressBar to Avalonia ProgressBar.
/// </summary>
public sealed class ProgressBarTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "ProgressBar";
}

/// <summary>
/// Transforms WPF Slider to Avalonia Slider.
/// </summary>
public sealed class SliderTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "Slider";
}

/// <summary>
/// Transforms WPF ScrollViewer to Avalonia ScrollViewer.
/// </summary>
public sealed class ScrollViewerTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "ScrollViewer";
}

/// <summary>
/// Transforms WPF TabControl to Avalonia TabControl.
/// </summary>
public sealed class TabControlTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "TabControl";
}

/// <summary>
/// Transforms WPF TabItem to Avalonia TabItem.
/// </summary>
public sealed class TabItemTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "TabItem";
}

/// <summary>
/// Transforms WPF Image to Avalonia Image.
/// </summary>
public sealed class ImageTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "Image";
}

/// <summary>
/// Transforms WPF Border to Avalonia Border.
/// </summary>
public sealed class BorderTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "Border";
}

/// <summary>
/// Transforms WPF Label to Avalonia Label.
/// </summary>
public sealed class LabelTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "Label";
}

/// <summary>
/// Transforms WPF Separator to Avalonia Separator.
/// </summary>
public sealed class SeparatorTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "Separator";
}

/// <summary>
/// Transforms WPF Expander to Avalonia Expander.
/// </summary>
public sealed class ExpanderTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "Expander";
}

/// <summary>
/// Transforms WPF GroupBox to Avalonia GroupBox (via Avalonia.Controls.GroupBox from Avalonia.Labs).
/// </summary>
public sealed class GroupBoxTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "GroupBox";

    protected override void TransformElementProperties(UnifiedXamlElement element, TransformationContext context)
    {
        element.AddDiagnostic(
            "GROUPBOX_AVALONIA_LABS",
            "GroupBox requires Avalonia.Labs.Controls package in Avalonia.",
            Core.Diagnostics.DiagnosticSeverity.Info);
    }
}

/// <summary>
/// Transforms WPF ToolTip to Avalonia ToolTip.
/// </summary>
public sealed class ToolTipTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "ToolTip";
}

/// <summary>
/// Transforms WPF ContextMenu to Avalonia ContextMenu.
/// </summary>
public sealed class ContextMenuTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "ContextMenu";
}

/// <summary>
/// Transforms WPF MenuItem to Avalonia MenuItem.
/// </summary>
public sealed class MenuItemTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "MenuItem";
}

/// <summary>
/// Transforms WPF Menu to Avalonia Menu.
/// </summary>
public sealed class MenuTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "Menu";
}
