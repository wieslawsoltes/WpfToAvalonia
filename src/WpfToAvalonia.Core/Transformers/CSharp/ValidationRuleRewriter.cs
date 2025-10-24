using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Rewrites validation rule classes from WPF to Avalonia.
/// Implements task 2.5.7.1.5: Handle binding validation rules
/// </summary>
/// <remarks>
/// WPF and Avalonia have different approaches to data validation:
///
/// WPF Validation:
/// - ValidationRule base class in System.Windows.Controls
/// - Validate() method returns ValidationResult
/// - Binding.ValidationRules collection
/// - ValidatesOnDataErrors, ValidatesOnExceptions, NotifyOnValidationError
/// - Validation.HasError attached property
/// - Validation.Errors collection
///
/// Avalonia Validation:
/// - Uses INotifyDataErrorInfo interface (recommended)
/// - Or DataValidationErrors.SetErrors() for custom validation
/// - EnableDataValidation property on bindings
/// - DataValidationErrors.HasErrors attached property
/// - DataValidationErrors.Errors collection
/// - No ValidationRule base class equivalent
///
/// Migration Strategy:
/// 1. Convert ValidationRule classes to INotifyDataErrorInfo implementation on ViewModels
/// 2. Or use DataAnnotations with DataValidationPlugins
/// 3. Update binding syntax to use EnableDataValidation=True
/// 4. Replace Validation.HasError with DataValidationErrors.HasErrors
/// 5. Update error templates and styles
///
/// Avalonia Validation Approaches:
/// A. INotifyDataErrorInfo (ViewModels implement validation)
/// B. DataAnnotations (attribute-based validation)
/// C. ReactiveUI validation (for MVVM)
/// D. Custom validation using DataValidationErrors
/// </remarks>
public sealed class ValidationRuleRewriter : WpfToAvaloniaRewriter
{
    private int _validationRuleImplementations;
    private int _validationHasErrorUsages;
    private int _validationErrorsUsages;
    private int _bindingValidationRules;
    private int _notifyDataErrorInfoImplementations;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationRuleRewriter"/> class.
    /// </summary>
    public ValidationRuleRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Gets the number of ValidationRule implementations detected.
    /// </summary>
    public int ValidationRuleImplementations => _validationRuleImplementations;

    /// <summary>
    /// Gets the number of Validation.HasError usages detected.
    /// </summary>
    public int ValidationHasErrorUsages => _validationHasErrorUsages;

    /// <summary>
    /// Gets the number of Validation.Errors usages detected.
    /// </summary>
    public int ValidationErrorsUsages => _validationErrorsUsages;

    /// <summary>
    /// Gets the number of binding ValidationRules usages detected.
    /// </summary>
    public int BindingValidationRules => _bindingValidationRules;

    /// <summary>
    /// Gets the number of INotifyDataErrorInfo implementations detected.
    /// </summary>
    public int NotifyDataErrorInfoImplementations => _notifyDataErrorInfoImplementations;

    /// <summary>
    /// Visits class declarations to detect validation rule implementations.
    /// </summary>
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (node.BaseList != null)
        {
            foreach (var baseType in node.BaseList.Types)
            {
                var baseTypeName = baseType.Type.ToString();

                // Detect ValidationRule inheritance
                if (baseTypeName == "ValidationRule" ||
                    baseTypeName == "System.Windows.Controls.ValidationRule")
                {
                    _validationRuleImplementations++;

                    var className = node.Identifier.Text;

                    Diagnostics.AddWarning(
                        "VALIDATION_RULE_NOT_SUPPORTED",
                        $"ValidationRule class '{className}' is not supported in Avalonia. " +
                        $"Avalonia uses different validation approaches:\\n" +
                        $"1. INotifyDataErrorInfo on ViewModels (recommended)\\n" +
                        $"2. DataAnnotations with DataValidationPlugins\\n" +
                        $"3. ReactiveUI validation\\n" +
                        $"4. Custom validation with DataValidationErrors",
                        null);

                    Diagnostics.AddInfo(
                        "VALIDATION_MIGRATION_GUIDE",
                        $"Migration options for '{className}':\\n" +
                        $"A. Move validation logic to ViewModel implementing INotifyDataErrorInfo\\n" +
                        $"B. Use DataAnnotations attributes on properties ([Required], [Range], etc.)\\n" +
                        $"C. Use Avalonia.Labs.DataValidation NuGet package\\n" +
                        $"D. Implement custom validation in property setters with DataValidationErrors.SetErrors()",
                        null);
                }
                // Detect INotifyDataErrorInfo implementation (good for Avalonia)
                else if (baseTypeName == "INotifyDataErrorInfo" ||
                         baseTypeName == "System.ComponentModel.INotifyDataErrorInfo")
                {
                    _notifyDataErrorInfoImplementations++;

                    var className = node.Identifier.Text;

                    Diagnostics.AddInfo(
                        "NOTIFY_DATA_ERROR_INFO_COMPATIBLE",
                        $"Class '{className}' implements INotifyDataErrorInfo - compatible with Avalonia! " +
                        $"Ensure bindings use EnableDataValidation=True to enable validation display.",
                        null);
                }
            }
        }

        return base.VisitClassDeclaration(node);
    }

    /// <summary>
    /// Visits member access expressions to detect validation API usage.
    /// </summary>
    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var symbolInfo = TryGetSymbolInfo(node);
        if (symbolInfo.HasValue && symbolInfo.Value.Symbol != null)
        {
            var symbol = symbolInfo.Value.Symbol;
            var containingType = symbol.ContainingType?.ToDisplayString();

            // Detect Validation.HasError
            if (node.Name.Identifier.Text == "HasError" &&
                containingType == "System.Windows.Controls.Validation")
            {
                _validationHasErrorUsages++;

                Diagnostics.AddWarning(
                    "VALIDATION_HAS_ERROR",
                    $"Validation.HasError is not available in Avalonia. " +
                    $"Replace with: DataValidationErrors.HasErrors attached property. " +
                    $"Add using: using Avalonia.Controls;",
                    null);
            }
            // Detect Validation.Errors
            else if (node.Name.Identifier.Text == "Errors" &&
                    containingType == "System.Windows.Controls.Validation")
            {
                _validationErrorsUsages++;

                Diagnostics.AddWarning(
                    "VALIDATION_ERRORS",
                    $"Validation.Errors is not available in Avalonia. " +
                    $"Replace with: DataValidationErrors.Errors attached property. " +
                    $"Returns IEnumerable<object> of validation errors.",
                    null);
            }
            // Detect Validation.ErrorTemplate
            else if (node.Name.Identifier.Text == "ErrorTemplate" &&
                    containingType == "System.Windows.Controls.Validation")
            {
                Diagnostics.AddWarning(
                    "VALIDATION_ERROR_TEMPLATE",
                    $"Validation.ErrorTemplate is not directly equivalent in Avalonia. " +
                    $"Use DataValidationErrors.ErrorTemplate or style DataValidationErrors pseudo-class. " +
                    $"Avalonia uses :error pseudo-class for styling validation errors.",
                    null);
            }
            // Detect ValidationRules property
            else if (node.Name.Identifier.Text == "ValidationRules")
            {
                _bindingValidationRules++;

                Diagnostics.AddWarning(
                    "BINDING_VALIDATION_RULES",
                    $"Binding.ValidationRules is not supported in Avalonia. " +
                    $"Move validation logic to:\\n" +
                    $"1. INotifyDataErrorInfo implementation on ViewModel\\n" +
                    $"2. DataAnnotations attributes on properties\\n" +
                    $"3. Property setters with DataValidationErrors.SetErrors()",
                    null);
            }
        }

        return base.VisitMemberAccessExpression(node);
    }

    /// <summary>
    /// Visits method declarations to detect Validate() method implementations.
    /// </summary>
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var methodName = node.Identifier.Text;

        // Detect ValidationRule.Validate() override
        if (methodName == "Validate")
        {
            var classDeclaration = node.Parent as ClassDeclarationSyntax;
            if (classDeclaration != null && InheritsFromValidationRule(classDeclaration))
            {
                Diagnostics.AddWarning(
                    "VALIDATE_METHOD_NOT_SUPPORTED",
                    $"ValidationRule.Validate() method is not supported in Avalonia. " +
                    $"Migrate validation logic to INotifyDataErrorInfo or use DataAnnotations.",
                    null);

                // Analyze validation logic
                var methodBody = node.Body?.ToString() ?? "";

                if (methodBody.Contains("ValidationResult.ValidResult"))
                {
                    Diagnostics.AddInfo(
                        "VALIDATION_RESULT_VALID",
                        $"ValidationResult.ValidResult → Return empty errors collection in INotifyDataErrorInfo",
                        null);
                }

                if (methodBody.Contains("new ValidationResult"))
                {
                    Diagnostics.AddInfo(
                        "VALIDATION_RESULT_ERROR",
                        $"new ValidationResult(false, ...) → Add error to HasErrors collection in INotifyDataErrorInfo",
                        null);
                }
            }
        }

        return base.VisitMethodDeclaration(node);
    }

    /// <summary>
    /// Visits object creation expressions to detect ValidationResult instantiation.
    /// </summary>
    public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        var symbolInfo = TryGetSymbolInfo(node);
        if (symbolInfo.HasValue && symbolInfo.Value.Symbol is IMethodSymbol methodSymbol)
        {
            var typeName = methodSymbol.ContainingType?.ToDisplayString();

            if (typeName == "System.Windows.Controls.ValidationResult")
            {
                Diagnostics.AddWarning(
                    "VALIDATION_RESULT_NOT_SUPPORTED",
                    $"ValidationResult is not used in Avalonia. " +
                    $"Use INotifyDataErrorInfo.GetErrors() to return error messages as strings or custom objects.",
                    null);
            }
        }

        return base.VisitObjectCreationExpression(node);
    }

    /// <summary>
    /// Checks if a class inherits from ValidationRule.
    /// </summary>
    private bool InheritsFromValidationRule(ClassDeclarationSyntax classDeclaration)
    {
        if (classDeclaration.BaseList == null)
        {
            return false;
        }

        foreach (var baseType in classDeclaration.BaseList.Types)
        {
            var baseTypeName = baseType.Type.ToString();
            if (baseTypeName.Contains("ValidationRule"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Visits using directives to detect validation namespaces.
    /// </summary>
    public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
    {
        var usingName = node.Name?.ToString() ?? "";

        if (usingName == "System.Windows.Controls" || usingName.StartsWith("System.Windows.Controls."))
        {
            // Could be using Validation class
            Diagnostics.AddInfo(
                "VALIDATION_USING_DIRECTIVE",
                $"If using WPF Validation class from {usingName}, " +
                $"add Avalonia equivalents: using Avalonia.Controls; (for DataValidationErrors)",
                null);
        }

        return base.VisitUsingDirective(node);
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
            return null;
        }
    }
}
