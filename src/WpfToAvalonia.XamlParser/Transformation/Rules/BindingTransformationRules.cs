using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformation.Rules;

/// <summary>
/// Base class for binding transformation rules.
/// WPF and Avalonia have similar binding syntax but with some differences.
/// </summary>
public abstract class BindingTransformationRuleBase : PropertyTransformationRuleBase
{
    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        return property.HasMarkupExtension &&
               property.MarkupExtension?.ExtensionName == "Binding";
    }
}

/// <summary>
/// Transforms basic WPF Binding expressions to Avalonia format.
/// Most binding syntax is compatible, but some parameters differ.
/// </summary>
public sealed class BasicBindingTransformationRule : BindingTransformationRuleBase
{
    public override string Name => "TransformBasicBinding";

    public override int Priority => 100; // High priority to run before other binding transformations

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        if (property.MarkupExtension == null) return property;

        var binding = property.MarkupExtension;

        // WPF: {Binding PropertyName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}
        // Avalonia: {Binding PropertyName, Mode=TwoWay} - most syntax is the same

        // Check for WPF-specific parameters that need transformation or removal
        var parametersToRemove = new List<string>();

        foreach (var param in binding.Parameters)
        {
            switch (param.Key)
            {
                case "UpdateSourceTrigger":
                    // Avalonia doesn't have UpdateSourceTrigger
                    // It always updates on PropertyChanged for TwoWay bindings
                    parametersToRemove.Add(param.Key);
                    context.RecordTransformation(
                        Name,
                        "Binding",
                        $"Removed UpdateSourceTrigger parameter (not needed in Avalonia)");
                    break;

                case "NotifyOnValidationError":
                    // Avalonia uses EnableDataValidation instead
                    if (param.Value?.ToString()?.ToLower() == "true")
                    {
                        binding.Parameters[param.Key] = "EnableDataValidation=True";
                        context.RecordTransformation(
                            Name,
                            "Binding",
                            "Transformed NotifyOnValidationError to EnableDataValidation");
                    }
                    else
                    {
                        parametersToRemove.Add(param.Key);
                    }
                    break;

                case "ValidatesOnDataErrors":
                case "ValidatesOnExceptions":
                    // These are combined into EnableDataValidation in Avalonia
                    parametersToRemove.Add(param.Key);
                    if (!binding.Parameters.ContainsKey("EnableDataValidation"))
                    {
                        binding.Parameters["EnableDataValidation"] = "True";
                        context.RecordTransformation(
                            Name,
                            "Binding",
                            $"Replaced {param.Key} with EnableDataValidation");
                    }
                    break;

                case "IsAsync":
                    // Avalonia doesn't support IsAsync parameter
                    parametersToRemove.Add(param.Key);
                    context.RecordTransformation(
                        Name,
                        "Binding",
                        "Removed IsAsync parameter (not supported in Avalonia)");
                    break;
            }
        }

        // Remove parameters that need to be removed
        foreach (var key in parametersToRemove)
        {
            binding.Parameters.Remove(key);
        }

        return property;
    }
}

/// <summary>
/// Transforms WPF RelativeSource bindings to Avalonia format.
/// WPF: {Binding RelativeSource={RelativeSource Mode=FindAncestor, AncestorType=Window}}
/// Avalonia: {Binding $parent[Window].PropertyName}
/// </summary>
public sealed class RelativeSourceBindingTransformationRule : BindingTransformationRuleBase
{
    public override string Name => "TransformRelativeSourceBinding";

    public override int Priority => 90;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        if (!base.CanTransformProperty(property)) return false;

        var binding = property.MarkupExtension;
        return binding?.Parameters.ContainsKey("RelativeSource") == true;
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        if (property.MarkupExtension == null) return property;

        var binding = property.MarkupExtension;

        if (binding.Parameters.TryGetValue("RelativeSource", out var relativeSourceValue))
        {
            var relativeSourceStr = relativeSourceValue?.ToString() ?? "";

            // Parse RelativeSource parameters
            if (relativeSourceStr.Contains("FindAncestor"))
            {
                // Extract AncestorType
                var ancestorTypeMatch = System.Text.RegularExpressions.Regex.Match(
                    relativeSourceStr,
                    @"AncestorType\s*=\s*(?:(?:\{x:Type\s+)?([a-zA-Z0-9_:]+))?");

                if (ancestorTypeMatch.Success && ancestorTypeMatch.Groups[1].Success)
                {
                    var ancestorType = ancestorTypeMatch.Groups[1].Value;

                    // Check for AncestorLevel
                    var levelMatch = System.Text.RegularExpressions.Regex.Match(
                        relativeSourceStr,
                        @"AncestorLevel\s*=\s*(\d+)");

                    var level = levelMatch.Success ? levelMatch.Groups[1].Value : "0";

                    // Transform to Avalonia syntax
                    // WPF: {Binding RelativeSource={RelativeSource FindAncestor, AncestorType=Window}, Path=Title}
                    // Avalonia: {Binding #^/Window/Title} or {Binding $parent[Window].Title}

                    var path = binding.Parameters.ContainsKey("Path")
                        ? binding.Parameters["Path"]?.ToString()
                        : binding.PositionalArgument?.ToString();

                    if (!string.IsNullOrEmpty(path))
                    {
                        // Use $parent syntax for Avalonia
                        binding.PositionalArgument = $"$parent[{ancestorType}].{path}";
                    }
                    else
                    {
                        binding.PositionalArgument = $"$parent[{ancestorType}]";
                    }

                    // Remove RelativeSource and Path parameters
                    binding.Parameters.Remove("RelativeSource");
                    binding.Parameters.Remove("Path");

                    context.RecordTransformation(
                        Name,
                        "Binding",
                        $"Transformed RelativeSource FindAncestor to $parent syntax");
                }
            }
            else if (relativeSourceStr.Contains("Self"))
            {
                // {Binding RelativeSource={RelativeSource Self}, Path=Width}
                // Avalonia uses $self or direct property reference

                var path = binding.Parameters.ContainsKey("Path")
                    ? binding.Parameters["Path"]?.ToString()
                    : binding.PositionalArgument?.ToString();

                if (!string.IsNullOrEmpty(path))
                {
                    binding.PositionalArgument = $"$self.{path}";
                }
                else
                {
                    binding.PositionalArgument = "$self";
                }

                binding.Parameters.Remove("RelativeSource");
                binding.Parameters.Remove("Path");

                context.RecordTransformation(
                    Name,
                    "Binding",
                    "Transformed RelativeSource Self to $self syntax");
            }
            else if (relativeSourceStr.Contains("TemplatedParent"))
            {
                // {Binding RelativeSource={RelativeSource TemplatedParent}, Path=Foreground}
                // Avalonia: {Binding $parent.Foreground} or {TemplateBinding Foreground}

                var path = binding.Parameters.ContainsKey("Path")
                    ? binding.Parameters["Path"]?.ToString()
                    : binding.PositionalArgument?.ToString();

                if (!string.IsNullOrEmpty(path))
                {
                    // For TemplatedParent, we could suggest TemplateBinding instead
                    context.RecordTransformation(
                        Name,
                        "Binding",
                        $"Consider using {{TemplateBinding {path}}} instead of RelativeSource TemplatedParent");

                    binding.PositionalArgument = $"$parent.{path}";
                }

                binding.Parameters.Remove("RelativeSource");
                binding.Parameters.Remove("Path");
            }
        }

        return property;
    }
}

/// <summary>
/// Transforms ElementName bindings.
/// WPF and Avalonia both support ElementName, but Avalonia also supports # syntax.
/// WPF: {Binding ElementName=MyTextBox, Path=Text}
/// Avalonia: {Binding #MyTextBox.Text} or {Binding ElementName=MyTextBox, Path=Text}
/// </summary>
public sealed class ElementNameBindingTransformationRule : BindingTransformationRuleBase
{
    public override string Name => "TransformElementNameBinding";

    public override int Priority => 85;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        if (!base.CanTransformProperty(property)) return false;

        var binding = property.MarkupExtension;
        return binding?.Parameters.ContainsKey("ElementName") == true;
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        if (property.MarkupExtension == null) return property;

        var binding = property.MarkupExtension;

        // ElementName bindings work the same in Avalonia
        // But we can optionally convert to # syntax for cleaner code
        if (context.Options.UseAvaloniaBindingSyntax)
        {
            if (binding.Parameters.TryGetValue("ElementName", out var elementName))
            {
                var path = binding.Parameters.ContainsKey("Path")
                    ? binding.Parameters["Path"]?.ToString()
                    : binding.PositionalArgument?.ToString();

                if (!string.IsNullOrEmpty(path))
                {
                    binding.PositionalArgument = $"#{elementName}.{path}";
                }
                else
                {
                    binding.PositionalArgument = $"#{elementName}";
                }

                binding.Parameters.Remove("ElementName");
                binding.Parameters.Remove("Path");

                context.RecordTransformation(
                    Name,
                    "Binding",
                    $"Converted ElementName binding to # syntax: #{elementName}");
            }
        }
        else
        {
            // Keep ElementName syntax - it works in Avalonia
            context.RecordTransformation(
                Name,
                "Binding",
                "ElementName binding preserved (compatible with Avalonia)");
        }

        return property;
    }
}

/// <summary>
/// Adds support for compiled bindings when requested.
/// WPF: {Binding PropertyName}
/// Avalonia (compiled): {CompiledBinding PropertyName}
/// </summary>
public sealed class CompiledBindingTransformationRule : BindingTransformationRuleBase
{
    public override string Name => "AddCompiledBindingSupport";

    public override int Priority => 50; // Lower priority, runs after other binding transformations

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        if (property.MarkupExtension == null) return property;

        // Only convert to CompiledBinding if the option is enabled
        if (context.Options.UseCompiledBindings)
        {
            var binding = property.MarkupExtension;

            // Check if this binding can be compiled
            // Compiled bindings require a DataType to be set on the control or ancestor
            // For now, just change the extension name
            binding.ExtensionName = "CompiledBinding";

            context.RecordTransformation(
                Name,
                "Binding",
                "Converted to CompiledBinding (requires DataType to be set)");

            // Note: The actual DataType attributes need to be added to the XAML elements
            // This should be done in a separate pass or flagged for manual review
        }

        return property;
    }
}

/// <summary>
/// Transforms WPF MultiBinding to Avalonia format.
/// WPF uses MultiBinding with IMultiValueConverter.
/// Avalonia 11+ supports MultiBinding but with different syntax.
/// </summary>
public sealed class MultiBindingTransformationRule : PropertyTransformationRuleBase
{
    public override string Name => "TransformMultiBinding";

    public override int Priority => 100;

    public override bool CanTransformProperty(UnifiedXamlProperty property)
    {
        return property.HasMarkupExtension &&
               property.MarkupExtension?.ExtensionName == "MultiBinding";
    }

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        if (property.MarkupExtension == null) return property;

        // MultiBinding is supported in Avalonia 11+ but with some differences
        // The basic syntax is similar, but converters may need adjustment

        var multiBinding = property.MarkupExtension;

        // Check for converter
        if (multiBinding.Parameters.ContainsKey("Converter"))
        {
            context.RecordTransformation(
                Name,
                "MultiBinding",
                "MultiBinding with converter requires manual review - ensure converter implements IMultiValueConverter");
        }

        // StringFormat is supported in both WPF and Avalonia
        if (multiBinding.Parameters.ContainsKey("StringFormat"))
        {
            context.RecordTransformation(
                Name,
                "MultiBinding",
                "StringFormat parameter preserved (compatible with Avalonia)");
        }

        return property;
    }
}

/// <summary>
/// Transforms binding paths that reference WPF-specific properties.
/// </summary>
public sealed class BindingPathTransformationRule : BindingTransformationRuleBase
{
    public override string Name => "TransformBindingPath";

    public override int Priority => 80;

    public override UnifiedXamlProperty? TransformProperty(UnifiedXamlProperty property, TransformationContext context)
    {
        if (property.MarkupExtension == null) return property;

        var binding = property.MarkupExtension;

        // Get the binding path
        var path = binding.Parameters.ContainsKey("Path")
            ? binding.Parameters["Path"]?.ToString()
            : binding.PositionalArgument?.ToString();

        if (!string.IsNullOrEmpty(path))
        {
            // Transform WPF property names to Avalonia equivalents
            var transformedPath = TransformPropertyPath(path);

            if (transformedPath != path)
            {
                if (binding.Parameters.ContainsKey("Path"))
                {
                    binding.Parameters["Path"] = transformedPath;
                }
                else
                {
                    binding.PositionalArgument = transformedPath;
                }

                context.RecordTransformation(
                    Name,
                    "Binding",
                    $"Transformed binding path: {path} → {transformedPath}");
            }
        }

        return property;
    }

    private string TransformPropertyPath(string path)
    {
        // Transform common WPF property names in binding paths
        var pathMappings = new Dictionary<string, string>
        {
            { "Visibility", "IsVisible" },
            { "IsReadOnly", "IsReadOnly" }, // Same in Avalonia
            { "SelectedItem", "SelectedItem" }, // Same
            // Add more mappings as needed
        };

        // Handle nested paths like "Parent.Visibility"
        foreach (var mapping in pathMappings)
        {
            if (path == mapping.Key)
            {
                return mapping.Value;
            }

            // Handle property paths with dots
            if (path.Contains($".{mapping.Key}"))
            {
                path = path.Replace($".{mapping.Key}", $".{mapping.Value}");
            }
        }

        return path;
    }
}
