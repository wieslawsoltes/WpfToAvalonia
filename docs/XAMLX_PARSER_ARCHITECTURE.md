# XamlX-based WPF XAML Parser & Transformation Architecture

## Overview

This document outlines the architecture for building a full semantic XAML parser for WPF based on XamlX, the same XAML compiler infrastructure used by Avalonia. This approach enables proper type resolution, markup extension evaluation, and intelligent transformations that simple XML parsing cannot achieve.

## Why XamlX?

### Advantages over Simple XML Parsing

1. **Semantic Analysis**: Full type resolution and semantic understanding of XAML
2. **Markup Extension Support**: Proper parsing and evaluation of {StaticResource}, {Binding}, etc.
3. **Type Safety**: Compile-time validation of property types and values
4. **Resource Resolution**: Proper handling of resource dictionaries and references
5. **AST-based Transformations**: Transform at semantic level, not text level
6. **Battle-tested**: Same infrastructure powers Avalonia's XAML compiler

### XamlX Components

- **XamlX.TypeSystem**: Abstract type system for XAML type resolution
- **XamlX.Parsers**: XAML document parsing to AST
- **XamlX.Transform**: AST transformation pipeline
- **XamlX.IL**: IL code generation (not needed for migration, only parsing)

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                    WPF XAML Migration Tool                   │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌────────────────────────────────────────────────────┐    │
│  │         Avalonia XAML Code Generator               │    │
│  │  - Serialize transformed AST to Avalonia XAML      │    │
│  │  - Generate xmlns declarations                     │    │
│  │  - Format and indent output                        │    │
│  └────────────────────────────────────────────────────┘    │
│                          ▲                                   │
│                          │                                   │
│  ┌────────────────────────────────────────────────────┐    │
│  │      WPF → Avalonia AST Transformer                │    │
│  │  - Type transformations (WPF → Avalonia)           │    │
│  │  - Namespace rewrites                              │    │
│  │  - Property transformations (Visibility→IsVisible) │    │
│  │  - Markup extension transformations                │    │
│  │  - Style/Template transformations                  │    │
│  └────────────────────────────────────────────────────┘    │
│                          ▲                                   │
│                          │                                   │
│  ┌────────────────────────────────────────────────────┐    │
│  │         XamlX Transformation Pipeline              │    │
│  │  - XamlX standard transformations                  │    │
│  │  - Type resolution                                 │    │
│  │  - Markup extension expansion                      │    │
│  │  - Property assignment graph building              │    │
│  └────────────────────────────────────────────────────┘    │
│                          ▲                                   │
│                          │                                   │
│  ┌────────────────────────────────────────────────────┐    │
│  │           WPF XAML Parser (XamlX-based)            │    │
│  │  - Parse XAML to XamlX AST                         │    │
│  │  - Use XDocumentXamlParser                         │    │
│  │  - Generate XamlDocument AST                       │    │
│  └────────────────────────────────────────────────────┘    │
│                          ▲                                   │
│                          │                                   │
│  ┌────────────────────────────────────────────────────┐    │
│  │         WPF XAML Language Definition               │    │
│  │  - WpfXamlIlLanguage configuration                 │    │
│  │  - WPF namespace mappings                          │    │
│  │  - WPF type converters                             │    │
│  │  - WPF markup extensions                           │    │
│  └────────────────────────────────────────────────────┘    │
│                          ▲                                   │
│                          │                                   │
│  ┌────────────────────────────────────────────────────┐    │
│  │           WPF Type System Bridge                   │    │
│  │  - WpfTypeSystemProvider (IXamlTypeSystem)         │    │
│  │  - WpfAssembly, WpfType, WpfProperty wrappers      │    │
│  │  - Dependency property mapping                     │    │
│  │  - WPF assembly loader (PresentationFramework)     │    │
│  └────────────────────────────────────────────────────┘    │
│                          ▲                                   │
│                          │                                   │
│  ┌────────────────────────────────────────────────────┐    │
│  │              XamlX Core Libraries                  │    │
│  │  - XamlX.TypeSystem                                │    │
│  │  - XamlX.Parsers                                   │    │
│  │  - XamlX.Transform                                 │    │
│  └────────────────────────────────────────────────────┘    │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## Key Components

### 1. WPF Type System Bridge

**Purpose**: Adapt .NET reflection-based WPF types to XamlX's abstract type system

**Components**:
- `WpfTypeSystemProvider`: Implements `IXamlTypeSystem`
- `WpfAssembly`: Wraps `System.Reflection.Assembly` for WPF assemblies
- `WpfType`: Wraps `System.Type` with WPF-specific semantics
- `WpfProperty`: Handles both CLR properties and DependencyProperties
- `WpfMethod`, `WpfField`: Wrap reflection members

**Key Responsibilities**:
- Load WPF reference assemblies (PresentationFramework, PresentationCore, WindowsBase)
- Resolve type references from XAML namespaces
- Map DependencyProperty system to XamlX property model
- Handle attached properties (Grid.Row, Canvas.Left, etc.)
- Cache type lookups for performance

### 2. WPF XAML Language Definition

**Purpose**: Configure XamlX to understand WPF XAML syntax and semantics

**Components**:
- `WpfXamlIlLanguage`: Configuration based on Avalonia's `AvaloniaXamlIlLanguage`
- Namespace mappings: `http://schemas.microsoft.com/winfx/2006/xaml/presentation` → WPF types
- Type converters for WPF types (Brush, Color, Thickness, GridLength, etc.)
- Content property conventions
- `clr-namespace:` resolution for WPF assemblies

**Markup Extensions**:
- `{StaticResource}`: Resolve from resource dictionaries
- `{DynamicResource}`: Mark for runtime resolution
- `{Binding}`: Parse binding paths, modes, converters
- `{x:Type}`: Type references
- `{x:Static}`: Static member access
- `{TemplateBinding}`: Template property binding
- `{RelativeSource}`: Relative binding sources

**XAML Directives**:
- `x:Name`: Element naming for code-behind
- `x:Key`: Resource dictionary keys
- `x:Class`: Code-behind class association
- `x:TypeArguments`: Generic type arguments
- `x:FieldModifier`: Field access modifiers

### 3. WPF XAML Parser

**Purpose**: Parse WPF XAML files into XamlX AST

**Components**:
- `XamlDocumentParser`: Uses XamlX's `XDocumentXamlParser`
- Error handling and diagnostics
- Source location tracking for error messages
- Incremental parsing support

**Output**: `XamlDocument` AST with:
- Element hierarchy
- Attribute assignments
- Markup extension expressions
- Resource dictionary definitions
- Type references (unresolved at this stage)

### 4. XamlX Transformation Pipeline

**Purpose**: Apply XamlX standard transformations for semantic analysis

**Key Transformations** (from XamlX):
- Type resolution: Map element names to WPF types
- Property resolution: Resolve property names to PropertyInfo/DependencyProperty
- Markup extension expansion: Evaluate static values where possible
- Content property expansion: Handle implicit content properties
- Property assignment ordering: Build dependency graph
- Type converter application: Apply type converters to attribute values

**Output**: Semantically rich AST with:
- Fully resolved type references
- Resolved property assignments
- Expanded markup extensions
- Validated semantics

### 5. WPF → Avalonia AST Transformer

**Purpose**: Transform WPF semantic AST to Avalonia semantic AST

**Transformation Categories**:

#### Type Transformations
- Map WPF types to Avalonia types in AST nodes
- `System.Windows.Controls.Button` → `Avalonia.Controls.Button`
- `System.Windows.DependencyObject` → `Avalonia.AvaloniaObject`
- `System.Windows.Controls.Label` → `Avalonia.Controls.TextBlock` (with warnings)

#### Namespace Transformations
- Rewrite xmlns declarations
- `http://schemas.microsoft.com/winfx/2006/xaml/presentation` → `https://github.com/avaloniaui`
- Update `clr-namespace:` references

#### Property Transformations
- Rename properties: `Visibility` → `IsVisible`
- Convert values: `Visible` → `True`, `Collapsed` → `False`
- Transform attached properties in AST
- Update type converters for Avalonia

#### Markup Extension Transformations
- `{StaticResource}` → Avalonia `{StaticResource}`
- `{DynamicResource}` → Avalonia `{DynamicResource}`
- `{Binding}` → Avalonia binding syntax
- `{TemplateBinding}` → Avalonia equivalent
- `{RelativeSource}` → Avalonia relative source patterns

#### Style & Template Transformations
- Transform `Style` elements
- Convert `Trigger` to Avalonia styles/pseudoclasses
- Transform `ControlTemplate` structure
- Update `DataTemplate` syntax
- Handle `VisualStateManager` (warn if not supported)

### 6. Avalonia XAML Code Generator

**Purpose**: Serialize transformed AST to Avalonia XAML text

**Components**:
- `XamlAstSerializer`: Custom serializer for Avalonia XAML
- xmlns declaration generator
- Indentation and formatting
- Comment generation for manual review items

**Output**: Valid Avalonia XAML (.axaml) files

## Data Flow

```
WPF XAML File (.xaml)
        ↓
    [Parse]
        ↓
XamlX AST (WPF types, unresolved)
        ↓
    [XamlX Transform Pipeline]
        ↓
XamlX AST (WPF types, resolved semantics)
        ↓
    [WPF → Avalonia Transformer]
        ↓
XamlX AST (Avalonia types, resolved semantics)
        ↓
    [Serialize]
        ↓
Avalonia XAML File (.axaml)
```

## Type System Mapping Examples

### Example 1: Simple Control

**WPF XAML**:
```xml
<Button xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        Content="Click Me"
        Visibility="Visible"/>
```

**XamlX AST (WPF)**:
```
XamlAstObjectNode
  Type: System.Windows.Controls.Button
  Properties:
    - Content: XamlConstantNode("Click Me")
    - Visibility: XamlConstantNode(Visibility.Visible)
```

**XamlX AST (Avalonia)**:
```
XamlAstObjectNode
  Type: Avalonia.Controls.Button
  Properties:
    - Content: XamlConstantNode("Click Me")
    - IsVisible: XamlConstantNode(true)
```

**Avalonia XAML**:
```xml
<Button xmlns="https://github.com/avaloniaui"
        Content="Click Me"
        IsVisible="True"/>
```

### Example 2: Resource & Binding

**WPF XAML**:
```xml
<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
  <StackPanel.Resources>
    <SolidColorBrush x:Key="MyBrush" Color="Red"/>
  </StackPanel.Resources>
  <TextBlock Text="{Binding Name}"
             Foreground="{StaticResource MyBrush}"/>
</StackPanel>
```

**XamlX AST (WPF - resolved)**:
```
XamlAstObjectNode
  Type: System.Windows.Controls.StackPanel
  Properties:
    - Resources: XamlAstResourceDictionary
        Entries:
          - Key: "MyBrush"
            Value: XamlAstObjectNode(System.Windows.Media.SolidColorBrush)
  Children:
    - XamlAstObjectNode(System.Windows.Controls.TextBlock)
        Properties:
          - Text: XamlBindingExtension(Path="Name")
          - Foreground: XamlStaticResourceExtension(Key="MyBrush")
```

**XamlX AST (Avalonia - transformed)**:
```
XamlAstObjectNode
  Type: Avalonia.Controls.StackPanel
  Properties:
    - Resources: XamlAstResourceDictionary
        Entries:
          - Key: "MyBrush"
            Value: XamlAstObjectNode(Avalonia.Media.SolidColorBrush)
  Children:
    - XamlAstObjectNode(Avalonia.Controls.TextBlock)
        Properties:
          - Text: XamlBindingExtension(Path="Name")
          - Foreground: XamlStaticResourceExtension(Key="MyBrush")
```

## Implementation Strategy

### Phase 1: Foundation (2 weeks)
1. Add XamlX as submodule
2. Create WpfToAvalonia.XamlParser project
3. Implement basic WPF type system bridge
4. Study Avalonia's XamlX integration

### Phase 2: WPF Type System (1-2 weeks)
1. Implement WpfTypeSystemProvider
2. WPF assembly loading
3. DependencyProperty mapping
4. Type resolution and caching

### Phase 3: WPF XAML Language (1-2 weeks)
1. Create WpfXamlIlLanguage
2. Namespace mappings
3. Type converters
4. Markup extension handlers

### Phase 4: Parser & Pipeline (1 week)
1. Implement XAML parser
2. Integrate XamlX transformation pipeline
3. Error handling and diagnostics

### Phase 5: AST Transformation (2-3 weeks)
1. Type transformations
2. Property transformations
3. Markup extension transformations
4. Style/template transformations

### Phase 6: Code Generation (1 week)
1. Avalonia XAML serializer
2. Formatting and indentation
3. Comment generation

### Phase 7: Testing & Validation (1-2 weeks)
1. Unit tests for all components
2. Integration tests with real WPF XAML
3. Performance benchmarking
4. Validation against Avalonia XAML compiler

## Reference Implementation Analysis

### Avalonia's XamlX Integration

**Key Files to Study**:
```
extern/Avalonia/src/Markup/Avalonia.Markup.Xaml/
├── XamlIl/
│   ├── CompilerExtensions/
│   │   ├── AvaloniaXamlIlLanguage.cs              # Language definition
│   │   ├── AvaloniaXamlIlWellKnownTypes.cs       # Type mappings
│   │   └── Transformers/                          # AST transformers
│   └── AvaloniaXamlIlCompiler.cs                  # Main compiler
├── MarkupExtensions/                              # Markup extension implementations
└── Parsers/                                       # XAML parsing

extern/XamlX/src/XamlX/
├── TypeSystem/                                    # Abstract type system
├── Parsers/                                       # XAML parsing to AST
├── Transform/                                     # AST transformations
└── IL/                                           # IL generation (not needed)
```

**Key Patterns to Adopt**:
1. Language configuration pattern (`AvaloniaXamlIlLanguage`)
2. Custom AST transformers in pipeline
3. Markup extension provider pattern
4. Type converter registration
5. Error reporting and diagnostics

## Benefits Over XML-based Approach

| Feature | XML-based | XamlX-based |
|---------|-----------|-------------|
| Type Resolution | Manual string matching | Full semantic resolution |
| Markup Extensions | Text parsing | Proper evaluation & transformation |
| Property Types | Unknown | Fully resolved |
| Resource Resolution | Limited | Complete with inheritance |
| Binding Analysis | Text-level | AST-level with validation |
| Error Detection | Parse errors only | Semantic errors too |
| Transformation Safety | String manipulation | Type-safe AST manipulation |
| Complex XAML | Limited support | Full support |
| Performance | Fast for simple cases | Optimized for complex cases |

## Next Steps

1. Add XamlX as git submodule to `extern/XamlX`
2. Create `WpfToAvalonia.XamlParser` project
3. Study Avalonia's implementation in detail
4. Implement `WpfTypeSystemProvider` foundation
5. Create basic XAML parsing pipeline
6. Iterate on transformations and validation

## Resources

- **XamlX Repository**: https://github.com/kekekeks/XamlX
- **Avalonia Markup**: extern/Avalonia/src/Markup/Avalonia.Markup.Xaml
- **XamlX Documentation**: Limited, rely on code analysis
- **WPF Reference**: Microsoft Docs for WPF XAML syntax
- **Avalonia Docs**: https://docs.avaloniaui.net/ for target XAML syntax
