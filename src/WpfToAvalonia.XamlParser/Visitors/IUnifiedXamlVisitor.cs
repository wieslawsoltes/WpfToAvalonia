using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Visitors;

/// <summary>
/// Interface for visitors that traverse the Unified XAML AST.
/// Implements the Visitor pattern for processing XAML nodes.
/// </summary>
public interface IUnifiedXamlVisitor
{
    /// <summary>
    /// Visits a XAML document.
    /// </summary>
    void VisitDocument(UnifiedXamlDocument document);

    /// <summary>
    /// Visits a XAML element.
    /// </summary>
    void VisitElement(UnifiedXamlElement element);

    /// <summary>
    /// Visits a XAML property.
    /// </summary>
    void VisitProperty(UnifiedXamlProperty property);

    /// <summary>
    /// Visits a markup extension.
    /// </summary>
    void VisitMarkupExtension(UnifiedXamlMarkupExtension markupExtension);

    /// <summary>
    /// Visits a comment.
    /// </summary>
    void VisitComment(UnifiedXamlComment comment);
}

/// <summary>
/// Interface for visitors that can transform the Unified XAML AST.
/// Returns transformed nodes instead of void.
/// </summary>
public interface IUnifiedXamlTransformVisitor
{
    /// <summary>
    /// Transforms a XAML document.
    /// </summary>
    UnifiedXamlDocument? TransformDocument(UnifiedXamlDocument document);

    /// <summary>
    /// Transforms a XAML element.
    /// </summary>
    UnifiedXamlElement? TransformElement(UnifiedXamlElement element);

    /// <summary>
    /// Transforms a XAML property.
    /// </summary>
    UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property);

    /// <summary>
    /// Transforms a markup extension.
    /// </summary>
    UnifiedXamlMarkupExtension? TransformMarkupExtension(UnifiedXamlMarkupExtension markupExtension);
}

/// <summary>
/// Interface for visitors that can return a result of type T.
/// Useful for collecting information from the AST.
/// </summary>
public interface IUnifiedXamlVisitor<T>
{
    /// <summary>
    /// Visits a XAML document and returns a result.
    /// </summary>
    T VisitDocument(UnifiedXamlDocument document);

    /// <summary>
    /// Visits a XAML element and returns a result.
    /// </summary>
    T VisitElement(UnifiedXamlElement element);

    /// <summary>
    /// Visits a XAML property and returns a result.
    /// </summary>
    T VisitProperty(UnifiedXamlProperty property);

    /// <summary>
    /// Visits a markup extension and returns a result.
    /// </summary>
    T VisitMarkupExtension(UnifiedXamlMarkupExtension markupExtension);

    /// <summary>
    /// Visits a comment and returns a result.
    /// </summary>
    T VisitComment(UnifiedXamlComment comment);
}
