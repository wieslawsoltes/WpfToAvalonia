# Quick Reference - Hybrid XAML Transformation Engine

## What is it?

A **three-layer parsing engine** that combines:
1. **XML** (fast, preserves formatting)
2. **XamlX** (semantic analysis, type-safe)
3. **Roslyn** (C# code-behind sync)

## Why?

| Need | Solution |
|------|----------|
| Fast parsing | ✅ XML layer |
| Preserve formatting | ✅ XML layer |
| Type resolution | ✅ XamlX layer |
| Parse {Binding} | ✅ XamlX layer |
| Validate XAML | ✅ XamlX layer |
| Sync C# code | ✅ Roslyn layer |
| Safe transformations | ✅ All three |

## Architecture (One Diagram)

```
WPF XAML
    │
    ├──> XML Parser ──┐
    │                 │
    ├──> XamlX Parser ├──> Unified AST ──> Transformations ──> Avalonia XAML
    │                 │
    └──> Roslyn ──────┘
```

## Key Components

### Unified AST
```csharp
UnifiedXamlElement
  ├─ XmlElement (formatting)
  ├─ SemanticNode (types)
  ├─ CodeBehindSymbol (C# field)
  ├─ Properties[]
  └─ Children[]
```

### Transformation Routing
```csharp
Simple rename → XML layer (fast)
Type change → Semantic layer (safe)
Binding → Semantic layer (parsed)
Code-behind → Roslyn layer (synced)
```

## File Structure

```
WpfToAvalonia.XamlParser/
├── UnifiedAst/          # Core AST nodes
├── Converters/          # XML/XamlX → Unified
├── TypeSystem/          # WPF type bridge for XamlX
├── Language/            # WpfXamlIlLanguage
├── Transformers/        # Hybrid transformers
├── CodeBehind/          # Roslyn integration
└── Serializers/         # Output generation
```

## 8-Week Plan (Summary)

| Week | Phase | Output |
|------|-------|--------|
| 1-2 | Unified AST | Core data structures |
| 2-4 | Bridges | XML + XamlX integration |
| 5 | Dual Parsing | Combined parser |
| 6 | Transformers | Hybrid framework |
| 6-7 | Roslyn | Code-behind sync |
| 7 | Transforms | Core transformations |
| 7-8 | Output | Serialization |
| 8 | Testing | Validation |

## Documents

| Document | Purpose | Length |
|----------|---------|--------|
| **[HYBRID_APPROACH_SUMMARY.md](HYBRID_APPROACH_SUMMARY.md)** | Executive overview | Quick read |
| **[HYBRID_ARCHITECTURE.md](HYBRID_ARCHITECTURE.md)** | Technical deep-dive | 600+ lines |
| **[IMPLEMENTATION_ROADMAP.md](IMPLEMENTATION_ROADMAP.md)** | Week-by-week plan | Detailed |
| **[MIGRATION_PLAN.md](MIGRATION_PLAN.md)** | Full project (M2.5) | Comprehensive |
| **[XAMLX_PARSER_ARCHITECTURE.md](XAMLX_PARSER_ARCHITECTURE.md)** | XamlX reference | Technical |

## Example Transformation

**Input (WPF)**:
```xml
<TextBlock Text="{Binding Name}" Visibility="Visible"/>
```

**Processing**:
- XML: Parse structure, preserve formatting
- XamlX: Resolve TextBlock type, parse {Binding Name}
- Transform: Visibility → IsVisible, Visible → True
- Output: Format-preserved Avalonia XAML

**Output (Avalonia)**:
```xml
<TextBlock Text="{Binding Name}" IsVisible="True"/>
```

## Success Metrics

✅ 100% formatting preservation
✅ 95%+ XAML coverage
✅ Full type safety
✅ < 1s for large files
✅ C# sync automatic

## Next Action

👉 **Start Phase 1**: Design `UnifiedXamlNode` hierarchy

---

**For full details, see the complete documentation in `docs/`**
