using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformers;

/// <summary>
/// Transforms WPF templates to Avalonia templates.
/// </summary>
/// <remarks>
/// Handles:
/// - DataTemplate transformation (mostly 1:1)
/// - ControlTemplate transformation (requires TargetType adjustment)
/// - ItemTemplate property transformation
/// - TemplateBinding → TemplateBinding (same in Avalonia)
/// - Template triggers (issues warnings)
/// </remarks>
public class TemplateTransformer : IXamlTransformer
{
    public string Name => "TemplateTransformer";
    public int Priority => 55; // Run after styles (50), before final cleanup

    public void Transform(UnifiedXamlDocument document, TransformationContext context)
    {
        if (document.Root == null)
        {
            context.Diagnostics.AddWarning(
                "TEMPLATE_TRANSFORM_NO_ROOT",
                "Document has no root element",
                null);
            return;
        }

        context.Diagnostics.AddInfo(
            "TEMPLATE_TRANSFORM_START",
            "Starting template transformation",
            null);

        // Transform all template elements
        TransformTemplates(document.Root, context);

        context.Diagnostics.AddInfo(
            "TEMPLATE_TRANSFORM_COMPLETE",
            "Template transformation complete",
            null);
    }

    private void TransformTemplates(UnifiedXamlElement element, TransformationContext context)
    {
        // Transform DataTemplate elements
        if (element.TypeName == "DataTemplate")
        {
            TransformDataTemplate(element, context);
        }
        // Transform ControlTemplate elements
        else if (element.TypeName == "ControlTemplate")
        {
            TransformControlTemplate(element, context);
        }
        // Transform HierarchicalDataTemplate elements
        else if (element.TypeName == "HierarchicalDataTemplate")
        {
            TransformHierarchicalDataTemplate(element, context);
        }

        // Check for template properties on elements
        TransformTemplateProperties(element, context);

        // Check for TemplateBinding usage
        CheckTemplateBindings(element, context);

        // Recursively transform children
        foreach (var child in element.Children)
        {
            TransformTemplates(child, context);
        }
    }

    private void TransformDataTemplate(UnifiedXamlElement element, TransformationContext context)
    {
        // DataTemplate is mostly 1:1 in Avalonia
        context.Diagnostics.AddInfo(
            "DATA_TEMPLATE_FOUND",
            "Processing DataTemplate (syntax is mostly compatible with Avalonia)",
            null);

        // Check for DataType property
        var dataTypeProperty = element.Properties.FirstOrDefault(p => p.PropertyName == "DataType");
        if (dataTypeProperty != null)
        {
            context.Diagnostics.AddInfo(
                "DATA_TEMPLATE_DATATYPE",
                $"DataTemplate with DataType (implicit template)",
                null);
        }

        // Check for triggers in DataTemplate
        var triggersProperty = element.Properties.FirstOrDefault(p =>
            p.PropertyName == "Triggers" && p.Kind == PropertyKind.PropertyElement);

        if (triggersProperty != null)
        {
            context.Diagnostics.AddWarning(
                "DATA_TEMPLATE_TRIGGERS",
                "DataTemplate contains Triggers. Avalonia doesn't support triggers in DataTemplates. Consider using styles or data binding instead.",
                null);
            context.Statistics.WarningsGenerated++;
        }

        context.Statistics.IncrementCount("DataTemplate");
    }

    private void TransformControlTemplate(UnifiedXamlElement element, TransformationContext context)
    {
        // ControlTemplate has some differences in Avalonia
        context.Diagnostics.AddInfo(
            "CONTROL_TEMPLATE_FOUND",
            "Processing ControlTemplate",
            null);

        // Check for TargetType property (required in both WPF and Avalonia)
        var targetTypeProperty = element.Properties.FirstOrDefault(p => p.PropertyName == "TargetType");
        if (targetTypeProperty == null)
        {
            context.Diagnostics.AddWarning(
                "CONTROL_TEMPLATE_NO_TARGETTYPE",
                "ControlTemplate missing TargetType attribute (required in Avalonia)",
                null);
            context.Statistics.WarningsGenerated++;
        }
        else
        {
            // Transform the TargetType value (e.g., "{x:Type Button}" → "Button")
            var targetType = targetTypeProperty.Value?.ToString();
            if (!string.IsNullOrEmpty(targetType) && targetType.Contains("{x:Type "))
            {
                // Extract type from markup extension
                var typeName = ExtractTypeFromMarkupExtension(targetType);
                if (!string.IsNullOrEmpty(typeName))
                {
                    targetTypeProperty.Value = typeName;
                    context.Diagnostics.AddInfo(
                        "CONTROL_TEMPLATE_TARGETTYPE_SIMPLIFIED",
                        $"Simplified TargetType from '{targetType}' to '{typeName}'",
                        null);
                }
            }
        }

        // Check for triggers in ControlTemplate
        var triggersProperty = element.Properties.FirstOrDefault(p =>
            p.PropertyName == "Triggers" && p.Kind == PropertyKind.PropertyElement);

        if (triggersProperty != null)
        {
            context.Diagnostics.AddWarning(
                "CONTROL_TEMPLATE_TRIGGERS",
                "ControlTemplate contains Triggers. Avalonia uses ControlTheme with Styles instead. Manual conversion required.",
                null);
            context.Statistics.WarningsGenerated++;
        }

        context.Statistics.IncrementCount("ControlTemplate");
    }

    private void TransformHierarchicalDataTemplate(UnifiedXamlElement element, TransformationContext context)
    {
        // HierarchicalDataTemplate is used for TreeView in WPF
        context.Diagnostics.AddWarning(
            "HIERARCHICAL_DATA_TEMPLATE",
            "HierarchicalDataTemplate found. Avalonia uses TreeDataTemplate instead. Manual conversion may be required.",
            null);
        context.Statistics.WarningsGenerated++;

        context.Diagnostics.AddInfo(
            "HIERARCHICAL_DATA_TEMPLATE_SUGGESTION",
            "Consider converting HierarchicalDataTemplate to Avalonia's TreeDataTemplate",
            null);

        context.Statistics.IncrementCount("HierarchicalDataTemplate");
    }

    private void TransformTemplateProperties(UnifiedXamlElement element, TransformationContext context)
    {
        // Common template properties: ItemTemplate, ContentTemplate, HeaderTemplate, etc.
        var templateProperties = element.Properties.Where(p =>
            p.PropertyName.EndsWith("Template") && p.Kind == PropertyKind.PropertyElement).ToList();

        foreach (var templateProp in templateProperties)
        {
            context.Diagnostics.AddInfo(
                "TEMPLATE_PROPERTY_FOUND",
                $"Found {element.TypeName}.{templateProp.PropertyName}",
                null);
            context.Statistics.IncrementCount($"{templateProp.PropertyName}");
        }
    }

    private void CheckTemplateBindings(UnifiedXamlElement element, TransformationContext context)
    {
        // TemplateBinding syntax is the same in Avalonia, but we validate it exists
        foreach (var property in element.Properties)
        {
            if (property.Value is string value && value.Contains("{TemplateBinding "))
            {
                context.Diagnostics.AddInfo(
                    "TEMPLATE_BINDING_FOUND",
                    $"TemplateBinding found in {element.TypeName}.{property.PropertyName}",
                    null);
                context.Statistics.IncrementCount("TemplateBinding");
            }
            else if (property.MarkupExtension?.ExtensionName == "TemplateBinding")
            {
                context.Diagnostics.AddInfo(
                    "TEMPLATE_BINDING_FOUND",
                    $"TemplateBinding found in {element.TypeName}.{property.PropertyName}",
                    null);
                context.Statistics.IncrementCount("TemplateBinding");
            }
        }
    }

    private string ExtractTypeFromMarkupExtension(string markupExtension)
    {
        // Extract type from "{x:Type SomeType}" or "{x:Type prefix:SomeType}"
        var trimmed = markupExtension.Trim();

        if (trimmed.StartsWith("{x:Type ") && trimmed.EndsWith("}"))
        {
            var start = "{x:Type ".Length;
            var end = trimmed.Length - 1;
            var typePart = trimmed.Substring(start, end - start).Trim();

            // Remove namespace prefix if present (e.g., "local:MyControl" → "MyControl")
            var colonIndex = typePart.IndexOf(':');
            if (colonIndex >= 0 && colonIndex < typePart.Length - 1)
            {
                return typePart.Substring(colonIndex + 1);
            }

            return typePart;
        }

        return string.Empty;
    }
}
