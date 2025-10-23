# Avalonia XAML Compilation Architecture Analysis

**Date**: 2025-10-23
**Purpose**: Document key patterns and extension points from Avalonia's XamlX implementation for use in WPF-to-Avalonia migration tool

---

## Executive Summary

Avalonia uses XamlX library for XAML compilation with a sophisticated transformer pipeline architecture. The system supports both compile-time IL generation and runtime XAML parsing through System.Reflection.Emit (SRE).

**Key Insights**:
1. **Transformer Pipeline**: Ordered sequence of AST transformers that progressively enrich and transform XAML nodes
2. **Type System Abstraction**: `IXamlTypeSystem` provides unified interface for both compile-time and runtime type resolution
3. **Language Configuration**: `XamlLanguageTypeMappings` defines framework-specific XAML semantics
4. **Custom Value Converters**: Extensible conversion system for text-to-type transformations
5. **Dual Compilation Modes**: Compile-time (IL generation) and runtime (SRE-based) XAML loading

---

## 1. Core Architecture Components

### 1.1 Compiler Entry Point

**File**: `AvaloniaXamlIlCompiler.cs`

The main compiler class that orchestrates XAML parsing and transformation:

```csharp
class AvaloniaXamlIlCompiler : XamlILCompiler
{
    // Parse XAML string -> XamlDocument (AST)
    public XamlDocument Parse(string xaml, IXamlType? overrideRootType)

    // Transform AST through transformer pipeline
    public void Compile(XamlDocument document, ...)

    // Transform multiple documents together (for resources, includes)
    public void TransformGroup(IReadOnlyCollection<IXamlDocumentResource> documents)
}
```

**Key Pattern**: The compiler separates parsing (text → AST) from transformation (AST → enriched AST) from emission (AST → IL).

### 1.2 Language Configuration

**File**: `AvaloniaXamlIlLanguage.cs`

Defines Avalonia-specific XAML semantics:

```csharp
static (XamlLanguageTypeMappings language, XamlLanguageEmitMappings emit) Configure(IXamlTypeSystem typeSystem)
{
    var mappings = new XamlLanguageTypeMappings(typeSystem)
    {
        // Core interfaces
        SupportInitialize = typeSystem.GetType("System.ComponentModel.ISupportInitialize"),
        ProvideValueTarget = typeSystem.GetType("Avalonia.Markup.Xaml.IProvideValueTarget"),
        RootObjectProvider = typeSystem.GetType("Avalonia.Markup.Xaml.IRootObjectProvider"),

        // Metadata attributes
        XmlnsAttributes = { typeSystem.GetType("Avalonia.Metadata.XmlnsDefinitionAttribute") },
        ContentAttributes = { typeSystem.GetType("Avalonia.Metadata.ContentAttribute") },

        // Deferred content (templates)
        DeferredContentPropertyAttributes = { typeSystem.GetType("Avalonia.Metadata.TemplateContentAttribute") },

        // Custom type converters
        // ... (defined in AttributeResolver)
    };

    var emit = new XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult>
    {
        // Custom property emitters
        ProvideValueTargetPropertyEmitter = XamlIlAvaloniaPropertyHelper.EmitProvideValueTarget,

        // Context type builder callback
        ContextTypeBuilderCallback = definition =>
        {
            EmitNameScopeField(mappings, typeSystem, definition);
            EmitEagerParentStackProvider(mappings, typeSystem, definition, runtimeHelpers);
        }
    };

    return (mappings, emit);
}
```

**Key Pattern**: Centralized configuration separates framework semantics from core XamlX logic.

### 1.3 Type System Abstraction

Avalonia provides two `IXamlTypeSystem` implementations:

1. **SreTypeSystem** (Runtime): Uses System.Reflection
2. **CecilTypeSystem** (Compile-time): Uses Mono.Cecil for IL generation

**Key Types**:
- `IXamlType` - Represents a type (class, interface, struct, enum)
- `IXamlProperty` - Represents a property (CLR or dependency/styled property)
- `IXamlMethod` - Represents a method
- `IXamlAssembly` - Represents an assembly
- `IXamlCustomAttribute` - Represents an attribute

**Pattern for WPF Migration**: We need to create similar wrappers:
- `WpfTypeSystemProvider : IXamlTypeSystem`
- `WpfTypeWrapper : IXamlType`
- `WpfPropertyWrapper : IXamlProperty`
- etc.

---

## 2. Transformer Pipeline Architecture

### 2.1 Transformer Ordering

The compiler builds an ordered list of transformers that process the XAML AST:

```csharp
// Before everything else - name resolution and basic transformations
Transformers.Insert(0, new XNameTransformer());
Transformers.Insert(1, new IgnoredDirectivesTransformer());
Transformers.Insert(2, new AvaloniaXamlIlDesignPropertiesTransformer());
Transformers.Insert(3, new AvaloniaBindingExtensionTransformer());

// Targeted insertions - property resolution
InsertBefore<PropertyReferenceResolver>(
    new AvaloniaXamlIlResolveClassesPropertiesTransformer(),
    new AvaloniaXamlIlTransformInstanceAttachedProperties(),
    new AvaloniaXamlIlTransformSyntheticCompiledBindingMembers()
);

InsertAfter<PropertyReferenceResolver>(
    new AvaloniaXamlIlAvaloniaPropertyResolver(),
    new AvaloniaXamlIlReorderClassesPropertiesTransformer(),
    new AvaloniaXamlIlClassesTransformer()
);

// Content and value conversion
InsertBefore<ContentConvertTransformer>(
    new AvaloniaXamlIlControlThemeTransformer(),
    new AvaloniaXamlIlSelectorTransformer(),
    new AvaloniaXamlIlQueryTransformer(),
    new AvaloniaXamlIlBindingPathParser(),
    new AvaloniaXamlIlSetterTransformer(),
    // ... many more
);

// After everything else - final cleanup
Transformers.Add(new AvaloniaXamlIlMetadataRemover());
Transformers.Add(new AvaloniaXamlIlEnsureResourceDictionaryCapacityTransformer());
```

**Key Insight**: Transformers are carefully ordered to build up semantic information progressively. Early transformers resolve names and types, middle transformers handle framework-specific patterns (bindings, styles), late transformers clean up metadata.

### 2.2 Transformer Interface

```csharp
interface IXamlAstTransformer
{
    IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node);
}
```

**Context Provides**:
- `context.ParentNodes()` - Access to parent nodes in AST
- `context.GetAvaloniaTypes()` - Framework type cache
- `context.Configuration` - Language mappings and settings
- `context.Visit()` - Recurse into children

### 2.3 Example Transformer: Binding Extension

**File**: `AvaloniaBindingExtensionTransformer.cs`

```csharp
class AvaloniaBindingExtensionTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        // Handle x:CompileBindings directive
        if (node is XamlAstObjectNode obj)
        {
            foreach (var directive in obj.Children.OfType<XamlAstXmlDirective>())
            {
                if (directive.Namespace == XamlNamespaces.Xaml2006 &&
                    directive.Name == "CompileBindings")
                {
                    bool compileBindings = ParseBoolean(directive.Values[0]);
                    return new AvaloniaXamlIlCompileBindingsNode(obj, compileBindings);
                }
            }
        }

        // Convert <Binding> to <CompiledBinding> or <ReflectionBinding>
        if (node is XamlAstXmlTypeReference tref &&
            tref.Name == "Binding" &&
            tref.XmlNamespace == "https://github.com/avaloniaui")
        {
            var compileBindings = context.ParentNodes()
                .OfType<AvaloniaXamlIlCompileBindingsNode>()
                .FirstOrDefault()?.CompileBindings ?? CompileBindingsByDefault;

            tref.Name = compileBindings ? "CompiledBinding" : "ReflectionBinding";
        }

        return node;
    }
}
```

**Pattern**: Transformers can:
1. Inspect parent context (via `ParentNodes()`)
2. Replace nodes (return new node)
3. Modify nodes in-place
4. Recurse into children (via `context.Visit()`)

---

## 3. Custom Value Conversion

### 3.1 Type Converters

Avalonia defines custom type converters via `AttributeResolver`:

```csharp
class AttributeResolver : IXamlCustomAttributeResolver
{
    private readonly List<KeyValuePair<IXamlType, IXamlType>> _converters;

    public AttributeResolver(IXamlTypeSystem typeSystem, XamlLanguageTypeMappings mappings)
    {
        AddType(typeSystem.GetType("Avalonia.Media.IImage"),
                typeSystem.GetType("Avalonia.Markup.Xaml.Converters.BitmapTypeConverter"));
        AddType(typeSystem.GetType("System.Uri"),
                typeSystem.GetType("Avalonia.Markup.Xaml.Converters.AvaloniaUriTypeConverter"));
        AddType(typeSystem.GetType("System.TimeSpan"),
                typeSystem.GetType("Avalonia.Markup.Xaml.Converters.TimeSpanTypeConverter"));
        AddType(typeSystem.GetType("Avalonia.Media.FontFamily"),
                typeSystem.GetType("Avalonia.Markup.Xaml.Converters.FontFamilyTypeConverter"));
        // ... more converters
    }

    public IXamlCustomAttribute? GetCustomAttribute(IXamlType type, IXamlType attributeType)
    {
        if (attributeType.Equals(_typeConverterAttribute))
        {
            var conv = LookupConverter(type);
            if (conv != null)
                return new ConstructedAttribute(_typeConverterAttribute, [conv], null);
        }
        return null;
    }
}
```

### 3.2 Custom Value Converter Callback

```csharp
public static bool CustomValueConverter(
    AstTransformationContext context,
    IXamlAstValueNode node,
    IReadOnlyList<IXamlCustomAttribute>? customAttributes,
    IXamlType type,
    out IXamlAstValueNode? result)
{
    if (!(node is XamlAstTextNode textNode))
    {
        result = null;
        return false;
    }

    var text = textNode.Text;
    var types = context.GetAvaloniaTypes();

    // Try framework-specific intrinsic parsing
    if (AvaloniaXamlIlLanguageParseIntrinsics.TryConvert(context, node, text, type, types, out result))
        return true;

    // Handle AvaloniaProperty lookup (for Setter.Property="Foo")
    if (type.FullName == "Avalonia.AvaloniaProperty")
    {
        var scope = context.ParentNodes()
            .OfType<AvaloniaXamlIlTargetTypeMetadataNode>()
            .FirstOrDefault();

        if (scope == null)
            throw new XamlLoadException("Unable to find scope for AvaloniaProperty lookup", node);

        result = XamlIlAvaloniaPropertyHelper.CreateNode(context, text, scope.TargetType, node);
        return true;
    }

    result = null;
    return false;
}
```

**Pattern**: Custom value converters allow framework-specific string parsing (e.g., "Foreground" → ForegroundProperty).

---

## 4. Runtime XAML Compilation

### 4.1 Entry Point

**File**: `AvaloniaRuntimeXamlLoader.cs`

```csharp
public static class AvaloniaRuntimeXamlLoader
{
    public static object Load(string xaml, Assembly? localAssembly = null,
                              object? rootInstance = null, Uri? uri = null,
                              bool designMode = false)
    {
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
        {
            return Load(stream, localAssembly, rootInstance, uri, designMode);
        }
    }

    public static object Load(Stream stream, Assembly? localAssembly = null,
                              object? rootInstance = null, Uri? uri = null,
                              bool designMode = false)
        => AvaloniaXamlIlRuntimeCompiler.Load(
            new RuntimeXamlLoaderDocument(uri, rootInstance, stream),
            new RuntimeXamlLoaderConfiguration {
                DesignMode = designMode,
                LocalAssembly = localAssembly
            });
}
```

### 4.2 Runtime Compiler

**File**: `AvaloniaXamlIlRuntimeCompiler.cs`

Uses System.Reflection.Emit to generate IL at runtime:

```csharp
static void InitializeSre()
{
    // Create SRE type system
    if (_sreTypeSystem == null)
        _sreTypeSystem = new SreTypeSystem();

    // Create dynamic assembly
    if (_sreBuilder == null)
    {
        var name = new AssemblyName(Guid.NewGuid().ToString("N"));
        _sreAsm = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
        _sreBuilder = _sreAsm.DefineDynamicModule("XamlIlLoader.ildump");
    }

    // Configure Avalonia XAML language
    if (_sreMappings is null || _sreEmitMappings is null)
        (_sreMappings, _sreEmitMappings) = AvaloniaXamlIlLanguage.Configure(_sreTypeSystem);

    // Resolve XML namespaces
    if (_sreXmlns == null)
        _sreXmlns = XamlXmlnsMappings.Resolve(_sreTypeSystem, _sreMappings);

    // Generate context type
    if (_sreContextType == null)
        _sreContextType = XamlILContextDefinition.GenerateContextClass(
            _sreTypeSystem.CreateTypeBuilder(_sreBuilder.DefineType("XamlIlContext")),
            _sreTypeSystem, _sreMappings, _sreEmitMappings);
}
```

**Key Insight**: Same transformer pipeline works for both compile-time (Cecil) and runtime (SRE) scenarios.

---

## 5. Key Patterns for WPF Migration

### 5.1 Type System Bridge Pattern

**What we need**:
```csharp
// Implement IXamlTypeSystem for WPF types
public class WpfTypeSystemProvider : IXamlTypeSystem
{
    private readonly Dictionary<Type, WpfTypeWrapper> _typeCache;
    private readonly Dictionary<Assembly, WpfAssemblyWrapper> _assemblyCache;

    public IXamlType FindType(string fullName) { ... }
    public IXamlAssembly FindAssembly(string name) { ... }
    public IEnumerable<IXamlAssembly> Assemblies => _assemblyCache.Values;
}

// Wrap CLR types
internal class WpfTypeWrapper : IXamlType
{
    private readonly Type _type;
    private readonly WpfTypeSystemProvider _typeSystem;

    public string FullName => _type.FullName;
    public IXamlType BaseType => _typeSystem.GetOrCreateType(_type.BaseType);
    public IReadOnlyList<IXamlProperty> Properties => LoadProperties();

    // Handle DependencyProperty detection
    private List<IXamlProperty> LoadProperties()
    {
        var properties = new List<IXamlProperty>();

        // CLR properties
        foreach (var prop in _type.GetProperties())
            properties.Add(new WpfPropertyWrapper(prop, this, _typeSystem));

        // Dependency properties (detect *Property fields)
        foreach (var field in _type.GetFields().Where(f => f.Name.EndsWith("Property")))
        {
            if (IsDependencyProperty(field))
                properties.Add(new WpfDependencyPropertyWrapper(field, this, _typeSystem));
        }

        return properties;
    }
}
```

### 5.2 XAML Language Configuration Pattern

**What we need**:
```csharp
public static class WpfXamlIlLanguage
{
    public static (XamlLanguageTypeMappings language, XamlLanguageEmitMappings emit) Configure(IXamlTypeSystem typeSystem)
    {
        var mappings = new XamlLanguageTypeMappings(typeSystem)
        {
            // WPF-specific attributes
            XmlnsAttributes = { typeSystem.GetType("System.Windows.Markup.XmlnsDefinitionAttribute") },
            ContentAttributes = { typeSystem.GetType("System.Windows.Markup.ContentPropertyAttribute") },

            // WPF service providers
            ProvideValueTarget = typeSystem.GetType("System.Windows.Markup.IProvideValueTarget"),
            RootObjectProvider = typeSystem.GetType("System.Windows.Markup.IRootObjectProvider"),

            // DependencyProperty system
            // ... custom handling needed
        };

        mappings.CustomAttributeResolver = new WpfAttributeResolver(typeSystem, mappings);

        // Custom value converter for WPF types
        var customConverter = new XamlValueConverter(WpfCustomValueConverter);

        return (mappings, null); // No emit mappings needed for parsing-only
    }

    public static bool WpfCustomValueConverter(
        AstTransformationContext context,
        IXamlAstValueNode node,
        IReadOnlyList<IXamlCustomAttribute>? customAttributes,
        IXamlType type,
        out IXamlAstValueNode? result)
    {
        if (!(node is XamlAstTextNode textNode))
        {
            result = null;
            return false;
        }

        // Handle DependencyProperty lookup (for Setter.Property="Foreground")
        if (type.FullName == "System.Windows.DependencyProperty")
        {
            // Create AST node for DependencyProperty resolution
            result = CreateDependencyPropertyNode(context, textNode.Text, ...);
            return true;
        }

        // Handle other WPF-specific conversions
        result = null;
        return false;
    }
}
```

### 5.3 Dual Parsing Strategy

**Recommendation**: Use XamlX for semantic analysis, but don't use IL emission. Instead:

1. **Parse with XamlX** (semantic layer):
   ```csharp
   var wpfTypeSystem = new WpfTypeSystemProvider(diagnostics);
   wpfTypeSystem.PreloadWpfAssemblies();

   var (languageMappings, emitMappings) = WpfXamlIlLanguage.Configure(wpfTypeSystem);
   var xmlnsMappings = XamlXmlnsMappings.Resolve(wpfTypeSystem, languageMappings);

   var compiler = new WpfXamlParser(
       new TransformerConfiguration(wpfTypeSystem, null, languageMappings, xmlnsMappings, customConverter));

   var document = compiler.Parse(wpfXamlText, overrideRootType: null);
   ```

2. **Parse with XDocument** (formatting layer):
   ```csharp
   var xmlDoc = XDocument.Parse(wpfXamlText, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
   ```

3. **Merge into Unified AST**:
   ```csharp
   var unifiedDoc = UnifiedXamlDocument.FromDualSource(xmlDoc, document);
   ```

4. **Transform**:
   ```csharp
   var transformer = new WpfToAvaloniaTransformer(mappingProvider, diagnostics);
   var avaloniaDoc = transformer.Transform(unifiedDoc);
   ```

5. **Serialize**:
   ```csharp
   var avaloniaXaml = avaloniaDoc.ToXDocument().ToString(SaveOptions.None);
   ```

---

## 6. Transformer Patterns to Implement

### 6.1 Namespace Transformer

```csharp
class WpfNamespaceTransformer : IUnifiedXamlTransformer
{
    public void Transform(UnifiedXamlElement element)
    {
        // xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        // -> xmlns="https://github.com/avaloniaui"

        if (element.XmlNamespace == "http://schemas.microsoft.com/winfx/2006/xaml/presentation")
            element.XmlNamespace = "https://github.com/avaloniaui";
    }
}
```

### 6.2 Control Type Transformer

```csharp
class WpfControlTypeTransformer : IUnifiedXamlTransformer
{
    public void Transform(UnifiedXamlElement element)
    {
        // <Window> -> <Window>
        // <TextBox> -> <TextBox>
        // Most controls map 1:1, but some need special handling

        if (element.Type?.FullName == "System.Windows.Controls.ListView")
        {
            element.TypeName = "ListBox"; // Avalonia doesn't have ListView
            AddWarning("ListView converted to ListBox - review view configuration");
        }
    }
}
```

### 6.3 Property Transformer

```csharp
class WpfPropertyTransformer : IUnifiedXamlTransformer
{
    public void Transform(UnifiedXamlProperty property)
    {
        // Visibility -> IsVisible
        if (property.Name == "Visibility")
        {
            property.Name = "IsVisible";

            // Convert value: Collapsed -> False, Visible -> True
            if (property.Value is UnifiedXamlLiteralValue literal)
            {
                if (literal.Text == "Collapsed" || literal.Text == "Hidden")
                    literal.Text = "False";
                else if (literal.Text == "Visible")
                    literal.Text = "True";
            }
        }
    }
}
```

### 6.4 Binding Transformer

```csharp
class WpfBindingTransformer : IUnifiedXamlTransformer
{
    public void Transform(UnifiedXamlMarkupExtension binding)
    {
        if (binding.TypeName != "Binding")
            return;

        // {Binding Path=Foo} -> {Binding Foo}
        // WPF allows Path as positional, Avalonia requires it

        // {Binding Mode=TwoWay} -> {Binding Mode=TwoWay}
        // Same syntax

        // {Binding RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type Window}}}
        // -> {Binding RelativeSource={RelativeSource FindAncestor, AncestorType=Window}}
        // Avalonia doesn't need x:Type wrapper
    }
}
```

---

## 7. Implementation Checklist

Based on Avalonia's architecture, here's what we need to implement:

### Phase 1: Type System Bridge ✅
- [x] WpfTypeSystemProvider implementing IXamlTypeSystem
- [x] WpfAssemblyWrapper implementing IXamlAssembly
- [x] WpfTypeWrapper implementing IXamlType
- [x] WpfPropertyWrapper implementing IXamlProperty
- [x] WpfMethodWrapper, WpfFieldWrapper, WpfConstructorWrapper
- [x] WpfDependencyPropertyWrapper (special handling)

### Phase 2: WPF XAML Language Definition
- [ ] WpfXamlIlLanguage.Configure() - Define WPF language mappings
- [ ] WpfAttributeResolver - Map WPF type converters
- [ ] WpfCustomValueConverter - Handle DependencyProperty lookup, etc.
- [ ] WPF xmlns mappings (http://schemas.microsoft.com/winfx/2006/xaml/presentation)

### Phase 3: Parsing Integration
- [ ] Create WpfXamlParser using XamlX
- [ ] Parse WPF XAML to XamlDocument (AST)
- [ ] Extract semantic information (types, properties, bindings)
- [ ] Handle WPF markup extensions ({Binding}, {StaticResource}, {DynamicResource}, {x:Type}, etc.)

### Phase 4: Unified AST
- [ ] UnifiedXamlDocument combining XML + XamlX semantic info
- [ ] Preserve formatting from XDocument
- [ ] Attach type info from XamlX AST
- [ ] Cross-reference nodes between layers

### Phase 5: Transformation
- [ ] WpfNamespaceTransformer
- [ ] WpfControlTypeTransformer
- [ ] WpfPropertyTransformer
- [ ] WpfBindingTransformer
- [ ] WpfStyleTransformer
- [ ] WpfResourceTransformer
- [ ] WpfTemplateTransformer

### Phase 6: Serialization
- [ ] UnifiedAstToXDocumentSerializer
- [ ] Preserve formatting where possible
- [ ] Generate valid Avalonia XAML
- [ ] Add diagnostic comments

---

## 8. Key Takeaways

1. **XamlX is Parser + Transformer, Not Emitter**: For our use case, we only need XamlX's parsing and AST capabilities, not IL emission.

2. **Type System is Central**: Everything flows through IXamlTypeSystem. Our WPF wrappers must faithfully represent WPF's type system.

3. **Transformers are Composable**: Build transformation pipeline from small, focused transformers.

4. **Context Matters**: Transformers can access parent nodes and metadata to make context-aware decisions.

5. **Dual Representation**: Keep both XML (for formatting) and semantic (for type safety) representations.

6. **Incremental Enrichment**: Start with minimal AST, progressively add semantic info through transformer pipeline.

7. **Framework Abstraction**: XamlX successfully abstracts XAML semantics from framework specifics through configuration and custom converters.

---

## 9. Next Steps

1. **Implement WpfXamlIlLanguage** (Task 2.5.3.1) - Define WPF language configuration
2. **Implement WPF Markup Extensions** (Task 2.5.3.2) - {Binding}, {StaticResource}, etc.
3. **Create WpfXamlParser** (Task 2.5.4.1) - Parse WPF XAML using XamlX
4. **Build Dual Parsing** (Task 2.5.4.0) - Merge XML + XamlX representations
5. **Implement Transformers** (Tasks 2.5.5.x) - Namespace, type, property transformations

---

## References

- `extern/Avalonia/src/Markup/Avalonia.Markup.Xaml.Loader/CompilerExtensions/`
- `extern/XamlX/src/XamlX/` - Core XamlX library
- Avalonia XAML docs: https://docs.avaloniaui.net/docs/concepts/xaml
