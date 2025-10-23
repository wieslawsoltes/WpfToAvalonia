# Hybrid XAML Transformation Engine - Implementation Roadmap

## Priority Overview

This roadmap outlines the phased implementation of the Hybrid XML/XamlX/Roslyn XAML Transformation Engine - our highest priority milestone for enabling production-quality WPF to Avalonia migration.

## Why This is Priority #1

The hybrid engine is foundational because:

1. **Unlocks Complex XAML**: Current XML-only approach can't handle bindings, resources, markup extensions
2. **Enables Type Safety**: XamlX provides semantic analysis for safe transformations
3. **Preserves Quality**: XML layer ensures perfect formatting preservation
4. **Scales to Production**: Can handle large, complex WPF applications
5. **Coordinates with C#**: Roslyn integration keeps XAML + code-behind in sync

## 8-Week Implementation Plan

### Phase 1: Unified AST Foundation (Week 1-2)

**Goal**: Create the unified AST architecture that bridges all three layers

**Deliverables**:
- [ ] `UnifiedXamlNode` base hierarchy
- [ ] `UnifiedXamlElement` (combines XElement + XamlAstObjectNode)
- [ ] `UnifiedXamlProperty` (combines XAttribute + semantic info)
- [ ] `UnifiedXamlDocument` container
- [ ] Visitor pattern for unified AST traversal
- [ ] Metadata storage (formatting, location, diagnostics)

**Files to Create**:
```
src/WpfToAvalonia.XamlParser/
├── UnifiedAst/
│   ├── UnifiedXamlNode.cs
│   ├── UnifiedXamlElement.cs
│   ├── UnifiedXamlProperty.cs
│   ├── UnifiedXamlMarkupExtension.cs
│   ├── UnifiedXamlDocument.cs
│   ├── FormattingHints.cs
│   ├── SourceLocation.cs
│   └── TransformationState.cs
└── Visitors/
    ├── IUnifiedXamlVisitor.cs
    └── UnifiedXamlVisitorBase.cs
```

**Success Criteria**:
- ✅ Can represent any XAML structure in unified AST
- ✅ Preserves all XML formatting information
- ✅ Supports semantic type information
- ✅ Visitor pattern works for traversal

---

### Phase 2: Bridges - XML & XamlX Integration (Week 2-4)

**Goal**: Build converters from XML and XamlX into the unified AST

#### Week 2-3: XML Bridge

**Deliverables**:
- [ ] `XElementToUnifiedConverter`
- [ ] Formatting preservation logic
- [ ] Source location tracking
- [ ] Namespace handling
- [ ] Comment preservation

**Files to Create**:
```
src/WpfToAvalonia.XamlParser/
├── Converters/
│   ├── XmlToUnifiedConverter.cs
│   ├── FormattingExtractor.cs
│   └── SourceLocationMapper.cs
```

**Success Criteria**:
- ✅ Parse any WPF XAML to unified AST via XML
- ✅ 100% formatting preservation
- ✅ All comments preserved
- ✅ Accurate source locations

#### Week 3-4: XamlX Integration

**Deliverables**:
- [ ] Add XamlX as git submodule (`extern/XamlX`)
- [ ] Study Avalonia's XamlX usage
- [ ] `WpfTypeSystemProvider` implementing `IXamlTypeSystem`
- [ ] `WpfAssembly`, `WpfType`, `WpfProperty` wrappers
- [ ] WPF assembly loader (PresentationFramework, etc.)
- [ ] `WpfXamlIlLanguage` configuration
- [ ] `XamlXToUnifiedConverter`

**Files to Create**:
```
src/WpfToAvalonia.XamlParser/
├── TypeSystem/
│   ├── WpfTypeSystemProvider.cs
│   ├── WpfAssembly.cs
│   ├── WpfType.cs
│   ├── WpfProperty.cs
│   ├── WpfMethod.cs
│   └── WpfAssemblyLoader.cs
├── Language/
│   ├── WpfXamlIlLanguage.cs
│   ├── WpfTypeConverters.cs
│   └── WpfMarkupExtensions.cs
└── Converters/
    └── XamlXToUnifiedConverter.cs
```

**Success Criteria**:
- ✅ XamlX can parse WPF XAML
- ✅ Type resolution works for WPF types
- ✅ Can convert XamlX AST to unified AST
- ✅ Semantic information properly extracted

---

### Phase 3: Dual Parsing & AST Merger (Week 5)

**Goal**: Parse XAML with both engines and merge into unified AST

**Deliverables**:
- [ ] `HybridXamlParser` orchestrator
- [ ] AST merging logic (align XML and XamlX nodes)
- [ ] Semantic enrichment pipeline
- [ ] Validation between layers
- [ ] Unified symbol table

**Files to Create**:
```
src/WpfToAvalonia.XamlParser/
├── HybridXamlParser.cs
├── AstMerger.cs
├── SemanticEnricher.cs
└── SymbolTable.cs
```

**Workflow**:
```
WPF XAML → [XML Parser] → XML AST ──┐
                                      ├→ [Merger] → Unified AST
WPF XAML → [XamlX Parser] → XamlX AST ┘
```

**Success Criteria**:
- ✅ Both parsers work on same XAML file
- ✅ AST merger correctly aligns nodes
- ✅ Unified AST has both formatting + semantics
- ✅ No loss of information

---

### Phase 4: Hybrid Transformation Framework (Week 6)

**Goal**: Build transformation framework that can use both layers

**Deliverables**:
- [ ] `HybridXamlTransformer` base class
- [ ] Transformation routing (XML vs semantic)
- [ ] `UnifiedTransformationPipeline`
- [ ] Transformation validation
- [ ] XML synchronization (semantic changes → XML updates)

**Files to Create**:
```
src/WpfToAvalonia.XamlParser/
├── Transformers/
│   ├── HybridXamlTransformer.cs
│   ├── TransformationRouter.cs
│   ├── UnifiedTransformationPipeline.cs
│   └── TransformationValidator.cs
└── Synchronization/
    └── XmlSynchronizer.cs
```

**Transformation Strategy**:

| Transformation Type | Layer | Reason |
|---------------------|-------|--------|
| Namespace rename | XML | Fast, simple text replacement |
| Simple element rename | XML | Format-preserving |
| Property type change | Semantic | Needs type validation |
| Binding transformation | Semantic | Complex parsing required |
| Markup extension | Semantic | Semantic evaluation needed |
| Attached property | Hybrid | Semantic validation + XML update |

**Success Criteria**:
- ✅ Can route transformations to appropriate layer
- ✅ XML transformations preserve formatting
- ✅ Semantic transformations are type-safe
- ✅ Changes propagate correctly between layers

---

### Phase 5: Roslyn Code-Behind Integration (Week 6-7)

**Goal**: Coordinate XAML transformations with C# code-behind

**Deliverables**:
- [ ] `RoslynCodeBehindAnalyzer`
- [ ] `CodeBehindSynchronizer`
- [ ] x:Name → C# field mapping
- [ ] Event handler transformation coordination
- [ ] Unified diagnostic system (XAML + C#)

**Files to Create**:
```
src/WpfToAvalonia.XamlParser/
├── CodeBehind/
│   ├── RoslynCodeBehindAnalyzer.cs
│   ├── CodeBehindSynchronizer.cs
│   ├── NamedElementMapper.cs
│   └── EventHandlerCoordinator.cs
└── Diagnostics/
    └── UnifiedDiagnosticCollector.cs
```

**Synchronization Flow**:
```
XAML x:Name="myButton" → Roslyn finds field → Validate types match
XAML Click="OnClick" → Roslyn finds method → Transform signature if needed
```

**Success Criteria**:
- ✅ x:Name elements mapped to C# fields
- ✅ Type consistency validated
- ✅ Event handlers transformed in sync
- ✅ Diagnostics span XAML + C#

---

### Phase 6: Core Transformations Implementation (Week 7)

**Goal**: Implement essential WPF → Avalonia transformations using hybrid approach

**Deliverables**:
- [ ] Namespace transformations (XML layer)
- [ ] Type transformations (Semantic layer)
- [ ] Property transformations (Hybrid)
- [ ] Visibility → IsVisible (Hybrid)
- [ ] Binding transformations (Semantic)
- [ ] Resource reference transformations (Semantic)

**Files to Create**:
```
src/WpfToAvalonia.XamlParser/
└── Transformers/
    ├── NamespaceTransformer.cs
    ├── TypeTransformer.cs
    ├── PropertyTransformer.cs
    ├── VisibilityToIsVisibleTransformer.cs
    ├── BindingTransformer.cs
    └── ResourceReferenceTransformer.cs
```

**Success Criteria**:
- ✅ Namespace transformations preserve formatting
- ✅ Type transformations are validated
- ✅ Property transformations handle type changes
- ✅ Bindings parse and transform correctly
- ✅ Resources resolve properly

---

### Phase 7: Serialization & Output (Week 7-8)

**Goal**: Generate Avalonia XAML from transformed unified AST

**Deliverables**:
- [ ] `UnifiedAstToXmlSerializer`
- [ ] Formatting preservation
- [ ] Avalonia namespace generation
- [ ] Comment generation for manual review items
- [ ] Output validation

**Files to Create**:
```
src/WpfToAvalonia.XamlParser/
├── Serializers/
│   ├── UnifiedAstToXmlSerializer.cs
│   ├── AvaloniaNamespaceGenerator.cs
│   └── FormattingApplier.cs
└── Validation/
    └── AvaloniaXamlValidator.cs
```

**Success Criteria**:
- ✅ Generated XAML preserves original formatting
- ✅ Valid Avalonia XAML syntax
- ✅ Comments added for manual review
- ✅ Output validates against Avalonia schema

---

### Phase 8: Testing & Integration (Week 8)

**Goal**: Comprehensive testing and integration with existing pipeline

**Deliverables**:
- [ ] Unit tests for all components
- [ ] Integration tests with real WPF XAML
- [ ] Performance benchmarks
- [ ] Integration with `TransformationPipeline`
- [ ] End-to-end migration tests

**Files to Create**:
```
tests/WpfToAvalonia.Tests/
├── XamlParser/
│   ├── UnifiedAstTests.cs
│   ├── XmlToUnifiedConverterTests.cs
│   ├── XamlXToUnifiedConverterTests.cs
│   ├── HybridParserTests.cs
│   ├── HybridTransformerTests.cs
│   └── IntegrationTests.cs
└── TestAssets/
    ├── SimpleWpfXaml/
    ├── ComplexBindings/
    ├── ResourceDictionaries/
    └── CodeBehind/
```

**Test Coverage**:
- ✅ All unified AST node types
- ✅ XML → Unified conversion
- ✅ XamlX → Unified conversion
- ✅ AST merging
- ✅ All transformation types
- ✅ Code-behind synchronization
- ✅ Large file performance
- ✅ Real WPF XAML samples

---

## Success Metrics

### Performance Targets

| Metric | Target | Current (XML-only) |
|--------|--------|-------------------|
| Simple XAML (< 100 lines) | < 50ms | ~10ms |
| Medium XAML (100-1000 lines) | < 200ms | ~50ms |
| Large XAML (> 1000 lines) | < 1s | ~200ms |
| Formatting preservation | 100% | 100% |
| Type safety | 100% | 0% |

### Quality Metrics

- ✅ Zero information loss (XML + Semantic)
- ✅ 100% formatting preservation
- ✅ Complete type resolution
- ✅ Validated transformations
- ✅ Code-behind synchronization

## Integration with Existing Work

### Leverage Current Infrastructure

**Already Complete** (Can Reuse):
- ✅ `DiagnosticCollector` and diagnostic codes
- ✅ `IMappingRepository` with WPF→Avalonia mappings
- ✅ `TransformationConfiguration`
- ✅ `CSharpFileTransformer` (Roslyn-based)
- ✅ Basic XML parsing in `XamlParser`

**New Components** (Phase 1-8):
- 🔨 Unified AST
- 🔨 XamlX integration
- 🔨 Hybrid transformation framework
- 🔨 Code-behind synchronization

**Integration Points**:
```csharp
// Existing pipeline
public class TransformationPipeline
{
    // Add hybrid XAML transformer alongside existing transformers
    transformers.Add(new HybridXamlFileTransformer(
        diagnostics,
        mappingRepository,
        configuration));
}
```

## Risk Mitigation

### Technical Risks

| Risk | Mitigation |
|------|-----------|
| XamlX complexity | Start with Avalonia reference implementation |
| Performance overhead | Lazy semantic analysis, caching, XML-first strategy |
| AST merging conflicts | Comprehensive validation, conflict resolution rules |
| Formatting loss | XML layer as source of truth for formatting |
| Type resolution failures | Fallback to XML-only mode with warnings |

### Timeline Risks

| Risk | Mitigation |
|------|-----------|
| XamlX learning curve | Week 2-3 dedicated to studying Avalonia code |
| Integration complexity | Phase 8 buffer for integration issues |
| Testing complexity | Continuous testing from Phase 1 |

## Next Immediate Actions

### Week 1 - Day 1-3
1. Design `UnifiedXamlNode` hierarchy (review with team if applicable)
2. Implement `UnifiedXamlElement`
3. Implement `UnifiedXamlProperty`
4. Create visitor pattern base classes
5. Unit tests for unified AST

### Week 1 - Day 4-5
1. Implement `XElementToUnifiedConverter`
2. Formatting preservation logic
3. Source location tracking
4. Integration tests with existing `XamlParser`

### Week 2 - Day 1-2
1. Add XamlX as git submodule
2. Study Avalonia's `AvaloniaXamlIlLanguage`
3. Document XamlX architecture patterns
4. Plan `WpfTypeSystemProvider` implementation

## References

- **Detailed Plan**: [docs/MIGRATION_PLAN.md](MIGRATION_PLAN.md) - Milestone 2.5
- **Architecture**: [docs/HYBRID_ARCHITECTURE.md](HYBRID_ARCHITECTURE.md)
- **XamlX Only**: [docs/XAMLX_PARSER_ARCHITECTURE.md](XAMLX_PARSER_ARCHITECTURE.md)
- **Avalonia Reference**: `extern/Avalonia/src/Markup/Avalonia.Markup.Xaml`
- **XamlX Repository**: https://github.com/kekekeks/XamlX

---

**Last Updated**: 2025-10-23
**Status**: Planning Complete → Ready for Phase 1 Implementation
