using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation.Rules;

/// <summary>
/// Base class for validation transformation rules.
/// Handles WPF validation features in XAML.
/// Implements part of task 2.5.7.1.5: Handle binding validation rules (XAML aspect)
/// </summary>
public abstract class ValidationTransformationRuleBase : PropertyTransformationRuleBase
{
    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        // Check for validation-related properties
        return property.PropertyName.Contains("Validation") ||
               (property.HasMarkupExtension &&
                HasValidationParameters(property.MarkupExtension));
    }

    private bool HasValidationParameters(UnifiedXamlMarkupExtension? extension)
    {
        if (extension == null) return false;

        return extension.Parameters.ContainsKey("ValidatesOnDataErrors") ||
               extension.Parameters.ContainsKey("ValidatesOnExceptions") ||
               extension.Parameters.ContainsKey("NotifyOnValidationError") ||
               extension.Parameters.ContainsKey("ValidationRules");
    }
}

/// <summary>
/// Transforms validation-related attached properties.
/// Maps WPF Validation.* attached properties to Avalonia DataValidationErrors.*.
/// </summary>
public sealed class ValidationAttachedPropertyRule : ElementTransformationRuleBase
{
    public override string Name => "TransformValidationAttachedProperties";

    public override int Priority => 70;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        return element.Properties.Any(p =>
            p.PropertyName.StartsWith("Validation."));
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        foreach (var property in element.Properties.ToList())
        {
            if (property.PropertyName.StartsWith("Validation."))
            {
                var propertyName = property.PropertyName.Substring("Validation.".Length);

                switch (propertyName)
                {
                    case "ErrorTemplate":
                        context.RecordTransformation(Name,
                            "VALIDATION_ERROR_TEMPLATE",
                            "Validation.ErrorTemplate is not directly supported in Avalonia. " +
                            "Use DataValidationErrors.ErrorTemplate or style the :error pseudo-class. " +
                            "Update to: DataValidationErrors.ErrorTemplate=\"{StaticResource ...}\"");
                        break;

                    case "HasError":
                        context.RecordTransformation(Name,
                            "VALIDATION_HAS_ERROR_XAML",
                            "Validation.HasError attached property. " +
                            "Update to: DataValidationErrors.HasErrors (note the plural 'Errors')");
                        break;

                    case "Errors":
                        context.RecordTransformation(Name,
                            "VALIDATION_ERRORS_XAML",
                            "Validation.Errors attached property. " +
                            "Update to: DataValidationErrors.Errors");
                        break;

                    default:
                        context.RecordTransformation(Name,
                            "VALIDATION_ATTACHED_PROPERTY",
                            $"Validation.{propertyName} attached property needs manual review for Avalonia compatibility.");
                        break;
                }

                context.RecordTransformation(
                    Name,
                    "ValidationAttachedProperty",
                    $"Validation.{propertyName} detected - requires update to DataValidationErrors equivalent");
            }
        }

        return element;
    }
}

/// <summary>
/// Transforms Validation.ErrorTemplate in styles and control templates.
/// </summary>
public sealed class ValidationErrorTemplateRule : ElementTransformationRuleBase
{
    public override string Name => "TransformValidationErrorTemplate";

    public override int Priority => 69;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        return element.ElementType?.Name == "Setter" &&
               element.Properties.Any(p =>
                   p.PropertyName == "Property" &&
                   p.Value?.ToString()?.Contains("Validation.ErrorTemplate") == true);
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        context.RecordTransformation(Name,
            "ERROR_TEMPLATE_SETTER",
            "Validation.ErrorTemplate setter detected. " +
            "In Avalonia, use DataValidationErrors.ErrorTemplate or create a style with :error pseudo-class. " +
            "Example: <Style Selector=\"TextBox:error\"><Setter Property=\"BorderBrush\" Value=\"Red\"/></Style>");

        context.RecordTransformation(
            Name,
            "ErrorTemplate",
            "Validation.ErrorTemplate setter needs Avalonia equivalent");

        return element;
    }
}

/// <summary>
/// Handles Binding.ValidationRules in XAML (property element syntax).
/// </summary>
public sealed class BindingValidationRulesRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformBindingValidationRules";

    public override int Priority => 68;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        return property.PropertyName == "Binding.ValidationRules" ||
               property.PropertyName == "ValidationRules";
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        context.RecordTransformation(Name,
            "VALIDATION_RULES_NOT_SUPPORTED",
            "Binding.ValidationRules is not supported in Avalonia. " +
            "Migration options:\n" +
            "1. Implement INotifyDataErrorInfo on ViewModel (recommended)\n" +
            "2. Use DataAnnotations attributes ([Required], [Range], etc.)\n" +
            "3. Use Avalonia.Labs.DataValidation NuGet package\n" +
            "4. Implement validation in property setters with DataValidationErrors.SetErrors()");

        context.RecordTransformation(
            Name,
            "ValidationRules",
            "Binding.ValidationRules detected - requires migration to Avalonia validation approach");

        return property;
    }
}

/// <summary>
/// Transforms validation-related binding parameters.
/// Already handled in BasicBindingTransformationRule, but provides additional guidance.
/// </summary>
public sealed class ValidationBindingParametersRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformValidationBindingParameters";

    public override int Priority => 67;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        if (!property.HasMarkupExtension || property.MarkupExtension == null)
            return false;

        var binding = property.MarkupExtension;
        return binding.Parameters.ContainsKey("ValidatesOnDataErrors") ||
               binding.Parameters.ContainsKey("ValidatesOnExceptions") ||
               binding.Parameters.ContainsKey("NotifyOnValidationError");
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        if (property.MarkupExtension == null) return property;

        var binding = property.MarkupExtension;

        // Check what validation parameters are present
        var hasValidatesOnDataErrors = binding.Parameters.ContainsKey("ValidatesOnDataErrors");
        var hasValidatesOnExceptions = binding.Parameters.ContainsKey("ValidatesOnExceptions");
        var hasNotifyOnValidationError = binding.Parameters.ContainsKey("NotifyOnValidationError");

        if (hasValidatesOnDataErrors || hasValidatesOnExceptions || hasNotifyOnValidationError)
        {
            context.RecordTransformation(Name,
                "VALIDATION_BINDING_PARAMETERS",
                "WPF validation parameters detected. " +
                "These are converted to EnableDataValidation=True in Avalonia. " +
                "Ensure your ViewModel implements INotifyDataErrorInfo or uses DataAnnotations.");

            context.RecordTransformation(
                Name,
                "ValidationParameters",
                "Validation parameters transformed to EnableDataValidation");
        }

        return property;
    }
}

/// <summary>
/// Detects ValidationRule element declarations in XAML.
/// </summary>
public sealed class ValidationRuleElementRule : ElementTransformationRuleBase
{
    public override string Name => "DetectValidationRuleElements";

    public override int Priority => 66;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        var typeName = element.ElementType?.Name ?? "";
        return typeName.EndsWith("ValidationRule") ||
               typeName == "ExceptionValidationRule" ||
               typeName == "DataErrorValidationRule";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        var typeName = element.ElementType?.Name ?? "";

        context.RecordTransformation(Name,
            "VALIDATION_RULE_ELEMENT",
            $"ValidationRule element '{typeName}' detected in XAML. " +
            "Avalonia doesn't support ValidationRule elements in XAML. " +
            "Remove this element and implement validation using:\n" +
            "1. INotifyDataErrorInfo on ViewModel\n" +
            "2. DataAnnotations attributes on properties\n" +
            "3. Custom validation logic in property setters");

        context.RecordTransformation(
            Name,
            "ValidationRuleElement",
            $"{typeName} element requires migration to Avalonia validation approach");

        return element;
    }
}

/// <summary>
/// Transforms AdornedElementPlaceholder used in validation error templates.
/// </summary>
public sealed class AdornedElementPlaceholderRule : ElementTransformationRuleBase
{
    public override string Name => "TransformAdornedElementPlaceholder";

    public override int Priority => 65;

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        return element.ElementType?.Name == "AdornedElementPlaceholder";
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        context.RecordTransformation(Name,
            "ADORNED_ELEMENT_PLACEHOLDER",
            "AdornedElementPlaceholder is used in WPF validation error templates. " +
            "Avalonia doesn't have an exact equivalent. " +
            "Use ContentPresenter or design a custom error template. " +
            "Consider using :error pseudo-class styling instead of error templates.");

        context.RecordTransformation(
            Name,
            "AdornedElementPlaceholder",
            "AdornedElementPlaceholder requires custom implementation in Avalonia");

        return element;
    }
}
