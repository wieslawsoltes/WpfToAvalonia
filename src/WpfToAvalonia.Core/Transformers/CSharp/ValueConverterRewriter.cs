using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Visitors;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Rewrites value converter classes from WPF to Avalonia.
/// Implements task 2.5.7.1.3: Transform value converters
/// </summary>
/// <remarks>
/// WPF and Avalonia both support value converters but with some differences:
///
/// WPF IValueConverter:
/// - Located in: System.Windows.Data
/// - Interface: IValueConverter with Convert() and ConvertBack()
/// - Parameters: object value, Type targetType, object parameter, CultureInfo culture
/// - Used in XAML: {Binding Path=..., Converter={StaticResource MyConverter}}
///
/// Avalonia IValueConverter:
/// - Located in: Avalonia.Data.Converters
/// - Interface: IValueConverter with Convert() and ConvertBack()
/// - Parameters: object? value, Type targetType, object? parameter, CultureInfo culture
/// - Used in XAML: Same pattern, but located in different namespace
///
/// Common Transformations:
/// 1. Update using directives: System.Windows.Data → Avalonia.Data.Converters
/// 2. Update base interface namespace
/// 3. Handle nullability differences (Avalonia uses nullable types)
/// 4. Transform DependencyProperty.UnsetValue to AvaloniaProperty.UnsetValue
/// 5. Update Binding.DoNothing to BindingOperations.DoNothing (both frameworks)
///
/// Common Built-in Converters:
/// WPF → Avalonia mappings:
/// - BooleanToVisibilityConverter → BoolToVisibilityConverter (different behavior)
/// - StringFormatConverter → StringFormatValueConverter
/// - No direct equivalent for many WPF converters (need custom implementation)
///
/// Special Cases:
/// - IMultiValueConverter: Supported in both, but with namespace change
/// - IValueConverter&lt;TIn, TOut&gt;: Avalonia supports strongly-typed converters
/// - FuncValueConverter: Avalonia-specific, allows lambda expressions
/// </remarks>
public sealed class ValueConverterRewriter : WpfToAvaloniaRewriter
{
    private int _valueConverterImplementations;
    private int _multiValueConverterImplementations;
    private int _dependencyPropertyUnsetValue;
    private int _bindingDoNothing;
    private int _visibilityConverters;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueConverterRewriter"/> class.
    /// </summary>
    public ValueConverterRewriter(
        SemanticModel semanticModel,
        DiagnosticCollector diagnostics,
        IMappingRepository mappingRepository)
        : base(semanticModel, diagnostics, mappingRepository)
    {
    }

    /// <summary>
    /// Gets the number of IValueConverter implementations detected.
    /// </summary>
    public int ValueConverterImplementations => _valueConverterImplementations;

    /// <summary>
    /// Gets the number of IMultiValueConverter implementations detected.
    /// </summary>
    public int MultiValueConverterImplementations => _multiValueConverterImplementations;

    /// <summary>
    /// Gets the number of DependencyProperty.UnsetValue usages detected.
    /// </summary>
    public int DependencyPropertyUnsetValue => _dependencyPropertyUnsetValue;

    /// <summary>
    /// Gets the number of Binding.DoNothing usages detected.
    /// </summary>
    public int BindingDoNothing => _bindingDoNothing;

    /// <summary>
    /// Gets the number of visibility converters detected.
    /// </summary>
    public int VisibilityConverters => _visibilityConverters;

    /// <summary>
    /// Visits class declarations to detect value converter implementations.
    /// </summary>
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (node.BaseList != null)
        {
            foreach (var baseType in node.BaseList.Types)
            {
                var baseTypeName = baseType.Type.ToString();

                // Detect IValueConverter implementation
                if (baseTypeName == "IValueConverter" ||
                    baseTypeName == "System.Windows.Data.IValueConverter")
                {
                    _valueConverterImplementations++;

                    var className = node.Identifier.Text;

                    Diagnostics.AddInfo(
                        "VALUE_CONVERTER_DETECTED",
                        $"Value converter class '{className}' detected. " +
                        $"Update interface: System.Windows.Data.IValueConverter → Avalonia.Data.Converters.IValueConverter. " +
                        $"Ensure using directive: using Avalonia.Data.Converters;",
                        null);

                    // Check for specific converter patterns
                    if (className.Contains("Visibility") || className.Contains("BooleanTo"))
                    {
                        _visibilityConverters++;
                        Diagnostics.AddWarning(
                            "VISIBILITY_CONVERTER_BEHAVIOR",
                            $"Visibility converter '{className}' detected. " +
                            $"WPF Visibility.Collapsed vs Visible differs from Avalonia's IsVisible (bool). " +
                            $"Consider using IsVisible property directly or update converter logic. " +
                            $"Avalonia pattern: IsVisible=true/false instead of Visibility enum.",
                            null);
                    }

                    Diagnostics.AddInfo(
                        "VALUE_CONVERTER_NULLABILITY",
                        $"Avalonia's IValueConverter uses nullable parameters. " +
                        $"Update Convert signature: object? value, Type targetType, object? parameter, CultureInfo culture",
                        null);
                }
                // Detect IMultiValueConverter implementation
                else if (baseTypeName == "IMultiValueConverter" ||
                         baseTypeName == "System.Windows.Data.IMultiValueConverter")
                {
                    _multiValueConverterImplementations++;

                    var className = node.Identifier.Text;

                    Diagnostics.AddInfo(
                        "MULTI_VALUE_CONVERTER_DETECTED",
                        $"Multi-value converter class '{className}' detected. " +
                        $"Update interface: System.Windows.Data.IMultiValueConverter → Avalonia.Data.Converters.IMultiValueConverter. " +
                        $"Supported in Avalonia 11+. Update method signature to use object?[] values.",
                        null);
                }
            }
        }

        return base.VisitClassDeclaration(node);
    }

    /// <summary>
    /// Visits member access expressions to detect converter-related API usage.
    /// </summary>
    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var symbolInfo = TryGetSymbolInfo(node);
        if (symbolInfo.HasValue && symbolInfo.Value.Symbol != null)
        {
            var symbol = symbolInfo.Value.Symbol;
            var containingType = symbol.ContainingType?.ToDisplayString();

            // Detect DependencyProperty.UnsetValue
            if (node.Name.Identifier.Text == "UnsetValue" &&
                containingType == "System.Windows.DependencyProperty")
            {
                _dependencyPropertyUnsetValue++;

                Diagnostics.AddWarning(
                    "DEPENDENCY_PROPERTY_UNSET_VALUE",
                    $"DependencyProperty.UnsetValue needs to be updated for Avalonia. " +
                    $"Replace with: AvaloniaProperty.UnsetValue. " +
                    $"Add using: using Avalonia;",
                    null);
            }
            // Detect Binding.DoNothing
            else if (node.Name.Identifier.Text == "DoNothing" &&
                    containingType == "System.Windows.Data.Binding")
            {
                _bindingDoNothing++;

                Diagnostics.AddWarning(
                    "BINDING_DO_NOTHING",
                    $"Binding.DoNothing needs to be updated for Avalonia. " +
                    $"Replace with: BindingOperations.DoNothing. " +
                    $"Add using: using Avalonia.Data;",
                    null);
            }
        }

        return base.VisitMemberAccessExpression(node);
    }

    /// <summary>
    /// Visits method declarations to detect Convert/ConvertBack implementations.
    /// </summary>
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var methodName = node.Identifier.Text;

        if (methodName == "Convert" || methodName == "ConvertBack")
        {
            // Check if this is part of an IValueConverter implementation
            var classDeclaration = node.Parent as ClassDeclarationSyntax;
            if (classDeclaration != null && ImplementsValueConverter(classDeclaration))
            {
                // Check parameters for nullable annotations
                var parameters = node.ParameterList.Parameters;
                if (parameters.Count >= 1)
                {
                    var firstParam = parameters[0];
                    var paramTypeName = firstParam.Type?.ToString() ?? "";

                    if (paramTypeName == "object" && !paramTypeName.Contains("?"))
                    {
                        Diagnostics.AddInfo(
                            "CONVERTER_NULLABLE_PARAMETER",
                            $"Value converter {methodName} method should use nullable parameter. " +
                            $"Update: object value → object? value",
                            null);
                    }
                }

                // Check for common WPF patterns in method body
                var methodBody = node.Body?.ToString() ?? "";

                if (methodBody.Contains("Visibility.Collapsed") || methodBody.Contains("Visibility.Visible"))
                {
                    Diagnostics.AddWarning(
                        "VISIBILITY_ENUM_USAGE",
                        $"Converter uses WPF Visibility enum. " +
                        $"Avalonia uses IsVisible (bool) property. " +
                        $"Update converter to return true/false instead of Visibility values. " +
                        $"Or use Avalonia's IsVisible property directly in binding.",
                        null);
                }

                if (methodBody.Contains("DependencyProperty.UnsetValue"))
                {
                    Diagnostics.AddWarning(
                        "UNSET_VALUE_IN_CONVERTER",
                        $"Converter uses DependencyProperty.UnsetValue. " +
                        $"Replace with: AvaloniaProperty.UnsetValue",
                        null);
                }

                if (methodBody.Contains("Binding.DoNothing"))
                {
                    Diagnostics.AddWarning(
                        "DO_NOTHING_IN_CONVERTER",
                        $"Converter uses Binding.DoNothing. " +
                        $"Replace with: BindingOperations.DoNothing",
                        null);
                }
            }
        }

        return base.VisitMethodDeclaration(node);
    }

    /// <summary>
    /// Checks if a class implements IValueConverter or IMultiValueConverter.
    /// </summary>
    private bool ImplementsValueConverter(ClassDeclarationSyntax classDeclaration)
    {
        if (classDeclaration.BaseList == null)
        {
            return false;
        }

        foreach (var baseType in classDeclaration.BaseList.Types)
        {
            var baseTypeName = baseType.Type.ToString();
            if (baseTypeName.Contains("IValueConverter") || baseTypeName.Contains("IMultiValueConverter"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Visits using directives to detect converter namespaces.
    /// </summary>
    public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
    {
        var usingName = node.Name?.ToString() ?? "";

        if (usingName == "System.Windows.Data")
        {
            Diagnostics.AddInfo(
                "CONVERTER_USING_DIRECTIVE",
                $"WPF data namespace detected: {usingName}. " +
                $"Add Avalonia equivalent: using Avalonia.Data.Converters; " +
                $"Also consider: using Avalonia.Data;",
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
