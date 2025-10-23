using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation;

/// <summary>
/// Base class for transformation rules.
/// </summary>
public abstract class TransformationRuleBase : ITransformationRule
{
    /// <summary>
    /// Gets the name of this transformation rule.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the priority of this rule. Higher priority rules run first.
    /// Default is 0.
    /// </summary>
    public virtual int Priority => 0;

    /// <summary>
    /// Determines if this rule can transform the given node.
    /// </summary>
    public abstract bool CanTransform(UnifiedXamlNode node);

    /// <summary>
    /// Transforms the given node.
    /// </summary>
    public abstract UnifiedXamlNode? Transform(UnifiedXamlNode node, TransformationContext context);
}

/// <summary>
/// Base class for element transformation rules.
/// </summary>
public abstract class ElementTransformationRuleBase : TransformationRuleBase, IElementTransformationRule
{
    public override bool CanTransform(UnifiedXamlNode node)
    {
        return node is UnifiedXamlElement element && CanTransformElement(element);
    }

    public override UnifiedXamlNode? Transform(UnifiedXamlNode node, TransformationContext context)
    {
        if (node is UnifiedXamlElement element)
        {
            return TransformElement(element, context);
        }
        return node;
    }

    /// <summary>
    /// Determines if this rule can transform the given element.
    /// </summary>
    public abstract bool CanTransformElement(UnifiedXamlElement element);

    /// <summary>
    /// Transforms the given element.
    /// </summary>
    public abstract UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context);
}

/// <summary>
/// Base class for property transformation rules.
/// </summary>
public abstract class PropertyTransformationRuleBase : TransformationRuleBase, IPropertyTransformationRule
{
    public override bool CanTransform(UnifiedXamlNode node)
    {
        return node is UnifiedXamlProperty property && CanTransformProperty(property);
    }

    public override UnifiedXamlNode? Transform(UnifiedXamlNode node, TransformationContext context)
    {
        if (node is UnifiedXamlProperty property)
        {
            return TransformProperty(property, context);
        }
        return node;
    }

    /// <summary>
    /// Determines if this rule can transform the given property.
    /// </summary>
    public abstract bool CanTransformProperty(UnifiedXamlProperty property);

    /// <summary>
    /// Transforms the given property.
    /// </summary>
    public abstract UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context);
}

/// <summary>
/// Base class for simple type-based element transformations.
/// Transforms elements based on their type name.
/// </summary>
public abstract class SimpleTypeTransformationRule : ElementTransformationRuleBase
{
    /// <summary>
    /// Gets the WPF type name to match (e.g., "Window", "Button").
    /// </summary>
    protected abstract string WpfTypeName { get; }

    /// <summary>
    /// Gets the Avalonia type name to use (e.g., "Window", "Button").
    /// If null, uses the same name as WPF.
    /// </summary>
    protected virtual string? AvaloniaTypeName => null;

    /// <summary>
    /// Gets the WPF namespace to match (optional).
    /// If null, matches any namespace.
    /// </summary>
    protected virtual string? WpfNamespace => null;

    /// <summary>
    /// Gets the Avalonia namespace to use.
    /// Default is Avalonia's main namespace.
    /// </summary>
    protected virtual string AvaloniaNamespace => "https://github.com/avaloniaui";

    public override string Name => $"Transform{WpfTypeName}ToAvalonia";

    public override bool CanTransformElement(UnifiedXamlElement element)
    {
        if (element.TypeName != WpfTypeName)
        {
            return false;
        }

        if (WpfNamespace != null && element.Namespace != WpfNamespace)
        {
            return false;
        }

        return true;
    }

    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element, TransformationContext context)
    {
        // Update type name if different
        var targetTypeName = AvaloniaTypeName ?? WpfTypeName;
        if (element.TypeName != targetTypeName)
        {
            element.TypeName = targetTypeName;
        }

        // Update namespace
        element.Namespace = AvaloniaNamespace;

        // Allow derived classes to perform additional transformations
        TransformElementProperties(element, context);

        context.RecordTransformation(Name, "Element", $"{WpfTypeName} → {targetTypeName}");

        return element;
    }

    /// <summary>
    /// Override this to transform element properties.
    /// </summary>
    protected virtual void TransformElementProperties(UnifiedXamlElement element, TransformationContext context)
    {
        // Default: no additional transformations
    }
}

/// <summary>
/// Base class for simple property rename transformations.
/// </summary>
public abstract class PropertyRenameRule : PropertyTransformationRuleBase
{
    /// <summary>
    /// Gets the WPF property name to match.
    /// </summary>
    protected abstract string WpfPropertyName { get; }

    /// <summary>
    /// Gets the Avalonia property name to use.
    /// </summary>
    protected abstract string AvaloniaPropertyName { get; }

    /// <summary>
    /// Gets the element type to match (optional).
    /// If null, applies to all elements.
    /// </summary>
    protected virtual string? ElementType => null;

    public override string Name => $"Rename{WpfPropertyName}To{AvaloniaPropertyName}";

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        if (property.PropertyName != WpfPropertyName)
        {
            return false;
        }

        if (ElementType != null && property.Parent is UnifiedXamlElement parent)
        {
            if (parent.TypeName != ElementType)
            {
                return false;
            }
        }

        return true;
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        property.PropertyName = AvaloniaPropertyName;

        // Allow derived classes to transform property value
        TransformPropertyValue(property, context);

        context.RecordTransformation(Name, "Property", $"{WpfPropertyName} → {AvaloniaPropertyName}");

        return property;
    }

    /// <summary>
    /// Override this to transform the property value.
    /// </summary>
    protected virtual void TransformPropertyValue(UnifiedXamlProperty property, TransformationContext context)
    {
        // Default: no value transformation
    }
}

/// <summary>
/// Base class for markup extension transformation rules.
/// </summary>
public abstract class MarkupExtensionTransformationRuleBase : TransformationRuleBase, IMarkupExtensionTransformationRule
{
    public override bool CanTransform(UnifiedXamlNode node)
    {
        return node is UnifiedXamlMarkupExtension markupExtension && CanTransformMarkupExtension(markupExtension);
    }

    public override UnifiedXamlNode? Transform(UnifiedXamlNode node, TransformationContext context)
    {
        if (node is UnifiedXamlMarkupExtension markupExtension)
        {
            return TransformMarkupExtension(markupExtension, context);
        }
        return node;
    }

    /// <summary>
    /// Determines if this rule can transform the given markup extension.
    /// </summary>
    public abstract bool CanTransformMarkupExtension(UnifiedXamlMarkupExtension markupExtension);

    /// <summary>
    /// Transforms the given markup extension.
    /// </summary>
    public abstract UnifiedXamlMarkupExtension? TransformMarkupExtension(UnifiedXamlMarkupExtension markupExtension, TransformationContext context);
}
