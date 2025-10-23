using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;
using WpfToAvalonia.Core.Diagnostics;

namespace WpfToAvalonia.XamlParser;

/// <summary>
/// Configures XamlX to understand WPF XAML language semantics.
/// Based on Avalonia's AvaloniaXamlIlLanguage but adapted for WPF.
/// </summary>
public static class WpfXamlIlLanguage
{
    /// <summary>
    /// WPF presentation namespace - the default XAML namespace for WPF controls.
    /// </summary>
    public const string WpfPresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>
    /// WPF XAML namespace - for x:Name, x:Key, x:Type, etc.
    /// </summary>
    public const string WpfXamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>
    /// WPF Blend namespace - for design-time properties like d:DesignWidth.
    /// </summary>
    public const string WpfBlendNamespace = "http://schemas.microsoft.com/expression/blend/2008";

    /// <summary>
    /// WPF markup compatibility namespace - for mc:Ignorable.
    /// </summary>
    public const string WpfMarkupCompatibilityNamespace = "http://schemas.openxmlformats.org/markup-compatibility/2006";

    /// <summary>
    /// Configures XamlX language mappings for WPF XAML.
    /// </summary>
    /// <param name="typeSystem">The WPF type system provider.</param>
    /// <param name="diagnostics">Diagnostic collector for warnings and errors.</param>
    /// <returns>Language type mappings configured for WPF.</returns>
    public static XamlLanguageTypeMappings Configure(
        IXamlTypeSystem typeSystem,
        DiagnosticCollector diagnostics)
    {
        var mappings = new XamlLanguageTypeMappings(typeSystem)
        {
            // WPF uses ISupportInitialize for BeginInit/EndInit pattern
            SupportInitialize = typeSystem.FindType("System.ComponentModel.ISupportInitialize"),

            // WPF markup extensions and service providers
            ProvideValueTarget = typeSystem.FindType("System.Windows.Markup.IProvideValueTarget"),
            RootObjectProvider = typeSystem.FindType("System.Windows.Markup.IRootObjectProvider"),
            UriContextProvider = typeSystem.FindType("System.Windows.Markup.IUriContext"),

            // Service provider (used by markup extensions)
            ServiceProvider = typeSystem.FindType("System.IServiceProvider") ?? typeSystem.GetType("System.IServiceProvider")
        };

        // Add WPF-specific attributes to collections
        var xmlnsAttr = typeSystem.FindType("System.Windows.Markup.XmlnsDefinitionAttribute");
        if (xmlnsAttr != null)
            mappings.XmlnsAttributes.Add(xmlnsAttr);

        var contentAttr = typeSystem.FindType("System.Windows.Markup.ContentPropertyAttribute");
        if (contentAttr != null)
            mappings.ContentAttributes.Add(contentAttr);

        var usableDuringInitAttr = typeSystem.FindType("System.Windows.Markup.UsableDuringInitializationAttribute");
        if (usableDuringInitAttr != null)
            mappings.UsableDuringInitializationAttributes.Add(usableDuringInitAttr);

        // Add custom attribute resolver for WPF type converters
        mappings.CustomAttributeResolver = new WpfAttributeResolver(typeSystem, mappings, diagnostics);

        return mappings;
    }

    /// <summary>
    /// Custom attribute resolver for WPF types.
    /// Provides type converter mappings for common WPF types.
    /// </summary>
    private class WpfAttributeResolver : IXamlCustomAttributeResolver
    {
        private readonly IXamlType _typeConverterAttribute;
        private readonly List<KeyValuePair<IXamlType, IXamlType>> _converters = new();
        private readonly DiagnosticCollector _diagnostics;

        public WpfAttributeResolver(
            IXamlTypeSystem typeSystem,
            XamlLanguageTypeMappings mappings,
            DiagnosticCollector diagnostics)
        {
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

            // Get the TypeConverter attribute type
            _typeConverterAttribute = mappings.TypeConverterAttributes.FirstOrDefault()
                ?? typeSystem.FindType("System.ComponentModel.TypeConverterAttribute");

            if (_typeConverterAttribute == null)
            {
                _diagnostics.AddWarning(
                    "WPF_TYPE_CONVERTER_ATTR_NOT_FOUND",
                    "TypeConverterAttribute not found in type system",
                    null);
                return;
            }

            // Register WPF type converters
            // These are the built-in WPF type converters that XamlX needs to know about

            void TryAddConverter(string typeName, string converterName)
            {
                var type = typeSystem.FindType(typeName);
                var converter = typeSystem.FindType(converterName);

                if (type != null && converter != null)
                {
                    _converters.Add(new KeyValuePair<IXamlType, IXamlType>(type, converter));
                }
                else
                {
                    if (type == null)
                        _diagnostics.AddInfo("WPF_TYPE_NOT_FOUND", $"Type '{typeName}' not found", null);
                    if (converter == null)
                        _diagnostics.AddInfo("WPF_CONVERTER_NOT_FOUND", $"Converter '{converterName}' not found", null);
                }
            }

            // Common WPF type converters
            TryAddConverter("System.Windows.Media.Brush", "System.Windows.Media.BrushConverter");
            TryAddConverter("System.Windows.Media.Color", "System.Windows.Media.ColorConverter");
            TryAddConverter("System.Windows.Thickness", "System.Windows.ThicknessConverter");
            TryAddConverter("System.Windows.CornerRadius", "System.Windows.CornerRadiusConverter");
            TryAddConverter("System.Windows.GridLength", "System.Windows.GridLengthConverter");
            TryAddConverter("System.Windows.Point", "System.Windows.PointConverter");
            TryAddConverter("System.Windows.Size", "System.Windows.SizeConverter");
            TryAddConverter("System.Windows.Rect", "System.Windows.RectConverter");
            TryAddConverter("System.Windows.Media.FontFamily", "System.Windows.Media.FontFamilyConverter");
            TryAddConverter("System.Windows.Media.ImageSource", "System.Windows.Media.ImageSourceConverter");
            TryAddConverter("System.Windows.Media.Transform", "System.Windows.Media.TransformConverter");
            TryAddConverter("System.Windows.Media.Geometry", "System.Windows.Media.GeometryConverter");
            TryAddConverter("System.Windows.Input.Cursor", "System.Windows.Input.CursorConverter");
            TryAddConverter("System.Windows.Duration", "System.Windows.DurationConverter");
            TryAddConverter("System.Windows.Media.Animation.KeyTime", "System.Windows.KeyTimeConverter");
            TryAddConverter("System.Globalization.CultureInfo", "System.ComponentModel.CultureInfoConverter");
            TryAddConverter("System.Uri", "System.UriTypeConverter");
        }

        private IXamlType? LookupConverter(IXamlType type)
        {
            foreach (var pair in _converters)
            {
                if (pair.Key.Equals(type))
                    return pair.Value;
            }
            return null;
        }

        public IXamlCustomAttribute? GetCustomAttribute(IXamlType type, IXamlType attributeType)
        {
            if (attributeType.Equals(_typeConverterAttribute))
            {
                var converter = LookupConverter(type);
                if (converter != null)
                {
                    return new ConstructedAttribute(
                        _typeConverterAttribute,
                        new List<object?> { converter },
                        null);
                }
            }

            return null;
        }

        public IXamlCustomAttribute? GetCustomAttribute(IXamlProperty property, IXamlType attributeType)
        {
            // Could implement property-specific type converter lookup here
            return null;
        }

        /// <summary>
        /// Constructed attribute for type converters.
        /// </summary>
        private class ConstructedAttribute : IXamlCustomAttribute
        {
            public IXamlType Type { get; }
            public List<object?> Parameters { get; }
            public Dictionary<string, object?> Properties { get; }

            public ConstructedAttribute(
                IXamlType type,
                List<object?>? parameters,
                Dictionary<string, object?>? properties)
            {
                Type = type;
                Parameters = parameters ?? new List<object?>();
                Properties = properties ?? new Dictionary<string, object?>();
            }

            public bool Equals(IXamlCustomAttribute? other) => false;
        }
    }

    /// <summary>
    /// Custom value converter for WPF-specific value conversions.
    /// Handles special cases like DependencyProperty lookups.
    /// </summary>
    public static bool CustomValueConverter(
        AstTransformationContext context,
        IXamlAstValueNode node,
        IReadOnlyList<IXamlCustomAttribute>? customAttributes,
        IXamlType type,
        [NotNullWhen(true)] out IXamlAstValueNode? result)
    {
        result = null;

        // Only process text nodes
        if (!(node is XamlAstTextNode textNode))
            return false;

        var text = textNode.Text;

        // Handle DependencyProperty lookup
        // In WPF XAML, you can reference dependency properties by name in certain contexts
        // For example: <Setter Property="Foreground" Value="Red"/>
        // The "Foreground" text needs to resolve to TextElement.ForegroundProperty
        if (type.FullName == "System.Windows.DependencyProperty")
        {
            // TODO: Implement DependencyProperty resolution
            // This requires finding the appropriate scope (Style, ControlTemplate, etc.)
            // and resolving the property name to a DependencyProperty field

            // For now, we'll leave this unimplemented and handle it in a later transformer
            return false;
        }

        // Handle other WPF-specific conversions as needed
        return false;
    }

    /// <summary>
    /// Gets the default XML namespace mappings for WPF XAML.
    /// </summary>
    public static Dictionary<string, string> GetDefaultNamespaceMappings()
    {
        return new Dictionary<string, string>
        {
            { "", WpfPresentationNamespace },  // Default namespace
            { "x", WpfXamlNamespace },
            { "d", WpfBlendNamespace },
            { "mc", WpfMarkupCompatibilityNamespace }
        };
    }
}
