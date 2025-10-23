using WpfToAvalonia.XamlParser.UnifiedAst;

namespace WpfToAvalonia.XamlParser.Transformers;

/// <summary>
/// Transforms WPF XAML namespaces to Avalonia namespaces.
/// </summary>
/// <remarks>
/// Transforms:
/// - xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" → xmlns="https://github.com/avaloniaui"
/// - xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" → xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" (unchanged)
/// - xmlns:d="http://schemas.microsoft.com/expression/blend/2008" → xmlns:d="http://schemas.microsoft.com/expression/blend/2008" (for design-time, unchanged)
/// - xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" → unchanged
/// </remarks>
public class NamespaceTransformer : IXamlTransformer
{
    public string Name => "NamespaceTransformer";
    public int Priority => 10; // Run early, before other transformers

    public void Transform(UnifiedXamlDocument document, TransformationContext context)
    {
        if (document.Root == null)
        {
            context.Diagnostics.AddWarning(
                "NAMESPACE_TRANSFORM_NO_ROOT",
                "Document has no root element",
                null);
            return;
        }

        context.Diagnostics.AddInfo(
            "NAMESPACE_TRANSFORM_START",
            "Starting namespace transformation",
            null);

        // Transform root element and all descendants
        TransformElementNamespace(document.Root, context);

        foreach (var descendant in document.Root.Descendants())
        {
            TransformElementNamespace(descendant, context);
        }

        // Update symbol table namespace prefixes
        TransformSymbolTableNamespaces(document, context);

        context.Diagnostics.AddInfo(
            "NAMESPACE_TRANSFORM_COMPLETE",
            $"Namespace transformation complete: {context.Statistics.NamespacesTransformed} namespaces transformed",
            null);
    }

    private void TransformElementNamespace(UnifiedXamlElement element, TransformationContext context)
    {
        if (element.Namespace == null)
            return;

        // Try to map WPF namespace to Avalonia namespace
        if (context.MappingProvider.TryGetNamespaceMapping(element.Namespace, out var avaloniaNamespace))
        {
            context.Diagnostics.AddInfo(
                "NAMESPACE_MAPPED",
                $"Mapping namespace: {element.Namespace} → {avaloniaNamespace}",
                null);

            element.Namespace = avaloniaNamespace;

            // Update XML namespace if XmlElement is present
            if (element.XmlElement != null && element.XmlNamespace != null)
            {
                // Note: XNamespace cannot be modified directly
                // The XmlElement will need to be recreated during serialization
                element.XmlNamespace = avaloniaNamespace;
            }

            context.Statistics.NamespacesTransformed++;
            context.Statistics.IncrementCount("Namespace");
        }
    }

    private void TransformSymbolTableNamespaces(UnifiedXamlDocument document, TransformationContext context)
    {
        // Create a new dictionary with mapped namespaces
        var newPrefixes = new Dictionary<string, string>();

        foreach (var (prefix, namespaceUri) in document.Symbols.NamespacePrefixes)
        {
            if (context.MappingProvider.TryGetNamespaceMapping(namespaceUri, out var avaloniaNamespace))
            {
                newPrefixes[prefix] = avaloniaNamespace!;

                context.Diagnostics.AddInfo(
                    "NAMESPACE_PREFIX_MAPPED",
                    $"Mapping prefix '{prefix}': {namespaceUri} → {avaloniaNamespace}",
                    null);
            }
            else
            {
                // Keep unmapped namespaces as-is
                newPrefixes[prefix] = namespaceUri;
            }
        }

        // Update symbol table
        document.Symbols.NamespacePrefixes.Clear();
        foreach (var (prefix, uri) in newPrefixes)
        {
            document.Symbols.NamespacePrefixes[prefix] = uri;
        }
    }
}
