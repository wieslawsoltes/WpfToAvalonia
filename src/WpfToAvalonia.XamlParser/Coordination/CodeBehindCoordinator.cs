using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Coordination;

/// <summary>
/// Coordinates XAML transformations with C# code-behind transformations.
/// Ensures consistency between XAML and code-behind during WPF to Avalonia migration.
/// </summary>
public sealed class CodeBehindCoordinator
{
    private readonly DiagnosticCollector _diagnostics;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeBehindCoordinator"/> class.
    /// </summary>
    public CodeBehindCoordinator(DiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    /// <summary>
    /// Coordinates a XAML document with its code-behind semantic model.
    /// </summary>
    public void CoordinateTransformation(
        UnifiedXamlDocument xamlDocument,
        SemanticModel? codeBehindModel)
    {
        if (xamlDocument == null)
        {
            throw new ArgumentNullException(nameof(xamlDocument));
        }

        if (codeBehindModel == null)
        {
            _diagnostics.AddInfo(
                "COORDINATOR_NO_CODE_BEHIND",
                "No code-behind model provided for coordination",
                xamlDocument.FilePath);
            return;
        }

        _diagnostics.AddInfo(
            "COORDINATOR_START",
            "Starting code-behind coordination",
            xamlDocument.FilePath);

        // Validate x:Class matches code-behind partial class
        ValidateXClassMatch(xamlDocument, codeBehindModel);

        // Sync XAML named elements with code-behind fields
        SyncNamedElementsWithFields(xamlDocument, codeBehindModel);

        // Transform event handler signatures
        TransformEventHandlers(xamlDocument, codeBehindModel);

        _diagnostics.AddInfo(
            "COORDINATOR_COMPLETE",
            "Code-behind coordination completed",
            xamlDocument.FilePath);
    }

    /// <summary>
    /// Validates that the XAML x:Class attribute matches the code-behind partial class.
    /// </summary>
    private void ValidateXClassMatch(UnifiedXamlDocument xamlDocument, SemanticModel codeBehindModel)
    {
        if (xamlDocument.Root == null)
        {
            return;
        }

        // Get x:Class from XAML root
        var xClass = xamlDocument.Root.GetProperty("x:Class")?.Value as string;
        if (string.IsNullOrEmpty(xClass))
        {
            _diagnostics.AddWarning(
                "COORDINATOR_NO_XCLASS",
                "XAML document has no x:Class attribute",
                xamlDocument.FilePath);
            return;
        }

        // Find matching class in code-behind
        var root = codeBehindModel.SyntaxTree.GetRoot();
        var classDeclarations = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c => c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));

        ClassDeclarationSyntax? matchingClass = null;
        foreach (var classDecl in classDeclarations)
        {
            var symbol = codeBehindModel.GetDeclaredSymbol(classDecl);
            if (symbol != null)
            {
                var fullName = symbol.ToDisplayString();
                if (fullName == xClass)
                {
                    matchingClass = classDecl;
                    break;
                }
            }
        }

        if (matchingClass == null)
        {
            _diagnostics.AddError(
                "COORDINATOR_XCLASS_MISMATCH",
                $"XAML x:Class '{xClass}' does not match any partial class in code-behind",
                xamlDocument.FilePath);
        }
        else
        {
            _diagnostics.AddInfo(
                "COORDINATOR_XCLASS_VALID",
                $"XAML x:Class '{xClass}' matches code-behind partial class",
                xamlDocument.FilePath);

            // Store the matching class for later use
            xamlDocument.SetMetadata("CodeBehindClass", matchingClass);
            var classSymbol = codeBehindModel.GetDeclaredSymbol(matchingClass);
            if (classSymbol != null)
            {
                xamlDocument.SetMetadata("CodeBehindClassSymbol", classSymbol);
            }
        }
    }

    /// <summary>
    /// Syncs XAML named elements (x:Name) with code-behind field declarations.
    /// </summary>
    private void SyncNamedElementsWithFields(UnifiedXamlDocument xamlDocument, SemanticModel codeBehindModel)
    {
        var symbolTable = xamlDocument.GetMetadata<Enrichment.UnifiedSymbolTable>("SymbolTable");
        if (symbolTable == null)
        {
            _diagnostics.AddWarning(
                "COORDINATOR_NO_SYMBOL_TABLE",
                "XAML document has no symbol table for named element synchronization",
                xamlDocument.FilePath);
            return;
        }

        var syncedFields = 0;
        var missingFields = new List<string>();

        foreach (var namedElement in symbolTable.NamedElements)
        {
            var elementName = namedElement.Key;
            var element = namedElement.Value.Element;

            // Check if code-behind field exists
            var fieldSymbol = element.GetMetadata<IFieldSymbol>("CodeBehindField");
            if (fieldSymbol == null)
            {
                missingFields.Add(elementName);
                _diagnostics.AddWarning(
                    "COORDINATOR_MISSING_FIELD",
                    $"XAML element '{elementName}' has no corresponding field in code-behind",
                    element.Location.FilePath,
                    element.Location.Line,
                    element.Location.Column);
            }
            else
            {
                // Validate field type matches element type
                ValidateFieldTypeMatch(element, fieldSymbol);
                syncedFields++;
            }
        }

        _diagnostics.AddInfo(
            "COORDINATOR_FIELD_SYNC",
            $"Synchronized {syncedFields} named elements with code-behind fields, {missingFields.Count} missing",
            xamlDocument.FilePath);
    }

    /// <summary>
    /// Validates that a code-behind field type matches the XAML element type.
    /// </summary>
    private void ValidateFieldTypeMatch(UnifiedXamlElement element, IFieldSymbol fieldSymbol)
    {
        if (element.ElementType == null)
        {
            return;
        }

        var xamlTypeName = element.ElementType.FullName;
        var fieldTypeName = fieldSymbol.Type.ToDisplayString();

        // Simple name comparison (could be enhanced with more sophisticated type matching)
        if (!CompareTypeNames(xamlTypeName, fieldTypeName))
        {
            _diagnostics.AddWarning(
                "COORDINATOR_TYPE_MISMATCH",
                $"XAML element type '{xamlTypeName}' doesn't match field type '{fieldTypeName}' for '{element.XName}'",
                element.Location.FilePath,
                element.Location.Line,
                element.Location.Column);
        }
    }

    /// <summary>
    /// Compares two type names for equivalence (handles WPF vs Avalonia type names).
    /// </summary>
    private bool CompareTypeNames(string xamlTypeName, string fieldTypeName)
    {
        // Extract the simple type name (without namespace)
        var xamlSimpleName = xamlTypeName.Split('.').Last();
        var fieldSimpleName = fieldTypeName.Split('.').Last();

        // Remove generic type indicators
        xamlSimpleName = xamlSimpleName.Split('<')[0];
        fieldSimpleName = fieldSimpleName.Split('<')[0];

        return xamlSimpleName == fieldSimpleName;
    }

    /// <summary>
    /// Transforms event handler signatures from WPF to Avalonia.
    /// </summary>
    private void TransformEventHandlers(UnifiedXamlDocument xamlDocument, SemanticModel codeBehindModel)
    {
        if (xamlDocument.Root == null)
        {
            return;
        }

        var eventHandlers = new List<EventHandlerInfo>();

        // Find all event handler references in XAML
        foreach (var element in xamlDocument.Root.DescendantsAndSelf())
        {
            foreach (var property in element.Properties)
            {
                // Check if property is an event (e.g., Click, Loaded, etc.)
                if (IsEventProperty(property.PropertyName) && property.Value is string handlerName)
                {
                    eventHandlers.Add(new EventHandlerInfo
                    {
                        EventName = property.PropertyName,
                        HandlerName = handlerName,
                        Element = element
                    });
                }
            }
        }

        if (eventHandlers.Count == 0)
        {
            _diagnostics.AddInfo(
                "COORDINATOR_NO_EVENT_HANDLERS",
                "No event handlers found in XAML",
                xamlDocument.FilePath);
            return;
        }

        _diagnostics.AddInfo(
            "COORDINATOR_EVENT_HANDLERS",
            $"Found {eventHandlers.Count} event handlers for signature validation",
            xamlDocument.FilePath);

        // Validate each event handler exists in code-behind
        foreach (var handler in eventHandlers)
        {
            ValidateEventHandlerSignature(handler, codeBehindModel, xamlDocument.FilePath);
        }
    }

    /// <summary>
    /// Determines if a property name is likely an event.
    /// </summary>
    private bool IsEventProperty(string propertyName)
    {
        // Common WPF/Avalonia events
        var commonEvents = new[]
        {
            "Click", "Loaded", "Unloaded", "GotFocus", "LostFocus",
            "MouseDown", "MouseUp", "MouseEnter", "MouseLeave",
            "KeyDown", "KeyUp", "TextChanged", "SelectionChanged",
            "Checked", "Unchecked", "PointerPressed", "PointerReleased"
        };

        return commonEvents.Contains(propertyName);
    }

    /// <summary>
    /// Validates that an event handler exists in code-behind with the correct signature.
    /// </summary>
    private void ValidateEventHandlerSignature(
        EventHandlerInfo handler,
        SemanticModel codeBehindModel,
        string? xamlFilePath)
    {
        var root = codeBehindModel.SyntaxTree.GetRoot();
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        MethodDeclarationSyntax? matchingMethod = null;
        foreach (var method in methods)
        {
            if (method.Identifier.Text == handler.HandlerName)
            {
                matchingMethod = method;
                break;
            }
        }

        if (matchingMethod == null)
        {
            _diagnostics.AddWarning(
                "COORDINATOR_MISSING_HANDLER",
                $"Event handler '{handler.HandlerName}' for event '{handler.EventName}' not found in code-behind",
                handler.Element.Location.FilePath,
                handler.Element.Location.Line,
                handler.Element.Location.Column);
        }
        else
        {
            // Check signature (should be void Method(object sender, EventArgs e) pattern)
            var parameters = matchingMethod.ParameterList.Parameters;
            if (parameters.Count != 2)
            {
                _diagnostics.AddWarning(
                    "COORDINATOR_HANDLER_SIGNATURE",
                    $"Event handler '{handler.HandlerName}' should have 2 parameters (sender, eventArgs)",
                    handler.Element.Location.FilePath,
                    handler.Element.Location.Line,
                    handler.Element.Location.Column);
            }

            _diagnostics.AddInfo(
                "COORDINATOR_HANDLER_VALID",
                $"Event handler '{handler.HandlerName}' validated successfully",
                xamlFilePath);
        }
    }

    /// <summary>
    /// Information about an event handler reference in XAML.
    /// </summary>
    private sealed class EventHandlerInfo
    {
        public required string EventName { get; init; }
        public required string HandlerName { get; init; }
        public required UnifiedXamlElement Element { get; init; }
    }
}
