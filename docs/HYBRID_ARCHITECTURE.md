# Hybrid XML/XamlX/Roslyn XAML Transformation Architecture

## Executive Summary

This document describes the unified hybrid architecture that combines three powerful parsing and analysis engines:

1. **XML Parsing (XDocument)**: Fast, format-preserving, structure-focused
2. **XamlX Parsing**: Semantic analysis, type resolution, XAML-aware
3. **Roslyn Analysis**: C# code-behind semantic model, coordination

The hybrid approach provides the best of all worlds: speed, formatting preservation, type safety, and semantic understanding.

## Architecture Philosophy

### The Power of Three Layers

```
┌──────────────────────────────────────────────────────────┐
│                                                           │
│        ╔═══════════════════════════════════════╗        │
│        ║    UNIFIED XAML AST (Central Hub)     ║        │
│        ║                                        ║        │
│        ║  • UnifiedXamlDocument                 ║        │
│        ║  • UnifiedXamlElement                  ║        │
│        ║  • UnifiedXamlProperty                 ║        │
│        ║  • Metadata + Semantics + Formatting   ║        │
│        ╚═══════════════════════════════════════╝        │
│                         ▲                                 │
│         ┌───────────────┼───────────────┐                │
│         │               │               │                │
│         ▼               ▼               ▼                │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐       │
│  │ XML Layer   │ │ XamlX Layer │ │Roslyn Layer │       │
│  │             │ │             │ │             │       │
│  │• Structure  │ │• Types      │ │• C# Model   │       │
│  │• Formatting │ │• Properties │ │• Fields     │       │
│  │• Fast Parse │ │• Semantics  │ │• Events     │       │
│  │• Whitespace │ │• Validation │ │• Bindings   │       │
│  └─────────────┘ └─────────────┘ └─────────────┘       │
│         ▲               ▲               ▲                │
│         │               │               │                │
│         └───────────────┴───────────────┘                │
│                         │                                 │
│                    WPF XAML File                          │
│                                                           │
└──────────────────────────────────────────────────────────┘
```

### Why Hybrid?

| Capability | XML Only | XamlX Only | Hybrid (XML+XamlX+Roslyn) |
|------------|----------|------------|---------------------------|
| **Speed** | ⚡⚡⚡ Very Fast | ⚡ Slow | ⚡⚡ Fast |
| **Format Preservation** | ✅ Perfect | ❌ Lost | ✅ Perfect |
| **Type Resolution** | ❌ None | ✅ Complete | ✅ Complete |
| **Markup Extensions** | ❌ Text only | ✅ Semantic | ✅ Semantic |
| **Binding Validation** | ❌ None | ✅ Good | ✅ Excellent (w/ C#) |
| **Code-behind Sync** | ❌ None | ⚠️ Limited | ✅ Full |
| **Resource Resolution** | ⚠️ Basic | ✅ Complete | ✅ Complete |
| **Error Messages** | ⚠️ XML errors | ✅ Semantic | ✅ Both |
| **Transformation Safety** | ⚠️ Text-based | ✅ Type-safe | ✅ Type-safe |

## Unified AST Design

### Core Hierarchy

```csharp
/// <summary>
/// Base node in the unified XAML AST
/// </summary>
public abstract class UnifiedXamlNode
{
    // XML Layer
    public XNode? XmlNode { get; set; }
    public string? XmlPath { get; set; }  // XPath-like identifier

    // XamlX Layer
    public IXamlAstNode? SemanticNode { get; set; }
    public IXamlType? ResolvedType { get; set; }

    // Roslyn Layer (for code-behind coordination)
    public ISymbol? CodeBehindSymbol { get; set; }

    // Metadata
    public SourceLocation Location { get; set; }
    public FormattingHints Formatting { get; set; }
    public List<TransformationDiagnostic> Diagnostics { get; set; }

    // Transformation tracking
    public TransformationState State { get; set; }
}

/// <summary>
/// Represents a XAML element (e.g., <Button>)
/// </summary>
public class UnifiedXamlElement : UnifiedXamlNode
{
    // XML Layer
    public XElement XmlElement { get; set; }
    public XNamespace XmlNamespace { get; set; }

    // XamlX Layer
    public XamlAstObjectNode? SemanticObject { get; set; }
    public IXamlType ElementType { get; set; }  // Resolved type

    // Unified Structure
    public string TypeName { get; set; }
    public string? Namespace { get; set; }
    public List<UnifiedXamlProperty> Properties { get; set; }
    public List<UnifiedXamlElement> Children { get; set; }

    // Special properties
    public string? XName { get; set; }  // x:Name value
    public string? XKey { get; set; }   // x:Key value
    public string? XClass { get; set; } // x:Class value
}

/// <summary>
/// Represents a XAML property/attribute (e.g., Text="Hello")
/// </summary>
public class UnifiedXamlProperty : UnifiedXamlNode
{
    // XML Layer
    public XAttribute? XmlAttribute { get; set; }

    // XamlX Layer
    public IXamlAstPropertyAssignment? SemanticProperty { get; set; }
    public IXamlProperty? PropertyInfo { get; set; }  // Resolved property
    public IXamlType? PropertyType { get; set; }

    // Unified Structure
    public string PropertyName { get; set; }
    public object? Value { get; set; }
    public PropertyKind Kind { get; set; }  // Attribute, Element, Attached

    // For attached properties (Grid.Row)
    public string? AttachedOwnerType { get; set; }

    // For markup extensions
    public UnifiedXamlMarkupExtension? MarkupExtension { get; set; }
}

/// <summary>
/// Represents a markup extension (e.g., {Binding Path})
/// </summary>
public class UnifiedXamlMarkupExtension : UnifiedXamlNode
{
    // XamlX Layer (primary source for markup extensions)
    public IXamlAstNode SemanticExtension { get; set; }

    // Unified Structure
    public string ExtensionName { get; set; }  // "Binding", "StaticResource", etc.
    public Dictionary<string, object> Parameters { get; set; }

    // Specific extension types
    public BindingExpression? Binding { get; set; }
    public ResourceReference? Resource { get; set; }
    public TypeReference? Type { get; set; }
}

public class UnifiedXamlDocument
{
    public UnifiedXamlElement Root { get; set; }
    public XDocument XmlDocument { get; set; }
    public XamlDocument SemanticDocument { get; set; }
    public ResourceDictionary Resources { get; set; }
    public SymbolTable Symbols { get; set; }
    public List<TransformationDiagnostic> Diagnostics { get; set; }
}
```

### Formatting Preservation

```csharp
public class FormattingHints
{
    // Whitespace
    public string? LeadingWhitespace { get; set; }
    public string? TrailingWhitespace { get; set; }
    public string? InnerWhitespace { get; set; }
    public int IndentLevel { get; set; }

    // Line breaks
    public bool PreserveLineBreak { get; set; }
    public bool HasNewlineAfter { get; set; }

    // Comments
    public List<XComment> AssociatedComments { get; set; }

    // Original text (for fallback)
    public string? OriginalText { get; set; }
}
```

## Hybrid Parsing Pipeline

### Step-by-Step Process

```
┌─────────────────────────────────────────────────────────────┐
│                    WPF XAML File Input                       │
│                     (MainWindow.xaml)                        │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ├─────────────────────┐
                       │                     │
                       ▼                     ▼
         ┌─────────────────────┐  ┌──────────────────────┐
         │  XML Parser         │  │  XamlX Parser        │
         │  (XDocument)        │  │  (XamlDocument)      │
         │                     │  │                      │
         │  • Fast             │  │  • Semantic          │
         │  • Preserves format │  │  • Type resolution   │
         │  • Structure        │  │  • Validation        │
         └──────────┬──────────┘  └──────────┬───────────┘
                    │                        │
                    ▼                        ▼
         ┌─────────────────────┐  ┌──────────────────────┐
         │  XML AST            │  │  XamlX AST           │
         │  (XElement tree)    │  │  (XamlAstObjectNode) │
         └──────────┬──────────┘  └──────────┬───────────┘
                    │                        │
                    └────────────┬───────────┘
                                 │
                                 ▼
                    ┌────────────────────────┐
                    │   AST Merger           │
                    │   • Align by path      │
                    │   • Merge metadata     │
                    │   • Validate           │
                    └────────────┬───────────┘
                                 │
                                 ▼
                    ┌────────────────────────┐
                    │  UnifiedXamlDocument   │
                    │                        │
                    │  • XML + Semantics     │
                    │  • Full type info      │
                    │  • Format hints        │
                    └────────────┬───────────┘
                                 │
                    ┌────────────┴───────────┐
                    │                        │
                    ▼                        ▼
         ┌──────────────────┐    ┌──────────────────────┐
         │ Roslyn C# Parser │    │ Semantic Enrichment  │
         │ (code-behind)    │    │ • Resolve resources  │
         │                  │    │ • Validate bindings  │
         │ • Fields (x:Name)│    │ • Cross-reference    │
         │ • Event handlers │    └──────────────────────┘
         └──────────────────┘
                    │
                    ▼
         ┌──────────────────────────────────┐
         │   Fully Enriched                 │
         │   UnifiedXamlDocument             │
         │                                   │
         │   Ready for Transformation        │
         └───────────────────────────────────┘
```

### Implementation

```csharp
public class HybridXamlParser
{
    private readonly XamlParser _xmlParser;
    private readonly WpfXamlXParser _xamlXParser;
    private readonly RoslynCodeBehindAnalyzer _roslynAnalyzer;

    public UnifiedXamlDocument Parse(string xamlFilePath, string? codeFilePath = null)
    {
        // Step 1: Parse with XML parser (fast, preserves formatting)
        var xmlDoc = _xmlParser.ParseFile(xamlFilePath);
        var xmlAst = BuildXmlAst(xmlDoc);

        // Step 2: Parse with XamlX parser (semantic analysis)
        var xamlXDoc = _xamlXParser.Parse(xamlFilePath);
        var semanticAst = xamlXDoc.Root;

        // Step 3: Merge both ASTs
        var unifiedDoc = MergeAsts(xmlAst, semanticAst, xmlDoc, xamlXDoc);

        // Step 4: Enrich with Roslyn (code-behind)
        if (codeFilePath != null)
        {
            var codeModel = _roslynAnalyzer.Analyze(codeFilePath);
            EnrichWithCodeBehind(unifiedDoc, codeModel);
        }

        // Step 5: Semantic enrichment
        EnrichSemantics(unifiedDoc);

        return unifiedDoc;
    }

    private UnifiedXamlDocument MergeAsts(
        XElement xmlRoot,
        XamlAstObjectNode semanticRoot,
        XDocument xmlDoc,
        XamlDocument xamlXDoc)
    {
        var unified = new UnifiedXamlDocument
        {
            XmlDocument = xmlDoc,
            SemanticDocument = xamlXDoc,
            Root = MergeElement(xmlRoot, semanticRoot, "/")
        };

        return unified;
    }

    private UnifiedXamlElement MergeElement(
        XElement xmlElement,
        XamlAstObjectNode? semanticNode,
        string path)
    {
        var unified = new UnifiedXamlElement
        {
            // XML layer
            XmlNode = xmlElement,
            XmlElement = xmlElement,
            XmlPath = path,
            XmlNamespace = xmlElement.Name.Namespace,

            // XamlX layer
            SemanticNode = semanticNode,
            SemanticObject = semanticNode,
            ElementType = semanticNode?.Type,

            // Unified
            TypeName = xmlElement.Name.LocalName,
            Namespace = xmlElement.Name.Namespace.NamespaceName,

            // Formatting
            Formatting = ExtractFormatting(xmlElement),
            Location = GetSourceLocation(xmlElement)
        };

        // Merge properties
        foreach (var attr in xmlElement.Attributes())
        {
            var semanticProp = FindSemanticProperty(semanticNode, attr.Name.LocalName);
            unified.Properties.Add(MergeProperty(attr, semanticProp));
        }

        // Merge children
        int childIndex = 0;
        foreach (var child in xmlElement.Elements())
        {
            var semanticChild = GetSemanticChild(semanticNode, childIndex);
            var childPath = $"{path}/{child.Name.LocalName}[{childIndex}]";
            unified.Children.Add(MergeElement(child, semanticChild, childPath));
            childIndex++;
        }

        return unified;
    }
}
```

## Hybrid Transformation Strategy

### Transformation Routing

```csharp
public class HybridTransformationRouter
{
    public TransformationLayer SelectLayer(UnifiedXamlNode node, TransformationType type)
    {
        return type switch
        {
            // Fast XML transformations
            TransformationType.SimpleNamespaceChange => TransformationLayer.Xml,
            TransformationType.SimpleRename => TransformationLayer.Xml,
            TransformationType.AddComment => TransformationLayer.Xml,

            // Semantic transformations
            TransformationType.TypeConversion => TransformationLayer.Semantic,
            TransformationType.MarkupExtension => TransformationLayer.Semantic,
            TransformationType.BindingTransform => TransformationLayer.Semantic,
            TransformationType.PropertyTypeChange => TransformationLayer.Semantic,

            // Hybrid (both layers)
            TransformationType.AttachedProperty => TransformationLayer.Hybrid,
            TransformationType.ResourceReference => TransformationLayer.Hybrid,

            _ => TransformationLayer.Semantic  // Default to semantic for safety
        };
    }
}

public enum TransformationLayer
{
    Xml,       // Fast, format-preserving
    Semantic,  // Type-safe, complex
    Hybrid     // Use both layers
}
```

### Example: Property Transformation (Visibility → IsVisible)

```csharp
public class VisibilityToIsVisibleTransformer : HybridTransformer
{
    public override void Transform(UnifiedXamlProperty property)
    {
        if (property.PropertyName != "Visibility")
            return;

        // Strategy: Use semantic layer for type checking, XML for actual change

        // 1. Semantic layer: Validate property type
        if (property.PropertyType?.FullName != "System.Windows.Visibility")
        {
            AddWarning("Property named 'Visibility' but not of type System.Windows.Visibility");
            return;
        }

        // 2. Semantic layer: Parse value
        var visibilityValue = property.SemanticProperty.Value;
        bool isVisibleValue = ConvertVisibilityToBool(visibilityValue);

        // 3. XML layer: Apply transformation (preserves formatting)
        if (property.XmlAttribute != null)
        {
            property.XmlAttribute.Name = "IsVisible";
            property.XmlAttribute.Value = isVisibleValue.ToString();

            // Preserve whitespace around attribute
            // (already preserved in FormattingHints)
        }

        // 4. Update unified node
        property.PropertyName = "IsVisible";
        property.Value = isVisibleValue;
        property.State = TransformationState.Transformed;

        // 5. Update semantic layer (for further transformations)
        if (property.SemanticProperty != null)
        {
            // Create new semantic node with Avalonia property type
            property.PropertyType = GetAvaloniaIsVisibleType();
        }
    }
}
```

### Example: Binding Transformation (Semantic-heavy)

```csharp
public class BindingExpressionTransformer : HybridTransformer
{
    public override void Transform(UnifiedXamlProperty property)
    {
        if (property.MarkupExtension?.ExtensionName != "Binding")
            return;

        var binding = property.MarkupExtension.Binding;

        // Semantic layer: Analyze binding
        var path = binding.Path;
        var mode = binding.Mode;
        var converter = binding.Converter;

        // Use Roslyn to validate binding path against DataContext type
        if (property.CodeBehindSymbol != null)
        {
            var dataContextType = InferDataContextType(property);
            ValidateBindingPath(path, dataContextType);
        }

        // Transform WPF binding to Avalonia binding
        if (converter != null)
        {
            // Warn: Converters may need manual adjustment
            AddWarning($"Binding converter '{converter}' requires manual review");
        }

        // Update semantic layer
        binding.ConverterParameter = TransformConverterParameter(binding.ConverterParameter);

        // XML layer: Reconstruct binding string
        var avaloniaBindingString = BuildAvaloniaBindingString(binding);
        property.XmlAttribute.Value = avaloniaBindingString;

        property.State = TransformationState.Transformed;
    }
}
```

## Roslyn Integration for Code-Behind

### Code-Behind Synchronization

```csharp
public class CodeBehindSynchronizer
{
    private readonly RoslynCodeBehindAnalyzer _analyzer;
    private readonly CSharpFileTransformer _csharpTransformer;

    public void SynchronizeCodeBehind(
        UnifiedXamlDocument xamlDoc,
        Document codeDocument)
    {
        // 1. Extract x:Name elements from XAML
        var namedElements = xamlDoc.Root.DescendantsAndSelf()
            .Where(e => !string.IsNullOrEmpty(e.XName))
            .ToList();

        // 2. Analyze code-behind
        var semanticModel = _analyzer.GetSemanticModel(codeDocument);
        var classSymbol = _analyzer.GetPartialClass(semanticModel);

        // 3. Map XAML names to code-behind fields
        foreach (var element in namedElements)
        {
            var fieldSymbol = FindField(classSymbol, element.XName);
            if (fieldSymbol != null)
            {
                element.CodeBehindSymbol = fieldSymbol;

                // Validate type consistency
                var xamlType = element.ElementType?.FullName;
                var csharpType = fieldSymbol.Type.ToString();

                if (xamlType != csharpType)
                {
                    xamlDoc.Diagnostics.Add(new TransformationDiagnostic
                    {
                        Code = "WA0650",
                        Message = $"Type mismatch: XAML={xamlType}, C#={csharpType}",
                        Location = element.Location
                    });
                }
            }
        }

        // 4. Find event handlers in XAML
        var eventHandlers = ExtractEventHandlers(xamlDoc);

        // 5. Coordinate transformation with C# transformer
        foreach (var handler in eventHandlers)
        {
            _csharpTransformer.TransformEventHandler(
                handler.EventName,
                handler.HandlerName,
                handler.WpfType,
                handler.AvaloniaType);
        }
    }
}
```

## Performance Optimization

### Lazy Semantic Analysis

```csharp
public class LazySemanticEnrichment
{
    // Only analyze elements that actually need transformation
    public void EnrichOnDemand(UnifiedXamlElement element)
    {
        if (element.State == TransformationState.NeedsSemanticAnalysis)
        {
            // Parse with XamlX only for this subtree
            var semanticNode = _xamlXParser.ParseSubtree(element.XmlElement);
            element.SemanticNode = semanticNode;
            element.ElementType = semanticNode.Type;
            element.State = TransformationState.Analyzed;
        }
    }
}
```

### Caching Strategy

```csharp
public class HybridParserCache
{
    private ConcurrentDictionary<string, UnifiedXamlDocument> _cache = new();
    private ConcurrentDictionary<string, XDocument> _xmlCache = new();
    private ConcurrentDictionary<string, XamlDocument> _semanticCache = new();

    public UnifiedXamlDocument GetOrParse(string xamlPath)
    {
        return _cache.GetOrAdd(xamlPath, path =>
        {
            // Check if we can reuse XML or semantic caches
            var xmlDoc = _xmlCache.GetOrAdd(path, ParseXml);
            var semanticDoc = _semanticCache.GetOrAdd(path, ParseSemantic);

            return Merge(xmlDoc, semanticDoc);
        });
    }
}
```

## Benefits Summary

### Hybrid Approach Advantages

1. **Best Performance**: XML parsing for structure (fast), XamlX only where needed
2. **Perfect Formatting**: XML layer preserves all whitespace, comments, formatting
3. **Type Safety**: XamlX provides full type resolution and validation
4. **Complex Scenarios**: Handles bindings, markup extensions, resources semantically
5. **Code-Behind Sync**: Roslyn ensures XAML + C# stay in sync
6. **Incremental**: Can parse large files incrementally with XML, enrich on-demand
7. **Error Messages**: Both XML parse errors AND semantic errors
8. **Transformation Flexibility**: Choose XML (fast) or semantic (safe) per transformation
9. **Validation**: Validate at both structural (XML) and semantic (types) levels
10. **Debugging**: Can inspect both raw XML and semantic AST

## Next Steps

1. Implement `UnifiedXamlNode` hierarchy
2. Create XML → Unified AST converter
3. Create XamlX → Unified AST converter
4. Implement AST merger
5. Create hybrid transformation framework
6. Implement Roslyn code-behind synchronizer
7. Build transformation routing logic
8. Performance testing and optimization
9. Comprehensive test suite (all three layers)

## References

- **XamlX**: extern/XamlX for semantic parsing
- **Avalonia**: extern/Avalonia/src/Markup for reference implementation
- **System.Xml.Linq**: .NET XDocument for XML parsing
- **Roslyn**: Microsoft.CodeAnalysis for C# analysis
