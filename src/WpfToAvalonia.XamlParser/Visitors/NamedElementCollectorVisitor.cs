using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Visitors;

/// <summary>
/// Visitor that collects all named elements (x:Name) from the Unified XAML AST.
/// </summary>
public sealed class NamedElementCollectorVisitor : UnifiedXamlCollectorVisitor<(string Name, UnifiedXamlElement Element)>
{
    /// <summary>
    /// Visits a XAML element and collects named elements.
    /// </summary>
    public override List<(string Name, UnifiedXamlElement Element)> VisitElement(UnifiedXamlElement element)
    {
        // Collect this element if it has a name
        if (!string.IsNullOrEmpty(element.XName))
        {
            Results.Add((element.XName, element));
        }

        // Continue visiting children
        return base.VisitElement(element);
    }
}

/// <summary>
/// Visitor that collects all elements of a specific type from the Unified XAML AST.
/// </summary>
public sealed class TypeCollectorVisitor : UnifiedXamlCollectorVisitor<UnifiedXamlElement>
{
    /// <summary>
    /// Gets or sets the type name to collect.
    /// </summary>
    public string? TypeName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use full type name matching.
    /// If false, matches only the type name without namespace.
    /// </summary>
    public bool UseFullTypeName { get; set; } = false;

    /// <summary>
    /// Visits a XAML element and collects elements of the specified type.
    /// </summary>
    public override List<UnifiedXamlElement> VisitElement(UnifiedXamlElement element)
    {
        if (string.IsNullOrEmpty(TypeName))
        {
            return base.VisitElement(element);
        }

        // Check if this element matches the type
        var matches = UseFullTypeName
            ? element.GetFullTypeName() == TypeName
            : element.TypeName == TypeName;

        if (matches)
        {
            Results.Add(element);
        }

        // Continue visiting children
        return base.VisitElement(element);
    }
}

/// <summary>
/// Visitor that collects all resource references from the Unified XAML AST.
/// </summary>
public sealed class ResourceReferenceCollectorVisitor : UnifiedXamlCollectorVisitor<(string ResourceKey, UnifiedXamlMarkupExtension Extension, UnifiedXamlProperty Property)>
{
    /// <summary>
    /// Visits a XAML property and collects resource references.
    /// </summary>
    public override List<(string ResourceKey, UnifiedXamlMarkupExtension Extension, UnifiedXamlProperty Property)> VisitProperty(UnifiedXamlProperty property)
    {
        // Check if this property has a resource reference
        if (property.MarkupExtension != null)
        {
            var extensionType = property.MarkupExtension.GetExtensionType();
            if (extensionType == MarkupExtensionType.StaticResource ||
                extensionType == MarkupExtensionType.DynamicResource)
            {
                var resourceKey = property.MarkupExtension.Resource?.ResourceKey;
                if (!string.IsNullOrEmpty(resourceKey))
                {
                    Results.Add((resourceKey, property.MarkupExtension, property));
                }
            }
        }

        // Continue visiting
        return base.VisitProperty(property);
    }
}

/// <summary>
/// Visitor that collects all binding expressions from the Unified XAML AST.
/// </summary>
public sealed class BindingCollectorVisitor : UnifiedXamlCollectorVisitor<(BindingExpression Binding, UnifiedXamlMarkupExtension Extension, UnifiedXamlProperty Property)>
{
    /// <summary>
    /// Visits a XAML property and collects binding expressions.
    /// </summary>
    public override List<(BindingExpression Binding, UnifiedXamlMarkupExtension Extension, UnifiedXamlProperty Property)> VisitProperty(UnifiedXamlProperty property)
    {
        // Check if this property has a binding
        if (property.MarkupExtension != null)
        {
            var extensionType = property.MarkupExtension.GetExtensionType();
            if (extensionType == MarkupExtensionType.Binding ||
                extensionType == MarkupExtensionType.TemplateBinding)
            {
                if (property.MarkupExtension.Binding != null)
                {
                    Results.Add((property.MarkupExtension.Binding, property.MarkupExtension, property));
                }
            }
        }

        // Continue visiting
        return base.VisitProperty(property);
    }
}

/// <summary>
/// Visitor that collects all attached properties from the Unified XAML AST.
/// </summary>
public sealed class AttachedPropertyCollectorVisitor : UnifiedXamlCollectorVisitor<UnifiedXamlProperty>
{
    /// <summary>
    /// Gets or sets the owner type to filter by (optional).
    /// If null, collects all attached properties.
    /// </summary>
    public string? OwnerType { get; set; }

    /// <summary>
    /// Visits a XAML property and collects attached properties.
    /// </summary>
    public override List<UnifiedXamlProperty> VisitProperty(UnifiedXamlProperty property)
    {
        // Check if this is an attached property
        if (property.IsAttached)
        {
            // Filter by owner type if specified
            if (string.IsNullOrEmpty(OwnerType) || property.AttachedOwnerType == OwnerType)
            {
                Results.Add(property);
            }
        }

        // Continue visiting
        return base.VisitProperty(property);
    }
}
