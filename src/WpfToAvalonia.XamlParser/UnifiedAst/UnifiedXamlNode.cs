using System.Xml.Linq;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.TypeSystem;

namespace WpfToAvalonia.XamlParser.UnifiedAst;

/// <summary>
/// Base class for all nodes in the unified XAML AST.
/// Combines XML representation, XamlX semantic information, and Roslyn code-behind symbols.
/// </summary>
public abstract class UnifiedXamlNode
{
    // === XML Layer ===

    /// <summary>
    /// Gets or sets the underlying XML node (XElement, XAttribute, etc.).
    /// </summary>
    public XNode? XmlNode { get; set; }

    /// <summary>
    /// Gets or sets the XPath-like identifier for this node in the XML tree.
    /// Example: "/Window[0]/StackPanel[0]/Button[0]"
    /// </summary>
    public string? XmlPath { get; set; }

    // === XamlX Semantic Layer ===

    /// <summary>
    /// Gets or sets the XamlX semantic AST node.
    /// This will be populated when XamlX parsing is performed.
    /// </summary>
    public object? SemanticNode { get; set; }

    /// <summary>
    /// Gets or sets the resolved type information from XamlX.
    /// This represents the CLR type of the XAML element or property.
    /// </summary>
    public IXamlType? ResolvedType { get; set; }

    // === Roslyn Layer (Code-Behind) ===

    /// <summary>
    /// Gets or sets the Roslyn symbol associated with this node (for code-behind synchronization).
    /// For example, an x:Name element maps to a field in the code-behind class.
    /// </summary>
    public object? CodeBehindSymbol { get; set; }

    // === Metadata ===

    /// <summary>
    /// Gets or sets the source location of this node in the original XAML file.
    /// </summary>
    public SourceLocation Location { get; set; } = new();

    /// <summary>
    /// Gets the formatting hints to preserve the original XAML appearance.
    /// </summary>
    public FormattingHints Formatting { get; set; } = new();

    /// <summary>
    /// Gets the diagnostics associated with this node.
    /// </summary>
    public List<TransformationDiagnostic> Diagnostics { get; } = new();

    // === Transformation Tracking ===

    /// <summary>
    /// Gets or sets the transformation state of this node.
    /// </summary>
    public TransformationState State { get; set; } = TransformationState.Unanalyzed;

    /// <summary>
    /// Gets or sets custom metadata attached to this node.
    /// Used for storing transformation-specific information.
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();

    // === Navigation ===

    /// <summary>
    /// Gets or sets the parent node.
    /// </summary>
    public UnifiedXamlNode? Parent { get; set; }

    /// <summary>
    /// Gets or sets the index of this node among its siblings.
    /// </summary>
    public int SiblingIndex { get; set; }

    // === Helper Methods ===

    /// <summary>
    /// Adds a diagnostic message to this node.
    /// </summary>
    public void AddDiagnostic(string code, string message, DiagnosticSeverity severity)
    {
        Diagnostics.Add(new TransformationDiagnostic
        {
            Code = code,
            Message = message,
            Severity = severity,
            FilePath = Location.FilePath,
            Line = Location.Line,
            Column = Location.Column
        });
    }

    /// <summary>
    /// Gets all ancestor nodes.
    /// </summary>
    public IEnumerable<UnifiedXamlNode> Ancestors()
    {
        var current = Parent;
        while (current != null)
        {
            yield return current;
            current = current.Parent;
        }
    }

    /// <summary>
    /// Gets a metadata value or default.
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
    /// Sets a metadata value.
    /// </summary>
    public void SetMetadata(string key, object value)
    {
        Metadata[key] = value;
    }
}

// IXamlType interface is now defined in TypeSystem/IXamlTypeSystem.cs
