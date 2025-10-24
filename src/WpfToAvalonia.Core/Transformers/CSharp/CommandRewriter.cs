using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Transforms WPF command-related code to Avalonia equivalents.
/// Implements task 2.5.7.3: Command binding transformations (C# aspect)
///
/// Handles:
/// - Task 2.5.7.3.1: ICommand implementations
/// - Task 2.5.7.3.3: RoutedCommand/RoutedUICommand → ICommand/ReactiveCommand
/// - Task 2.5.7.3.5: Input gestures and keyboard shortcuts (code-behind aspect)
/// </summary>
public sealed class CommandRewriter : WpfToAvaloniaRewriter
{
    private int _routedCommandUsages;
    private int _commandBindingUsages;
    private int _inputBindingUsages;
    private int _keyGestureUsages;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandRewriter"/> class.
    /// </summary>
    public CommandRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // Check if class implements ICommand
        if (node.BaseList != null)
        {
            foreach (var baseType in node.BaseList.Types)
            {
                var typeInfo = SemanticModel.GetTypeInfo(baseType.Type);
                var baseTypeName = typeInfo.Type?.Name ?? baseType.Type.ToString();

                if (baseTypeName == "ICommand" || baseTypeName == "System.Windows.Input.ICommand")
                {
                    Diagnostics.AddInfo(
                        "ICOMMAND_IMPLEMENTATION",
                        $"ICommand implementation detected in class '{node.Identifier.Text}'. " +
                        $"ICommand interface is supported in Avalonia. " +
                        $"Update using directive: System.Windows.Input → System.Windows.Input (works for both frameworks). " +
                        $"Consider using ReactiveCommand from ReactiveUI for more features.",
                        null);
                }
            }
        }

        return base.VisitClassDeclaration(node);
    }

    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        var typeInfo = SemanticModel.GetTypeInfo(node.Declaration.Type);
        var typeName = typeInfo.Type?.ToDisplayString() ?? node.Declaration.Type.ToString();

        CheckCommandType(typeName);

        return base.VisitFieldDeclaration(node);
    }

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        var typeInfo = SemanticModel.GetTypeInfo(node.Type);
        var typeName = typeInfo.Type?.ToDisplayString() ?? node.Type.ToString();

        CheckCommandType(typeName);

        return base.VisitPropertyDeclaration(node);
    }

    public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        var typeInfo = SemanticModel.GetTypeInfo(node.Type);
        var typeName = typeInfo.Type?.ToDisplayString() ?? node.Type.ToString();

        // Check for RoutedCommand/RoutedUICommand instantiation
        if (typeName.Contains("RoutedCommand") || typeName.Contains("RoutedUICommand"))
        {
            _routedCommandUsages++;
            Diagnostics.AddWarning(
                "ROUTED_COMMAND_INSTANTIATION",
                $"RoutedCommand/RoutedUICommand instantiation detected: {typeName}. " +
                $"Avalonia doesn't have RoutedCommand. " +
                $"Migration options:\n" +
                $"1. Replace with ICommand implementation (RelayCommand, DelegateCommand)\n" +
                $"2. Use ReactiveCommand from ReactiveUI:\n" +
                $"   Install-Package Avalonia.ReactiveUI\n" +
                $"   var cmd = ReactiveCommand.Create(() => {{ /* execute */ }});\n" +
                $"3. Create custom ICommand implementation",
                null);
        }

        // Check for CommandBinding
        if (typeName.Contains("CommandBinding"))
        {
            _commandBindingUsages++;
            Diagnostics.AddWarning(
                "COMMAND_BINDING_INSTANTIATION",
                "CommandBinding instantiation detected. " +
                "Avalonia doesn't have CommandBinding (WPF command routing infrastructure). " +
                "Remove command bindings and use direct command bindings in XAML:\n" +
                "<Button Command=\"{Binding MyCommand}\"/>",
                null);
        }

        // Check for KeyBinding/MouseBinding
        if (typeName.Contains("KeyBinding") || typeName.Contains("MouseBinding"))
        {
            _inputBindingUsages++;
            Diagnostics.AddInfo(
                "INPUT_BINDING_INSTANTIATION",
                $"{typeName} instantiation detected. " +
                "Avalonia supports KeyBindings with similar syntax. " +
                "For KeyBinding, usage is mostly compatible. " +
                "For MouseBinding, consider using pointer events instead.",
                null);
        }

        // Check for KeyGesture
        if (typeName.Contains("KeyGesture"))
        {
            _keyGestureUsages++;
            Diagnostics.AddInfo(
                "KEY_GESTURE_INSTANTIATION",
                "KeyGesture instantiation detected. " +
                "Avalonia supports KeyGesture with similar syntax. " +
                "Namespace: Avalonia.Input.KeyGesture",
                null);
        }

        return base.VisitObjectCreationExpression(node);
    }

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var symbolInfo = SemanticModel.GetSymbolInfo(node);
        var symbol = symbolInfo.Symbol;

        if (symbol != null)
        {
            var containingType = symbol.ContainingType?.ToDisplayString() ?? "";

            // Check for static command classes
            if (containingType == "System.Windows.Input.ApplicationCommands" ||
                containingType == "System.Windows.Input.NavigationCommands" ||
                containingType == "System.Windows.Input.MediaCommands" ||
                containingType == "System.Windows.Input.ComponentCommands" ||
                containingType == "System.Windows.Input.EditingCommands")
            {
                var commandName = symbol.Name;
                Diagnostics.AddWarning(
                    "STATIC_COMMAND_USAGE",
                    $"WPF static command '{containingType}.{commandName}' detected. " +
                    $"These static command classes are not available in Avalonia. " +
                    $"Migration options:\n" +
                    $"1. Implement command in ViewModel as ICommand property\n" +
                    $"2. For Copy/Cut/Paste in TextBox, use built-in commands:\n" +
                    $"   - Avalonia provides CopyCommand, CutCommand, PasteCommand on TextBox\n" +
                    $"3. For other commands, create custom ICommand implementations",
                    null);
            }

            // Check for CommandManager
            if (containingType == "System.Windows.Input.CommandManager")
            {
                Diagnostics.AddWarning(
                    "COMMAND_MANAGER_USAGE",
                    $"CommandManager.{symbol.Name} detected. " +
                    "Avalonia doesn't have CommandManager. " +
                    "CommandManager.InvalidateRequerySuggested() is not needed in Avalonia. " +
                    "If using ReactiveCommand, it handles CanExecute changes automatically. " +
                    "For ICommand implementations, raise CanExecuteChanged event directly.",
                    null);
            }
        }

        return base.VisitMemberAccessExpression(node);
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        // Check for CommandBinding.Add calls
        if (node.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var symbolInfo = SemanticModel.GetSymbolInfo(memberAccess);
            var symbol = symbolInfo.Symbol;

            if (symbol != null && symbol.ContainingType != null)
            {
                var containingType = symbol.ContainingType.ToDisplayString();

                // Check for CommandBindings.Add
                if (containingType.Contains("CommandBindingCollection") && symbol.Name == "Add")
                {
                    Diagnostics.AddWarning(
                        "COMMAND_BINDINGS_ADD",
                        "CommandBindings.Add() detected. " +
                        "Avalonia doesn't have CommandBinding infrastructure. " +
                        "Remove this code and bind commands directly in XAML or ViewModel.",
                        null);
                }

                // Check for InputBindings.Add
                if (containingType.Contains("InputBindingCollection") && symbol.Name == "Add")
                {
                    Diagnostics.AddInfo(
                        "INPUT_BINDINGS_ADD",
                        "InputBindings.Add() detected. " +
                        "Avalonia supports InputBindings (especially KeyBindings) with similar API. " +
                        "Namespace: Avalonia.Input → KeyBinding",
                        null);
                }
            }
        }

        return base.VisitInvocationExpression(node);
    }

    private void CheckCommandType(string typeName)
    {
        if (typeName.Contains("RoutedCommand") || typeName.Contains("RoutedUICommand"))
        {
            _routedCommandUsages++;
            Diagnostics.AddWarning(
                "ROUTED_COMMAND_TYPE",
                $"RoutedCommand/RoutedUICommand type detected: {typeName}. " +
                "Replace with ICommand or ReactiveCommand. " +
                "Update type declaration to: ICommand",
                null);
        }

        if (typeName.Contains("ICommand") && typeName.Contains("System.Windows.Input"))
        {
            Diagnostics.AddInfo(
                "ICOMMAND_TYPE",
                "ICommand type detected. " +
                "ICommand is supported in Avalonia. " +
                "Namespace System.Windows.Input works for both WPF and Avalonia. " +
                "Alternatively, use ReactiveCommand from ReactiveUI for enhanced features.",
                null);
        }
    }
}
