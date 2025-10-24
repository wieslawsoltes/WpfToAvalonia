using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Rewrites Freezable object usage from WPF to Avalonia equivalents.
/// Implements task 2.5.3.2: Transform freezable objects
/// </summary>
/// <remarks>
/// Freezable is a WPF base class that provides:
/// - Immutability (Freeze() makes object read-only)
/// - Change notifications
/// - Clone/CloneCurrentValue support
/// - Thread-safe read access when frozen
///
/// WPF Freezable hierarchy includes:
/// - Brush, Pen, Transform (visual objects)
/// - Animation classes
/// - Geometry classes
/// - Effects
///
/// Avalonia Approach:
/// - No Freezable base class
/// - Immutable types where appropriate (e.g., Color)
/// - Avalonia objects are generally mutable but use proper change notifications
/// - Thread safety is handled differently
///
/// Migration Strategy:
/// 1. Remove Freezable base class inheritance
/// 2. Remove Freeze() and IsFrozen checks
/// 3. Replace CreateInstanceCore() with proper constructors
/// 4. Replace CloneCore/CloneCurrentValueCore with Clone() methods
/// 5. Keep change notification patterns (INotifyPropertyChanged still works)
/// </remarks>
public sealed class FreezableRewriter : WpfToAvaloniaRewriter
{
    private int _freezableInheritance;
    private int _freezeCalls;
    private int _isFrozenChecks;
    private int _cloneCalls;
    private int _freezableCollections;

    /// <summary>
    /// Initializes a new instance of the <see cref="FreezableRewriter"/> class.
    /// </summary>
    public FreezableRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Gets the number of Freezable inheritance detected.
    /// </summary>
    public int FreezableInheritance => _freezableInheritance;

    /// <summary>
    /// Gets the number of Freeze() calls detected.
    /// </summary>
    public int FreezeCalls => _freezeCalls;

    /// <summary>
    /// Gets the number of IsFrozen checks detected.
    /// </summary>
    public int IsFrozenChecks => _isFrozenChecks;

    /// <summary>
    /// Gets the number of Clone calls detected.
    /// </summary>
    public int CloneCalls => _cloneCalls;

    /// <summary>
    /// Gets the number of FreezableCollection usages detected.
    /// </summary>
    public int FreezableCollections => _freezableCollections;

    /// <summary>
    /// Visits class declarations to detect Freezable inheritance.
    /// </summary>
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (node.BaseList != null)
        {
            foreach (var baseType in node.BaseList.Types)
            {
                var baseTypeName = baseType.Type.ToString();

                if (baseTypeName == "Freezable" || baseTypeName == "System.Windows.Freezable")
                {
                    _freezableInheritance++;

                    var className = node.Identifier.Text;

                    Diagnostics.AddWarning(
                        "FREEZABLE_NOT_SUPPORTED",
                        $"Class '{className}' inherits from Freezable, which is not supported in Avalonia. " +
                        $"Remove Freezable base class. Alternatives:\n" +
                        $"1. Use AvaloniaObject as base class (for dependency properties)\n" +
                        $"2. Implement INotifyPropertyChanged for change notifications\n" +
                        $"3. Make immutable types where appropriate\n" +
                        $"4. Remove Freeze() calls and frozen state checks",
                        null);

                    Diagnostics.AddInfo(
                        "FREEZABLE_MIGRATION_GUIDE",
                        $"Migration steps for '{className}':\n" +
                        $"1. Change base class: Freezable → AvaloniaObject or INotifyPropertyChanged\n" +
                        $"2. Remove CreateInstanceCore() override (use constructors)\n" +
                        $"3. Remove CloneCore/CloneCurrentValueCore (implement Clone() if needed)\n" +
                        $"4. Remove all Freeze() and IsFrozen references\n" +
                        $"5. Update FreezeCore implementation to standard property setters",
                        null);
                }
            }
        }

        return base.VisitClassDeclaration(node);
    }

    /// <summary>
    /// Visits invocation expressions to detect Freezable method calls.
    /// </summary>
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;
            var symbolInfo = TryGetSymbolInfo(memberAccess);

            if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IMethodSymbol methodSymbol)
            {
                var containingType = methodSymbol.ContainingType?.ToDisplayString();

                // Detect Freeze() calls
                if (methodName == "Freeze" && IsFreezableType(containingType))
                {
                    _freezeCalls++;

                    Diagnostics.AddWarning(
                        "FREEZE_NOT_SUPPORTED",
                        $"Freeze() call on {memberAccess.Expression} is not supported in Avalonia. " +
                        $"Avalonia doesn't have the Freezable concept. Remove Freeze() calls. " +
                        $"If immutability is needed, consider redesigning the type as immutable.",
                        null);
                }
                // Detect Clone/CloneCurrentValue calls
                else if ((methodName == "Clone" || methodName == "CloneCurrentValue") && IsFreezableType(containingType))
                {
                    _cloneCalls++;

                    Diagnostics.AddWarning(
                        "FREEZABLE_CLONE_NOT_SUPPORTED",
                        $"Freezable.{methodName}() is not available in Avalonia. " +
                        $"Implement custom Clone() method if needed, or use copy constructors. " +
                        $"For visual objects like Brush/Pen/Transform, consider if cloning is actually necessary.",
                        null);
                }
                // Detect GetAsFrozen/GetCurrentValueAsFrozen
                else if ((methodName == "GetAsFrozen" || methodName == "GetCurrentValueAsFrozen") && IsFreezableType(containingType))
                {
                    Diagnostics.AddWarning(
                        "GET_AS_FROZEN_NOT_SUPPORTED",
                        $"Freezable.{methodName}() is not available in Avalonia. " +
                        $"Remove this call. If you need a copy, implement custom Clone() method.",
                        null);
                }
            }
        }

        return base.VisitInvocationExpression(node);
    }

    /// <summary>
    /// Visits member access expressions to detect IsFrozen checks.
    /// </summary>
    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (node.Name.Identifier.Text == "IsFrozen")
        {
            var symbolInfo = TryGetSymbolInfo(node);
            if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IPropertySymbol propertySymbol)
            {
                var containingType = propertySymbol.ContainingType?.ToDisplayString();

                if (IsFreezableType(containingType))
                {
                    _isFrozenChecks++;

                    Diagnostics.AddWarning(
                        "IS_FROZEN_NOT_SUPPORTED",
                        $"IsFrozen property check on {node.Expression} is not supported in Avalonia. " +
                        $"Remove IsFrozen checks. Avalonia objects don't have frozen/unfrozen states. " +
                        $"If you need to track mutability, implement custom state tracking.",
                        null);
                }
            }
        }
        // Detect CanFreeze property
        else if (node.Name.Identifier.Text == "CanFreeze")
        {
            var symbolInfo = TryGetSymbolInfo(node);
            if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IPropertySymbol propertySymbol)
            {
                var containingType = propertySymbol.ContainingType?.ToDisplayString();

                if (IsFreezableType(containingType))
                {
                    Diagnostics.AddWarning(
                        "CAN_FREEZE_NOT_SUPPORTED",
                        $"CanFreeze property is not supported in Avalonia. Remove CanFreeze checks.",
                        null);
                }
            }
        }

        return base.VisitMemberAccessExpression(node);
    }

    /// <summary>
    /// Visits object creation expressions to detect FreezableCollection usage.
    /// </summary>
    public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        var symbolInfo = TryGetSymbolInfo(node);
        if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IMethodSymbol methodSymbol)
        {
            var typeName = methodSymbol.ContainingType?.ToDisplayString();

            if (typeName?.StartsWith("System.Windows.FreezableCollection") == true)
            {
                _freezableCollections++;

                Diagnostics.AddWarning(
                    "FREEZABLE_COLLECTION_NOT_SUPPORTED",
                    $"FreezableCollection<T> is not available in Avalonia. " +
                    $"Use AvaloniaList<T> or ObservableCollection<T> instead. " +
                    $"Note: Avalonia collections don't support freezing.",
                    null);
            }
        }

        return base.VisitObjectCreationExpression(node);
    }

    /// <summary>
    /// Visits method declarations to detect Freezable override methods.
    /// </summary>
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var methodName = node.Identifier.Text;

        // Detect Freezable override methods
        if (node.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword)))
        {
            if (methodName is "CreateInstanceCore" or "CloneCore" or "CloneCurrentValueCore" or
                "GetAsFrozenCore" or "GetCurrentValueAsFrozenCore" or "FreezeCore")
            {
                Diagnostics.AddWarning(
                    "FREEZABLE_OVERRIDE_NOT_SUPPORTED",
                    $"Freezable override method '{methodName}' is not supported in Avalonia. " +
                    $"Remove this override. Migration guidance:\n" +
                    $"- CreateInstanceCore → Use constructors\n" +
                    $"- CloneCore/CloneCurrentValueCore → Implement custom Clone() method\n" +
                    $"- FreezeCore → Remove (no frozen state in Avalonia)\n" +
                    $"- GetAsFrozenCore/GetCurrentValueAsFrozenCore → Remove",
                    null);
            }
        }

        return base.VisitMethodDeclaration(node);
    }

    /// <summary>
    /// Checks if a type name is a Freezable type.
    /// </summary>
    private bool IsFreezableType(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return false;
        }

        return typeName == "System.Windows.Freezable" ||
               typeName.StartsWith("System.Windows.Media.") ||
               typeName.StartsWith("System.Windows.Media.Animation.") ||
               typeName.StartsWith("System.Windows.Media.Effects.");
    }

    /// <summary>
    /// Tries to get symbol info, catching exceptions from stale semantic models.
    /// </summary>
    private SymbolInfo? TryGetSymbolInfo(SyntaxNode node)
    {
        try
        {
            return SemanticModel.GetSymbolInfo(node);
        }
        catch
        {
            // Node might not exist in semantic model after previous transformations
            return null;
        }
    }
}
