# Hybrid XML/XamlX/Roslyn Approach - Executive Summary

## The Problem

Current XML-only XAML transformation has limitations:

❌ **No Type Information**: Can't validate property types
❌ **Text-based Bindings**: Can't parse `{Binding Path=Name, Mode=TwoWay}`
❌ **No Resource Resolution**: Can't resolve `{StaticResource MyBrush}`
❌ **No Semantic Validation**: Can't detect invalid XAML
❌ **No Code-Behind Sync**: XAML and C# transformations are disconnected

## The Solution: Hybrid Architecture

Combine **THREE** powerful engines into one unified system:

### 1. XML Layer (System.Xml.Linq)
✅ **Speed**: Extremely fast parsing
✅ **Formatting**: 100% preservation of whitespace, comments
✅ **Structure**: Perfect for simple transformations

### 2. XamlX Layer (extern/XamlX)
✅ **Type Resolution**: Full semantic understanding
✅ **Markup Extensions**: Proper parsing of {Binding}, {StaticResource}
✅ **Validation**: Compile-time XAML validation
✅ **Same as Avalonia**: Uses Avalonia's own XAML compiler

### 3. Roslyn Layer (Microsoft.CodeAnalysis)
✅ **Code-Behind Sync**: C# and XAML stay synchronized
✅ **Field Generation**: x:Name → C# field mapping
✅ **Event Handlers**: Coordinate event transformations
✅ **Type Validation**: Ensure XAML and C# types match

## The Architecture

```
                    ┌──────────────────────┐
                    │   WPF XAML File      │
                    └──────────┬───────────┘
                               │
                   ┌───────────┴────────────┐
                   │                        │
                   ▼                        ▼
         ┌─────────────────┐    ┌─────────────────┐
         │  XML Parser     │    │  XamlX Parser   │
         │  (Fast)         │    │  (Semantic)     │
         └────────┬────────┘    └────────┬────────┘
                  │                      │
                  └──────────┬───────────┘
                             │
                             ▼
                ┌────────────────────────┐
                │   UNIFIED XAML AST     │
                │                        │
                │  • XML + Semantics     │
                │  • Formatting + Types  │
                │  • Structure + Values  │
                └────────────┬───────────┘
                             │
                   ┌─────────┴─────────┐
                   │                   │
                   ▼                   ▼
         ┌─────────────────┐  ┌─────────────────┐
         │ Roslyn Analyzer │  │ Transformations │
         │ (C# sync)       │  │ (Hybrid)        │
         └─────────────────┘  └────────┬────────┘
                                       │
                                       ▼
                          ┌────────────────────┐
                          │  Avalonia XAML     │
                          │  • Valid           │
                          │  • Formatted       │
                          │  • Type-safe       │
                          └────────────────────┘
```

## Transformation Strategy

Different transformations use different layers:

| Transformation | Layer | Why |
|----------------|-------|-----|
| **Namespace change** | XML | Fast text replacement |
| **Simple rename** | XML | Preserve formatting |
| **Property type change** | Semantic | Need type validation |
| **{Binding} transform** | Semantic | Complex parsing |
| **{StaticResource}** | Semantic | Need resource resolution |
| **Attached property** | Hybrid | Validate + Update |
| **x:Name fields** | Roslyn | Sync with C# |
| **Event handlers** | Roslyn | Coordinate signatures |

## Benefits

### vs. XML-Only Approach

| Feature | XML-Only | Hybrid |
|---------|----------|--------|
| Speed | ⚡⚡⚡ | ⚡⚡ |
| Formatting | ✅ | ✅ |
| Type Safety | ❌ | ✅ |
| Bindings | ⚠️ Text | ✅ Parsed |
| Resources | ⚠️ Limited | ✅ Full |
| Validation | ❌ | ✅ |
| C# Sync | ❌ | ✅ |

### vs. XamlX-Only Approach

| Feature | XamlX-Only | Hybrid |
|---------|------------|--------|
| Speed | ⚡ | ⚡⚡ |
| Formatting | ❌ Lost | ✅ Preserved |
| Type Safety | ✅ | ✅ |
| Bindings | ✅ | ✅ |
| Resources | ✅ | ✅ |
| Simple Transforms | Slow | Fast (XML) |

## Real-World Example

### Input (WPF XAML):
```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
    <Window.Resources>
        <SolidColorBrush x:Key="MyBrush" Color="Red"/>
    </Window.Resources>

    <StackPanel>
        <TextBlock x:Name="titleText"
                   Text="{Binding Title}"
                   Foreground="{StaticResource MyBrush}"
                   Visibility="Visible"/>
    </StackPanel>
</Window>
```

### Hybrid Processing:

**XML Layer**:
- Parses structure (fast)
- Preserves all whitespace and indentation
- Identifies elements and attributes

**XamlX Layer**:
- Resolves `Window` → `System.Windows.Window`
- Resolves `TextBlock` → `System.Windows.Controls.TextBlock`
- Parses `{Binding Title}` → BindingExpression(Path="Title")
- Parses `{StaticResource MyBrush}` → ResourceReference("MyBrush")
- Resolves resource "MyBrush" → SolidColorBrush

**Roslyn Layer**:
- Finds `MainWindow.xaml.cs`
- Validates `x:Class="MyApp.MainWindow"` matches C# class
- Maps `x:Name="titleText"` → expects field `TextBlock titleText`

**Unified AST**:
```
UnifiedXamlElement
  Type: System.Windows.Window (resolved)
  XmlElement: <Window...> (formatted)
  CodeBehindSymbol: MainWindow class

  Property: Resources
    ResourceDictionary
      Entry: "MyBrush" → SolidColorBrush (resolved)

  Child: UnifiedXamlElement
    Type: System.Windows.Controls.StackPanel

    Child: UnifiedXamlElement
      Type: System.Windows.Controls.TextBlock
      XName: "titleText"
      CodeBehindSymbol: titleText field

      Property: Text
        MarkupExtension: Binding
          Path: "Title" (validated against DataContext)

      Property: Foreground
        MarkupExtension: StaticResource
          ResourceKey: "MyBrush"
          ResolvedResource: SolidColorBrush (Color=Red)

      Property: Visibility
        Value: Visibility.Visible (enum)
        PropertyType: System.Windows.Visibility (resolved)
```

**Transformations**:

1. **Namespace** (XML layer): Fast text replacement
   - `xmlns="...microsoft..."` → `xmlns="https://github.com/avaloniaui"`

2. **Type mappings** (Semantic layer): Type-safe
   - `System.Windows.Window` → `Avalonia.Controls.Window`
   - `System.Windows.Controls.TextBlock` → `Avalonia.Controls.TextBlock`

3. **Property transform** (Hybrid):
   - Semantic: Validate `Visibility` type
   - Transform: `Visibility` → `IsVisible`, `Visible` → `True`
   - XML: Update attribute with formatting preserved

4. **Binding** (Semantic layer): Parsed and validated
   - Parse binding expression
   - Validate path (if possible)
   - Transform to Avalonia binding syntax

5. **Code-behind sync** (Roslyn):
   - Update C# class: `Window` → `Avalonia.Controls.Window`
   - Update field: `TextBlock titleText` → `Avalonia.Controls.TextBlock titleText`

### Output (Avalonia XAML):
```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="https://github.com/avaloniaui">
    <Window.Resources>
        <SolidColorBrush x:Key="MyBrush" Color="Red"/>
    </Window.Resources>

    <StackPanel>
        <TextBlock x:Name="titleText"
                   Text="{Binding Title}"
                   Foreground="{StaticResource MyBrush}"
                   IsVisible="True"/>
    </StackPanel>
</Window>
```

**Note**: Formatting perfectly preserved, types validated, bindings transformed!

## Implementation Timeline

**8 Weeks Total**:

- **Weeks 1-2**: Unified AST Foundation
- **Weeks 2-4**: XML + XamlX Bridges
- **Week 5**: Dual Parsing & Merger
- **Week 6**: Hybrid Transformation Framework
- **Weeks 6-7**: Roslyn Integration
- **Week 7**: Core Transformations
- **Weeks 7-8**: Serialization & Output
- **Week 8**: Testing & Integration

## Why This is Worth It

### Unlocks Production Migration

Current XML-only approach can handle **~60%** of XAML:
- ✅ Simple controls
- ✅ Basic properties
- ⚠️ Simple text bindings
- ❌ Complex bindings
- ❌ Resources
- ❌ Markup extensions
- ❌ Styles with triggers
- ❌ Templates

Hybrid approach can handle **~95%** of XAML:
- ✅ All controls
- ✅ All properties (type-safe)
- ✅ All bindings (parsed)
- ✅ Resources (resolved)
- ✅ Markup extensions (evaluated)
- ✅ Styles (transformed)
- ✅ Templates (transformed)
- ⚠️ Very complex custom scenarios (manual review)

### Foundation for Future Features

The unified AST enables:
- **Refactoring tools**: Rename, extract resource, etc.
- **XAML IntelliSense**: Type-aware suggestions
- **Live preview**: Render XAML during transformation
- **Bidirectional editing**: Edit unified AST, sync to XML
- **XAML analyzer**: Custom rules and warnings

## Success Criteria

✅ **100%** formatting preservation
✅ **100%** type resolution for standard WPF
✅ **95%+** successful transformation of production XAML
✅ **< 1 second** for files under 1000 lines
✅ **Zero** information loss
✅ **Full** code-behind synchronization

## Next Steps

1. **Week 1**: Start Phase 1 - Unified AST design
2. **Week 2**: Complete Unified AST + XML bridge
3. **Week 3**: Add XamlX as submodule, study Avalonia
4. **Week 4**: Implement WPF type system bridge
5. **Continue**: Follow [IMPLEMENTATION_ROADMAP.md](IMPLEMENTATION_ROADMAP.md)

## Documentation

- **Detailed Architecture**: [HYBRID_ARCHITECTURE.md](HYBRID_ARCHITECTURE.md)
- **Implementation Plan**: [IMPLEMENTATION_ROADMAP.md](IMPLEMENTATION_ROADMAP.md)
- **Full Project Plan**: [MIGRATION_PLAN.md](MIGRATION_PLAN.md) (Milestone 2.5)
- **XamlX Reference**: [XAMLX_PARSER_ARCHITECTURE.md](XAMLX_PARSER_ARCHITECTURE.md)

---

**This is the foundation that will enable production-quality WPF to Avalonia migration.**
