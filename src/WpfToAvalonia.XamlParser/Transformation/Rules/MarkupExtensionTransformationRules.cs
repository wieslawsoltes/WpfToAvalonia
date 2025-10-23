using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation.Rules;

/// <summary>
/// Transforms x:Array markup extension (NOT supported in Avalonia).
/// WPF uses x:Array to create arrays in XAML, but Avalonia doesn't support this.
/// </summary>
public sealed class XArrayMarkupExtensionTransformer : MarkupExtensionTransformationRuleBase
{
    public override string Name => "XArrayToAlternative";
    public override int Priority => 200;

    public override bool CanTransformMarkupExtension(UnifiedXamlMarkupExtension markupExtension)
    {
        return markupExtension.ExtensionName == "x:Array" || markupExtension.ExtensionName == "Array";
    }

    public override UnifiedXamlMarkupExtension? TransformMarkupExtension(
        UnifiedXamlMarkupExtension markupExtension,
        TransformationContext context)
    {
        var typeName = markupExtension.Parameters.GetValueOrDefault("Type")?.ToString() ?? "Unknown";

        var guidance = $@"x:Array markup extension is NOT supported in Avalonia.

WPF Pattern:
<SomeProperty>
    <x:Array Type=""{{x:Type {typeName}}}"">
        <{typeName}>Item1</{typeName}>
        <{typeName}>Item2</{typeName}>
    </x:Array>
</SomeProperty>

Avalonia Alternatives:

Option 1: Use a Collection in Code-Behind
// C# code-behind
public ObservableCollection<{typeName}> Items {{ get; }} = new()
{{
    new {typeName}(...),
    new {typeName}(...),
}};

// XAML
<SomeControl ItemsSource=""{{Binding Items}}"" />

Option 2: Define Items Directly in XAML (for ItemsControl)
<ItemsControl>
    <ItemsControl.Items>
        <{typeName}>Item1</{typeName}>
        <{typeName}>Item2</{typeName}>
    </ItemsControl.Items>
</ItemsControl>

Option 3: Use x:CompileBindings with IEnumerable
// ViewModel
public IEnumerable<{typeName}> Items => new[] {{ item1, item2 }};

// XAML with compiled bindings
<ItemsControl ItemsSource=""{{Binding Items}}"" />

Recommendation: Use Option 1 (code-behind collection) for best performance and maintainability.";

        markupExtension.AddDiagnostic(
            "XARRAY_NOT_SUPPORTED",
            guidance,
            Core.Diagnostics.DiagnosticSeverity.Warning);

        context.RecordTransformation(
            Name,
            "MarkupExtension",
            $"x:Array with Type={typeName} requires alternative approach in Avalonia");

        return markupExtension;
    }
}

/// <summary>
/// Validates x:Static markup extension (supported in Avalonia with minor differences).
/// </summary>
public sealed class XStaticMarkupExtensionTransformer : MarkupExtensionTransformationRuleBase
{
    public override string Name => "XStaticValidation";
    public override int Priority => 150;

    public override bool CanTransformMarkupExtension(UnifiedXamlMarkupExtension markupExtension)
    {
        return markupExtension.ExtensionName == "x:Static" || markupExtension.ExtensionName == "Static";
    }

    public override UnifiedXamlMarkupExtension? TransformMarkupExtension(
        UnifiedXamlMarkupExtension markupExtension,
        TransformationContext context)
    {
        var member = markupExtension.PositionalArgument?.ToString() ??
                     markupExtension.Parameters.GetValueOrDefault("Member")?.ToString();

        if (string.IsNullOrEmpty(member))
        {
            markupExtension.AddDiagnostic(
                "XSTATIC_NO_MEMBER",
                "x:Static requires a Member parameter. Syntax: {x:Static namespace:TypeName.StaticMember}",
                Core.Diagnostics.DiagnosticSeverity.Warning);
        }
        else
        {
            // x:Static is supported in Avalonia - just record that we've seen it
            context.RecordTransformation(
                Name,
                "MarkupExtension",
                $"x:Static Member={member} - compatible with Avalonia");
        }

        return markupExtension;
    }
}

/// <summary>
/// Validates x:Type markup extension (fully supported in Avalonia).
/// </summary>
public sealed class XTypeMarkupExtensionTransformer : MarkupExtensionTransformationRuleBase
{
    public override string Name => "XTypeValidation";
    public override int Priority => 150;

    public override bool CanTransformMarkupExtension(UnifiedXamlMarkupExtension markupExtension)
    {
        return markupExtension.ExtensionName == "x:Type" || markupExtension.ExtensionName == "Type";
    }

    public override UnifiedXamlMarkupExtension? TransformMarkupExtension(
        UnifiedXamlMarkupExtension markupExtension,
        TransformationContext context)
    {
        var typeName = markupExtension.PositionalArgument?.ToString() ??
                       markupExtension.Parameters.GetValueOrDefault("TypeName")?.ToString();

        if (!string.IsNullOrEmpty(typeName))
        {
            // x:Type is fully supported in Avalonia
            context.RecordTransformation(
                Name,
                "MarkupExtension",
                $"x:Type TypeName={typeName} - compatible with Avalonia");
        }

        return markupExtension;
    }
}

/// <summary>
/// Validates x:Null markup extension (supported in Avalonia).
/// </summary>
public sealed class XNullMarkupExtensionTransformer : MarkupExtensionTransformationRuleBase
{
    public override string Name => "XNullValidation";
    public override int Priority => 150;

    public override bool CanTransformMarkupExtension(UnifiedXamlMarkupExtension markupExtension)
    {
        return markupExtension.ExtensionName == "x:Null" || markupExtension.ExtensionName == "Null";
    }

    public override UnifiedXamlMarkupExtension? TransformMarkupExtension(
        UnifiedXamlMarkupExtension markupExtension,
        TransformationContext context)
    {
        // x:Null is supported in Avalonia
        context.RecordTransformation(
            Name,
            "MarkupExtension",
            "x:Null - compatible with Avalonia");

        return markupExtension;
    }
}
