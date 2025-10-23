using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvalonia.Core.Analyzers;

/// <summary>
/// Analyzes WPF dependency properties to determine if they should be converted to DirectProperty.
/// </summary>
public sealed class DirectPropertyAnalyzer
{
    private readonly SemanticModel _semanticModel;

    public DirectPropertyAnalyzer(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
    }

    /// <summary>
    /// Determines if a dependency property should be converted to DirectProperty instead of StyledProperty.
    /// </summary>
    public bool ShouldUseDirectProperty(PropertyDeclarationSyntax property, FieldDeclarationSyntax? dependencyPropertyField = null)
    {
        // DirectProperty is preferred when:
        // 1. Property is read-only
        // 2. Property has a backing field
        // 3. Property doesn't need styling/coercion/validation
        // 4. Property is performance-critical

        if (IsReadOnlyProperty(property))
        {
            return true;
        }

        if (HasBackingField(property))
        {
            return HasSimpleImplementation(property);
        }

        return false;
    }

    /// <summary>
    /// Finds the backing field for a property, if one exists.
    /// </summary>
    public FieldDeclarationSyntax? FindBackingField(PropertyDeclarationSyntax property)
    {
        var containingClass = property.Parent as ClassDeclarationSyntax;
        if (containingClass == null)
        {
            return null;
        }

        // Common patterns:
        // 1. _propertyName
        // 2. _PropertyName
        // 3. m_propertyName
        // 4. propertyName

        var propertyName = property.Identifier.Text;
        var possibleFieldNames = new[]
        {
            $"_{char.ToLower(propertyName[0])}{propertyName.Substring(1)}",
            $"_{propertyName}",
            $"m_{char.ToLower(propertyName[0])}{propertyName.Substring(1)}",
            $"{char.ToLower(propertyName[0])}{propertyName.Substring(1)}"
        };

        var fields = containingClass.Members.OfType<FieldDeclarationSyntax>();

        foreach (var field in fields)
        {
            foreach (var variable in field.Declaration.Variables)
            {
                if (possibleFieldNames.Contains(variable.Identifier.Text))
                {
                    return field;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Generates DirectProperty registration syntax.
    /// </summary>
    public string GenerateDirectPropertyRegistration(
        PropertyDeclarationSyntax property,
        string ownerType,
        FieldDeclarationSyntax? backingField)
    {
        var propertyName = property.Identifier.Text;
        var propertyType = property.Type.ToString();
        var isReadOnly = IsReadOnlyProperty(property);

        if (isReadOnly)
        {
            // Read-only DirectProperty
            return $"AvaloniaProperty.RegisterDirect<{ownerType}, {propertyType}>(" +
                   $"\"{propertyName}\", " +
                   $"o => o.{propertyName})";
        }
        else
        {
            // Read-write DirectProperty
            return $"AvaloniaProperty.RegisterDirect<{ownerType}, {propertyType}>(" +
                   $"\"{propertyName}\", " +
                   $"o => o.{propertyName}, " +
                   $"(o, v) => o.{propertyName} = v)";
        }
    }

    /// <summary>
    /// Generates the backing field for a DirectProperty if needed.
    /// </summary>
    public FieldDeclarationSyntax GenerateBackingField(PropertyDeclarationSyntax property, string? defaultValue = null)
    {
        var propertyName = property.Identifier.Text;
        var fieldName = $"_{char.ToLower(propertyName[0])}{propertyName.Substring(1)}";
        var propertyType = property.Type;

        var field = SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(propertyType)
            .WithVariables(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(fieldName)
                    .WithInitializer(defaultValue != null
                        ? SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(defaultValue))
                        : null!))))
        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));

        return field;
    }

    /// <summary>
    /// Transforms a CLR property wrapper to use DirectProperty pattern.
    /// </summary>
    public PropertyDeclarationSyntax TransformToDirectPropertyWrapper(
        PropertyDeclarationSyntax property,
        FieldDeclarationSyntax backingField)
    {
        var backingFieldName = backingField.Declaration.Variables.First().Identifier.Text;
        var propertyFieldName = $"{property.Identifier.Text}Property";

        // Generate getter and setter using RaiseAndSetIfChanged pattern
        var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                SyntaxFactory.IdentifierName(backingFieldName)))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        var setter = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                SyntaxFactory.ParseExpression($"SetAndRaise({propertyFieldName}, ref {backingFieldName}, value)")))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        var accessorList = SyntaxFactory.AccessorList(
            SyntaxFactory.List(new[] { getter, setter }));

        return property.WithAccessorList(accessorList);
    }

    private bool IsReadOnlyProperty(PropertyDeclarationSyntax property)
    {
        if (property.AccessorList == null)
        {
            return false;
        }

        var setter = property.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);

        // No setter = read-only
        if (setter == null)
        {
            return true;
        }

        // Private setter = read-only for public API
        return setter.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword));
    }

    private bool HasBackingField(PropertyDeclarationSyntax property)
    {
        if (property.AccessorList == null)
        {
            return false;
        }

        // Check if getter returns a field
        var getter = property.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
        if (getter == null)
        {
            return false;
        }

        // Look for field access in getter
        if (getter.ExpressionBody != null)
        {
            var expression = getter.ExpressionBody.Expression;
            return expression is IdentifierNameSyntax;
        }

        if (getter.Body != null)
        {
            var returnStatement = getter.Body.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();
            if (returnStatement != null)
            {
                return returnStatement.Expression is IdentifierNameSyntax;
            }
        }

        return false;
    }

    private bool HasSimpleImplementation(PropertyDeclarationSyntax property)
    {
        if (property.AccessorList == null)
        {
            return false;
        }

        // Simple implementation = just getting/setting a backing field
        // No complex logic, validation, coercion, etc.

        var getter = property.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
        var setter = property.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);

        // Check getter is simple
        if (getter != null)
        {
            if (getter.ExpressionBody != null)
            {
                // Expression body is simple
                return true;
            }

            if (getter.Body != null && getter.Body.Statements.Count > 1)
            {
                // Multiple statements = complex
                return false;
            }
        }

        // Check setter is simple
        if (setter != null)
        {
            if (setter.ExpressionBody != null)
            {
                // Expression body is simple
                return true;
            }

            if (setter.Body != null && setter.Body.Statements.Count > 1)
            {
                // Multiple statements = complex (validation, etc.)
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Analyzes if a property needs RaisePropertyChanged notification.
    /// </summary>
    public bool NeedsPropertyChangedNotification(PropertyDeclarationSyntax property)
    {
        // Check if the containing class implements INotifyPropertyChanged
        var containingClass = property.Parent as ClassDeclarationSyntax;
        if (containingClass == null)
        {
            return false;
        }

        if (containingClass.BaseList == null)
        {
            return false;
        }

        return containingClass.BaseList.Types.Any(t =>
            t.Type.ToString().Contains("INotifyPropertyChanged") ||
            t.Type.ToString().Contains("AvaloniaObject"));
    }
}
