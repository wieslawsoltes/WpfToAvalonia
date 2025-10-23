# WPF Runtime Assembly Loading & IL Rewriting Strategy

## Overview

This document outlines advanced techniques for loading and transforming WPF code at runtime, enabling direct use of WPF assemblies with minimal modifications through:

1. **Assembly Loading**: Load WPF assemblies directly into the transformation environment
2. **Type Forwarding**: Redirect WPF types to Avalonia equivalents
3. **IL Rewriting**: Transform MSIL bytecode to replace WPF API calls with Avalonia
4. **Build-Time Transformation**: MSBuild tasks and analyzers for compile-time transformation
5. **XAML Compilation**: Use WPF's own XAML compiler with post-processing

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                 WPF Application Source                       │
│  • WPF DLLs (PresentationFramework.dll, etc.)              │
│  • Application assemblies with WPF references              │
│  • XAML files compiled into baml resources                 │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
         ┌─────────────────────────────┐
         │   Assembly Loader           │
         │  • Resolve WPF assemblies   │
         │  • Load with MetadataLoadContext │
         │  • Inspect types & members  │
         └──────────────┬──────────────┘
                        │
            ┌───────────┴───────────┐
            │                       │
            ▼                       ▼
  ┌──────────────────┐   ┌──────────────────┐
  │ Type Forwarder   │   │ IL Rewriter      │
  │ • Type mapping   │   │ • Cecil/Mono.Cecil │
  │ • Assembly refs  │   │ • Rewrite calls  │
  │ • Custom binder  │   │ • Replace types  │
  └──────────┬───────┘   └────────┬─────────┘
             │                    │
             └──────────┬─────────┘
                        │
                        ▼
           ┌────────────────────────┐
           │ Transformed Assembly   │
           │ • Avalonia references  │
           │ • Rewritten IL         │
           │ • Type forwards        │
           └────────────────────────┘
```

## 1. Assembly Loading with MetadataLoadContext

### Concept

Load WPF assemblies in a reflection-only context to inspect types without executing code.

### Implementation

```csharp
public class WpfAssemblyLoader
{
    private MetadataLoadContext? _context;
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();

    public void Initialize(string[] referencePaths)
    {
        var resolver = new PathAssemblyResolver(referencePaths);
        _context = new MetadataLoadContext(resolver);
    }

    public Assembly LoadWpfAssembly(string assemblyName)
    {
        if (_loadedAssemblies.TryGetValue(assemblyName, out var cached))
            return cached;

        var assembly = _context!.LoadFromAssemblyName(assemblyName);
        _loadedAssemblies[assemblyName] = assembly;
        return assembly;
    }

    public IEnumerable<Type> GetTypesFromAssembly(Assembly assembly)
    {
        return assembly.GetTypes();
    }
}
```

### Benefits

- ✅ No need to actually run WPF code
- ✅ Can inspect private types and members
- ✅ Access to compiled XAML (baml) in resources
- ✅ Cross-platform inspection (inspect WPF on Linux/Mac)

## 2. Type Forwarding

### Concept

Use `TypeForwardedToAttribute` and custom assembly resolution to redirect WPF types to Avalonia.

### Strategy A: Assembly-Level Type Forwards

```csharp
// In a "WPF-to-Avalonia Bridge" assembly
[assembly: TypeForwardedTo(typeof(Avalonia.Controls.Button))]
[assembly: TypeForwardedTo(typeof(Avalonia.Controls.TextBox))]
// ... for all WPF types
```

### Strategy B: Custom AssemblyLoadContext

```csharp
public class AvaloniaTypeForwardingContext : AssemblyLoadContext
{
    private readonly Dictionary<string, Type> _typeForwards = new()
    {
        ["System.Windows.Controls.Button"] = typeof(Avalonia.Controls.Button),
        ["System.Windows.Controls.TextBox"] = typeof(Avalonia.Controls.TextBox),
        ["System.Windows.DependencyObject"] = typeof(Avalonia.AvaloniaObject),
        // ... mapping for all types
    };

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // If requesting WPF assembly, return Avalonia equivalent
        if (assemblyName.Name == "PresentationFramework")
        {
            return typeof(Avalonia.Controls.Control).Assembly;
        }
        return null;
    }

    public Type? ResolveType(string fullTypeName)
    {
        if (_typeForwards.TryGetValue(fullTypeName, out var avaloniaType))
        {
            return avaloniaType;
        }
        return null;
    }
}
```

### Strategy C: Source Generators (C# 9+)

```csharp
// Roslyn source generator that creates type forward stubs
[Generator]
public class WpfToAvaloniaTypeForwardGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        // Generate adapter classes that forward to Avalonia types
        var source = @"
namespace System.Windows.Controls
{
    // Adapter that forwards to Avalonia
    public class Button : Avalonia.Controls.Button
    {
        // WPF-compatible API surface
    }
}";
        context.AddSource("WpfTypeForwards.g.cs", source);
    }
}
```

## 3. IL Rewriting with Mono.Cecil

### Concept

Rewrite MSIL bytecode to replace WPF type references and method calls with Avalonia equivalents.

### Implementation

```csharp
using Mono.Cecil;
using Mono.Cecil.Cil;

public class WpfToAvaloniaILRewriter
{
    private readonly ModuleDefinition _module;
    private readonly Dictionary<string, string> _typeMapping;

    public void RewriteAssembly(string inputPath, string outputPath)
    {
        using var assembly = AssemblyDefinition.ReadAssembly(inputPath);

        foreach (var module in assembly.Modules)
        {
            // Rewrite type references
            RewriteTypeReferences(module);

            // Rewrite method calls
            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.HasBody)
                    {
                        RewriteMethodBody(method);
                    }
                }
            }
        }

        assembly.Write(outputPath);
    }

    private void RewriteTypeReferences(ModuleDefinition module)
    {
        foreach (var typeRef in module.GetTypeReferences())
        {
            if (_typeMapping.TryGetValue(typeRef.FullName, out var avaloniaType))
            {
                // Replace type reference
                var newTypeRef = module.ImportReference(
                    Type.GetType(avaloniaType)
                );
                // Update all usages
            }
        }
    }

    private void RewriteMethodBody(MethodDefinition method)
    {
        var il = method.Body.GetILProcessor();

        foreach (var instruction in method.Body.Instructions.ToList())
        {
            // Replace WPF API calls
            if (instruction.OpCode == OpCodes.Call ||
                instruction.OpCode == OpCodes.Callvirt)
            {
                if (instruction.Operand is MethodReference methodRef)
                {
                    var transformed = TransformMethodCall(methodRef);
                    if (transformed != null)
                    {
                        il.Replace(instruction,
                            il.Create(instruction.OpCode, transformed));
                    }
                }
            }

            // Replace property getters/setters
            // Example: get_Visibility -> get_IsVisible
            if (instruction.Operand is MethodReference propMethod)
            {
                if (propMethod.Name == "get_Visibility")
                {
                    var isVisibleGetter = GetAvaloniaMethod("get_IsVisible");
                    il.Replace(instruction,
                        il.Create(instruction.OpCode, isVisibleGetter));
                }
            }
        }
    }

    private MethodReference? TransformMethodCall(MethodReference wpfMethod)
    {
        // Map WPF method to Avalonia equivalent
        var mapping = new Dictionary<string, string>
        {
            ["System.Windows.DependencyObject::GetValue"] =
                "Avalonia.AvaloniaObject::GetValue",
            ["System.Windows.DependencyObject::SetValue"] =
                "Avalonia.AvaloniaObject::SetValue",
            // ... many more mappings
        };

        var key = $"{wpfMethod.DeclaringType.FullName}::{wpfMethod.Name}";
        if (mapping.TryGetValue(key, out var avaloniaMethod))
        {
            return CreateMethodReference(avaloniaMethod);
        }
        return null;
    }
}
```

### Advanced: Field Type Transformation

```csharp
private void TransformFields(TypeDefinition type)
{
    foreach (var field in type.Fields)
    {
        // Transform DependencyProperty fields to StyledProperty
        if (field.FieldType.FullName ==
            "System.Windows.DependencyProperty")
        {
            field.FieldType = module.ImportReference(
                typeof(Avalonia.StyledProperty<>));
        }
    }
}
```

## 4. Build-Time Transformation (MSBuild)

### MSBuild Task for IL Rewriting

```xml
<Project>
  <UsingTask
    TaskName="WpfToAvaloniaRewriteTask"
    AssemblyFile="WpfToAvalonia.Build.dll" />

  <Target Name="RewriteWpfToAvalonia" AfterTargets="Compile">
    <WpfToAvaloniaRewriteTask
      InputAssembly="$(TargetPath)"
      OutputAssembly="$(TargetPath).rewritten.dll"
      MappingFile="wpf-to-avalonia-mappings.json" />

    <!-- Replace original with rewritten -->
    <Move SourceFiles="$(TargetPath).rewritten.dll"
          DestinationFiles="$(TargetPath)" />
  </Target>
</Project>
```

### MSBuild Task Implementation

```csharp
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class WpfToAvaloniaRewriteTask : Task
{
    [Required]
    public string InputAssembly { get; set; }

    [Required]
    public string OutputAssembly { get; set; }

    public string MappingFile { get; set; }

    public override bool Execute()
    {
        try
        {
            var rewriter = new WpfToAvaloniaILRewriter();
            rewriter.LoadMappings(MappingFile);
            rewriter.RewriteAssembly(InputAssembly, OutputAssembly);

            Log.LogMessage(MessageImportance.High,
                $"Rewrote WPF assembly: {InputAssembly}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
            return false;
        }
    }
}
```

## 5. Roslyn Analyzers & Code Fixes

### Analyzer for WPF Usage Detection

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class WpfUsageAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "WPF001",
        title: "WPF type detected",
        messageFormat: "Type '{0}' is a WPF type, consider using Avalonia equivalent '{1}'",
        category: "Migration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        if (IsWpfType(namedType))
        {
            var avaloniaEquivalent = GetAvaloniaEquivalent(namedType);
            var diagnostic = Diagnostic.Create(Rule,
                namedType.Locations[0],
                namedType.Name,
                avaloniaEquivalent);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

### Code Fix Provider

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp)]
public class WpfToAvaloniaCodeFixProvider : CodeFixProvider
{
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Replace with Avalonia type",
                createChangedDocument: c => ReplaceWithAvaloniaTypeAsync(
                    context.Document, diagnostic, c),
                equivalenceKey: "ReplaceWithAvalonia"),
            diagnostic);
    }

    private async Task<Document> ReplaceWithAvaloniaTypeAsync(
        Document document,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        // Use Roslyn to rewrite syntax tree
        // Replace WPF type with Avalonia type
        // ...
    }
}
```

## 6. BAML (Binary XAML) Decompilation

### Concept

WPF compiles XAML to BAML (Binary Application Markup Language). We can decompile and transform it.

### Implementation

```csharp
public class BamlDecompiler
{
    public XDocument DecompileBaml(Stream bamlStream)
    {
        // Use BamlReader or third-party libraries
        // ILSpy.BamlDecompiler is a good option

        using var reader = new System.Windows.Markup.XamlReader();
        // Decompile BAML back to XAML
        var xaml = DecompileBamlToXaml(bamlStream);
        return XDocument.Parse(xaml);
    }

    public void TransformCompiledXaml(Assembly wpfAssembly)
    {
        var resources = wpfAssembly.GetManifestResourceNames()
            .Where(r => r.EndsWith(".baml"));

        foreach (var resourceName in resources)
        {
            using var stream = wpfAssembly.GetManifestResourceStream(resourceName);
            var xaml = DecompileBaml(stream);

            // Now transform using our hybrid engine
            var transformed = TransformXaml(xaml);

            // Compile to Avalonia XAML or embed as resource
        }
    }
}
```

## 7. Runtime Type Shimming

### Concept

Create runtime shims that present WPF API but delegate to Avalonia.

### Implementation

```csharp
// Shim assembly that can be referenced by WPF code
namespace System.Windows.Controls
{
    // This looks like WPF Button but uses Avalonia internally
    public class Button : IWpfControl
    {
        private readonly Avalonia.Controls.Button _avaloniaButton;

        public Button()
        {
            _avaloniaButton = new Avalonia.Controls.Button();
        }

        // WPF API surface
        public object Content
        {
            get => _avaloniaButton.Content;
            set => _avaloniaButton.Content = value;
        }

        public Visibility Visibility
        {
            get => _avaloniaButton.IsVisible ?
                   Visibility.Visible : Visibility.Collapsed;
            set => _avaloniaButton.IsVisible = (value == Visibility.Visible);
        }

        // Expose underlying Avalonia control
        public Avalonia.Controls.Button AvaloniaControl => _avaloniaButton;
    }

    // WPF enum for compatibility
    public enum Visibility
    {
        Visible,
        Hidden,
        Collapsed
    }
}
```

## 8. Incremental Migration Strategy

### Phase 1: Side-by-Side References

```csharp
// Allow both WPF and Avalonia references
// Use assembly aliases to disambiguate
extern alias WPF;
extern alias Avalonia;

using WpfButton = WPF::System.Windows.Controls.Button;
using AvaloniaButton = Avalonia::Avalonia.Controls.Button;

public class HybridControl
{
    // Can use both during migration
    private WpfButton? _wpfButton;
    private AvaloniaButton? _avaloniaButton;
}
```

### Phase 2: Gradual Type Replacement

Use analyzers to mark each replaced type:

```csharp
[WpfReplaced(typeof(Avalonia.Controls.Button))]
public class MyViewModel
{
    // Analyzer warns if WPF Button is used
    public ICommand ButtonCommand { get; set; }
}
```

## Integration with Hybrid XAML Engine

### Combined Approach

```
WPF Application
    ├── Load WPF assemblies (MetadataLoadContext)
    ├── Decompile BAML to XAML
    ├── Parse XAML with Hybrid Engine
    │   ├── XML Layer (structure)
    │   ├── WPF Type Resolution (loaded assemblies)
    │   └── Semantic Analysis (XamlX)
    ├── Transform to Avalonia
    │   ├── IL Rewriting (assemblies)
    │   ├── XAML Transformation (hybrid engine)
    │   └── Type Forwarding (runtime)
    └── Output Avalonia Application
        ├── Rewritten assemblies
        └── Transformed XAML
```

## Performance Considerations

| Technique | Compile-Time | Runtime | Development |
|-----------|--------------|---------|-------------|
| IL Rewriting | One-time | Fast | Slow (rebuild) |
| Type Forwarding | None | Medium | Fast |
| Shimming | None | Slow | Fast |
| Source Gen | Build-time | Fast | Fast |
| Analyzers | Incremental | N/A | Fast |

## Recommended Approach

**For Migration Tool**:
1. ✅ IL Rewriting for assemblies (one-time transform)
2. ✅ Hybrid XAML engine for XAML files
3. ✅ MSBuild tasks for build-time automation
4. ✅ Analyzers for detecting remaining WPF usage

**For Runtime Compatibility** (if needed):
1. Type forwarding for simple redirects
2. Shimming for complex WPF APIs
3. Custom AssemblyLoadContext for dynamic loading

## Next Steps

1. Implement WpfAssemblyLoader with MetadataLoadContext
2. Create basic IL rewriter with Mono.Cecil
3. Build MSBuild task for build-time transformation
4. Integrate with hybrid XAML parser for BAML decompilation
5. Create Roslyn analyzer for WPF usage detection

## Benefits

✅ **Direct WPF Code Usage**: Load existing WPF assemblies without recompilation
✅ **Type-Safe Transformation**: IL rewriting maintains type safety
✅ **Build-Time Efficiency**: MSBuild tasks automate transformation
✅ **Incremental Migration**: Analyzers help gradual migration
✅ **BAML Support**: Handle compiled XAML in WPF assemblies
✅ **Runtime Flexibility**: Type forwarding for dynamic scenarios
