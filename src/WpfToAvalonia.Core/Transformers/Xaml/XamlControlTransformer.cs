using System.Xml.Linq;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.Xaml;

/// <summary>
/// Transforms XAML control elements from WPF to Avalonia.
/// </summary>
public sealed class XamlControlTransformer : WpfToAvaloniaXamlVisitor
{
    private int _controlsChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="XamlControlTransformer"/> class.
    /// </summary>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <param name="mappingRepository">The mapping repository.</param>
    public XamlControlTransformer(
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Visits an element and transforms control types.
    /// </summary>
    public override XElement Visit(XElement element)
    {
        var localName = element.Name.LocalName;
        var namespaceName = element.Name.Namespace.NamespaceName;

        // Skip if not in a WPF-like namespace
        if (!IsWpfNamespace(namespaceName) && !string.IsNullOrEmpty(namespaceName))
        {
            return element;
        }

        // Try to find a type mapping for this control
        // We need to construct the full type name
        var fullTypeName = $"System.Windows.Controls.{localName}";
        var mapping = MappingRepository.FindTypeMapping(fullTypeName);

        // Try other common WPF namespaces
        if (mapping == null)
        {
            fullTypeName = $"System.Windows.{localName}";
            mapping = MappingRepository.FindTypeMapping(fullTypeName);
        }

        if (mapping == null)
        {
            fullTypeName = $"System.Windows.Shapes.{localName}";
            mapping = MappingRepository.FindTypeMapping(fullTypeName);
        }

        if (mapping != null && mapping.TypeNameChanged)
        {
            // Extract the simple name from the Avalonia type
            var avaloniaSimpleName = GetSimpleTypeName(mapping.AvaloniaTypeName);

            // Create new element with transformed name
            var newElement = new XElement(
                element.Name.Namespace + avaloniaSimpleName,
                element.Attributes(),
                element.Nodes());

            _controlsChanged++;

            Diagnostics.AddInfo(
                DiagnosticCodes.XamlControlTransformed,
                $"Transformed control: {localName} → {avaloniaSimpleName}",
                FilePath);

            if (mapping.RequiresManualReview)
            {
                Diagnostics.AddWarning(
                    DiagnosticCodes.XamlControlTransformed,
                    $"Control transformation requires manual review: {mapping.Notes}",
                    FilePath);
            }

            return newElement;
        }

        return element;
    }

    /// <summary>
    /// Gets the count of controls that were changed.
    /// </summary>
    public int ControlsChanged => _controlsChanged;

    private string GetSimpleTypeName(string fullTypeName)
    {
        var lastDotIndex = fullTypeName.LastIndexOf('.');
        return lastDotIndex >= 0 ? fullTypeName.Substring(lastDotIndex + 1) : fullTypeName;
    }
}
