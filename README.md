# WPF to Avalonia Migration Tool

An automated tool for migrating WPF (Windows Presentation Foundation) applications to Avalonia UI, leveraging Roslyn and MSBuild APIs for intelligent code transformation.

## Project Status

🚀 **Active Development** - Milestone 3 (XAML Transformation Engine) core completed. C# and XAML transformation engines are functional.

## Overview

This tool provides automated migration of WPF codebases to Avalonia, including:

- **C# Code Transformation**: Automatic conversion of namespaces, types, dependency properties, and API calls
- **XAML Migration**: Intelligent XAML transformation with namespace, control, and property mapping
- **Project File Conversion**: Automatic .csproj and package reference updates
- **Semantic Analysis**: Roslyn-powered semantic analysis for context-aware transformations
- **Comprehensive Reporting**: Detailed migration reports with warnings and manual review items

## Architecture

### Projects

- **WpfToAvalonia.Core**: Core transformation engine using Roslyn APIs
- **WpfToAvalonia.Mappings**: Mapping database for WPF→Avalonia API mappings
- **WpfToAvalonia.Analyzers**: Roslyn analyzers for detecting WPF usage patterns
- **WpfToAvalonia.CLI**: Command-line interface for migration operations
- **WpfToAvalonia.Tests**: Unit and integration tests

### Key Technologies

- **.NET 8.0**: Target framework for all libraries
- **Roslyn (Microsoft.CodeAnalysis)**: C# syntax and semantic analysis
- **MSBuild APIs**: Project and solution loading
- **System.CommandLine**: CLI framework
- **System.Text.Json**: Mapping data serialization

## Current Progress

### Milestone 1: Foundation & Architecture ✅ COMPLETED

- [x] 1.1 Project Setup & Infrastructure
  - [x] Solution structure created
  - [x] All projects initialized
  - [x] NuGet dependencies configured
  - [x] .editorconfig and .gitignore in place

- [x] 1.2 Mapping Database Design
  - [x] Mapping data structures (NamespaceMapping, TypeMapping, PropertyMapping, EventMapping)
  - [x] JSON-based mapping repository with query APIs
  - [x] Initial core mappings database populated with:
    - 11 namespace mappings
    - 23 type mappings (Window, Button, TextBox, Grid, panels, etc.)
    - 19 property mappings (Visibility→IsVisible, layout properties, etc.)
    - 12 event mappings (Click, Mouse→Pointer events, etc.)

- [x] 1.3 Architecture Foundation
  - [x] Transformation pipeline interfaces (ITransformationPipeline, ITransformer)
  - [x] Context and result models (TransformationContext, TransformationResult)
  - [x] Diagnostic system (DiagnosticCollector with error/warning/info levels)
  - [x] Visitor patterns (WpfToAvaloniaRewriter for C#, WpfToAvaloniaXamlVisitor for XAML)
  - [x] Workspace management (MSBuildWorkspaceManager with compilation support)
  - [x] Configuration system (TransformationConfiguration with JSON loader)

### Milestone 2: C# Code Transformation Engine ✅ CORE COMPLETED

- [x] 2.1 Roslyn Syntax Rewriter Foundation
  - [x] WpfToAvaloniaRewriter base class with semantic analysis
  - [x] UsingDirectivesRewriter for namespace transformations
  - [x] Trivia preservation and diagnostic reporting
  - [x] CSharpFileTransformer for document-level transformations

- [x] 2.2 Type Reference Transformation
  - [x] TypeReferenceRewriter with semantic model integration
  - [x] IdentifierName and QualifiedName transformations
  - [x] WPF type detection and mapping lookup
  - [x] Manual review flagging for complex types

- [x] 2.3 Dependency Property Conversion (Detection)
  - [x] DependencyPropertyRewriter for analysis
  - [x] Detection of DependencyProperty.Register calls
  - [x] Detection of RegisterAttached for attached properties
  - [x] CLR property wrapper detection (GetValue/SetValue)
  - [x] Manual review flagging for conversions

- [x] 2.4 Member Access and API Transformation
  - [x] PropertyAccessRewriter for property transformations
  - [x] Visibility → IsVisible with type conversion warnings
  - [x] EventHandlerRewriter for event subscriptions
  - [x] Mouse event → Pointer event transformations
  - [x] Value conversion detection and warnings

### Milestone 3: XAML Transformation Engine ✅ CORE COMPLETED

- [x] 3.1 XAML Parser Infrastructure
  - [x] XDocument-based parser with whitespace preservation
  - [x] Parse error recovery and diagnostic reporting
  - [x] File I/O operations with formatting preservation

- [x] 3.2 XAML Namespace Transformation
  - [x] WPF → Avalonia default namespace transformation
  - [x] xmlns declarations updates
  - [x] clr-namespace transformations with assembly references

- [x] 3.3 Control and Element Transformation
  - [x] XamlControlTransformer with type mapping lookup
  - [x] Element name transformations (e.g., Label → TextBlock)
  - [x] Recursive element tree transformation

- [x] 3.4 Property and Attribute Transformation
  - [x] XamlPropertyTransformer for attribute transformations
  - [x] Visibility → IsVisible with value conversion
  - [x] Attached property syntax updates
  - [x] Property type change detection and warnings
  - [x] XamlFileTransformer orchestrating all XAML transformations

### Next Priority Milestones

- **Milestone 2.5**: 🔥 **Hybrid XML/XamlX/Roslyn XAML Transformation Engine** (HIGH PRIORITY)
  - **Unified AST Architecture**: Combines power of XML, XamlX, and Roslyn
  - **XML Layer**: Fast parsing, perfect formatting preservation, structure-focused (System.Xml.Linq)
  - **XamlX Layer**: Semantic analysis, type resolution, markup extension evaluation (extern/XamlX)
  - **Roslyn Layer**: C# code-behind coordination, field generation, event handler sync
  - **Hybrid Transformations**: XML-level for simple/fast, semantic-level for complex/type-safe
  - **Best of All Worlds**: Speed + Formatting + Type Safety + Semantic Understanding
  - Reference implementations:
    - extern/Avalonia/src/Markup (Avalonia XAML compiler)
    - System.Xml.Linq (XML parsing)
    - Microsoft.CodeAnalysis (Roslyn semantic analysis)

- **Milestone 4**: Project File Transformation
- **Milestone 5**: Analyzer Infrastructure

## Documentation

### Architecture & Design
- **[Hybrid Approach Summary](docs/HYBRID_APPROACH_SUMMARY.md)** - Executive overview of the hybrid XML/XamlX/Roslyn architecture
- **[Hybrid Architecture](docs/HYBRID_ARCHITECTURE.md)** - Detailed technical architecture (600+ lines)
- **[XamlX Parser Architecture](docs/XAMLX_PARSER_ARCHITECTURE.md)** - XamlX-specific design and patterns
- **[Implementation Roadmap](docs/IMPLEMENTATION_ROADMAP.md)** - 8-week phased implementation plan

### Project Planning
- **[Migration Plan](docs/MIGRATION_PLAN.md)** - Complete project plan with all milestones (1000+ tasks)
- **[Mapping Database](src/WpfToAvalonia.Mappings/Data/core-mappings.json)** - WPF→Avalonia mappings

## Key Features (Planned)

### Core WPF→Avalonia Mappings

#### Namespaces
- `System.Windows` → `Avalonia`
- `System.Windows.Controls` → `Avalonia.Controls`
- `System.Windows.Data` → `Avalonia.Data`
- `System.Windows.Input` → `Avalonia.Input`
- `System.Windows.Media` → `Avalonia.Media`

#### Types
- `DependencyObject` → `AvaloniaObject`
- `DependencyProperty` → `StyledProperty` / `DirectProperty`
- `Label` → `TextBlock` (with review)
- Most controls map 1:1 with namespace change

#### Properties
- `Visibility` (enum) → `IsVisible` (bool)
  - `Visible` → `true`
  - `Collapsed`/`Hidden` → `false`

#### Events
- `MouseEnter` → `PointerEntered`
- `MouseLeave` → `PointerExited`
- `MouseMove` → `PointerMoved`
- `MouseDown` → `PointerPressed`
- `MouseUp` → `PointerReleased`

## Building

```bash
# Restore and build
dotnet restore
dotnet build

# Run tests
dotnet test

# Build CLI tool
dotnet build src/WpfToAvalonia.CLI
```

## Usage (Future)

```bash
# Analyze a WPF project
wpf2avalonia analyze --project MyWpfApp.csproj

# Perform migration (dry run)
wpf2avalonia migrate --project MyWpfApp.csproj --dry-run

# Perform actual migration with backup
wpf2avalonia migrate --project MyWpfApp.csproj --backup

# Generate migration report
wpf2avalonia report --output migration-report.html
```

## Documentation

- [Migration Plan](docs/MIGRATION_PLAN.md) - Complete project plan with all milestones
- [Mapping Database](src/WpfToAvalonia.Mappings/Data/core-mappings.json) - Current WPF→Avalonia mappings

## Contributing

This project is in early development. Contributions are welcome once the core architecture is established.

## License

TBD

## Acknowledgments

- [Avalonia UI](https://avaloniaui.net/) - The cross-platform XAML framework
- [Roslyn](https://github.com/dotnet/roslyn) - The .NET Compiler Platform
- Avalonia documentation and community migration guides

---

**Project Start Date**: October 23, 2025
**Current Phase**: Milestone 3 - XAML Transformation Engine (Core Complete)
**Completion Status**: ~60% (3 of 12 milestones core features complete)
