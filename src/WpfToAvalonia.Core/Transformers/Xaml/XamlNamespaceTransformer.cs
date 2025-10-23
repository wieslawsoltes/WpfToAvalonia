using System.Xml.Linq;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.Xaml;

/// <summary>
/// Transforms XAML namespaces from WPF to Avalonia.
/// </summary>
public sealed class XamlNamespaceTransformer : WpfToAvaloniaXamlVisitor
{
    private const string WpfDefaultNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private const string AvaloniaDefaultNamespace = "https://github.com/avaloniaui";
    private const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    private int _namespacesChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="XamlNamespaceTransformer"/> class.
    /// </summary>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <param name="mappingRepository">The mapping repository.</param>
    public XamlNamespaceTransformer(
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Transforms the root element's namespaces.
    /// </summary>
    public XElement TransformRoot(XElement root)
    {
        if (root == null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        // Transform the default namespace
        if (root.Name.Namespace.NamespaceName == WpfDefaultNamespace)
        {
            var newRoot = new XElement(
                XName.Get(root.Name.LocalName, AvaloniaDefaultNamespace),
                root.Attributes(),
                root.Nodes());

            root = newRoot;
            _namespacesChanged++;

            Diagnostics.AddInfo(
                DiagnosticCodes.XamlNamespaceTransformed,
                $"Transformed default namespace: WPF → Avalonia",
                FilePath);
        }

        // Transform xmlns declarations
        var namespaceAttributes = root.Attributes()
            .Where(a => a.IsNamespaceDeclaration)
            .ToList();

        foreach (var nsAttr in namespaceAttributes)
        {
            var nsValue = nsAttr.Value;

            // Transform WPF default namespace
            if (nsValue == WpfDefaultNamespace)
            {
                nsAttr.Value = AvaloniaDefaultNamespace;
                _namespacesChanged++;

                Diagnostics.AddInfo(
                    DiagnosticCodes.XamlNamespaceTransformed,
                    $"Transformed xmlns declaration: WPF → Avalonia",
                    FilePath);
            }
            // Transform clr-namespace declarations
            else if (nsValue.StartsWith("clr-namespace:System.Windows", StringComparison.Ordinal))
            {
                var transformedNs = TransformClrNamespace(nsValue);
                if (transformedNs != nsValue)
                {
                    nsAttr.Value = transformedNs;
                    _namespacesChanged++;

                    Diagnostics.AddInfo(
                        DiagnosticCodes.XamlNamespaceTransformed,
                        $"Transformed clr-namespace: {nsValue} → {transformedNs}",
                        FilePath);
                }
            }
        }

        // Add Avalonia-specific namespaces if not present
        var hasAvaloniaNamespace = root.Attributes()
            .Any(a => a.Value == AvaloniaDefaultNamespace);

        if (!hasAvaloniaNamespace)
        {
            root.Add(new XAttribute("xmlns", AvaloniaDefaultNamespace));
        }

        // Ensure x: namespace is present
        var xNamespace = root.GetNamespaceOfPrefix("x");
        if (xNamespace == null || xNamespace.NamespaceName != XamlNamespace)
        {
            root.Add(new XAttribute(XNamespace.Xmlns + "x", XamlNamespace));
        }

        return root;
    }

    /// <summary>
    /// Visits an element (not used for namespace transformation, use TransformRoot instead).
    /// </summary>
    public override XElement Visit(XElement element)
    {
        return element;
    }

    /// <summary>
    /// Transforms a clr-namespace declaration from WPF to Avalonia.
    /// </summary>
    private string TransformClrNamespace(string clrNamespace)
    {
        // Extract the namespace part
        // Format: clr-namespace:System.Windows.Controls;assembly=PresentationFramework
        var parts = clrNamespace.Split(';');
        var namespacePart = parts[0];
        var assemblyPart = parts.Length > 1 ? parts[1] : null;

        if (namespacePart.StartsWith("clr-namespace:", StringComparison.Ordinal))
        {
            var ns = namespacePart.Substring("clr-namespace:".Length);
            var mapping = MappingRepository.FindNamespaceMapping(ns);

            if (mapping != null)
            {
                var newNamespace = $"clr-namespace:{mapping.AvaloniaNamespace}";

                // Update assembly reference if present
                if (assemblyPart != null && assemblyPart.Contains("PresentationFramework"))
                {
                    newNamespace += ";assembly=Avalonia.Controls";
                }
                else if (assemblyPart != null)
                {
                    newNamespace += ";" + assemblyPart;
                }

                return newNamespace;
            }
        }

        return clrNamespace;
    }

    /// <summary>
    /// Gets the count of namespaces that were changed.
    /// </summary>
    public int NamespacesChanged => _namespacesChanged;
}
