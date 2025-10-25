using System.Xml.Linq;
using WpfToAvalonia.Core.Diagnostics;

namespace WpfToAvalonia.XamlParser.UnifiedAst;

/// <summary>
/// Represents a complete XAML document in the unified AST.
/// This is the root of the hybrid AST that combines XML, XamlX, and Roslyn information.
/// </summary>
public sealed class UnifiedXamlDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedXamlDocument"/> class.
    /// </summary>
    public UnifiedXamlDocument()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedXamlDocument"/> class from an XDocument.
    /// </summary>
    /// <param name="xmlDocument">The XML document to convert.</param>
    /// <param name="diagnostics">Diagnostic collector.</param>
    public UnifiedXamlDocument(XDocument xmlDocument, DiagnosticCollector? diagnostics = null)
    {
        XmlDocument = xmlDocument ?? throw new ArgumentNullException(nameof(xmlDocument));
        DiagnosticCollector = diagnostics;

        // Extract XML declaration info
        if (xmlDocument.Declaration != null)
        {
            HasXmlDeclaration = true;
            Encoding = xmlDocument.Declaration.Encoding ?? "UTF-8";
        }

        // Convert root element
        if (xmlDocument.Root != null)
        {
            Root = UnifiedXamlElement.FromXElement(xmlDocument.Root, this);

            // Extract namespace prefixes
            ExtractNamespacePrefixes(xmlDocument.Root);
        }
    }

    /// <summary>
    /// Extracts namespace prefixes from the root element.
    /// </summary>
    private void ExtractNamespacePrefixes(XElement rootElement)
    {
        foreach (var attr in rootElement.Attributes())
        {
            if (attr.IsNamespaceDeclaration)
            {
                var prefix = attr.Name.LocalName;
                var namespaceUri = attr.Value;

                // xmlns="..." has prefix "xmlns"
                if (attr.Name == XNamespace.Xmlns + "xmlns")
                    prefix = "";

                Symbols.RegisterNamespacePrefix(prefix, namespaceUri);
            }
        }
    }

    /// <summary>
    /// Gets or sets the root element of the document.
    /// </summary>
    public UnifiedXamlElement? Root { get; set; }

    /// <summary>
    /// Gets the comments that appear before the root element (e.g., file-level documentation).
    /// </summary>
    public List<UnifiedXamlComment> LeadingComments { get; } = new();

    /// <summary>
    /// Gets the comments that appear after the root element.
    /// </summary>
    public List<UnifiedXamlComment> TrailingComments { get; } = new();

    // === XML Layer ===

    /// <summary>
    /// Gets or sets the underlying XDocument (XML representation).
    /// </summary>
    public XDocument? XmlDocument { get; set; }

    /// <summary>
    /// Gets or sets the file path of the source XAML file.
    /// </summary>
    public string? FilePath { get; set; }

    // === XamlX Semantic Layer ===

    /// <summary>
    /// Gets or sets the XamlX semantic document.
    /// This will be populated when XamlX parsing is performed.
    /// </summary>
    public object? SemanticDocument { get; set; }

    // === Resources ===

    /// <summary>
    /// Gets the resource dictionary for this document.
    /// </summary>
    public ResourceDictionary Resources { get; } = new();

    // === Symbol Table ===

    /// <summary>
    /// Gets the symbol table containing all named elements and types.
    /// </summary>
    public SymbolTable Symbols { get; } = new();

    // === Diagnostics ===

    /// <summary>
    /// Gets the diagnostics collected during parsing and transformation.
    /// </summary>
    public List<TransformationDiagnostic> Diagnostics { get; } = new();

    /// <summary>
    /// Gets the diagnostic collector for this document.
    /// </summary>
    public DiagnosticCollector? DiagnosticCollector { get; set; }

    // === Metadata ===

    /// <summary>
    /// Gets or sets custom metadata for the document.
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

    /// <summary>
    /// Gets or sets the encoding of the original file.
    /// </summary>
    public string Encoding { get; set; } = "UTF-8";

    /// <summary>
    /// Gets or sets a value indicating whether the original file had an XML declaration.
    /// </summary>
    public bool HasXmlDeclaration { get; set; } = true;

    // === Helper Methods ===

    /// <summary>
    /// Gets all elements with x:Name attributes.
    /// </summary>
    public IEnumerable<UnifiedXamlElement> GetNamedElements()
    {
        if (Root == null)
        {
            yield break;
        }

        foreach (var element in Root.DescendantsAndSelf())
        {
            if (!string.IsNullOrEmpty(element.XName))
            {
                yield return element;
            }
        }
    }

    /// <summary>
    /// Gets all elements of a specific type.
    /// </summary>
    public IEnumerable<UnifiedXamlElement> GetElementsByType(string typeName)
    {
        if (Root == null)
        {
            yield break;
        }

        foreach (var element in Root.DescendantsAndSelf())
        {
            if (element.TypeName == typeName)
            {
                yield return element;
            }
        }
    }

    /// <summary>
    /// Finds an element by its x:Name.
    /// </summary>
    public UnifiedXamlElement? FindElementByName(string name)
    {
        return GetNamedElements().FirstOrDefault(e => e.XName == name);
    }

    /// <summary>
    /// Collects all diagnostics from the document tree.
    /// </summary>
    public List<TransformationDiagnostic> CollectAllDiagnostics()
    {
        var allDiagnostics = new List<TransformationDiagnostic>(Diagnostics);

        if (Root != null)
        {
            foreach (var element in Root.DescendantsAndSelf())
            {
                allDiagnostics.AddRange(element.Diagnostics);

                foreach (var property in element.Properties)
                {
                    allDiagnostics.AddRange(property.Diagnostics);
                }
            }
        }

        return allDiagnostics;
    }

    /// <summary>
    /// Gets metadata value or default.
    /// </summary>
    public T? GetMetadata<T>(string key, T? defaultValue = default)
    {
        if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// Sets metadata value.
    /// </summary>
    public void SetMetadata(string key, object value)
    {
        Metadata[key] = value;
    }
}

/// <summary>
/// Represents a resource dictionary.
/// </summary>
public sealed class ResourceDictionary
{
    /// <summary>
    /// Gets the resources keyed by their x:Key value.
    /// </summary>
    public Dictionary<string, object> Entries { get; } = new();

    /// <summary>
    /// Gets the merged resource dictionaries.
    /// </summary>
    public List<ResourceDictionary> MergedDictionaries { get; } = new();

    /// <summary>
    /// Tries to resolve a resource by key.
    /// </summary>
    public bool TryGetResource(string key, out object? resource)
    {
        if (Entries.TryGetValue(key, out resource))
        {
            return true;
        }

        // Check merged dictionaries
        foreach (var merged in MergedDictionaries)
        {
            if (merged.TryGetResource(key, out resource))
            {
                return true;
            }
        }

        resource = null;
        return false;
    }

    /// <summary>
    /// Adds a resource.
    /// </summary>
    public void AddResource(string key, object value)
    {
        Entries[key] = value;
    }
}

/// <summary>
/// Symbol table for named elements and types in the XAML document.
/// </summary>
public sealed class SymbolTable
{
    /// <summary>
    /// Gets the named elements (x:Name).
    /// Key = name, Value = element
    /// </summary>
    public Dictionary<string, UnifiedXamlElement> NamedElements { get; } = new();

    /// <summary>
    /// Gets the type usage information.
    /// Key = type name, Value = list of elements using that type
    /// </summary>
    public Dictionary<string, List<UnifiedXamlElement>> TypeUsage { get; } = new();

    /// <summary>
    /// Gets the namespace prefix mappings.
    /// Key = prefix, Value = namespace URI
    /// </summary>
    public Dictionary<string, string> NamespacePrefixes { get; } = new();

    /// <summary>
    /// Registers a named element.
    /// </summary>
    public void RegisterNamedElement(string name, UnifiedXamlElement element)
    {
        NamedElements[name] = element;
    }

    /// <summary>
    /// Registers type usage.
    /// </summary>
    public void RegisterTypeUsage(string typeName, UnifiedXamlElement element)
    {
        if (!TypeUsage.TryGetValue(typeName, out var list))
        {
            list = new List<UnifiedXamlElement>();
            TypeUsage[typeName] = list;
        }
        list.Add(element);
    }

    /// <summary>
    /// Registers a namespace prefix.
    /// </summary>
    public void RegisterNamespacePrefix(string prefix, string namespaceUri)
    {
        NamespacePrefixes[prefix] = namespaceUri;
    }
}
