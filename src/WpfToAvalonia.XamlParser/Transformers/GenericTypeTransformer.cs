using WpfToAvalonia.XamlParser.UnifiedAst;
using System.Text.RegularExpressions;

namespace WpfToAvalonia.XamlParser.Transformers;

/// <summary>
/// Transforms WPF generic type parameters to Avalonia generic type parameters.
/// </summary>
/// <remarks>
/// Handles x:TypeArguments attribute on generic types:
/// - WPF syntax: x:TypeArguments="sys:String" or x:TypeArguments="local:MyType"
/// - Avalonia syntax: Same, but namespace mappings may differ
/// - Transforms generic collection types (List&lt;T&gt;, Dictionary&lt;K,V&gt;, etc.)
/// - Handles nested generics
/// </remarks>
public class GenericTypeTransformer : IXamlTransformer
{
    public string Name => "GenericTypeTransformer";
    public int Priority => 25; // Run early, after namespace transformation

    private static readonly Dictionary<string, string> GenericTypeMappings = new()
    {
        // Map WPF generic types to Avalonia equivalents (if different)
        // Most generic types are the same, but collection types from WPF-specific namespaces need attention

        // System.Collections types (same in both)
        { "List", "List" },
        { "Dictionary", "Dictionary" },
        { "ObservableCollection", "ObservableCollection" },
        { "Collection", "Collection" },

        // WPF-specific generic types that might need mapping
        { "FreezableCollection", "AvaloniaList" }, // Avalonia doesn't have FreezableCollection
    };

    public void Transform(UnifiedXamlDocument document, TransformationContext context)
    {
        if (document.Root == null)
        {
            context.Diagnostics.AddWarning(
                "GENERIC_TYPE_TRANSFORM_NO_ROOT",
                "Document has no root element",
                null);
            return;
        }

        context.Diagnostics.AddInfo(
            "GENERIC_TYPE_TRANSFORM_START",
            "Starting generic type transformation",
            null);

        // Transform all elements
        TransformElementGenericTypes(document.Root, context);

        foreach (var descendant in document.Root.Descendants())
        {
            TransformElementGenericTypes(descendant, context);
        }

        context.Diagnostics.AddInfo(
            "GENERIC_TYPE_TRANSFORM_COMPLETE",
            $"Generic type transformation complete",
            null);
    }

    private void TransformElementGenericTypes(UnifiedXamlElement element, TransformationContext context)
    {
        // Check if element has x:TypeArguments attribute
        var typeArgumentsProperty = element.Properties.FirstOrDefault(p => p.PropertyName == "TypeArguments");

        if (typeArgumentsProperty != null)
        {
            TransformTypeArguments(element, typeArgumentsProperty, context);
        }

        // Check if the element type name itself contains generic parameters
        // (though this is rare in XAML, usually x:TypeArguments is used)
        if (element.TypeName.Contains("`") || element.TypeName.Contains("<"))
        {
            TransformGenericTypeName(element, context);
        }

        // Transform WPF-specific generic collection types
        TransformGenericCollectionType(element, context);
    }

    private void TransformTypeArguments(UnifiedXamlElement element, UnifiedXamlProperty typeArgumentsProperty, TransformationContext context)
    {
        if (typeArgumentsProperty.Value is not string typeArgsValue)
        {
            return;
        }

        var originalValue = typeArgsValue;
        context.Diagnostics.AddInfo(
            "GENERIC_TYPE_ARGUMENTS_FOUND",
            $"Found x:TypeArguments=\"{originalValue}\" on {element.TypeName}",
            null);

        // Parse type arguments (can be comma-separated for multiple type parameters)
        var typeArgs = ParseTypeArguments(typeArgsValue);
        var transformedTypeArgs = new List<string>();
        bool wasTransformed = false;

        foreach (var typeArg in typeArgs)
        {
            var transformedArg = TransformTypeArgument(typeArg, context, out var argWasTransformed);
            transformedTypeArgs.Add(transformedArg);
            wasTransformed |= argWasTransformed;
        }

        if (wasTransformed)
        {
            var newValue = string.Join(",", transformedTypeArgs);
            typeArgumentsProperty.Value = newValue;
            context.Diagnostics.AddInfo(
                "GENERIC_TYPE_ARGUMENTS_TRANSFORMED",
                $"Transformed x:TypeArguments: '{originalValue}' → '{newValue}'",
                null);
            context.Statistics.PropertiesTransformed++;
        }
        else
        {
            context.Diagnostics.AddInfo(
                "GENERIC_TYPE_ARGUMENTS_COMPATIBLE",
                $"x:TypeArguments=\"{originalValue}\" is compatible with Avalonia",
                null);
        }

        context.Statistics.IncrementCount("TypeArguments");
    }

    private List<string> ParseTypeArguments(string typeArgsValue)
    {
        // Parse comma-separated type arguments
        // Handle nested generics like "Dictionary(String, List(Int32))"
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        int depth = 0;

        foreach (var ch in typeArgsValue)
        {
            if (ch == '(' || ch == '<')
            {
                depth++;
                current.Append(ch);
            }
            else if (ch == ')' || ch == '>')
            {
                depth--;
                current.Append(ch);
            }
            else if (ch == ',' && depth == 0)
            {
                // Top-level comma separator
                var typeArg = current.ToString().Trim();
                if (!string.IsNullOrEmpty(typeArg))
                {
                    result.Add(typeArg);
                }
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        // Add the last type argument
        var lastArg = current.ToString().Trim();
        if (!string.IsNullOrEmpty(lastArg))
        {
            result.Add(lastArg);
        }

        return result;
    }

    private string TransformTypeArgument(string typeArg, TransformationContext context, out bool wasTransformed)
    {
        wasTransformed = false;
        var trimmed = typeArg.Trim();

        // Type argument format: "prefix:TypeName" or just "TypeName"
        // May also have nested generics: "collections:List(sys:String)"

        // Extract namespace prefix and type name
        var parts = trimmed.Split(':');
        string prefix = parts.Length > 1 ? parts[0] : string.Empty;
        string typeName = parts.Length > 1 ? parts[1] : parts[0];

        // Check for generic type mappings
        var baseTypeName = typeName.Contains('(') ? typeName.Substring(0, typeName.IndexOf('(')) : typeName;

        if (GenericTypeMappings.TryGetValue(baseTypeName, out var avaloniaTypeName) && baseTypeName != avaloniaTypeName)
        {
            wasTransformed = true;
            var transformed = string.IsNullOrEmpty(prefix)
                ? avaloniaTypeName
                : $"{prefix}:{avaloniaTypeName}";

            // If there are nested generic parameters, preserve them
            if (typeName.Contains('('))
            {
                var genericParams = typeName.Substring(typeName.IndexOf('('));
                transformed += genericParams;
            }

            context.Diagnostics.AddInfo(
                "GENERIC_TYPE_MAPPED",
                $"Mapped generic type: {typeName} → {avaloniaTypeName}",
                null);

            return transformed;
        }

        return typeArg;
    }

    private void TransformGenericTypeName(UnifiedXamlElement element, TransformationContext context)
    {
        var typeName = element.TypeName;

        // CLR generic syntax uses backtick: List`1, Dictionary`2
        // XAML typically doesn't use this directly, but we handle it for completeness
        if (typeName.Contains("`"))
        {
            context.Diagnostics.AddInfo(
                "GENERIC_TYPE_CLR_SYNTAX",
                $"CLR generic syntax found: {typeName}. This is unusual in XAML. Use x:TypeArguments attribute instead.",
                null);
        }

        // C#-style generic syntax with angle brackets (rare in XAML)
        if (typeName.Contains("<") && typeName.Contains(">"))
        {
            context.Diagnostics.AddWarning(
                "GENERIC_TYPE_ANGLE_BRACKETS",
                $"C# generic syntax found: {typeName}. This is not valid in XAML. Use x:TypeArguments attribute instead.",
                null);
            context.Statistics.WarningsGenerated++;
        }
    }

    private void TransformGenericCollectionType(UnifiedXamlElement element, TransformationContext context)
    {
        // Check for WPF-specific generic collection types that need transformation
        if (GenericTypeMappings.TryGetValue(element.TypeName, out var avaloniaType) && element.TypeName != avaloniaType)
        {
            var originalType = element.TypeName;
            element.TypeName = avaloniaType;

            context.Diagnostics.AddInfo(
                "GENERIC_COLLECTION_TYPE_TRANSFORMED",
                $"Transformed generic collection type: {originalType} → {avaloniaType}",
                null);
            context.Statistics.ElementsTransformed++;
        }

        // Check for FreezableCollection specifically (common in WPF)
        if (element.TypeName.StartsWith("FreezableCollection"))
        {
            context.Diagnostics.AddWarning(
                "FREEZABLE_COLLECTION_WARNING",
                $"FreezableCollection found. Avalonia doesn't have Freezable types. Consider using AvaloniaList or ObservableCollection.",
                null);
            context.Statistics.WarningsGenerated++;
        }
    }
}
