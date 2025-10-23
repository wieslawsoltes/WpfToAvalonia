using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.UnifiedAst;
using WpfToAvalonia.XamlParser.Visitors;

namespace WpfToAvalonia.XamlParser.Transformation;

/// <summary>
/// Main transformation engine that applies transformation rules to convert WPF XAML to Avalonia XAML.
/// </summary>
public sealed class TransformationEngine
{
    private readonly List<ITransformationRule> _rules = new();
    private readonly DiagnosticCollector _diagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransformationEngine"/> class.
    /// </summary>
    public TransformationEngine(DiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    /// <summary>
    /// Adds a transformation rule.
    /// </summary>
    public void AddRule(ITransformationRule rule)
    {
        _rules.Add(rule);
    }

    /// <summary>
    /// Adds multiple transformation rules.
    /// </summary>
    public void AddRules(IEnumerable<ITransformationRule> rules)
    {
        _rules.AddRange(rules);
    }

    /// <summary>
    /// Registers all default transformation rules.
    /// </summary>
    public void RegisterDefaultRules()
    {
        // Element transformation rules
        AddRules(GetDefaultElementRules());

        // Property transformation rules
        AddRules(GetDefaultPropertyRules());

        // Sort rules by priority (higher priority first)
        _rules.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        // Transformation rules registered and sorted by priority
    }

    /// <summary>
    /// Transforms a XAML document from WPF to Avalonia.
    /// </summary>
    public UnifiedXamlDocument Transform(UnifiedXamlDocument document, TransformationOptions? options = null)
    {
        options ??= new TransformationOptions();
        var context = new TransformationContext(document, options);

        if (document.Root == null)
        {
            _diagnostics.AddWarning("TRANSFORM_NO_ROOT", "Document has no root element", document.FilePath);
            return document;
        }

        // Create a transformation visitor
        var transformer = new TransformationVisitor(_rules, context);

        // Transform the document
        transformer.VisitDocument(document);

        // Post-process: Apply restructuring rules that need to run after all transformations
        if (document.Root != null)
        {
            ApplyPostProcessingRules(document.Root, context);
        }

        // Collect diagnostics from the AST nodes
        var diagnosticCollector = new Visitors.DiagnosticCollectorVisitor();
        var astDiagnostics = diagnosticCollector.VisitDocument(document);
        _diagnostics.AddRange(astDiagnostics);

        // Log statistics
        LogStatistics(context);

        return document;
    }

    private void ApplyPostProcessingRules(UnifiedXamlElement element, TransformationContext context)
    {
        // First, recursively process all children
        foreach (var child in element.Children.ToList()) // ToList to avoid modification during iteration
        {
            ApplyPostProcessingRules(child, context);
        }

        // Also process property values that are elements (like Window.Resources containing Style elements)
        foreach (var property in element.Properties.ToList())
        {
            if (property.Value is UnifiedXamlElement propertyElement)
            {
                ApplyPostProcessingRules(propertyElement, context);
            }
        }

        // Then apply post-processing rules to this element
        var restructuringRule = _rules.OfType<Rules.StyleTriggersRestructuringRule>().FirstOrDefault();
        if (restructuringRule != null && restructuringRule.CanTransformElement(element))
        {
            restructuringRule.TransformElement(element, context);
        }

        // Finally, apply cleanup rules to remove converted triggers
        var cleanupRule = _rules.OfType<Rules.ConvertedTriggerCleanupRule>().FirstOrDefault();
        if (cleanupRule != null)
        {
            // Remove converted triggers from children
            for (int i = element.Children.Count - 1; i >= 0; i--)
            {
                var child = element.Children[i];
                if (cleanupRule.CanTransformElement(child))
                {
                    element.Children.RemoveAt(i);
                    cleanupRule.TransformElement(child, context);
                }
            }
        }
    }

    private void LogStatistics(TransformationContext context)
    {
        var byRule = context.Statistics.GetTransformationsByRule();
        var total = context.Statistics.TotalTransformations;

        _diagnostics.AddInfo(
            "TRANSFORM_COMPLETE",
            $"Transformation complete: {total} transformations applied",
            context.Document.FilePath);

        foreach (var (ruleName, count) in byRule.OrderByDescending(kvp => kvp.Value))
        {
            _diagnostics.AddInfo(
                "TRANSFORM_RULE_STATS",
                $"  {ruleName}: {count} transformation(s)",
                context.Document.FilePath);
        }
    }

    private IEnumerable<ITransformationRule> GetDefaultElementRules()
    {
        // Window and container transformations
        yield return new Rules.WindowTransformationRule();
        yield return new Rules.UserControlTransformationRule();
        yield return new Rules.PageTransformationRule();

        // Layout panels
        yield return new Rules.StackPanelTransformationRule();
        yield return new Rules.GridTransformationRule();
        yield return new Rules.DockPanelTransformationRule();
        yield return new Rules.WrapPanelTransformationRule();
        yield return new Rules.CanvasTransformationRule();
        yield return new Rules.UniformGridTransformationRule();
        yield return new Rules.PanelTransformationRule();
        yield return new Rules.ViewboxTransformationRule();
        yield return new Rules.ScrollContentPresenterTransformationRule();

        // Controls
        yield return new Rules.TextBlockTransformationRule();
        yield return new Rules.ButtonTransformationRule();
        yield return new Rules.TextBoxTransformationRule();
        yield return new Rules.CheckBoxTransformationRule();
        yield return new Rules.RadioButtonTransformationRule();
        yield return new Rules.ComboBoxTransformationRule();
        yield return new Rules.ListBoxTransformationRule();
        yield return new Rules.ListViewTransformationRule();
        yield return new Rules.ListViewItemTransformationRule();
        yield return new Rules.DataGridTransformationRule();
        yield return new Rules.TreeViewTransformationRule();
        yield return new Rules.ProgressBarTransformationRule();
        yield return new Rules.SliderTransformationRule();
        yield return new Rules.ScrollViewerTransformationRule();
        yield return new Rules.TabControlTransformationRule();
        yield return new Rules.TabItemTransformationRule();
        yield return new Rules.ImageTransformationRule();
        yield return new Rules.BorderTransformationRule();
        yield return new Rules.LabelTransformationRule();
        yield return new Rules.SeparatorTransformationRule();
        yield return new Rules.ExpanderTransformationRule();
        yield return new Rules.GroupBoxTransformationRule();
        yield return new Rules.ToolTipTransformationRule();
        yield return new Rules.ContextMenuTransformationRule();
        yield return new Rules.MenuItemTransformationRule();
        yield return new Rules.MenuTransformationRule();
    }

    private IEnumerable<ITransformationRule> GetDefaultPropertyRules()
    {
        // Common properties
        yield return new Rules.VisibilityPropertyRule();
        yield return new Rules.FocusablePropertyRule();
        yield return new Rules.IsEnabledPropertyRule();
        yield return new Rules.ToolTipPropertyRule();
        yield return new Rules.ContextMenuPropertyRule();
        yield return new Rules.CursorPropertyRule();

        // Font properties
        yield return new Rules.FontFamilyPropertyRule();
        yield return new Rules.FontSizePropertyRule();
        yield return new Rules.FontWeightPropertyRule();
        yield return new Rules.FontStylePropertyRule();

        // Brush properties
        yield return new Rules.ForegroundPropertyRule();
        yield return new Rules.BackgroundPropertyRule();
        yield return new Rules.BorderBrushPropertyRule();

        // Layout properties
        yield return new Rules.BorderThicknessPropertyRule();
        yield return new Rules.PaddingPropertyRule();
        yield return new Rules.MarginPropertyRule();
        yield return new Rules.HorizontalAlignmentPropertyRule();
        yield return new Rules.VerticalAlignmentPropertyRule();
        yield return new Rules.WidthPropertyRule();
        yield return new Rules.HeightPropertyRule();
        yield return new Rules.MinWidthPropertyRule();
        yield return new Rules.MinHeightPropertyRule();
        yield return new Rules.MaxWidthPropertyRule();
        yield return new Rules.MaxHeightPropertyRule();

        // Transform properties
        yield return new Rules.OpacityPropertyRule();
        yield return new Rules.RenderTransformPropertyRule();
        yield return new Rules.RenderTransformOriginPropertyRule();

        // Value transformation rules (colors, resources, thickness, etc.)
        yield return new Rules.ColorValueTransformationRule();
        yield return new Rules.ThicknessValueTransformationRule();
        yield return new Rules.ResourceReferenceTransformationRule();
        yield return new Rules.GridLengthValueTransformationRule();
        yield return new Rules.GeometryValueTransformationRule();
        yield return new Rules.DurationValueTransformationRule();
        yield return new Rules.CornerRadiusValueTransformationRule();

        // Binding transformation rules
        yield return new Rules.BasicBindingTransformationRule();
        yield return new Rules.RelativeSourceBindingTransformationRule();
        yield return new Rules.ElementNameBindingTransformationRule();
        yield return new Rules.BindingPathTransformationRule();
        yield return new Rules.CompiledBindingTransformationRule();
        yield return new Rules.MultiBindingTransformationRule();

        // Style and template transformation rules
        yield return new Rules.StyleElementTransformationRule();
        yield return new Rules.SetterTransformationRule();
        yield return new Rules.TriggerTransformationRule();
        yield return new Rules.ResourceDictionaryTransformationRule();
        yield return new Rules.ControlTemplateTransformationRule();
        yield return new Rules.DataTemplateTransformationRule();

        // Compatibility transformation rules (convert WPF features to Avalonia equivalents)
        yield return new Rules.TriggerToStyleSelectorTransformer();
        yield return new Rules.DataTriggerToBindingTransformer();
        yield return new Rules.EventTriggerToAnimationTransformer();
        yield return new Rules.MultiTriggerTransformer();
        yield return new Rules.VisualStateManagerTransformer();
        yield return new Rules.StyleToControlThemeTransformer();
        yield return new Rules.StyleTriggersRestructuringRule();
        yield return new Rules.ConvertedTriggerCleanupRule(); // Cleanup rule - runs last (priority 1)

        // Markup extension transformation rules
        yield return new Rules.XArrayMarkupExtensionTransformer();
        yield return new Rules.XStaticMarkupExtensionTransformer();
        yield return new Rules.XTypeMarkupExtensionTransformer();
        yield return new Rules.XNullMarkupExtensionTransformer();
    }
}

/// <summary>
/// Visitor that applies transformation rules to the AST.
/// </summary>
internal sealed class TransformationVisitor : UnifiedXamlVisitorBase
{
    private readonly List<ITransformationRule> _rules;
    private readonly TransformationContext _context;

    public TransformationVisitor(List<ITransformationRule> rules, TransformationContext context)
    {
        _rules = rules;
        _context = context;
    }

    public override void VisitElement(UnifiedXamlElement element)
    {
        _context.CurrentElement = element;

        // Apply element transformation rules
        foreach (var rule in _rules.OfType<IElementTransformationRule>())
        {
            if (rule.CanTransformElement(element))
            {
                rule.TransformElement(element, _context);
            }
        }

        // Continue visiting children and properties
        base.VisitElement(element);
    }

    public override void VisitProperty(UnifiedXamlProperty property)
    {
        // Apply property transformation rules
        foreach (var rule in _rules.OfType<IPropertyTransformationRule>())
        {
            if (rule.CanTransformProperty(property))
            {
                rule.TransformProperty(property, _context);
            }
        }

        // Continue visiting
        base.VisitProperty(property);
    }

    public override void VisitMarkupExtension(UnifiedXamlMarkupExtension markupExtension)
    {
        // Apply markup extension transformation rules
        foreach (var rule in _rules.OfType<IMarkupExtensionTransformationRule>())
        {
            if (rule.CanTransformMarkupExtension(markupExtension))
            {
                rule.TransformMarkupExtension(markupExtension, _context);
            }
        }

        // Continue visiting
        base.VisitMarkupExtension(markupExtension);
    }
}
