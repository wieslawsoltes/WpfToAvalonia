using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Enrichment;

/// <summary>
/// Exception thrown when type resolution fails and TypeResolutionPolicy is set to Required.
/// </summary>
public sealed class TypeResolutionException : Exception
{
    /// <summary>
    /// Initializes a new instance of TypeResolutionException for a single unresolved type.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="element">The element that failed type resolution.</param>
    public TypeResolutionException(string message, UnifiedXamlElement element)
        : base(message)
    {
        Element = element;
        UnresolvedTypes = new List<UnresolvedTypeInfo>
        {
            new UnresolvedTypeInfo
            {
                TypeName = element.TypeReference?.FullName ?? element.GetFullTypeName(),
                Location = element.Location,
                Element = element
            }
        };
    }

    /// <summary>
    /// Initializes a new instance of TypeResolutionException for multiple unresolved types.
    /// </summary>
    /// <param name="unresolvedTypes">The list of unresolved types.</param>
    public TypeResolutionException(IEnumerable<UnresolvedTypeInfo> unresolvedTypes)
        : base(BuildMessage(unresolvedTypes))
    {
        UnresolvedTypes = unresolvedTypes.ToList();
        Element = unresolvedTypes.FirstOrDefault()?.Element;
    }

    /// <summary>
    /// Gets the element that failed type resolution (first element if multiple).
    /// </summary>
    public UnifiedXamlElement? Element { get; }

    /// <summary>
    /// Gets the list of all unresolved types.
    /// </summary>
    public IReadOnlyList<UnresolvedTypeInfo> UnresolvedTypes { get; }

    private static string BuildMessage(IEnumerable<UnresolvedTypeInfo> unresolvedTypes)
    {
        var types = unresolvedTypes.ToList();
        if (types.Count == 0)
        {
            return "Type resolution failed";
        }

        if (types.Count == 1)
        {
            var info = types[0];
            return $"Cannot resolve type: {info.TypeName} at {info.Location.FilePath}:{info.Location.Line}";
        }

        return $"Cannot resolve {types.Count} types: {string.Join(", ", types.Take(5).Select(t => t.TypeName))}" +
               (types.Count > 5 ? $" and {types.Count - 5} more" : "");
    }
}

/// <summary>
/// Information about an unresolved type.
/// </summary>
public sealed class UnresolvedTypeInfo
{
    /// <summary>
    /// Gets or sets the full type name that could not be resolved.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets or sets the source location where the type was referenced.
    /// </summary>
    public required SourceLocation Location { get; init; }

    /// <summary>
    /// Gets or sets the element that references this unresolved type.
    /// </summary>
    public UnifiedXamlElement? Element { get; init; }

    /// <summary>
    /// Gets or sets the namespace where the type was expected to be found.
    /// </summary>
    public string? Namespace { get; init; }
}
