using System.Xml.Linq;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.Xaml;

/// <summary>
/// Transforms XAML property attributes from WPF to Avalonia.
/// </summary>
public sealed class XamlPropertyTransformer : WpfToAvaloniaXamlVisitor
{
    private int _propertiesChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="XamlPropertyTransformer"/> class.
    /// </summary>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <param name="mappingRepository">The mapping repository.</param>
    public XamlPropertyTransformer(
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Visits an element and transforms its properties.
    /// </summary>
    public override XElement Visit(XElement element)
    {
        var localName = element.Name.LocalName;
        var namespaceName = element.Name.Namespace.NamespaceName;

        // Skip if not in a WPF or Avalonia namespace
        if (!IsWpfNamespace(namespaceName) && !IsAvaloniaNamespace(namespaceName))
        {
            return element;
        }

        // Try to determine the owner type for property mappings
        var ownerTypeName = GetOwnerTypeName(localName, namespaceName);

        // Transform attributes
        var newAttributes = new List<XAttribute>();
        foreach (var attr in element.Attributes())
        {
            // Skip namespace declarations
            if (attr.IsNamespaceDeclaration)
            {
                newAttributes.Add(attr);
                continue;
            }

            var transformedAttr = TransformAttribute(attr, ownerTypeName);
            newAttributes.Add(transformedAttr);
        }

        // Create new element with transformed attributes
        var newElement = new XElement(
            element.Name,
            newAttributes,
            element.Nodes());

        return newElement;
    }

    /// <summary>
    /// Transforms an attribute based on property mappings.
    /// </summary>
    private XAttribute TransformAttribute(XAttribute attribute, string ownerTypeName)
    {
        var attrName = attribute.Name.LocalName;
        var attrValue = attribute.Value;

        // Check if this is an attached property (contains a dot)
        if (attrName.Contains('.'))
        {
            return TransformAttachedProperty(attribute);
        }

        // Look up property mapping
        var mapping = MappingRepository.FindPropertyMapping(attrName, ownerTypeName);

        if (mapping != null)
        {
            var newValue = attrValue;

            // Handle value transformation based on mapping
            if (mapping.TypeChanged && !string.IsNullOrEmpty(mapping.ValueConversionRule))
            {
                newValue = TransformPropertyValue(attrValue, mapping);
            }

            var newAttr = new XAttribute(attribute.Name.Namespace + mapping.AvaloniaPropertyName, newValue);
            _propertiesChanged++;

            Diagnostics.AddInfo(
                DiagnosticCodes.XamlPropertyTransformed,
                $"Transformed property: {attrName} → {mapping.AvaloniaPropertyName}",
                FilePath);

            if (mapping.TypeChanged)
            {
                Diagnostics.AddWarning(
                    DiagnosticCodes.XamlPropertyTypeChanged,
                    $"Property type changed: {mapping.WpfPropertyType} → {mapping.AvaloniaPropertyType}. " +
                    $"Value transformed: '{attrValue}' → '{newValue}'",
                    FilePath);
            }

            if (mapping.RequiresManualReview)
            {
                Diagnostics.AddWarning(
                    DiagnosticCodes.XamlPropertyTransformed,
                    $"Property transformation requires manual review: {mapping.Notes}",
                    FilePath);
            }

            return newAttr;
        }

        return attribute;
    }

    /// <summary>
    /// Transforms an attached property attribute.
    /// </summary>
    private XAttribute TransformAttachedProperty(XAttribute attribute)
    {
        var attrName = attribute.Name.LocalName;
        var parts = attrName.Split('.');

        if (parts.Length != 2)
        {
            return attribute;
        }

        var ownerType = parts[0];
        var propertyName = parts[1];

        // Try to find a type mapping for the owner type
        var fullTypeName = $"System.Windows.Controls.{ownerType}";
        var typeMapping = MappingRepository.FindTypeMapping(fullTypeName);

        if (typeMapping == null)
        {
            fullTypeName = $"System.Windows.{ownerType}";
            typeMapping = MappingRepository.FindTypeMapping(fullTypeName);
        }

        if (typeMapping != null && typeMapping.TypeNameChanged)
        {
            var avaloniaSimpleName = GetSimpleTypeName(typeMapping.AvaloniaTypeName);
            var newAttrName = $"{avaloniaSimpleName}.{propertyName}";
            var newAttr = new XAttribute(attribute.Name.Namespace + newAttrName, attribute.Value);

            _propertiesChanged++;

            Diagnostics.AddInfo(
                DiagnosticCodes.XamlPropertyTransformed,
                $"Transformed attached property: {attrName} → {newAttrName}",
                FilePath);

            return newAttr;
        }

        return attribute;
    }

    /// <summary>
    /// Transforms a property value based on the conversion rule.
    /// </summary>
    private string TransformPropertyValue(string value, PropertyMapping mapping)
    {
        if (string.IsNullOrEmpty(mapping.ValueConversionRule))
        {
            return value;
        }

        // Handle Visibility → IsVisible conversion
        if (mapping.ValueConversionRule.Contains("Visibility"))
        {
            return value.ToLowerInvariant() switch
            {
                "visible" => "True",
                "collapsed" => "False",
                "hidden" => "False",
                _ => value
            };
        }

        // Add more conversion rules as needed
        return value;
    }

    /// <summary>
    /// Gets the owner type name for property lookups.
    /// </summary>
    private string GetOwnerTypeName(string localName, string namespaceName)
    {
        // Try to construct the full type name
        if (IsWpfNamespace(namespaceName))
        {
            return $"System.Windows.Controls.{localName}";
        }

        if (IsAvaloniaNamespace(namespaceName))
        {
            return $"Avalonia.Controls.{localName}";
        }

        return localName;
    }

    /// <summary>
    /// Checks if the namespace is an Avalonia namespace.
    /// </summary>
    private bool IsAvaloniaNamespace(string namespaceName)
    {
        if (string.IsNullOrEmpty(namespaceName))
        {
            return false;
        }

        return namespaceName.Contains("avaloniaui", StringComparison.OrdinalIgnoreCase) ||
               namespaceName.StartsWith("Avalonia", StringComparison.Ordinal);
    }

    private string GetSimpleTypeName(string fullTypeName)
    {
        var lastDotIndex = fullTypeName.LastIndexOf('.');
        return lastDotIndex >= 0 ? fullTypeName.Substring(lastDotIndex + 1) : fullTypeName;
    }

    /// <summary>
    /// Gets the count of properties that were changed.
    /// </summary>
    public int PropertiesChanged => _propertiesChanged;
}
