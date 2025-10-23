using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Rewrites event subscriptions from WPF events to Avalonia events.
/// </summary>
public sealed class EventHandlerRewriter : WpfToAvaloniaRewriter
{
    private int _eventReferencesChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventHandlerRewriter"/> class.
    /// </summary>
    /// <param name="semanticModel">The semantic model for the syntax tree.</param>
    /// <param name="diagnostics">The diagnostic collector.</param>
    /// <param name="mappingRepository">The mapping repository.</param>
    public EventHandlerRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Visits an assignment expression to handle event subscriptions.
    /// </summary>
    public override SyntaxNode? VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        // Check for event subscriptions (+=) and unsubscriptions (-=)
        if (node.Kind() != SyntaxKind.AddAssignmentExpression &&
            node.Kind() != SyntaxKind.SubtractAssignmentExpression)
        {
            return base.VisitAssignmentExpression(node);
        }

        if (node.Left is not MemberAccessExpressionSyntax memberAccess)
        {
            return base.VisitAssignmentExpression(node);
        }

        var symbolInfo = SemanticModel.GetSymbolInfo(memberAccess);
        var eventSymbol = symbolInfo.Symbol as IEventSymbol;

        if (eventSymbol == null)
        {
            return base.VisitAssignmentExpression(node);
        }

        var eventName = eventSymbol.Name;
        var ownerTypeName = eventSymbol.ContainingType?.ToDisplayString();

        // Look up event mapping
        var mapping = MappingRepository.FindEventMapping(eventName, ownerTypeName);
        if (mapping == null)
        {
            mapping = MappingRepository.FindEventMapping(eventName);
        }

        if (mapping == null)
        {
            return base.VisitAssignmentExpression(node);
        }

        // Transform the event name
        var newName = SyntaxFactory.IdentifierName(mapping.AvaloniaEventName)
            .WithTriviaFrom(memberAccess.Name);

        var newMemberAccess = memberAccess.WithName((SimpleNameSyntax)newName);
        var newAssignment = node.WithLeft(newMemberAccess);

        _eventReferencesChanged++;

        // Report the transformation
        var lineSpan = node.GetLocation().GetLineSpan();
        Diagnostics.AddInfo(
            DiagnosticCodes.EventTransformed,
            $"Transformed event: {eventName} → {mapping.AvaloniaEventName}",
            lineSpan.Path,
            lineSpan.StartLinePosition.Line + 1,
            lineSpan.StartLinePosition.Character + 1);

        if (mapping.RequiresManualReview)
        {
            Diagnostics.AddWarning(
                DiagnosticCodes.EventRequiresManualReview,
                $"Event transformation requires manual review: {mapping.Notes}",
                lineSpan.Path,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1);
        }

        return newAssignment;
    }

    /// <summary>
    /// Gets the count of event references that were changed.
    /// </summary>
    public int EventReferencesChanged => _eventReferencesChanged;
}
