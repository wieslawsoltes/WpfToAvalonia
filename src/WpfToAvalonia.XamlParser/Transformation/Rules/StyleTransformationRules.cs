using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation.Rules;

/// <summary>
/// Transforms WPF Style elements to Avalonia format.
/// Both WPF and Avalonia support styles, but with some syntax differences.
/// </summary>
public sealed class StyleElementTransformationRule : ElementTransformationRuleBase
{
    public override string Name => "TransformStyle";

    public override int Priority => 100;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        return element.TypeName == "Style";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        // Style element name stays the same, but we need to transform its properties

        // Transform TargetType attribute
        var targetTypeProperty = element.Properties.FirstOrDefault(p => p.PropertyName == "TargetType");
        if (targetTypeProperty != null)
        {
            TransformTargetType(targetTypeProperty, context);
        }

        // Transform BasedOn attribute (style inheritance)
        var basedOnProperty = element.Properties.FirstOrDefault(p => p.PropertyName == "BasedOn");
        if (basedOnProperty != null)
        {
            context.RecordTransformation(
                Name,
                "Style",
                "BasedOn syntax is compatible with Avalonia");
        }

        // Transform x:Key (resource key)
        if (!string.IsNullOrEmpty(element.XKey))
        {
            context.RecordTransformation(
                Name,
                "Style",
                $"Style with key '{element.XKey}' preserved");
        }

        context.RecordTransformation(
            Name,
            "Element",
            "Style element transformed");

        return element;
    }

    private void TransformTargetType(UnifiedXamlProperty property, TransformationContext context)
    {
        var targetType = property.GetStringValue();
        if (string.IsNullOrEmpty(targetType)) return;

        // Transform WPF type names to Avalonia equivalents
        var typeMapping = new Dictionary<string, string>
        {
            { "System.Windows.Controls.Button", "Button" },
            { "System.Windows.Controls.TextBlock", "TextBlock" },
            { "System.Windows.Controls.TextBox", "TextBox" },
            { "System.Windows.Window", "Window" },
            // Add more as needed
        };

        // Handle x:Type markup extension
        if (property.HasMarkupExtension && property.MarkupExtension?.ExtensionName == "x:Type")
        {
            var typeArg = property.MarkupExtension.PositionalArgument?.ToString();
            if (!string.IsNullOrEmpty(typeArg))
            {
                foreach (var mapping in typeMapping)
                {
                    if (typeArg.Contains(mapping.Key))
                    {
                        property.MarkupExtension.PositionalArgument = typeArg.Replace(mapping.Key, mapping.Value);
                        context.RecordTransformation(
                            Name,
                            "TargetType",
                            $"Transformed TargetType: {mapping.Key} → {mapping.Value}");
                        return;
                    }
                }
            }
        }

        // Handle simple type name
        if (typeMapping.TryGetValue(targetType, out var avaloniaType))
        {
            property.Value = avaloniaType;
            context.RecordTransformation(
                Name,
                "TargetType",
                $"Transformed TargetType: {targetType} → {avaloniaType}");
        }
    }
}

/// <summary>
/// Transforms WPF Setter elements within styles.
/// Setters work similarly in both WPF and Avalonia, but property names may differ.
/// </summary>
public sealed class SetterTransformationRule : ElementTransformationRuleBase
{
    public override string Name => "TransformSetter";

    public override int Priority => 90;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        return element.TypeName == "Setter";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        // Transform the Property attribute
        var propertyAttr = element.Properties.FirstOrDefault(p => p.PropertyName == "Property");
        if (propertyAttr != null)
        {
            var propertyName = propertyAttr.GetStringValue();
            if (!string.IsNullOrEmpty(propertyName))
            {
                var transformedName = TransformPropertyName(propertyName);
                if (transformedName != propertyName)
                {
                    propertyAttr.Value = transformedName;
                    context.RecordTransformation(
                        Name,
                        "Setter",
                        $"Transformed Setter Property: {propertyName} → {transformedName}");
                }
            }
        }

        // Value attribute transformation is handled by property transformation rules

        return element;
    }

    private string TransformPropertyName(string propertyName)
    {
        // Common WPF to Avalonia property mappings for style setters
        var mappings = new Dictionary<string, string>
        {
            { "Visibility", "IsVisible" },
            { "Background", "Background" }, // Same
            { "Foreground", "Foreground" }, // Same
            { "BorderBrush", "BorderBrush" }, // Same
            { "BorderThickness", "BorderThickness" }, // Same
            { "Padding", "Padding" }, // Same
            { "Margin", "Margin" }, // Same
            { "FontFamily", "FontFamily" }, // Same
            { "FontSize", "FontSize" }, // Same
            { "FontWeight", "FontWeight" }, // Same
            { "ToolTip", "ToolTip.Tip" },
        };

        return mappings.TryGetValue(propertyName, out var avaloniaName)
            ? avaloniaName
            : propertyName;
    }
}

/// <summary>
/// Transforms WPF Trigger elements to Avalonia pseudoclasses or styles.
/// WPF: Style.Triggers with Trigger, DataTrigger, EventTrigger
/// Avalonia: Pseudoclasses (:pointerover, :pressed) or style selectors
/// </summary>
public sealed class TriggerTransformationRule : ElementTransformationRuleBase
{
    public override string Name => "TransformTrigger";

    public override int Priority => 95;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        // Note: MultiTrigger is handled by MultiTriggerTransformer in CompatibilityTransformationRules.cs
        return element.TypeName is "Trigger" or "DataTrigger" or "EventTrigger";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        // WPF triggers don't have a direct 1:1 mapping in Avalonia
        // They need to be converted to style selectors or behaviors

        var triggerType = element.TypeName;

        switch (triggerType)
        {
            case "Trigger":
                TransformPropertyTrigger(element, context);
                break;

            case "DataTrigger":
                TransformDataTrigger(element, context);
                break;

            case "EventTrigger":
                TransformEventTrigger(element, context);
                break;
        }

        return element;
    }

    private void TransformPropertyTrigger(UnifiedXamlElement element, TransformationContext context)
    {
        // WPF: <Trigger Property="IsMouseOver" Value="True">
        // Avalonia: Use pseudoclass or style selector

        var propertyAttr = element.Properties.FirstOrDefault(p => p.PropertyName == "Property");
        var valueAttr = element.Properties.FirstOrDefault(p => p.PropertyName == "Value");

        if (propertyAttr != null && valueAttr != null)
        {
            var property = propertyAttr.GetStringValue();
            var value = valueAttr.GetStringValue();

            // Map common WPF trigger properties to Avalonia pseudoclasses
            var pseudoclassMapping = new Dictionary<(string, string), string>
            {
                { ("IsMouseOver", "True"), ":pointerover" },
                { ("IsPressed", "True"), ":pressed" },
                { ("IsFocused", "True"), ":focus" },
                { ("IsEnabled", "False"), ":disabled" },
                { ("IsSelected", "True"), ":selected" },
                { ("IsChecked", "True"), ":checked" },
            };

            if (pseudoclassMapping.TryGetValue((property ?? "", value ?? ""), out var pseudoclass))
            {
                context.RecordTransformation(
                    Name,
                    "Trigger",
                    $"Convert Trigger (Property={property}, Value={value}) to style with '{pseudoclass}' selector");

                // Note: The actual conversion to style syntax would happen in a parent style processor
                // This is flagged for manual review or automated style restructuring
            }
            else
            {
                context.RecordTransformation(
                    Name,
                    "Trigger",
                    $"Trigger with Property={property} requires manual conversion (no direct pseudoclass equivalent)");
            }
        }
    }

    private void TransformDataTrigger(UnifiedXamlElement element, TransformationContext context)
    {
        // DataTrigger is not directly supported in Avalonia
        // Needs to be converted to behaviors or reactive bindings

        context.RecordTransformation(
            Name,
            "DataTrigger",
            "DataTrigger requires manual conversion to Avalonia behaviors or reactive patterns");

        element.AddDiagnostic(
            "DATATRIGGER_NOT_SUPPORTED",
            "DataTrigger is not directly supported in Avalonia. Consider using behaviors or reactive extensions.",
            Core.Diagnostics.DiagnosticSeverity.Warning);
    }

    private void TransformEventTrigger(UnifiedXamlElement element, TransformationContext context)
    {
        // EventTrigger (typically for animations) has limited support in Avalonia
        // Animations are usually done via code or behaviors

        context.RecordTransformation(
            Name,
            "EventTrigger",
            "EventTrigger for animations should be converted to Avalonia animation syntax or code-behind");

        element.AddDiagnostic(
            "EVENTTRIGGER_LIMITED",
            "EventTrigger support is limited in Avalonia. Consider using Avalonia's animation system or behaviors.",
            Core.Diagnostics.DiagnosticSeverity.Warning);
    }
}

/// <summary>
/// Transforms ResourceDictionary elements.
/// Both WPF and Avalonia use ResourceDictionary, but with some differences.
/// </summary>
public sealed class ResourceDictionaryTransformationRule : ElementTransformationRuleBase
{
    public override string Name => "TransformResourceDictionary";

    public override int Priority => 100;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        return element.TypeName == "ResourceDictionary";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        // ResourceDictionary structure is similar, but namespaces differ

        // Check for MergedDictionaries
        var mergedDicts = element.Children.FirstOrDefault(c => c.TypeName == "ResourceDictionary.MergedDictionaries");
        if (mergedDicts != null)
        {
            context.RecordTransformation(
                Name,
                "ResourceDictionary",
                "MergedDictionaries found - verify resource paths are correct for Avalonia");
        }

        // Transform any WPF-specific resources
        foreach (var child in element.Children)
        {
            if (child.TypeName == "Style")
            {
                // Styles will be handled by StyleElementTransformationRule
                continue;
            }

            // Check for color/brush resources that might need transformation
            if (child.TypeName is "SolidColorBrush" or "LinearGradientBrush" or "RadialGradientBrush")
            {
                context.RecordTransformation(
                    Name,
                    "Resource",
                    $"Brush resource '{child.XKey}' - syntax is compatible with Avalonia");
            }
        }

        context.RecordTransformation(
            Name,
            "Element",
            "ResourceDictionary structure preserved");

        return element;
    }
}

/// <summary>
/// Transforms WPF ControlTemplate elements to Avalonia format.
/// Templates work similarly but with some syntax differences.
/// </summary>
public sealed class ControlTemplateTransformationRule : ElementTransformationRuleBase
{
    public override string Name => "TransformControlTemplate";

    public override int Priority => 90;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        return element.TypeName == "ControlTemplate";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        // Transform TargetType
        var targetTypeProperty = element.Properties.FirstOrDefault(p => p.PropertyName == "TargetType");
        if (targetTypeProperty != null)
        {
            context.RecordTransformation(
                Name,
                "ControlTemplate",
                "ControlTemplate TargetType preserved - verify control type exists in Avalonia");
        }

        // Check for TemplateBinding usage
        foreach (var descendant in GetAllDescendants(element))
        {
            var templateBindings = descendant.Properties
                .Where(p => p.HasMarkupExtension && p.MarkupExtension?.ExtensionName == "TemplateBinding")
                .ToList();

            if (templateBindings.Any())
            {
                context.RecordTransformation(
                    Name,
                    "ControlTemplate",
                    $"Found {templateBindings.Count} TemplateBinding(s) - syntax is compatible with Avalonia");
            }
        }

        // Check for ContentPresenter (commonly used in templates)
        var hasContentPresenter = GetAllDescendants(element)
            .Any(e => e.TypeName == "ContentPresenter");

        if (hasContentPresenter)
        {
            context.RecordTransformation(
                Name,
                "ControlTemplate",
                "ContentPresenter found - compatible with Avalonia");
        }

        return element;
    }

    private IEnumerable<UnifiedXamlElement> GetAllDescendants(UnifiedXamlElement element)
    {
        foreach (var child in element.Children)
        {
            yield return child;
            foreach (var descendant in GetAllDescendants(child))
            {
                yield return descendant;
            }
        }
    }
}

/// <summary>
/// Transforms WPF DataTemplate elements to Avalonia format.
/// DataTemplates are similar in both frameworks.
/// </summary>
public sealed class DataTemplateTransformationRule : ElementTransformationRuleBase
{
    public override string Name => "TransformDataTemplate";

    public override int Priority => 90;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        return element.TypeName == "DataTemplate";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        // Check for DataType attribute (for implicit DataTemplates)
        var dataTypeProperty = element.Properties.FirstOrDefault(p => p.PropertyName == "DataType");
        if (dataTypeProperty != null)
        {
            context.RecordTransformation(
                Name,
                "DataTemplate",
                "DataTemplate with DataType (implicit template) - syntax compatible with Avalonia");
        }

        // Check for x:Key (explicit template)
        if (!string.IsNullOrEmpty(element.XKey))
        {
            context.RecordTransformation(
                Name,
                "DataTemplate",
                $"DataTemplate with key '{element.XKey}' preserved");
        }

        return element;
    }
}
