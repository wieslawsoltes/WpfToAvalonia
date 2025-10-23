using System.Xml.Linq;

namespace WpfToAvalonia.Core.Visitors;

/// <summary>
/// Defines a visitor for traversing and transforming XAML documents.
/// </summary>
public interface IXamlVisitor
{
    /// <summary>
    /// Visits an XML element and returns a potentially transformed element.
    /// </summary>
    /// <param name="element">The XML element to visit.</param>
    /// <returns>The transformed element, or the original if no transformation occurred.</returns>
    XElement Visit(XElement element);
}

/// <summary>
/// Base class for XAML transformers that convert WPF XAML to Avalonia XAML.
/// </summary>
public abstract class WpfToAvaloniaXamlVisitor : IXamlVisitor
{
    /// <summary>
    /// Gets the diagnostic collector for reporting issues.
    /// </summary>
    protected Core.Diagnostics.DiagnosticCollector Diagnostics { get; }

    /// <summary>
    /// Gets the mapping repository for looking up WPF to Avalonia mappings.
    /// </summary>
    protected Mappings.IMappingRepository MappingRepository { get; }

    /// <summary>
    /// Gets or sets the file path of the XAML file being transformed.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfToAvaloniaXamlVisitor"/> class.
    /// </summary>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <param name="mappingRepository">The mapping repository.</param>
    protected WpfToAvaloniaXamlVisitor(
        Core.Diagnostics.DiagnosticCollector diagnostics,
        Mappings.IMappingRepository mappingRepository)
    {
        Diagnostics = diagnostics;
        MappingRepository = mappingRepository;
    }

    /// <summary>
    /// Visits an XML element and returns a potentially transformed element.
    /// </summary>
    /// <param name="element">The XML element to visit.</param>
    /// <returns>The transformed element.</returns>
    public abstract XElement Visit(XElement element);

    /// <summary>
    /// Recursively visits all descendant elements.
    /// </summary>
    /// <param name="element">The root element to start from.</param>
    /// <returns>The transformed element tree.</returns>
    public XElement VisitRecursive(XElement element)
    {
        var transformed = Visit(element);

        // Transform all child elements recursively
        var children = transformed.Elements().ToList();
        foreach (var child in children)
        {
            child.ReplaceWith(VisitRecursive(child));
        }

        return transformed;
    }

    /// <summary>
    /// Determines whether a namespace is a WPF namespace.
    /// </summary>
    /// <param name="namespaceUri">The namespace URI to check.</param>
    /// <returns>True if this is a WPF namespace, otherwise false.</returns>
    protected bool IsWpfNamespace(string namespaceUri)
    {
        if (string.IsNullOrEmpty(namespaceUri))
            return false;

        return namespaceUri.Contains("schemas.microsoft.com/winfx") ||
               namespaceUri.StartsWith("clr-namespace:System.Windows", StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the local name of an element (without namespace prefix).
    /// </summary>
    /// <param name="element">The XML element.</param>
    /// <returns>The local name.</returns>
    protected string GetLocalName(XElement element)
    {
        return element.Name.LocalName;
    }

    /// <summary>
    /// Gets the namespace prefix from an element name.
    /// </summary>
    /// <param name="element">The XML element.</param>
    /// <returns>The namespace prefix, or empty string if no prefix.</returns>
    protected string GetPrefix(XElement element)
    {
        return element.GetPrefixOfNamespace(element.Name.Namespace) ?? string.Empty;
    }
}
