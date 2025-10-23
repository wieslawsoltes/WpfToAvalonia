using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvalonia.Core.Diagnostics;

namespace WpfToAvalonia.Core.Transformers.CSharp;

/// <summary>
/// Transforms WPF DependencyProperty declarations to Avalonia StyledProperty or DirectProperty.
/// </summary>
public sealed class DependencyPropertyTransformer : CSharpSyntaxRewriter
{
    private readonly DiagnosticCollector _diagnostics;
    private readonly SemanticModel _semanticModel;

    public DependencyPropertyTransformer(DiagnosticCollector diagnostics, SemanticModel semanticModel)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
    }

    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        // Check if this is a DependencyProperty field
        if (!IsDependencyPropertyField(node))
        {
            return base.VisitFieldDeclaration(node);
        }

        var variable = node.Declaration.Variables.FirstOrDefault();
        if (variable == null || variable.Initializer?.Value == null)
        {
            return base.VisitFieldDeclaration(node);
        }

        // Analyze the DependencyProperty registration
        var analysis = AnalyzeDependencyProperty(node, variable);
        if (analysis == null)
        {
            return base.VisitFieldDeclaration(node);
        }

        // Determine if this should be StyledProperty or DirectProperty
        var useDirectProperty = ShouldUseDirectProperty(analysis);

        // Generate the appropriate Avalonia property
        var newField = useDirectProperty
            ? GenerateDirectProperty(node, analysis)
            : GenerateStyledProperty(node, analysis);

        _diagnostics.AddInfo(
            "DEPENDENCY_PROPERTY_TRANSFORMED",
            $"Transformed DependencyProperty '{analysis.PropertyName}' to {(useDirectProperty ? "DirectProperty" : "StyledProperty")}",
            node.GetLocation().GetLineSpan().Path,
            node.GetLocation().GetLineSpan().StartLinePosition.Line);

        return newField;
    }

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        // Check if this is a CLR property wrapper for a DependencyProperty
        if (!IsClrPropertyWrapper(node, out var getValueCall, out var setValueCall))
        {
            return base.VisitPropertyDeclaration(node);
        }

        // Transform GetValue/SetValue calls to Avalonia equivalents
        var newNode = node;

        if (getValueCall != null)
        {
            var newGetter = TransformGetter(node.AccessorList!, getValueCall);
            newNode = newNode.WithAccessorList(newGetter);
        }

        if (setValueCall != null)
        {
            var newSetter = TransformSetter(node.AccessorList!, setValueCall);
            newNode = newNode.WithAccessorList(newSetter);
        }

        return newNode;
    }

    private bool IsDependencyPropertyField(FieldDeclarationSyntax node)
    {
        var typeName = node.Declaration.Type.ToString();
        return typeName.Contains("DependencyProperty");
    }

    private bool IsClrPropertyWrapper(
        PropertyDeclarationSyntax node,
        out InvocationExpressionSyntax? getValueCall,
        out InvocationExpressionSyntax? setValueCall)
    {
        getValueCall = null;
        setValueCall = null;

        if (node.AccessorList == null)
        {
            return false;
        }

        // Check getter for GetValue call
        var getter = node.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
        if (getter?.Body != null || getter?.ExpressionBody != null)
        {
            InvocationExpressionSyntax? getterInvocation = null;

            if (getter.ExpressionBody != null)
            {
                getterInvocation = getter.ExpressionBody.Expression as InvocationExpressionSyntax;
            }
            else if (getter.Body != null)
            {
                getterInvocation = getter.Body.Statements.FirstOrDefault()?.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            }

            if (getterInvocation != null && IsGetValueCall(getterInvocation))
            {
                getValueCall = getterInvocation;
            }
        }

        // Check setter for SetValue call
        var setter = node.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);
        if (setter?.Body != null || setter?.ExpressionBody != null)
        {
            InvocationExpressionSyntax? setterInvocation = null;

            if (setter.ExpressionBody != null)
            {
                setterInvocation = setter.ExpressionBody.Expression as InvocationExpressionSyntax;
            }
            else if (setter.Body != null)
            {
                setterInvocation = setter.Body.Statements.FirstOrDefault()?.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            }

            if (setterInvocation != null && IsSetValueCall(setterInvocation))
            {
                setValueCall = setterInvocation;
            }
        }

        return getValueCall != null || setValueCall != null;
    }

    private bool IsGetValueCall(InvocationExpressionSyntax invocation)
    {
        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        return memberAccess?.Name.Identifier.Text == "GetValue";
    }

    private bool IsSetValueCall(InvocationExpressionSyntax invocation)
    {
        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        return memberAccess?.Name.Identifier.Text == "SetValue";
    }

    private DependencyPropertyAnalysis? AnalyzeDependencyProperty(FieldDeclarationSyntax field, VariableDeclaratorSyntax variable)
    {
        var initializer = variable.Initializer?.Value as InvocationExpressionSyntax;
        if (initializer == null)
        {
            return null;
        }

        var memberAccess = initializer.Expression as MemberAccessExpressionSyntax;
        if (memberAccess == null)
        {
            return null;
        }

        var methodName = memberAccess.Name.Identifier.Text;
        var isAttached = methodName == "RegisterAttached" || methodName == "RegisterAttachedReadOnly";
        var isReadOnly = methodName == "RegisterReadOnly" || methodName == "RegisterAttachedReadOnly";

        // Extract arguments
        var args = initializer.ArgumentList.Arguments;
        if (args.Count < 2)
        {
            return null;
        }

        var propertyName = ExtractStringLiteral(args[0].Expression);
        var propertyType = args[1].Expression.ToString();

        var ownerType = args.Count > 2 ? args[2].Expression.ToString() : null;
        var metadata = args.Count > 3 ? args[3].Expression : null;

        return new DependencyPropertyAnalysis
        {
            FieldName = variable.Identifier.Text,
            PropertyName = propertyName,
            PropertyType = propertyType,
            OwnerType = ownerType,
            IsAttached = isAttached,
            IsReadOnly = isReadOnly,
            Metadata = metadata,
            OriginalField = field
        };
    }

    private bool ShouldUseDirectProperty(DependencyPropertyAnalysis analysis)
    {
        // DirectProperty is preferred when:
        // 1. Property is read-only
        // 2. Property doesn't use complex metadata (coercion, validation)
        // 3. Property wraps a simple backing field

        if (analysis.IsReadOnly)
        {
            return true;
        }

        // Check if metadata is simple (just default value)
        if (analysis.Metadata != null)
        {
            var metadataStr = analysis.Metadata.ToString();
            if (metadataStr.Contains("CoerceValueCallback") ||
                metadataStr.Contains("ValidateValueCallback") ||
                metadataStr.Contains("PropertyChangedCallback"))
            {
                return false; // Use StyledProperty for complex metadata
            }
        }

        return false; // Default to StyledProperty for safety
    }

    private FieldDeclarationSyntax GenerateStyledProperty(FieldDeclarationSyntax original, DependencyPropertyAnalysis analysis)
    {
        // Generate: public static readonly StyledProperty<Type> PropertyNameProperty = ...
        var propertyFieldName = analysis.FieldName;

        // Build the registration call
        var registrationMethod = analysis.IsAttached ? "RegisterAttached" : "Register";
        if (analysis.IsReadOnly)
        {
            registrationMethod += "ReadOnly";
        }

        // Build registration syntax
        var registration = $"AvaloniaProperty.{registrationMethod}<{analysis.PropertyType}>(\"{analysis.PropertyName}\", {analysis.OwnerType ?? "typeof(OwnerClass)"})";

        if (analysis.Metadata != null)
        {
            // Transform WPF PropertyMetadata to Avalonia StyledPropertyMetadata
            var avaloniaMetadata = TransformMetadata(analysis.Metadata);
            registration = $"AvaloniaProperty.{registrationMethod}<{analysis.PropertyType}>(\"{analysis.PropertyName}\", {analysis.OwnerType ?? "typeof(OwnerClass)"}, {avaloniaMetadata})";
        }

        // Create new field declaration
        var newType = analysis.IsReadOnly
            ? $"DirectProperty<{analysis.OwnerType ?? "OwnerClass"}, {analysis.PropertyType}>"
            : $"StyledProperty<{analysis.PropertyType}>";

        var newDeclaration = SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName(newType))
            .WithVariables(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(propertyFieldName)
                    .WithInitializer(
                        SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.ParseExpression(registration))))))
        .WithModifiers(original.Modifiers);

        return newDeclaration;
    }

    private FieldDeclarationSyntax GenerateDirectProperty(FieldDeclarationSyntax original, DependencyPropertyAnalysis analysis)
    {
        // Generate: public static readonly DirectProperty<OwnerType, PropertyType> PropertyNameProperty = ...
        var propertyFieldName = analysis.FieldName;
        var ownerType = analysis.OwnerType ?? "OwnerClass";

        // DirectProperty requires getter and optional setter delegates
        var registration = $"AvaloniaProperty.RegisterDirect<{ownerType}, {analysis.PropertyType}>(\"{analysis.PropertyName}\", o => o.{analysis.PropertyName}, (o, v) => o.{analysis.PropertyName} = v)";

        if (analysis.IsReadOnly)
        {
            registration = $"AvaloniaProperty.RegisterDirect<{ownerType}, {analysis.PropertyType}>(\"{analysis.PropertyName}\", o => o.{analysis.PropertyName})";
        }

        var newType = $"DirectProperty<{ownerType}, {analysis.PropertyType}>";

        var newDeclaration = SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.ParseTypeName(newType))
            .WithVariables(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(propertyFieldName)
                    .WithInitializer(
                        SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.ParseExpression(registration))))))
        .WithModifiers(original.Modifiers);

        return newDeclaration;
    }

    private string TransformMetadata(ExpressionSyntax wpfMetadata)
    {
        // Transform WPF PropertyMetadata to Avalonia equivalent
        var metadataStr = wpfMetadata.ToString();

        // Basic transformation (this can be enhanced)
        if (metadataStr.StartsWith("new PropertyMetadata"))
        {
            // Extract default value and callbacks
            var args = ExtractConstructorArguments(wpfMetadata);

            if (args.Count == 1)
            {
                // Just default value
                return $"defaultValue: {args[0]}";
            }
            else if (args.Count == 2)
            {
                // Default value + PropertyChangedCallback
                return $"defaultValue: {args[0]}, notifying: {args[1]}";
            }
        }

        _diagnostics.AddWarning(
            "METADATA_TRANSFORM_INCOMPLETE",
            $"PropertyMetadata transformation may be incomplete: {metadataStr}",
            wpfMetadata.GetLocation().GetLineSpan().Path);

        return "/* TODO: Transform metadata */";
    }

    private AccessorListSyntax TransformGetter(AccessorListSyntax accessorList, InvocationExpressionSyntax getValueCall)
    {
        // Transform: (Type)GetValue(PropertyNameProperty)
        // To: GetValue(PropertyNameProperty)
        // Avalonia's GetValue already returns the correct type

        var getter = accessorList.Accessors.First(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);

        // Find the property field being accessed
        var propertyArg = getValueCall.ArgumentList.Arguments.FirstOrDefault();
        if (propertyArg == null)
        {
            return accessorList;
        }

        var newGetExpression = SyntaxFactory.ParseExpression($"GetValue({propertyArg.Expression})");

        var newGetter = getter.ExpressionBody != null
            ? getter.WithExpressionBody(SyntaxFactory.ArrowExpressionClause(newGetExpression))
            : getter.WithBody(SyntaxFactory.Block(
                SyntaxFactory.ReturnStatement(newGetExpression)));

        return accessorList.WithAccessors(
            SyntaxFactory.List(accessorList.Accessors.Select(a =>
                a.Kind() == SyntaxKind.GetAccessorDeclaration ? newGetter : a)));
    }

    private AccessorListSyntax TransformSetter(AccessorListSyntax accessorList, InvocationExpressionSyntax setValueCall)
    {
        // SetValue pattern is the same in Avalonia
        return accessorList;
    }

    private string ExtractStringLiteral(ExpressionSyntax expression)
    {
        if (expression is LiteralExpressionSyntax literal)
        {
            return literal.Token.ValueText;
        }

        return expression.ToString().Trim('"');
    }

    private List<string> ExtractConstructorArguments(ExpressionSyntax expression)
    {
        var args = new List<string>();

        if (expression is ObjectCreationExpressionSyntax objectCreation)
        {
            if (objectCreation.ArgumentList != null)
            {
                args.AddRange(objectCreation.ArgumentList.Arguments.Select(a => a.ToString()));
            }
        }

        return args;
    }

    private class DependencyPropertyAnalysis
    {
        public required string FieldName { get; init; }
        public required string PropertyName { get; init; }
        public required string PropertyType { get; init; }
        public string? OwnerType { get; init; }
        public bool IsAttached { get; init; }
        public bool IsReadOnly { get; init; }
        public ExpressionSyntax? Metadata { get; init; }
        public required FieldDeclarationSyntax OriginalField { get; init; }
    }
}
