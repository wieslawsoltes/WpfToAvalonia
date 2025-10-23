# WPF Compatibility Layer on Avalonia - Implementation Plan

**Project**: WpfToAvalonia
**Date Created**: 2025-10-23
**Status**: Planning Phase
**Estimated Duration**: 20-25 weeks (500-600 hours)

---

## Executive Summary

### Vision

Create a **WPF Compatibility Layer** that allows WPF applications to run on top of Avalonia's windowing and rendering infrastructure with **minimal to zero code changes**. This will be achieved by:

1. **Using WPF managed assemblies** directly (WindowsBase, PresentationCore, PresentationFramework)
2. **Shimming critical types** to redirect to Avalonia implementations
3. **Runtime interception** of WPF calls to Avalonia equivalents
4. **Compile-time source linking** for customized behavior

### Key Benefits

**For WpfToAvalonia Migration Tool**:
- Validates transformation accuracy by running original WPF code
- Provides runtime comparison between WPF and transformed Avalonia apps
- Enables gradual migration (run parts as WPF, parts as Avalonia)
- Tests edge cases and compatibility issues

**For WPF Applications**:
- Cross-platform execution (macOS, Linux) without rewriting
- Modern rendering pipeline (GPU-accelerated via Skia/Direct3D)
- Access to Avalonia ecosystem and controls
- Performance improvements from Avalonia's optimized compositor

**Symbiotic Relationship**:
- WPF Compat Layer → Validates WpfToAvalonia transformations
- WpfToAvalonia → Provides transformation patterns for compat layer
- Both share type mapping database and property metadata

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    WPF Application Code                      │
│         (Unchanged - uses System.Windows.*)                  │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              WPF Managed Assemblies (Linked)                 │
│    WindowsBase.dll, PresentationCore.dll (source-linked)    │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│           WPF-Avalonia Compatibility Shim Layer              │
│  • Type Mapping (DependencyObject → AvaloniaObject)         │
│  • Property System Bridge (DP → StyledProperty)             │
│  • Event Router (RoutedEvent → RoutedEvent)                 │
│  • Visual Tree Adapter (Visual → Visual)                    │
│  • Layout System Bridge (UIElement → Layoutable)            │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              Avalonia Framework Core                         │
│  Avalonia.Base, Avalonia.Controls, Avalonia.Layout          │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│         Avalonia Platform Backends (Windowing)               │
│    Win32, macOS, X11, Wayland, iOS, Android, Browser        │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│            Avalonia Rendering Engine                         │
│      Skia, Direct3D11, Metal, OpenGL, Vulkan                │
└─────────────────────────────────────────────────────────────┘
```

### Feasibility Assessment

Based on comprehensive architecture analysis of WPF and Avalonia source code:

| Feature Category | Feasibility | Coverage | Effort |
|-----------------|-------------|----------|--------|
| Property System | **High (90%)** | Full DependencyProperty → StyledProperty/DirectProperty | Medium |
| Visual Tree | **High (95%)** | UIElement hierarchy maps 1:1 to Avalonia.Visual | Low |
| Layout System | **Very High (98%)** | Identical Measure/Arrange two-pass | Very Low |
| Event Routing | **High (85%)** | Routed events compatible with adapter | Medium |
| Data Binding | **High (80%)** | Core binding modes identical, advanced features differ | High |
| Controls | **High (90%)** | Most controls have Avalonia equivalents | Medium |
| Styling/Triggers | **Medium (70%)** | Triggers need complex transformation | High |
| Resources | **High (85%)** | ResourceDictionary compatible | Medium |
| XAML Loading | **High (90%)** | Can reuse WPF XAML with namespace shims | Medium |
| Rendering | **Medium (65%)** | GPU-first vs mixed, custom DrawingVisual harder | High |
| 3D Support | **Not Feasible (0%)** | Avalonia is 2D-only | N/A |

**Overall Feasibility: 80-85% for mainstream WPF applications**

---

## Table of Contents

1. [Phase 1: Foundation & Type System](#phase-1-foundation--type-system-weeks-1-4)
2. [Phase 2: Property & Event Systems](#phase-2-property--event-systems-weeks-5-8)
3. [Phase 3: Visual Tree & Layout](#phase-3-visual-tree--layout-weeks-9-12)
4. [Phase 4: Controls & Templates](#phase-4-controls--templates-weeks-13-16)
5. [Phase 5: Data Binding & Resources](#phase-5-data-binding--resources-weeks-17-20)
6. [Phase 6: Integration & Testing](#phase-6-integration--testing-weeks-21-25)
7. [Integration with WpfToAvalonia](#integration-with-wpftoavalonia)
8. [Technical Specifications](#technical-specifications)
9. [Risk Assessment](#risk-assessment)

---

## Phase 1: Foundation & Type System (Weeks 1-4)

### Goal
Establish the foundational infrastructure for WPF-Avalonia interop, including type mapping, assembly management, and basic property system bridge.

### 1.1 Project Structure Setup

- [ ] **1.1.1** Create WpfCompat solution structure
  - [ ] 1.1.1.1 Create `src/WpfCompat.Runtime` - Core compatibility layer
  - [ ] 1.1.1.2 Create `src/WpfCompat.Shims` - Type shims and adapters
  - [ ] 1.1.1.3 Create `src/WpfCompat.Build` - MSBuild tasks for source linking
  - [ ] 1.1.1.4 Create `tests/WpfCompat.Tests` - Unit and integration tests
  - [ ] 1.1.1.5 Create `samples/WpfCompat.Samples` - Demo applications

- [ ] **1.1.2** Configure WPF source linking
  - [ ] 1.1.2.1 Add WPF repository as git submodule (already in extern/wpf)
  - [ ] 1.1.2.2 Identify managed assemblies for source linking (WindowsBase, PresentationCore, PresentationFramework)
  - [ ] 1.1.2.3 Create MSBuild props for conditional compilation symbols (`AVALONIA_COMPAT`, `NO_HWND`, `NO_NATIVE_GRAPHICS`)
  - [ ] 1.1.2.4 Configure build to exclude native dependencies (MilCore, wpfgfx)
  - [ ] 1.1.2.5 Set up incremental source file inclusion strategy

- [ ] **1.1.3** Configure Avalonia dependencies
  - [ ] 1.1.3.1 Reference Avalonia.Base (property system, observables)
  - [ ] 1.1.3.2 Reference Avalonia.Controls (control hierarchy)
  - [ ] 1.1.3.3 Reference Avalonia.Layout (layout system)
  - [ ] 1.1.3.4 Reference Avalonia.Markup.Xaml (XAML loading)
  - [ ] 1.1.3.5 Reference Avalonia.Skia (rendering backend)
  - [ ] 1.1.3.6 Reference Avalonia.Desktop (windowing)

### 1.2 Type Mapping Database

- [ ] **1.2.1** Design type mapping infrastructure
  - [ ] 1.2.1.1 Create `TypeMapping` class with WPF→Avalonia pairs
  - [ ] 1.2.1.2 Design property characteristic analyzer (for StyledProperty vs DirectProperty selection)
  - [ ] 1.2.1.3 Create metadata storage for property callbacks, validation, coercion
  - [ ] 1.2.1.4 Design mapping versioning for Avalonia API evolution
  - [ ] 1.2.1.5 Create diagnostic system for unmapped types

- [ ] **1.2.2** Populate core type mappings
  - [ ] 1.2.2.1 Map `System.Windows.DependencyObject` → `Avalonia.AvaloniaObject`
  - [ ] 1.2.2.2 Map `System.Windows.DependencyProperty` → `Avalonia.AvaloniaProperty<T>`
  - [ ] 1.2.2.3 Map `System.Windows.UIElement` → `Avalonia.Visual`
  - [ ] 1.2.2.4 Map `System.Windows.FrameworkElement` → `Avalonia.Controls.Control`
  - [ ] 1.2.2.5 Map `System.Windows.Application` → `Avalonia.Application`
  - [ ] 1.2.2.6 Map `System.Windows.Window` → `Avalonia.Controls.Window`
  - [ ] 1.2.2.7 Create mapping for 50+ core WPF types (see Type Mapping Reference)

- [ ] **1.2.3** Property mapping database
  - [ ] 1.2.3.1 Map common properties (Width, Height, Margin, Padding, etc.)
  - [ ] 1.2.3.2 Map naming differences (Visibility → IsVisible, etc.)
  - [ ] 1.2.3.3 Map attached properties (Grid.Row, Canvas.Left, etc.)
  - [ ] 1.2.3.4 Identify property metadata patterns (inheritance, coercion, validation)
  - [ ] 1.2.3.5 Create heuristics for StyledProperty vs DirectProperty selection

### 1.3 DependencyObject Bridge

- [ ] **1.3.1** Implement DependencyObject shim
  - [ ] 1.3.1.1 Create `WpfCompat.DependencyObject` as base class
  - [ ] 1.3.1.2 Implement `GetValue(DependencyProperty)` → `GetValue(AvaloniaProperty<T>)`
  - [ ] 1.3.1.3 Implement `SetValue(DependencyProperty, object)` → `SetValue(AvaloniaProperty<T>, T)`
  - [ ] 1.3.1.4 Implement `ClearValue(DependencyProperty)` → `ClearValue(AvaloniaProperty<T>)`
  - [ ] 1.3.1.5 Implement `ReadLocalValue(DependencyProperty)` using Avalonia's local value storage
  - [ ] 1.3.1.6 Bridge `DependencyPropertyChanged` events to Avalonia observables

- [ ] **1.3.2** Implement DependencyProperty shim
  - [ ] 1.3.2.1 Create `WpfCompat.DependencyProperty` wrapper around `AvaloniaProperty<T>`
  - [ ] 1.3.2.2 Implement `Register(name, type, ownerType, metadata)` → creates StyledProperty or DirectProperty
  - [ ] 1.3.2.3 Implement `RegisterAttached(name, type, ownerType, metadata)`
  - [ ] 1.3.2.4 Implement `RegisterReadOnly(name, type, ownerType, metadata)` → DirectProperty
  - [ ] 1.3.2.5 Implement `AddOwner(property, ownerType, metadata)`
  - [ ] 1.3.2.6 Implement `OverrideMetadata(property, ownerType, metadata)`

- [ ] **1.3.3** Property metadata bridge
  - [ ] 1.3.3.1 Map `PropertyMetadata` → `StyledPropertyMetadata<T>`
  - [ ] 1.3.3.2 Map `FrameworkPropertyMetadata` flags (Inherits, AffectsMeasure, AffectsArrange, etc.)
  - [ ] 1.3.3.3 Bridge `PropertyChangedCallback` to Avalonia's `Changed` observable
  - [ ] 1.3.3.4 Bridge `CoerceValueCallback` to Avalonia's coercion system
  - [ ] 1.3.3.5 Bridge `ValidateValueCallback` (Avalonia has different validation model)

### 1.4 Testing Infrastructure

- [ ] **1.4.1** Unit test framework
  - [ ] 1.4.1.1 Create xUnit test project for WpfCompat.Runtime
  - [ ] 1.4.1.2 Test DependencyProperty registration and retrieval
  - [ ] 1.4.1.3 Test property value get/set operations
  - [ ] 1.4.1.4 Test property metadata callbacks
  - [ ] 1.4.1.5 Test property inheritance
  - [ ] 1.4.1.6 Test attached properties

- [ ] **1.4.2** Integration test applications
  - [ ] 1.4.2.1 Create minimal WPF app with custom DependencyProperties
  - [ ] 1.4.2.2 Create WPF app using only linked source (no native deps)
  - [ ] 1.4.2.3 Verify app runs on Avalonia windowing
  - [ ] 1.4.2.4 Compare property behavior WPF vs WpfCompat
  - [ ] 1.4.2.5 Performance benchmarks for property access

---

## Phase 2: Property & Event Systems (Weeks 5-8)

### Goal
Complete the property system with advanced features and implement event routing compatibility.

### 2.1 Advanced Property Features

- [ ] **2.1.1** Attached property system
  - [ ] 2.1.1.1 Implement `GetValue`/`SetValue` for attached properties
  - [ ] 2.1.1.2 Support for attached property inheritance (e.g., `TextElement.FontSize`)
  - [ ] 2.1.1.3 Bridge `DependencyPropertyHelper` utilities
  - [ ] 2.1.1.4 Test Grid.Row, Canvas.Left, and other layout attached properties
  - [ ] 2.1.1.5 Create validation for attached property usage patterns

- [ ] **2.1.2** Property value inheritance
  - [ ] 2.1.2.1 Implement WPF's inheritance traversal algorithm
  - [ ] 2.1.2.2 Map inheritable properties (FontFamily, FontSize, Foreground, etc.)
  - [ ] 2.1.2.3 Bridge to Avalonia's `Inherits` flag in StyledPropertyMetadata
  - [ ] 2.1.2.4 Handle inheritance boundaries (e.g., Window vs UserControl)
  - [ ] 2.1.2.5 Test inheritance through visual tree

- [ ] **2.1.3** Property precedence system
  - [ ] 2.1.3.1 Implement WPF's value precedence (Local > Style > Inherited > Default)
  - [ ] 2.1.3.2 Bridge to Avalonia's value priority system
  - [ ] 2.1.3.3 Implement `ReadLocalValue` for local-only values
  - [ ] 2.1.3.4 Support style setters (via Avalonia styles)
  - [ ] 2.1.3.5 Support template setters

- [ ] **2.1.4** Property coercion and validation
  - [ ] 2.1.4.1 Implement `CoerceValue(DependencyProperty)` API
  - [ ] 2.1.4.2 Bridge `CoerceValueCallback` to Avalonia's coercion
  - [ ] 2.1.4.3 Implement `ValidateValueCallback` (may need custom validation layer)
  - [ ] 2.1.4.4 Test coercion scenarios (Min/Max bounds, etc.)
  - [ ] 2.1.4.5 Test validation rejection scenarios

### 2.2 Event Routing System

- [ ] **2.2.1** RoutedEvent infrastructure
  - [ ] 2.2.1.1 Create `WpfCompat.RoutedEvent` wrapper around Avalonia.Interactivity.RoutedEvent
  - [ ] 2.2.1.2 Implement `RegisterRoutedEvent(name, routingStrategy, handlerType, ownerType)`
  - [ ] 2.2.1.3 Implement `AddOwner(routedEvent, ownerType)`
  - [ ] 2.2.1.4 Map WPF routing strategies (Bubble, Tunnel, Direct) to Avalonia equivalents
  - [ ] 2.2.1.5 Create event registration database

- [ ] **2.2.2** Event routing adapter
  - [ ] 2.2.2.1 Implement `RaiseEvent(RoutedEventArgs)` bridge
  - [ ] 2.2.2.2 Bridge WPF's separate Tunnel/Bubble to Avalonia's combined flags
  - [ ] 2.2.2.3 Implement `AddHandler(RoutedEvent, Delegate, bool handledEventsToo)`
  - [ ] 2.2.2.4 Implement `RemoveHandler(RoutedEvent, Delegate)`
  - [ ] 2.2.2.5 Support for class-level handlers (`EventManager.RegisterClassHandler`)

- [ ] **2.2.3** RoutedEventArgs bridge
  - [ ] 2.2.2.1 Create `WpfCompat.RoutedEventArgs` base class
  - [ ] 2.2.3.2 Implement `Handled` property with Avalonia interop
  - [ ] 2.2.3.3 Implement `Source` and `OriginalSource` properties
  - [ ] 2.2.3.4 Bridge event bubbling through visual tree
  - [ ] 2.2.3.5 Bridge event tunneling (PreviewXXX events)

- [ ] **2.2.4** Command system
  - [ ] 2.2.4.1 Implement `ICommand` bridge (already compatible interface)
  - [ ] 2.2.4.2 Implement `RoutedCommand` → Avalonia command binding
  - [ ] 2.2.4.3 Implement `RoutedUICommand` with text/gestures
  - [ ] 2.2.4.4 Bridge `CommandManager` for CanExecute routing
  - [ ] 2.2.4.5 Support for command parameters and targets

### 2.3 Event Testing

- [ ] **2.3.1** Event routing tests
  - [ ] 2.3.1.1 Test basic button click events
  - [ ] 2.3.1.2 Test event bubbling through nested controls
  - [ ] 2.3.1.3 Test preview (tunnel) events
  - [ ] 2.3.1.4 Test event handled state
  - [ ] 2.3.1.5 Test class-level handlers
  - [ ] 2.3.1.6 Benchmark event routing performance

- [ ] **2.3.2** Command binding tests
  - [ ] 2.3.2.1 Test Button.Command binding
  - [ ] 2.3.2.2 Test CanExecute updates
  - [ ] 2.3.2.3 Test command parameters
  - [ ] 2.3.2.4 Test routed commands
  - [ ] 2.3.2.5 Integration with MVVM patterns

---

## Phase 3: Visual Tree & Layout (Weeks 9-12)

### Goal
Implement visual tree management and layout system compatibility.

### 3.1 Visual Tree Bridge

- [ ] **3.1.1** UIElement base implementation
  - [ ] 3.1.1.1 Create `WpfCompat.UIElement` inheriting from Avalonia.Visual
  - [ ] 3.1.1.2 Implement `Measure(Size availableSize)` → Avalonia's Measure
  - [ ] 3.1.1.3 Implement `Arrange(Rect finalRect)` → Avalonia's Arrange
  - [ ] 3.1.1.4 Implement `DesiredSize` property bridge
  - [ ] 3.1.1.5 Implement `RenderSize` property bridge
  - [ ] 3.1.1.6 Implement `InvalidateMeasure()` and `InvalidateArrange()`

- [ ] **3.1.2** Visual tree navigation
  - [ ] 3.1.2.1 Implement `VisualTreeHelper` static methods
  - [ ] 3.1.2.2 Implement `GetParent(Visual)` → Avalonia's VisualParent
  - [ ] 3.1.2.3 Implement `GetChildrenCount(Visual)` and `GetChild(Visual, int)`
  - [ ] 3.1.2.4 Implement visual tree hit testing
  - [ ] 3.1.2.5 Implement `TransformToAncestor` and coordinate transformations

- [ ] **3.1.3** FrameworkElement implementation
  - [ ] 3.1.3.1 Create `WpfCompat.FrameworkElement` inheriting from WpfCompat.UIElement
  - [ ] 3.1.3.2 Implement Width, Height, MinWidth, MaxWidth, MinHeight, MaxHeight
  - [ ] 3.1.3.3 Implement Margin property (already compatible)
  - [ ] 3.1.3.4 Implement HorizontalAlignment and VerticalAlignment
  - [ ] 3.1.3.5 Implement DataContext property with inheritance
  - [ ] 3.1.3.6 Implement Name property (x:Name)

### 3.2 Layout System

- [ ] **3.2.1** Layout panel base
  - [ ] 3.2.1.1 Create `WpfCompat.Panel` base class
  - [ ] 3.2.1.2 Implement `Children` collection with Avalonia's Controls
  - [ ] 3.2.1.3 Implement `MeasureOverride(Size constraint)` bridge
  - [ ] 3.2.1.4 Implement `ArrangeOverride(Size finalSize)` bridge
  - [ ] 3.2.1.5 Implement Background property (via Avalonia's Background)

- [ ] **3.2.2** Core layout panels
  - [ ] 3.2.2.1 Implement `StackPanel` (already exists in Avalonia, create compat wrapper)
  - [ ] 3.2.2.2 Implement `Grid` with row/column definitions (bridge to Avalonia.Controls.Grid)
  - [ ] 3.2.2.3 Implement `Canvas` with attached Left/Top/Right/Bottom
  - [ ] 3.2.2.4 Implement `DockPanel` (exists in Avalonia)
  - [ ] 3.2.2.5 Implement `WrapPanel` (exists in Avalonia)
  - [ ] 3.2.2.6 Create shims for less common panels (UniformGrid, VirtualizingStackPanel)

- [ ] **3.2.3** Layout properties and attached properties
  - [ ] 3.2.3.1 Implement Grid.Row, Grid.Column, Grid.RowSpan, Grid.ColumnSpan
  - [ ] 3.2.3.2 Implement Canvas.Left, Canvas.Top, Canvas.Right, Canvas.Bottom
  - [ ] 3.2.3.3 Implement DockPanel.Dock
  - [ ] 3.2.3.4 Test layout precedence and conflicts
  - [ ] 3.2.3.5 Test complex nested layouts

### 3.3 Rendering Bridge

- [ ] **3.3.1** Visual rendering
  - [ ] 3.3.1.1 Implement `OnRender(DrawingContext)` → Avalonia's Render
  - [ ] 3.3.1.2 Create `DrawingContext` wrapper around Avalonia's DrawingContext
  - [ ] 3.3.1.3 Bridge WPF drawing primitives (DrawRectangle, DrawEllipse, DrawText, etc.)
  - [ ] 3.3.1.4 Bridge Pen and Brush types to Avalonia equivalents
  - [ ] 3.3.1.5 Implement `InvalidateVisual()` → Avalonia's InvalidateVisual

- [ ] **3.3.2** Brushes and pens
  - [ ] 3.3.2.1 Map `SolidColorBrush` → Avalonia.Media.SolidColorBrush
  - [ ] 3.3.2.2 Map `LinearGradientBrush` → Avalonia.Media.LinearGradientBrush
  - [ ] 3.3.2.3 Map `RadialGradientBrush` → Avalonia.Media.RadialGradientBrush
  - [ ] 3.3.2.4 Map `ImageBrush` → Avalonia.Media.ImageBrush
  - [ ] 3.3.2.5 Map `Pen` properties (Thickness, DashStyle, etc.)
  - [ ] 3.3.2.6 Create diagnostic for unsupported brush types (DrawingBrush)

- [ ] **3.3.3** Transforms
  - [ ] 3.3.3.1 Implement `RenderTransform` → Avalonia's RenderTransform
  - [ ] 3.3.3.2 Map transform types (TranslateTransform, RotateTransform, ScaleTransform, SkewTransform)
  - [ ] 3.3.3.3 Map `TransformGroup` → Avalonia's TransformGroup
  - [ ] 3.3.3.4 Map `MatrixTransform` → Avalonia's MatrixTransform
  - [ ] 3.3.3.5 Detect LayoutTransform usage and emit warning (not supported in Avalonia)

### 3.4 Layout Testing

- [ ] **3.4.1** Layout accuracy tests
  - [ ] 3.4.1.1 Test StackPanel vertical/horizontal layout
  - [ ] 3.4.1.2 Test Grid with row/column definitions and spans
  - [ ] 3.4.1.3 Test Canvas absolute positioning
  - [ ] 3.4.1.4 Test complex nested layouts
  - [ ] 3.4.1.5 Test margin, padding, alignment interactions
  - [ ] 3.4.1.6 Compare layout results with WPF baseline

- [ ] **3.4.2** Visual rendering tests
  - [ ] 3.4.2.1 Test basic shape rendering (rectangles, ellipses, lines)
  - [ ] 3.4.2.2 Test brush rendering (solid, gradient)
  - [ ] 3.4.2.3 Test transform rendering
  - [ ] 3.4.2.4 Visual regression testing (screenshot comparison)
  - [ ] 3.4.2.5 Performance benchmarks for rendering

---

## Phase 4: Controls & Templates (Weeks 13-16)

### Goal
Implement WPF control library with template support.

### 4.1 Base Control Infrastructure

- [ ] **4.1.1** Control base class
  - [ ] 4.1.1.1 Create `WpfCompat.Control` inheriting from FrameworkElement
  - [ ] 4.1.1.2 Implement Template property (ControlTemplate)
  - [ ] 4.1.1.3 Implement `OnApplyTemplate()` lifecycle
  - [ ] 4.1.1.4 Implement Background, Foreground, BorderBrush, BorderThickness
  - [ ] 4.1.1.5 Implement Padding, FontFamily, FontSize, FontWeight
  - [ ] 4.1.1.6 Implement IsEnabled, IsTabStop, TabIndex

- [ ] **4.1.2** ContentControl
  - [ ] 4.1.2.1 Implement Content property with template support
  - [ ] 4.1.2.2 Implement ContentTemplate (DataTemplate)
  - [ ] 4.1.2.3 Implement ContentTemplateSelector (if feasible)
  - [ ] 4.1.2.4 Create default content presenter template part

- [ ] **4.1.3** ItemsControl
  - [ ] 4.1.3.1 Implement Items and ItemsSource properties
  - [ ] 4.1.3.2 Implement ItemTemplate (DataTemplate)
  - [ ] 4.1.3.3 Implement ItemsPanel template
  - [ ] 4.1.3.4 Implement ItemContainerStyle
  - [ ] 4.1.3.5 Create items presenter template part

### 4.2 Core Controls

- [ ] **4.2.1** Simple controls
  - [ ] 4.2.1.1 Implement `Button` → Avalonia.Controls.Button wrapper
  - [ ] 4.2.1.2 Implement `TextBlock` → Avalonia.Controls.TextBlock wrapper
  - [ ] 4.2.1.3 Implement `TextBox` → Avalonia.Controls.TextBox wrapper
  - [ ] 4.2.1.4 Implement `CheckBox` → Avalonia.Controls.CheckBox wrapper
  - [ ] 4.2.1.5 Implement `RadioButton` → Avalonia.Controls.RadioButton wrapper
  - [ ] 4.2.1.6 Implement `Label` → Avalonia.Controls.Label wrapper

- [ ] **4.2.2** Container controls
  - [ ] 4.2.2.1 Implement `Border` → Avalonia.Controls.Border wrapper
  - [ ] 4.2.2.2 Implement `ScrollViewer` → Avalonia.Controls.ScrollViewer wrapper
  - [ ] 4.2.2.3 Implement `GroupBox` → create custom control
  - [ ] 4.2.2.4 Implement `Expander` → Avalonia.Controls.Expander wrapper
  - [ ] 4.2.2.5 Implement `TabControl` and `TabItem` → Avalonia equivalents

- [ ] **4.2.3** Selection controls
  - [ ] 4.2.3.1 Implement `ListBox` → Avalonia.Controls.ListBox wrapper
  - [ ] 4.2.3.2 Implement `ComboBox` → Avalonia.Controls.ComboBox wrapper
  - [ ] 4.2.3.3 Implement `ListView` (can use ListBox with custom template)
  - [ ] 4.2.3.4 Implement `TreeView` → Avalonia.Controls.TreeView wrapper
  - [ ] 4.2.3.5 Implement selection behaviors (SelectionMode, SelectedItem, etc.)

- [ ] **4.2.4** Complex controls
  - [ ] 4.2.4.1 Implement `DataGrid` → Avalonia.Controls.DataGrid wrapper
  - [ ] 4.2.4.2 Implement `Menu` and `MenuItem` → Avalonia equivalents
  - [ ] 4.2.4.3 Implement `ToolBar` → create custom control
  - [ ] 4.2.4.4 Implement `StatusBar` → create custom control
  - [ ] 4.2.4.5 Implement `RichTextBox` (limited - Avalonia doesn't have full equivalent)

### 4.3 Template System

- [ ] **4.3.1** ControlTemplate implementation
  - [ ] 4.3.1.1 Create `ControlTemplate` class with XAML parsing
  - [ ] 4.3.1.2 Implement template instantiation from XAML
  - [ ] 4.3.1.3 Implement `TemplateBinding` markup extension
  - [ ] 4.3.1.4 Implement template parts (GetTemplateChild)
  - [ ] 4.3.1.5 Support for VisualState trigger equivalents

- [ ] **4.3.2** DataTemplate implementation
  - [ ] 4.3.2.1 Create `DataTemplate` class with XAML parsing
  - [ ] 4.3.2.2 Implement data template instantiation
  - [ ] 4.3.2.3 Implement DataType matching
  - [ ] 4.3.2.4 Support for ContentPresenter template application
  - [ ] 4.3.2.5 Support for ItemsPresenter template application

- [ ] **4.3.3** Template selectors
  - [ ] 4.3.3.1 Implement `DataTemplateSelector` base class
  - [ ] 4.3.3.2 Bridge selector logic to Avalonia's template selection
  - [ ] 4.3.3.3 Support for ContentTemplateSelector
  - [ ] 4.3.3.4 Support for ItemTemplateSelector
  - [ ] 4.3.3.5 Test conditional template selection

### 4.4 Control Testing

- [ ] **4.4.1** Control functionality tests
  - [ ] 4.4.1.1 Test all core controls with default templates
  - [ ] 4.4.1.2 Test custom control templates
  - [ ] 4.4.1.3 Test data templates with binding
  - [ ] 4.4.1.4 Test template parts resolution
  - [ ] 4.4.1.5 Test control styling

- [ ] **4.4.2** Integration tests
  - [ ] 4.4.2.1 Create sample WPF app with various controls
  - [ ] 4.4.2.2 Run app on WpfCompat layer
  - [ ] 4.4.2.3 Verify visual appearance matches WPF
  - [ ] 4.4.2.4 Test interaction (clicks, selections, input)
  - [ ] 4.4.2.5 Performance benchmarks for control rendering

---

## Phase 5: Data Binding & Resources (Weeks 17-20)

### Goal
Implement data binding engine and resource management.

### 5.1 Binding Infrastructure

- [ ] **5.1.1** Binding class implementation
  - [ ] 5.1.1.1 Create `WpfCompat.Binding` class
  - [ ] 5.1.1.2 Implement Path property parsing
  - [ ] 5.1.1.3 Implement Mode property (OneWay, TwoWay, OneTime, OneWayToSource, Default)
  - [ ] 5.1.1.4 Implement Source, RelativeSource, ElementName properties
  - [ ] 5.1.1.5 Implement Converter property (IValueConverter bridge)
  - [ ] 5.1.1.6 Implement UpdateSourceTrigger (PropertyChanged, LostFocus, Explicit, Default)

- [ ] **5.1.2** Binding engine bridge
  - [ ] 5.1.2.1 Bridge WPF binding to Avalonia's binding system
  - [ ] 5.1.2.2 Implement binding path resolution (navigate property chains)
  - [ ] 5.1.2.3 Implement DataContext propagation through visual tree
  - [ ] 5.1.2.4 Bridge property change notifications (INotifyPropertyChanged)
  - [ ] 5.1.2.5 Implement binding expression evaluation
  - [ ] 5.1.2.6 Implement two-way binding synchronization

- [ ] **5.1.3** Binding markup extensions
  - [ ] 5.1.3.1 Implement `{Binding}` XAML markup extension
  - [ ] 5.1.3.2 Implement `{StaticResource}` markup extension
  - [ ] 5.1.3.3 Implement `{DynamicResource}` markup extension
  - [ ] 5.1.3.4 Implement `{TemplateBinding}` markup extension
  - [ ] 5.1.3.5 Implement `{RelativeSource}` with FindAncestor, Self, TemplatedParent modes
  - [ ] 5.1.3.6 Implement `{x:Static}` markup extension

### 5.2 Value Converters

- [ ] **5.2.1** IValueConverter bridge
  - [ ] 5.2.1.1 Map WPF IValueConverter to Avalonia IValueConverter (already compatible)
  - [ ] 5.2.1.2 Implement converter parameter passing
  - [ ] 5.2.1.3 Implement culture info support
  - [ ] 5.2.1.4 Test converter usage in bindings
  - [ ] 5.2.1.5 Create library of common converters (BooleanToVisibilityConverter, etc.)

- [ ] **5.2.2** Multi-value converters
  - [ ] 5.2.2.1 Create `IMultiValueConverter` interface
  - [ ] 5.2.2.2 Implement `MultiBinding` class
  - [ ] 5.2.2.3 Bridge multi-value binding to Avalonia (may need custom implementation)
  - [ ] 5.2.2.4 Test multi-binding scenarios
  - [ ] 5.2.2.5 Performance optimization for multi-bindings

### 5.3 Resource Management

- [ ] **5.3.1** ResourceDictionary implementation
  - [ ] 5.3.1.1 Create `WpfCompat.ResourceDictionary` class
  - [ ] 5.3.1.2 Implement resource key lookup with fallback
  - [ ] 5.3.1.3 Implement MergedDictionaries support
  - [ ] 5.3.1.4 Implement Source property for external XAML dictionaries
  - [ ] 5.3.1.5 Bridge to Avalonia's resource system

- [ ] **5.3.2** Resource resolution
  - [ ] 5.3.2.1 Implement FindResource(key) with exception on missing
  - [ ] 5.3.2.2 Implement TryFindResource(key) with null on missing
  - [ ] 5.3.2.3 Implement resource lookup through visual tree (element → app)
  - [ ] 5.3.2.4 Support for StaticResource resolution at parse time
  - [ ] 5.3.2.5 Support for DynamicResource resolution at runtime

- [ ] **5.3.3** Application resources
  - [ ] 5.3.3.1 Implement Application.Resources property
  - [ ] 5.3.3.2 Load application-level resource dictionaries from XAML
  - [ ] 5.3.3.3 Support for theme resource dictionaries
  - [ ] 5.3.3.4 Test resource override scenarios
  - [ ] 5.3.3.5 Test resource inheritance

### 5.4 Advanced Binding Features

- [ ] **5.4.1** Binding validation
  - [ ] 5.4.1.1 Implement IDataErrorInfo support
  - [ ] 5.4.1.2 Implement INotifyDataErrorInfo support
  - [ ] 5.4.1.3 Implement ValidationRule base class
  - [ ] 5.4.1.4 Implement Validation.Errors attached property
  - [ ] 5.4.1.5 Implement validation error templates

- [ ] **5.4.2** Priority bindings
  - [ ] 5.4.2.1 Implement PriorityBinding class (if Avalonia supports)
  - [ ] 5.4.2.2 Implement fallback value selection logic
  - [ ] 5.4.2.3 Test priority binding scenarios
  - [ ] 5.4.2.4 Fallback to first binding if not supported

### 5.5 Binding Testing

- [ ] **5.5.1** Binding accuracy tests
  - [ ] 5.5.1.1 Test OneWay binding updates
  - [ ] 5.5.1.2 Test TwoWay binding synchronization
  - [ ] 5.5.1.3 Test binding to nested properties (Path="Parent.Child.Value")
  - [ ] 5.5.1.4 Test ElementName binding
  - [ ] 5.5.1.5 Test RelativeSource binding (FindAncestor)
  - [ ] 5.5.1.6 Test converter application
  - [ ] 5.5.1.7 Test binding to collections (ObservableCollection)

- [ ] **5.5.2** Resource resolution tests
  - [ ] 5.5.2.1 Test StaticResource lookup
  - [ ] 5.5.2.2 Test DynamicResource updates
  - [ ] 5.5.2.3 Test merged dictionaries
  - [ ] 5.5.2.4 Test resource fallback chain
  - [ ] 5.5.2.5 Test theme resources

---

## Phase 6: Integration & Testing (Weeks 21-25)

### Goal
Complete integration, comprehensive testing, and real-world application validation.

### 6.1 XAML Loading

- [ ] **6.1.1** XAML parser integration
  - [ ] 6.1.1.1 Integrate with Avalonia.Markup.Xaml parser
  - [ ] 6.1.1.2 Implement namespace mapping (xmlns:wpf → WpfCompat types)
  - [ ] 6.1.1.3 Support for x:Class, x:Name, x:Key directives
  - [ ] 6.1.1.4 Support for markup extensions in XAML
  - [ ] 6.1.1.5 Load WPF XAML files with minimal modifications

- [ ] **6.1.2** InitializeComponent generation
  - [ ] 6.1.2.1 Create MSBuild task for XAML compilation
  - [ ] 6.1.2.2 Generate InitializeComponent() method
  - [ ] 6.1.2.3 Generate field declarations for x:Name elements
  - [ ] 6.1.2.4 Connect event handlers from XAML
  - [ ] 6.1.2.5 Test with WPF UserControl and Window

- [ ] **6.1.3** XAML designer support
  - [ ] 6.1.3.1 Investigate Visual Studio XAML previewer compatibility
  - [ ] 6.1.3.2 Investigate Rider XAML previewer compatibility
  - [ ] 6.1.3.3 Provide design-time metadata
  - [ ] 6.1.3.4 Support for d:DesignWidth, d:DesignHeight
  - [ ] 6.1.3.5 Support for d:DataContext

### 6.2 Application Lifecycle

- [ ] **6.2.1** Application class
  - [ ] 6.2.1.1 Implement WpfCompat.Application inheriting from Avalonia.Application
  - [ ] 6.2.2.2 Implement Run() method with main window
  - [ ] 6.2.1.3 Implement Startup, Exit, Activated, Deactivated events
  - [ ] 6.2.1.4 Implement Application.Current static property
  - [ ] 6.2.1.5 Implement shutdown modes (OnLastWindowClose, OnMainWindowClose, OnExplicitShutdown)

- [ ] **6.2.2** Window management
  - [ ] 6.2.2.1 Implement Window.Show(), ShowDialog() → Avalonia window methods
  - [ ] 6.2.2.2 Implement Window.Close() with closing event
  - [ ] 6.2.2.3 Implement window state (Minimized, Maximized, Normal)
  - [ ] 6.2.2.4 Implement window positioning and sizing
  - [ ] 6.2.2.5 Implement dialog result for ShowDialog()
  - [ ] 6.2.2.6 Test multi-window applications

- [ ] **6.2.3** Dispatcher integration
  - [ ] 6.2.3.1 Bridge Dispatcher.Invoke to Avalonia's dispatcher
  - [ ] 6.2.3.2 Bridge Dispatcher.BeginInvoke for async operations
  - [ ] 6.2.3.3 Implement DispatcherPriority mapping
  - [ ] 6.2.3.4 Implement Dispatcher.CheckAccess and VerifyAccess
  - [ ] 6.2.3.5 Test cross-thread UI updates

### 6.3 Style and Trigger System

- [ ] **6.3.1** Style implementation
  - [ ] 6.3.1.1 Create WpfCompat.Style class
  - [ ] 6.3.1.2 Implement Setters collection
  - [ ] 6.3.1.3 Implement TargetType property
  - [ ] 6.3.1.4 Implement BasedOn property for style inheritance
  - [ ] 6.3.1.5 Bridge to Avalonia's style system

- [ ] **6.3.2** Trigger system
  - [ ] 6.3.2.1 Implement Trigger base class
  - [ ] 6.3.2.2 Implement PropertyTrigger → Avalonia pseudo-classes where possible
  - [ ] 6.3.2.3 Implement DataTrigger (limited support, may need custom logic)
  - [ ] 6.3.2.4 Implement EventTrigger → bridge to Avalonia interactions
  - [ ] 6.3.2.5 Emit warnings for unsupported trigger scenarios
  - [ ] 6.3.2.6 Create diagnostic guide for trigger migration

### 6.4 Comprehensive Testing

- [ ] **6.4.1** Unit test coverage
  - [ ] 6.4.1.1 Achieve 80%+ code coverage for WpfCompat.Runtime
  - [ ] 6.4.1.2 Test all property system scenarios
  - [ ] 6.4.1.3 Test all event routing scenarios
  - [ ] 6.4.1.4 Test all binding scenarios
  - [ ] 6.4.1.5 Test all resource scenarios
  - [ ] 6.4.1.6 Test all control behaviors

- [ ] **6.4.2** Integration test suites
  - [ ] 6.4.2.1 Create 10+ sample WPF applications of varying complexity
  - [ ] 6.4.2.2 Run all samples on WpfCompat layer
  - [ ] 6.4.2.3 Compare visual appearance with WPF
  - [ ] 6.4.2.4 Test on Windows, macOS, Linux
  - [ ] 6.4.2.5 Test with different Avalonia backends (Skia, Direct3D)
  - [ ] 6.4.2.6 Measure compatibility percentage

- [ ] **6.4.3** Real-world application testing
  - [ ] 6.4.3.1 Port an existing open-source WPF app (e.g., WPF demos)
  - [ ] 6.4.3.2 Document migration steps and issues
  - [ ] 6.4.3.3 Measure lines of code changed
  - [ ] 6.4.3.4 Benchmark performance vs native WPF
  - [ ] 6.4.3.5 Create case study documentation

### 6.5 Performance Optimization

- [ ] **6.5.1** Property system optimization
  - [ ] 6.5.1.1 Optimize property lookup caching
  - [ ] 6.5.1.2 Optimize property change notifications
  - [ ] 6.5.1.3 Reduce allocations in property get/set
  - [ ] 6.5.1.4 Benchmark property access performance

- [ ] **6.5.2** Binding performance
  - [ ] 6.5.2.1 Optimize binding path resolution
  - [ ] 6.5.2.2 Cache binding expressions
  - [ ] 6.5.2.3 Optimize converter invocations
  - [ ] 6.5.2.4 Benchmark binding update performance

- [ ] **6.5.3** Rendering performance
  - [ ] 6.5.3.1 Leverage Avalonia's GPU acceleration
  - [ ] 6.5.3.2 Optimize visual tree traversal
  - [ ] 6.5.3.3 Reduce draw call overhead
  - [ ] 6.5.3.4 Benchmark rendering frame rates

### 6.6 Documentation

- [ ] **6.6.1** API documentation
  - [ ] 6.6.1.1 XML doc comments for all public APIs
  - [ ] 6.6.1.2 Generate API documentation site (DocFX or similar)
  - [ ] 6.6.1.3 Document compatibility matrix (supported/unsupported features)
  - [ ] 6.6.1.4 Document known differences from WPF
  - [ ] 6.6.1.5 Document migration guide

- [ ] **6.6.2** Sample applications
  - [ ] 6.6.2.1 Create "Hello World" WPF on Avalonia sample
  - [ ] 6.6.2.2 Create data binding sample
  - [ ] 6.6.2.3 Create custom control sample
  - [ ] 6.6.2.4 Create MVVM sample
  - [ ] 6.6.2.5 Create multi-window sample
  - [ ] 6.6.2.6 Create style and template sample

- [ ] **6.6.3** Troubleshooting guides
  - [ ] 6.6.3.1 Common migration issues and solutions
  - [ ] 6.6.3.2 Debugging WpfCompat applications
  - [ ] 6.6.3.3 Performance tuning guide
  - [ ] 6.6.3.4 Cross-platform considerations
  - [ ] 6.6.3.5 FAQ document

---

## Integration with WpfToAvalonia

### Bidirectional Benefits

The WPF Compatibility Layer and WpfToAvalonia migration tool create a powerful symbiotic relationship:

```
┌────────────────────────────────────────────────────────────┐
│                  WpfCompat Layer ↔ WpfToAvalonia           │
│                   Symbiotic Relationship                    │
└────────────────────────────────────────────────────────────┘

WpfCompat → WpfToAvalonia:
├─ Validates transformation accuracy (run original WPF on Avalonia)
├─ Tests edge cases and compatibility issues
├─ Provides runtime validation of transformed code
├─ Identifies missing transformations
└─ Enables A/B comparison (WPF mode vs transformed mode)

WpfToAvalonia → WpfCompat:
├─ Provides transformation patterns for compat layer
├─ Shares type mapping database
├─ Shares property metadata knowledge
├─ Provides test cases for validation
└─ Documents migration patterns
```

### 7.1 Shared Infrastructure

- [ ] **7.1.1** Unified type mapping database
  - [ ] 7.1.1.1 Extract type mapping from WpfToAvalonia into shared library
  - [ ] 7.1.1.2 Use same mapping for both compat layer and transformation
  - [ ] 7.1.1.3 Version mapping database for Avalonia API evolution
  - [ ] 7.1.1.4 Share property characteristic analysis logic

- [ ] **7.1.2** Shared property metadata
  - [ ] 7.1.2.1 Extract DependencyPropertyTransformer metadata analysis
  - [ ] 7.1.2.2 Use same StyledProperty vs DirectProperty heuristics
  - [ ] 7.1.2.3 Share callback, coercion, validation patterns
  - [ ] 7.1.2.4 Sync metadata updates between projects

- [ ] **7.1.3** Shared diagnostics
  - [ ] 7.1.3.1 Use same DiagnosticCollector infrastructure
  - [ ] 7.1.3.2 Share diagnostic codes for unsupported features
  - [ ] 7.1.3.3 Generate compatibility reports using same format
  - [ ] 7.1.3.4 Unify warning messages

### 7.2 Validation Workflow

- [ ] **7.2.1** Runtime validation
  - [ ] 7.2.1.1 Run original WPF app on WpfCompat layer
  - [ ] 7.2.1.2 Run transformed Avalonia app
  - [ ] 7.2.1.3 Compare visual output (screenshot diff)
  - [ ] 7.2.1.4 Compare behavior (automated interaction tests)
  - [ ] 7.2.1.5 Report discrepancies

- [ ] **7.2.2** Transformation accuracy
  - [ ] 7.2.2.1 Use WpfCompat to identify missing transformations
  - [ ] 7.2.2.2 Validate property transformations (DependencyProperty → StyledProperty)
  - [ ] 7.2.2.3 Validate event routing transformations
  - [ ] 7.2.2.4 Validate binding transformations
  - [ ] 7.2.2.5 Use findings to improve WpfToAvalonia rules

### 7.3 Gradual Migration Support

- [ ] **7.3.1** Hybrid execution mode
  - [ ] 7.3.1.1 Allow mix of WpfCompat controls and native Avalonia controls
  - [ ] 7.3.1.2 Enable gradual transformation (transform one window at a time)
  - [ ] 7.3.1.3 Support for WPF UserControl in Avalonia app
  - [ ] 7.3.1.4 Support for Avalonia control in WpfCompat app
  - [ ] 7.3.1.5 Provide migration path guide

- [ ] **7.3.2** A/B comparison mode
  - [ ] 7.3.2.1 Run same UI twice (WpfCompat vs transformed Avalonia)
  - [ ] 7.3.2.2 Side-by-side visual comparison
  - [ ] 7.3.2.3 Performance comparison
  - [ ] 7.3.2.4 Generate comparison report
  - [ ] 7.3.2.5 Guide developers on what to transform

### 7.4 Testing Integration

- [ ] **7.4.1** Shared test infrastructure
  - [ ] 7.4.1.1 Create shared WPF sample apps
  - [ ] 7.4.1.2 Test apps on WpfCompat layer
  - [ ] 7.4.1.3 Transform apps with WpfToAvalonia
  - [ ] 7.4.1.4 Compare results
  - [ ] 7.4.1.5 Use differences to improve both projects

- [ ] **7.4.2** Continuous validation
  - [ ] 7.4.2.1 CI/CD pipeline running both WpfCompat and transformed apps
  - [ ] 7.4.2.2 Automated screenshot comparison
  - [ ] 7.4.2.3 Automated behavior testing
  - [ ] 7.4.2.4 Regression detection
  - [ ] 7.4.2.5 Nightly compatibility reports

---

## Technical Specifications

### 8.1 Architecture Decisions

**Source Linking Strategy**:
- Link WPF managed source files from extern/wpf/src
- Exclude files with native dependencies (P/Invoke to MilCore, wpfgfx)
- Use conditional compilation for platform-specific code
- Create shims for excluded APIs

**Property System Strategy**:
- DependencyProperty → StyledProperty for styled/inherited properties
- DependencyProperty → DirectProperty for direct CLR properties
- Heuristic: Check for AffectsRender, AffectsMeasure, Inherits flags
- Fallback: Default to StyledProperty

**Event System Strategy**:
- Map RoutedEvent 1:1 to Avalonia.Interactivity.RoutedEvent
- Separate WPF Tunnel/Bubble → Avalonia combined flags
- Bridge event observables for reactive scenarios
- Maintain WPF's AddHandler/RemoveHandler API surface

**XAML Loading Strategy**:
- Use Avalonia.Markup.Xaml parser as foundation
- Add WPF-specific markup extensions
- Transform xmlns during load (wpf: → avalonia:)
- Generate InitializeComponent via MSBuild task

### 8.2 Performance Targets

| Metric | Target | Rationale |
|--------|--------|-----------|
| Property Get/Set | < 1.5x WPF | Acceptable overhead for cross-platform |
| Binding Update | < 2x WPF | Acceptable for binding flexibility |
| Layout Pass | < 1.2x WPF | Leverage Avalonia's optimized layout |
| Render Frame | 60 FPS | GPU acceleration should match/exceed WPF |
| Memory Overhead | < 30% | Some overhead acceptable for compat |
| App Startup | < 2x WPF | Acceptable for cross-platform benefits |

### 8.3 Compatibility Targets

**Must Support (95%+ apps rely on these)**:
- DependencyProperty and attached properties
- Routed events (Bubble, Tunnel, Direct)
- Basic data binding (OneWay, TwoWay, converters)
- Core controls (Button, TextBox, ListBox, etc.)
- Layout panels (Grid, StackPanel, Canvas, DockPanel)
- Styles and resource dictionaries
- ControlTemplate and DataTemplate
- XAML loading

**Should Support (70%+ apps use these)**:
- Property triggers
- MultiBinding
- RelativeSource binding
- ItemsControl customization
- ScrollViewer
- Validation
- Converters

**Nice to Have (30%+ apps use these)**:
- DataTrigger (limited)
- EventTrigger → interactions
- Visual effects (limited)
- Custom rendering (DrawingVisual)

**Cannot Support (document limitations)**:
- Visual3D (3D graphics)
- LayoutTransform
- Full Freezable pattern
- WPF-specific APIs (UIAutomation native, Win32 interop)

### 8.4 Cross-Platform Considerations

**Windows**:
- Primary development platform
- Use Win32 or Direct3D11 backend
- Validate against WPF baseline

**macOS**:
- Use Cocoa windowing
- Use Metal or Skia rendering
- Test NSWindow integration

**Linux**:
- Use X11 or Wayland windowing
- Use Skia or OpenGL rendering
- Test Gtk integration

**Platform-Specific Issues**:
- Font rendering differences (ClearType on Windows, CoreText on macOS)
- Input method differences (IME handling)
- File path separators
- Dialog behaviors (MessageBox, FileDialog)

---

## Risk Assessment

### 9.1 Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| WPF source incompatibility with .NET 9+ | Medium | High | Pin to .NET 8 WPF, track upstream changes |
| Avalonia API breaking changes | Medium | High | Version lock Avalonia, coordinate with Avalonia team |
| Performance degradation | Medium | Medium | Benchmark early, optimize hot paths |
| Incomplete feature coverage | High | Medium | Document limitations, provide workarounds |
| Property system complexity | High | High | Extensive testing, shared logic with WpfToAvalonia |
| Event routing edge cases | Medium | Medium | Test with complex scenarios, fallback to simpler routing |
| XAML parsing differences | Low | Medium | Use Avalonia's parser, add WPF extensions |
| Memory leaks from event handlers | Medium | Medium | Automated leak detection, weak references |

### 9.2 Project Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Scope creep | High | High | Strict prioritization, MVP first |
| Insufficient testing | Medium | High | Test-driven development, CI/CD |
| Documentation lag | Medium | Medium | Doc-as-you-go, samples for each feature |
| Community adoption | Medium | Low | Clear value prop, good samples, active support |
| Maintenance burden | High | Medium | Modular design, shared infra with WpfToAvalonia |

### 9.3 Success Criteria

**Phase 1-2 (Weeks 1-8)**:
- [ ] Simple WPF app (Button, TextBox, Binding) runs on WpfCompat
- [ ] Property system tests passing (100 tests)
- [ ] Event routing tests passing (50 tests)

**Phase 3-4 (Weeks 9-16)**:
- [ ] Medium complexity WPF app (Grid, ListBox, Templates) runs
- [ ] Layout tests passing (200 tests)
- [ ] Control tests passing (150 tests)
- [ ] Visual regression tests passing

**Phase 5-6 (Weeks 17-25)**:
- [ ] Complex WPF app (MVVM, Resources, Styles) runs
- [ ] Binding tests passing (100 tests)
- [ ] At least 1 real-world WPF app ported with < 5% code changes
- [ ] Performance within 2x of WPF for common scenarios
- [ ] Documentation complete
- [ ] 80%+ feature coverage for mainstream WPF apps

---

## Appendix

### A. Type Mapping Reference

**Core Types**:
```
System.Windows.DependencyObject → Avalonia.AvaloniaObject
System.Windows.DependencyProperty → Avalonia.AvaloniaProperty<T>
System.Windows.UIElement → Avalonia.Visual
System.Windows.FrameworkElement → Avalonia.Controls.Control
System.Windows.Application → Avalonia.Application
System.Windows.Window → Avalonia.Controls.Window
```

**Controls** (50+ mappings):
```
System.Windows.Controls.Button → Avalonia.Controls.Button
System.Windows.Controls.TextBox → Avalonia.Controls.TextBox
System.Windows.Controls.ListBox → Avalonia.Controls.ListBox
System.Windows.Controls.ComboBox → Avalonia.Controls.ComboBox
System.Windows.Controls.DataGrid → Avalonia.Controls.DataGrid
... (see full mapping database)
```

**Panels**:
```
System.Windows.Controls.StackPanel → Avalonia.Controls.StackPanel
System.Windows.Controls.Grid → Avalonia.Controls.Grid
System.Windows.Controls.Canvas → Avalonia.Controls.Canvas
System.Windows.Controls.DockPanel → Avalonia.Controls.DockPanel
System.Windows.Controls.WrapPanel → Avalonia.Controls.WrapPanel
```

**Data Binding**:
```
System.Windows.Data.Binding → Avalonia.Data.Binding
System.Windows.Data.IValueConverter → Avalonia.Data.Converters.IValueConverter (same interface)
System.Windows.Data.BindingMode → Avalonia.Data.BindingMode
System.ComponentModel.INotifyPropertyChanged → (same, no change)
```

### B. Property Characteristic Analysis

**StyledProperty Indicators**:
- Property has AffectsRender, AffectsMeasure, or AffectsArrange metadata
- Property has Inherits flag (FontFamily, FontSize, etc.)
- Property is commonly set via styles
- Property has default value in metadata

**DirectProperty Indicators**:
- Read-only DependencyProperty (DependencyPropertyKey)
- Property wraps CLR field directly
- Property is performance-critical (avoid boxing)
- Property rarely changes

**Heuristic Decision Tree**:
```
Is ReadOnly?
  YES → DirectProperty
  NO → Has Inherits or AffectsRender/Measure/Arrange?
    YES → StyledProperty
    NO → Is performance-critical?
      YES → DirectProperty
      NO → StyledProperty (default)
```

### C. MSBuild Integration Example

**WpfCompat.Build/WpfCompat.props**:
```xml
<Project>
  <PropertyGroup>
    <WpfCompatEnabled>true</WpfCompatEnabled>
    <UseAvaloniaWindowing>true</UseAvaloniaWindowing>
    <DefineConstants>$(DefineConstants);AVALONIA_COMPAT;NO_HWND</DefineConstants>
  </PropertyGroup>

  <ItemGroup>
    <!-- Link WPF source files -->
    <Compile Include="$(WpfSourceRoot)/WindowsBase/**/*.cs"
             Exclude="$(WpfSourceRoot)/WindowsBase/**/*Native*.cs"
             Link="LinkedSource/WindowsBase/%(RecursiveDir)%(Filename)%(Extension)" />
    <!-- More source linking -->
  </ItemGroup>

  <ItemGroup>
    <!-- Avalonia dependencies -->
    <PackageReference Include="Avalonia" Version="11.0.0" />
    <PackageReference Include="Avalonia.Desktop" Version="11.0.0" />
    <PackageReference Include="Avalonia.Skia" Version="11.0.0" />
  </ItemGroup>
</Project>
```

### D. Sample Code Transformation

**Original WPF Code**:
```csharp
using System.Windows;
using System.Windows.Controls;

public partial class MainWindow : Window
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register("Message", typeof(string), typeof(MainWindow));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public MainWindow()
    {
        InitializeComponent();
        Message = "Hello WPF";
    }
}
```

**WpfCompat (No Code Changes)**:
```csharp
// SAME CODE - runs on Avalonia via WpfCompat layer
using System.Windows;
using System.Windows.Controls;

public partial class MainWindow : Window
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register("Message", typeof(string), typeof(MainWindow));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public MainWindow()
    {
        InitializeComponent(); // Loads XAML via Avalonia
        Message = "Hello WPF on Avalonia";
    }
}
```

**What Happens Under the Hood**:
1. `System.Windows.Window` → `WpfCompat.Window` → `Avalonia.Controls.Window`
2. `DependencyProperty.Register` → Creates `StyledProperty<string>` in Avalonia
3. `GetValue`/`SetValue` → Bridges to Avalonia's property system
4. `InitializeComponent()` → Avalonia XAML loader with WPF namespaces

---

## Conclusion

This implementation plan provides a comprehensive roadmap for creating a WPF Compatibility Layer on Avalonia, enabling WPF applications to run with minimal code changes on Avalonia's cross-platform windowing and rendering infrastructure.

**Key Takeaways**:
1. **Feasibility**: 80-85% of mainstream WPF apps can run with this approach
2. **Effort**: ~500-600 hours over 20-25 weeks
3. **Symbiosis**: Bidirectional benefits with WpfToAvalonia migration tool
4. **Strategy**: Source linking + shims + runtime bridging
5. **Value**: Cross-platform WPF + validation for transformations

**Next Steps**:
1. Review and approve plan
2. Set up Phase 1 infrastructure
3. Begin property system implementation
4. Iterate with continuous testing

---

**Document Version**: 1.0
**Last Updated**: 2025-10-23
**Status**: Planning Complete, Ready for Implementation
