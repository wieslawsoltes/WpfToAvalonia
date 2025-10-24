using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Rewrites WPF-specific attributes to Avalonia equivalents.
/// Implements task 2.5.3.4: Map WPF-specific attributes
/// </summary>
/// <remarks>
/// Maps WPF attributes to Avalonia equivalents or provides guidance when no direct mapping exists.
///
/// Common WPF Attributes:
/// - TemplatePart: Maps to Avalonia's TemplatePart (same)
/// - ContentProperty: Maps to Avalonia's ContentAttribute
/// - DefaultEvent: Not used in Avalonia
/// - DefaultProperty: Not used in Avalonia
/// - TypeConverter: Maps to Avalonia's TypeConverter
/// - ValueSerializer: Not commonly used in Avalonia
/// - Localizability: Not in Avalonia (different localization approach)
/// - DesignTimeVisible: Maps to Avalonia's DesignOnly
/// - Bindable: Not needed in Avalonia (all properties are bindable)
/// - Category/Description: Used by designers, less common in Avalonia
///
/// Dependency Property Attributes:
/// - DependsOn: Not in Avalonia
/// - DesignerSerializationVisibility: Similar concept exists
/// - EditorBrowsable: Same in Avalonia
/// </remarks>
public sealed class WpfAttributeRewriter : WpfToAvaloniaRewriter
{
    private int _contentPropertyAttributes;
    private int _defaultEventAttributes;
    private int _bindableAttributes;
    private int _dependsOnAttributes;
    private int _localizabilityAttributes;
    private int _designTimeAttributes;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfAttributeRewriter"/> class.
    /// </summary>
    public WpfAttributeRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Gets the number of ContentProperty attributes detected.
    /// </summary>
    public int ContentPropertyAttributes => _contentPropertyAttributes;

    /// <summary>
    /// Gets the number of DefaultEvent attributes detected.
    /// </summary>
    public int DefaultEventAttributes => _defaultEventAttributes;

    /// <summary>
    /// Gets the number of Bindable attributes detected.
    /// </summary>
    public int BindableAttributes => _bindableAttributes;

    /// <summary>
    /// Gets the number of DependsOn attributes detected.
    /// </summary>
    public int DependsOnAttributes => _dependsOnAttributes;

    /// <summary>
    /// Gets the number of Localizability attributes detected.
    /// </summary>
    public int LocalizabilityAttributes => _localizabilityAttributes;

    /// <summary>
    /// Gets the number of design-time attributes detected.
    /// </summary>
    public int DesignTimeAttributes => _designTimeAttributes;

    /// <summary>
    /// Visits attribute lists to detect WPF-specific attributes.
    /// </summary>
    public override SyntaxNode? VisitAttributeList(AttributeListSyntax node)
    {
        foreach (var attribute in node.Attributes)
        {
            var symbolInfo = TryGetSymbolInfo(attribute);
            if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IMethodSymbol methodSymbol)
            {
                var attributeTypeName = methodSymbol.ContainingType?.ToDisplayString();
                var attributeName = methodSymbol.ContainingType?.Name ?? "";

                // ContentProperty attribute
                if (attributeName == "ContentPropertyAttribute")
                {
                    _contentPropertyAttributes++;

                    string? propertyName = GetAttributeParameter(attribute, 0);

                    Diagnostics.AddInfo(
                        "CONTENT_PROPERTY_NEEDS_UPDATE",
                        $"[ContentProperty(\"{propertyName}\")] needs to be updated for Avalonia. " +
                        $"Change to: [Content(\"{propertyName}\")] " +
                        $"Namespace: System.Windows.Markup → Avalonia.Metadata",
                        null);
                }
                // DefaultEvent attribute
                else if (attributeName == "DefaultEventAttribute")
                {
                    _defaultEventAttributes++;

                    Diagnostics.AddWarning(
                        "DEFAULT_EVENT_NOT_USED",
                        $"[DefaultEvent] attribute is not commonly used in Avalonia. " +
                        $"Remove this attribute. Avalonia designer doesn't use DefaultEvent.",
                        null);
                }
                // DefaultProperty attribute
                else if (attributeName == "DefaultPropertyAttribute")
                {
                    Diagnostics.AddWarning(
                        "DEFAULT_PROPERTY_NOT_USED",
                        $"[DefaultProperty] attribute is not used in Avalonia. " +
                        $"Remove this attribute.",
                        null);
                }
                // Bindable attribute
                else if (attributeName == "BindableAttribute")
                {
                    _bindableAttributes++;

                    Diagnostics.AddInfo(
                        "BINDABLE_NOT_NEEDED",
                        $"[Bindable] attribute is not needed in Avalonia. " +
                        $"All Avalonia properties support binding by default. Remove this attribute.",
                        null);
                }
                // DependsOn attribute (for dependency property ordering)
                else if (attributeName == "DependsOnAttribute")
                {
                    _dependsOnAttributes++;

                    string? dependencyProperty = GetAttributeParameter(attribute, 0);

                    Diagnostics.AddWarning(
                        "DEPENDS_ON_NOT_SUPPORTED",
                        $"[DependsOn(\"{dependencyProperty}\")] is not supported in Avalonia. " +
                        $"Avalonia's property system doesn't require explicit property ordering. " +
                        $"Remove this attribute and ensure property change handlers manage dependencies correctly.",
                        null);
                }
                // Localizability attribute
                else if (attributeName == "LocalizabilityAttribute")
                {
                    _localizabilityAttributes++;

                    Diagnostics.AddWarning(
                        "LOCALIZABILITY_NOT_SUPPORTED",
                        $"[Localizability] attribute is not supported in Avalonia. " +
                        $"Avalonia uses a different localization approach. " +
                        $"Remove this attribute and use Avalonia's localization system (IResourceProvider, etc.).",
                        null);
                }
                // DesignTimeVisible attribute
                else if (attributeName == "DesignTimeVisibleAttribute")
                {
                    _designTimeAttributes++;

                    Diagnostics.AddInfo(
                        "DESIGN_TIME_VISIBLE_CHECK",
                        $"[DesignTimeVisible] attribute - Avalonia has similar designer attributes. " +
                        $"Consider using [DesignOnly] attribute if needed.",
                        null);
                }
                // Category attribute (for property grid)
                else if (attributeName == "CategoryAttribute")
                {
                    Diagnostics.AddInfo(
                        "CATEGORY_ATTRIBUTE",
                        $"[Category] attribute is used by designers. " +
                        $"Avalonia designer may not use this. Keep if needed for other tools.",
                        null);
                }
                // Description attribute (for property grid)
                else if (attributeName == "DescriptionAttribute")
                {
                    Diagnostics.AddInfo(
                        "DESCRIPTION_ATTRIBUTE",
                        $"[Description] attribute is used by designers. " +
                        $"Avalonia designer may not use this. Keep if needed for other tools.",
                        null);
                }
                // TypeConverter attribute
                else if (attributeName == "TypeConverterAttribute")
                {
                    string? converterType = GetAttributeParameter(attribute, 0);

                    Diagnostics.AddInfo(
                        "TYPE_CONVERTER_CHECK",
                        $"[TypeConverter({converterType})] - Avalonia supports TypeConverter. " +
                        $"Ensure the converter is updated to work with Avalonia types. " +
                        $"WPF converters need to be rewritten for Avalonia.",
                        null);
                }
                // DesignerSerializationVisibility
                else if (attributeName == "DesignerSerializationVisibilityAttribute")
                {
                    Diagnostics.AddInfo(
                        "DESIGNER_SERIALIZATION_VISIBILITY",
                        $"[DesignerSerializationVisibility] - Similar concept exists in Avalonia. " +
                        $"Check if the attribute is still appropriate for Avalonia designer.",
                        null);
                }
                // MarkupExtensionReturnType
                else if (attributeName == "MarkupExtensionReturnTypeAttribute")
                {
                    Diagnostics.AddInfo(
                        "MARKUP_EXTENSION_RETURN_TYPE",
                        $"[MarkupExtensionReturnType] - Avalonia markup extensions work similarly. " +
                        $"Update namespace: System.Windows.Markup → Avalonia.Markup.Xaml",
                        null);
                }
                // ValueSerializer
                else if (attributeName == "ValueSerializerAttribute")
                {
                    Diagnostics.AddWarning(
                        "VALUE_SERIALIZER_NOT_COMMON",
                        $"[ValueSerializer] is not commonly used in Avalonia. " +
                        $"If custom serialization is needed, check Avalonia documentation for alternatives.",
                        null);
                }
            }
        }

        return base.VisitAttributeList(node);
    }

    /// <summary>
    /// Gets a parameter value from an attribute.
    /// </summary>
    private string? GetAttributeParameter(AttributeSyntax attribute, int index)
    {
        if (attribute.ArgumentList?.Arguments.Count > index)
        {
            var arg = attribute.ArgumentList.Arguments[index];
            return arg.Expression.ToString().Trim('"');
        }
        return null;
    }

    /// <summary>
    /// Tries to get symbol info, catching exceptions from stale semantic models.
    /// </summary>
    private SymbolInfo? TryGetSymbolInfo(SyntaxNode node)
    {
        try
        {
            return SemanticModel.GetSymbolInfo(node);
        }
        catch
        {
            // Node might not exist in semantic model after previous transformations
            return null;
        }
    }
}
