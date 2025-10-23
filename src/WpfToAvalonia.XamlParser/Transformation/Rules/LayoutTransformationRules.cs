namespace WpfToAvalonia.XamlParser.Transformation.Rules;

/// <summary>
/// Transforms WPF StackPanel to Avalonia StackPanel.
/// </summary>
public sealed class StackPanelTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "StackPanel";
}

/// <summary>
/// Transforms WPF Grid to Avalonia Grid.
/// </summary>
public sealed class GridTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "Grid";
}

/// <summary>
/// Transforms WPF DockPanel to Avalonia DockPanel.
/// </summary>
public sealed class DockPanelTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "DockPanel";
}

/// <summary>
/// Transforms WPF WrapPanel to Avalonia WrapPanel.
/// </summary>
public sealed class WrapPanelTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "WrapPanel";
}

/// <summary>
/// Transforms WPF Canvas to Avalonia Canvas.
/// </summary>
public sealed class CanvasTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "Canvas";
}

/// <summary>
/// Transforms WPF UniformGrid to Avalonia UniformGrid.
/// </summary>
public sealed class UniformGridTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "UniformGrid";
}

/// <summary>
/// Transforms WPF Panel to Avalonia Panel.
/// </summary>
public sealed class PanelTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "Panel";
}

/// <summary>
/// Transforms WPF Viewbox to Avalonia Viewbox.
/// </summary>
public sealed class ViewboxTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "Viewbox";
}

/// <summary>
/// Transforms WPF ScrollContentPresenter to Avalonia ScrollContentPresenter.
/// </summary>
public sealed class ScrollContentPresenterTransformationRule : SimpleTypeTransformationRule
{
    protected override string WpfTypeName => "ScrollContentPresenter";
}
