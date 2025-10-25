using WpfToAvalonia.XamlParser.TypeSystem;

namespace WpfToAvalonia.XamlParser.UnifiedAst;

/// <summary>
/// Represents a fully-qualified type name with optional namespace and type resolution.
/// This replaces string-based type representation with a structured, validated approach.
/// </summary>
public sealed record QualifiedTypeName
{
    /// <summary>
    /// Initializes a new instance of QualifiedTypeName.
    /// </summary>
    /// <param name="localName">The local type name (e.g., "Button", "Grid").</param>
    /// <param name="namespace">Optional namespace URI or clr-namespace string.</param>
    /// <param name="resolvedType">Optional resolved type information from type system.</param>
    public QualifiedTypeName(string localName, string? @namespace = null, IXamlType? resolvedType = null)
    {
        if (string.IsNullOrWhiteSpace(localName))
            throw new ArgumentException("Local name cannot be empty", nameof(localName));

        LocalName = localName;
        Namespace = @namespace;
        ResolvedType = resolvedType;
    }

    /// <summary>
    /// Gets the local name without namespace prefix (e.g., "Button").
    /// </summary>
    public string LocalName { get; }

    /// <summary>
    /// Gets the namespace URI or clr-namespace string.
    /// Examples:
    /// - "http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    /// - "clr-namespace:MyApp.Controls;assembly=MyApp"
    /// - null (for default namespace)
    /// </summary>
    public string? Namespace { get; }

    /// <summary>
    /// Gets the resolved type information from the type system.
    /// May be null if type resolution hasn't been performed or failed.
    /// </summary>
    public IXamlType? ResolvedType { get; init; }

    /// <summary>
    /// Gets the full type name including namespace.
    /// Uses ResolvedType.FullName if available, otherwise constructs from namespace.
    /// </summary>
    public string FullName => ResolvedType?.FullName ?? GetFullNameFromNamespace();

    /// <summary>
    /// Gets a value indicating whether this type has been resolved.
    /// </summary>
    public bool IsResolved => ResolvedType != null;

    /// <summary>
    /// Creates a new QualifiedTypeName with updated resolved type.
    /// </summary>
    public QualifiedTypeName WithResolvedType(IXamlType resolvedType)
    {
        if (resolvedType == null)
            throw new ArgumentNullException(nameof(resolvedType));
        return new QualifiedTypeName(LocalName, Namespace, resolvedType);
    }

    /// <summary>
    /// Creates a new QualifiedTypeName with a different namespace.
    /// </summary>
    public QualifiedTypeName WithNamespace(string? @namespace)
        => new QualifiedTypeName(LocalName, @namespace, ResolvedType);

    /// <summary>
    /// Creates a new QualifiedTypeName with a different local name.
    /// </summary>
    public QualifiedTypeName WithLocalName(string localName)
        => new QualifiedTypeName(localName, Namespace, ResolvedType);

    /// <summary>
    /// Parses a qualified type name from string format.
    /// Supports formats:
    /// - "Button" (local name only)
    /// - "local:MyControl" (with prefix)
    /// - "system:String" (with prefix)
    /// </summary>
    /// <param name="qualifiedName">The qualified name to parse.</param>
    /// <param name="namespacePrefixes">Optional dictionary mapping prefixes to namespace URIs.</param>
    public static QualifiedTypeName Parse(string qualifiedName, IDictionary<string, string>? namespacePrefixes = null)
    {
        if (string.IsNullOrWhiteSpace(qualifiedName))
            throw new ArgumentException("Qualified name cannot be empty", nameof(qualifiedName));

        var colonIndex = qualifiedName.IndexOf(':');

        if (colonIndex < 0)
        {
            // No prefix, just local name
            return new QualifiedTypeName(qualifiedName);
        }

        var prefix = qualifiedName.Substring(0, colonIndex);
        var localName = qualifiedName.Substring(colonIndex + 1);

        if (string.IsNullOrWhiteSpace(localName))
            throw new ArgumentException($"Invalid qualified name: '{qualifiedName}' - local name is empty", nameof(qualifiedName));

        string? @namespace = null;
        if (namespacePrefixes?.TryGetValue(prefix, out @namespace) == true)
        {
            return new QualifiedTypeName(localName, @namespace);
        }

        // Unknown prefix - store as-is for later resolution
        return new QualifiedTypeName(localName, prefix + ":");
    }

    /// <summary>
    /// Tries to parse a qualified type name from string format.
    /// </summary>
    public static bool TryParse(string qualifiedName, out QualifiedTypeName result, IDictionary<string, string>? namespacePrefixes = null)
    {
        try
        {
            result = Parse(qualifiedName, namespacePrefixes);
            return true;
        }
        catch
        {
            result = null!;
            return false;
        }
    }

    /// <summary>
    /// Gets the full type name from namespace information.
    /// </summary>
    private string GetFullNameFromNamespace()
    {
        if (string.IsNullOrEmpty(Namespace))
            return LocalName;

        // Parse clr-namespace format: "clr-namespace:MyNamespace;assembly=MyAssembly"
        if (Namespace.StartsWith("clr-namespace:", StringComparison.Ordinal))
        {
            var clrPart = Namespace.Substring("clr-namespace:".Length);
            var parts = clrPart.Split(';');
            var ns = parts[0];

            if (!string.IsNullOrEmpty(ns))
            {
                return $"{ns}.{LocalName}";
            }
        }

        // For standard XML namespaces, use local name only
        // (e.g., http://schemas.microsoft.com/winfx/2006/xaml/presentation)
        return LocalName;
    }

    /// <summary>
    /// Checks if this type matches another type name.
    /// </summary>
    public bool Matches(string localName, string? @namespace = null)
    {
        if (!string.Equals(LocalName, localName, StringComparison.Ordinal))
            return false;

        if (@namespace == null)
            return true;

        return string.Equals(Namespace, @namespace, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks if this type matches a full name.
    /// </summary>
    public bool MatchesFullName(string fullName)
    {
        return string.Equals(FullName, fullName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets a string representation for debugging.
    /// </summary>
    public override string ToString()
    {
        if (ResolvedType != null)
            return $"{LocalName} ({ResolvedType.FullName})";

        if (!string.IsNullOrEmpty(Namespace))
            return $"{LocalName} [{Namespace}]";

        return LocalName;
    }

    /// <summary>
    /// Creates a QualifiedTypeName for a well-known WPF type.
    /// </summary>
    public static QualifiedTypeName ForWpfType(string localName)
        => new QualifiedTypeName(localName, "http://schemas.microsoft.com/winfx/2006/xaml/presentation");

    /// <summary>
    /// Creates a QualifiedTypeName for a well-known Avalonia type.
    /// </summary>
    public static QualifiedTypeName ForAvaloniaType(string localName)
        => new QualifiedTypeName(localName, "https://github.com/avaloniaui");

    /// <summary>
    /// Creates a QualifiedTypeName for a XAML directive (x: namespace).
    /// </summary>
    public static QualifiedTypeName ForXamlDirective(string localName)
        => new QualifiedTypeName(localName, "http://schemas.microsoft.com/winfx/2006/xaml");

    /// <summary>
    /// Creates a QualifiedTypeName from CLR type information.
    /// </summary>
    public static QualifiedTypeName FromClrType(string fullTypeName)
    {
        var lastDotIndex = fullTypeName.LastIndexOf('.');
        if (lastDotIndex > 0 && lastDotIndex < fullTypeName.Length - 1)
        {
            var ns = fullTypeName.Substring(0, lastDotIndex);
            var localName = fullTypeName.Substring(lastDotIndex + 1);
            return new QualifiedTypeName(localName, $"clr-namespace:{ns}");
        }

        // No namespace - just use local name
        return new QualifiedTypeName(fullTypeName);
    }
}
