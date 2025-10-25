using System.Xml.Linq;
using WpfToAvalonia.XamlParser.TypeSystem;

namespace WpfToAvalonia.XamlParser.UnifiedAst;

/// <summary>
/// Represents a XAML element (e.g., &lt;Button&gt;) in the unified AST.
/// Combines XML structure, XamlX semantic information, and formatting.
/// </summary>
public sealed class UnifiedXamlElement : UnifiedXamlNode
{
    // === XML Layer ===

    /// <summary>
    /// Gets the source XElement from the original XAML file.
    /// This is read-only and used for source position tracking and diagnostics only.
    /// DO NOT use for transformations - the AST (TypeReference, Properties, Children) is authoritative.
    /// After transformations, the serializer generates fresh XML from the AST.
    /// </summary>
    public XElement? SourceXmlElement { get; init; }

    /// <summary>
    /// Gets or sets the underlying XElement.
    /// </summary>
    [Obsolete("Use SourceXmlElement for read-only access to source XML. This property will be removed in v2.0.")]
    public XElement? XmlElement
    {
        get => SourceXmlElement;
        set => throw new InvalidOperationException(
            "XmlElement is read-only. Use SourceXmlElement for source tracking. " +
            "To create elements, use object initializers with SourceXmlElement.");
    }

    /// <summary>
    /// Gets or sets the XML namespace of this element.
    /// </summary>
    public XNamespace? XmlNamespace { get; set; }

    // === XamlX Semantic Layer ===

    /// <summary>
    /// Gets or sets the XamlX object node (when available).
    /// Represents the semantic AST node from XamlX parser.
    /// </summary>
    public object? SemanticObject { get; set; }

    /// <summary>
    /// Gets or sets the resolved CLR type of this element.
    /// Example: System.Windows.Controls.Button
    /// May be null if type resolution hasn't been performed or failed.
    /// </summary>
    public IXamlType? ElementType { get; set; }

    /// <summary>
    /// Gets the resolved element type, throwing if not resolved.
    /// Use this when type information is required for the operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if ElementType is null.</exception>
    public IXamlType ElementTypeOrThrow => ElementType
        ?? throw new InvalidOperationException(
            $"Element type not resolved for '{TypeReference?.FullName ?? GetFullTypeName()}' at {Location.FilePath}:{Location.Line}. " +
            "Ensure type resolution enrichment has been run with Required policy.");

    /// <summary>
    /// Gets a value indicating whether this element's type has been resolved.
    /// </summary>
    public bool IsTypeResolved => ElementType != null;

    // === Unified Structure ===

    /// <summary>
    /// Gets or sets the type name (local name without namespace).
    /// Example: "Button", "StackPanel"
    /// </summary>
    [Obsolete("Use TypeReference for type-safe access. This property will be removed in v2.0.")]
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace of the type.
    /// Example: "http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    /// </summary>
    [Obsolete("Use TypeReference for type-safe access. This property will be removed in v2.0.")]
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the strongly-typed type reference.
    /// This is the preferred way to access type information with compile-time validation.
    /// </summary>
    public QualifiedTypeName? TypeReference { get; set; }

    /// <summary>
    /// Gets the properties (attributes and property elements) of this element.
    /// </summary>
    public List<UnifiedXamlProperty> Properties { get; } = new();

    /// <summary>
    /// Gets the child elements.
    /// </summary>
    public List<UnifiedXamlElement> Children { get; } = new();

    /// <summary>
    /// Gets the comments associated with this element.
    /// Comments are preserved during transformation to maintain documentation.
    /// </summary>
    public List<UnifiedXamlComment> Comments { get; } = new();

    /// <summary>
    /// Gets or sets the text content of this element (if it's a simple text element).
    /// </summary>
    public string? TextContent { get; set; }

    // === Special XAML Directives ===

    /// <summary>
    /// Gets or sets the x:Name value.
    /// Used for code-behind field generation.
    /// </summary>
    public string? XName { get; set; }

    /// <summary>
    /// Gets or sets the x:Key value.
    /// Used in resource dictionaries.
    /// </summary>
    public string? XKey { get; set; }

    /// <summary>
    /// Gets or sets the x:Class value.
    /// Used to specify the code-behind class name.
    /// </summary>
    public string? XClass { get; set; }

    /// <summary>
    /// Gets or sets the x:FieldModifier value.
    /// Used to specify field access modifier (public, internal, etc.).
    /// </summary>
    public string? XFieldModifier { get; set; }

    /// <summary>
    /// Gets or sets the x:Shared value.
    /// Used in resource dictionaries to control resource sharing.
    /// </summary>
    public bool? XShared { get; set; }

    /// <summary>
    /// Gets or sets the x:TypeArguments value.
    /// Used to specify generic type arguments for generic types.
    /// Example: &lt;generic:GenericControl x:TypeArguments="sys:String"&gt;
    /// </summary>
    public string? XTypeArguments { get; set; }

    // === Content Properties ===

    /// <summary>
    /// Gets or sets a value indicating whether this element uses its content property implicitly.
    /// Example: Button has ContentProperty = "Content", so &lt;Button&gt;Click Me&lt;/Button&gt;
    /// implicitly sets the Content property.
    /// </summary>
    public bool UsesContentProperty { get; set; }

    /// <summary>
    /// Gets or sets the name of the content property.
    /// </summary>
    public string? ContentPropertyName { get; set; }

    // === Helper Methods ===

    /// <summary>
    /// Gets a property by name.
    /// </summary>
    public UnifiedXamlProperty? GetProperty(string propertyName)
    {
        return Properties.FirstOrDefault(p => p.PropertyName == propertyName);
    }

    /// <summary>
    /// Adds a property to this element.
    /// </summary>
    public void AddProperty(UnifiedXamlProperty property)
    {
        property.Parent = this;
        property.SiblingIndex = Properties.Count;
        Properties.Add(property);
    }

    /// <summary>
    /// Adds a child element.
    /// </summary>
    public void AddChild(UnifiedXamlElement child)
    {
        child.Parent = this;
        child.SiblingIndex = Children.Count;
        Children.Add(child);
    }

    /// <summary>
    /// Gets all descendant elements recursively.
    /// </summary>
    public IEnumerable<UnifiedXamlElement> Descendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.Descendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Gets all descendants and self.
    /// </summary>
    public IEnumerable<UnifiedXamlElement> DescendantsAndSelf()
    {
        yield return this;
        foreach (var descendant in Descendants())
        {
            yield return descendant;
        }
    }

    /// <summary>
    /// Gets the full type name including namespace.
    /// </summary>
    public string GetFullTypeName()
    {
        // Prefer TypeReference (new strongly-typed API)
        if (TypeReference != null)
        {
            return TypeReference.FullName;
        }

        // Fallback to ElementType
        if (ElementType != null)
        {
            return ElementType.FullName;
        }

        // Fallback to legacy string-based approach
        #pragma warning disable CS0618 // Type or member is obsolete
        if (!string.IsNullOrEmpty(Namespace))
        {
            // Try to construct from namespace
            // This is a heuristic for clr-namespace declarations
            if (Namespace.StartsWith("clr-namespace:", StringComparison.Ordinal))
            {
                var parts = Namespace.Substring("clr-namespace:".Length).Split(';');
                var clrNamespace = parts[0];
                return $"{clrNamespace}.{TypeName}";
            }
        }

        return TypeName;
        #pragma warning restore CS0618
    }

    public override string ToString()
    {
        #pragma warning disable CS0618 // Type or member is obsolete
        var typeName = TypeReference?.LocalName ?? TypeName;
        #pragma warning restore CS0618

        var result = $"<{typeName}";
        if (!string.IsNullOrEmpty(XName))
        {
            result += $" x:Name=\"{XName}\"";
        }
        result += ">";
        return result;
    }

    /// <summary>
    /// Creates a UnifiedXamlElement from an XElement.
    /// </summary>
    /// <param name="xElement">The XElement to convert.</param>
    /// <param name="document">The parent document.</param>
    /// <returns>A new UnifiedXamlElement.</returns>
    public static UnifiedXamlElement FromXElement(XElement xElement, UnifiedXamlDocument? document = null)
    {
        var element = new UnifiedXamlElement
        {
            SourceXmlElement = xElement,
            XmlNamespace = xElement.Name.Namespace,
            #pragma warning disable CS0618
            TypeName = xElement.Name.LocalName,
            Namespace = xElement.Name.NamespaceName
            #pragma warning restore CS0618
        };

        // Extract special XAML attributes (x:Name, x:Key, etc.)
        const string xamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
        element.XName = xElement.Attribute(XNamespace.Get(xamlNamespace) + "Name")?.Value;
        element.XKey = xElement.Attribute(XNamespace.Get(xamlNamespace) + "Key")?.Value;
        element.XClass = xElement.Attribute(XNamespace.Get(xamlNamespace) + "Class")?.Value;
        element.XFieldModifier = xElement.Attribute(XNamespace.Get(xamlNamespace) + "FieldModifier")?.Value;
        element.XTypeArguments = xElement.Attribute(XNamespace.Get(xamlNamespace) + "TypeArguments")?.Value;

        var xShared = xElement.Attribute(XNamespace.Get(xamlNamespace) + "Shared")?.Value;
        if (xShared != null && bool.TryParse(xShared, out var shared))
        {
            element.XShared = shared;
        }

        // Convert attributes to properties
        foreach (var attr in xElement.Attributes())
        {
            // Skip namespace declarations and special x: attributes we've already processed
            if (attr.IsNamespaceDeclaration ||
                (attr.Name.Namespace == XNamespace.Get(xamlNamespace) &&
                 (attr.Name.LocalName == "Name" || attr.Name.LocalName == "Key" ||
                  attr.Name.LocalName == "Class" || attr.Name.LocalName == "FieldModifier" ||
                  attr.Name.LocalName == "Shared")))
            {
                continue;
            }

            var property = new UnifiedXamlProperty
            {
                PropertyName = attr.Name.LocalName,
                Value = attr.Value,
                Parent = element
            };

            element.Properties.Add(property);
        }

        // Convert child elements
        foreach (var childElement in xElement.Elements())
        {
            var child = FromXElement(childElement, document);
            child.Parent = element;
            element.Children.Add(child);
        }

        // Extract text content if element has no child elements
        if (!xElement.HasElements && !string.IsNullOrWhiteSpace(xElement.Value))
        {
            element.TextContent = xElement.Value;
        }

        return element;
    }
}
