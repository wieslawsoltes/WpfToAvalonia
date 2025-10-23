using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Visitors;

/// <summary>
/// Visitor that collects all diagnostics from the Unified XAML AST.
/// </summary>
public sealed class DiagnosticCollectorVisitor : UnifiedXamlCollectorVisitor<TransformationDiagnostic>
{
    /// <summary>
    /// Minimum severity level to collect.
    /// </summary>
    public DiagnosticSeverity MinimumSeverity { get; set; } = DiagnosticSeverity.Info;

    /// <summary>
    /// Visits a XAML document and collects diagnostics.
    /// </summary>
    public override List<TransformationDiagnostic> VisitDocument(UnifiedXamlDocument document)
    {
        Results.Clear();

        // Collect document-level diagnostics
        CollectDiagnostics(document.Diagnostics);

        // Visit the tree
        if (document.Root != null)
        {
            VisitElement(document.Root);
        }

        return Results;
    }

    /// <summary>
    /// Visits a XAML element and collects diagnostics.
    /// </summary>
    public override List<TransformationDiagnostic> VisitElement(UnifiedXamlElement element)
    {
        CollectDiagnostics(element.Diagnostics);
        return base.VisitElement(element);
    }

    /// <summary>
    /// Visits a XAML property and collects diagnostics.
    /// </summary>
    public override List<TransformationDiagnostic> VisitProperty(UnifiedXamlProperty property)
    {
        CollectDiagnostics(property.Diagnostics);
        return base.VisitProperty(property);
    }

    /// <summary>
    /// Visits a markup extension and collects diagnostics.
    /// </summary>
    public override List<TransformationDiagnostic> VisitMarkupExtension(UnifiedXamlMarkupExtension markupExtension)
    {
        CollectDiagnostics(markupExtension.Diagnostics);
        return base.VisitMarkupExtension(markupExtension);
    }

    private void CollectDiagnostics(IEnumerable<TransformationDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Severity >= MinimumSeverity)
            {
                Results.Add(diagnostic);
            }
        }
    }
}
