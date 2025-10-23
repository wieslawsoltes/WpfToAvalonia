# WPF to Avalonia Compatibility Layer - Architecture Analysis

## Executive Summary

This document provides a thorough architectural analysis of both WPF and Avalonia frameworks, identifying key compatibility points and differences for creating a comprehensive WPF compatibility layer. Both frameworks share fundamental design patterns but diverge in implementation details, property systems, rendering, and platform abstraction.

---

## 1. PROPERTY SYSTEMS - CORE FOUNDATION

### 1.1 WPF DependencyProperty System

**Location**: `extern/wpf/src/Microsoft.DotNet.Wpf/src/WindowsBase/System/Windows/`

**Architecture Overview**:
- **Base Classes**: 
  - `DependencyProperty` (sealed): Defines property metadata, registration, and validation
  - `DependencyObject`: Base class for all objects using dependency properties
  - `PropertyMetadata`: Contains default values, change callbacks, and validation
  
**Key Characteristics**:
```
DependencyProperty.Register(
    name: string,                          // Property name
    propertyType: Type,                    // Property type (e.g., string, int)
    ownerType: Type,                       // Owning class (e.g., Button)
    typeMetadata: PropertyMetadata,        // Default values & callbacks
    validateValueCallback: Delegate        // Optional validation
)
```

**Property Metadata Features**:
- **DefaultValueWasSet()**: Explicit tracking of default value initialization
- **PropertyChangedCallback**: Called when property value changes
- **CoerceValueCallback**: Validates/adjusts values before setting
- **FrameworkPropertyMetadata**: Extended metadata for layout properties

**Inheritance and Attachment**:
- **Attached Properties**: Can be set on any DependencyObject regardless of type
- **Property Inheritance**: Some properties inherit down the visual tree (e.g., FontSize)
- **Freezable Pattern**: Immutable objects with change notification

**Property Access**:
- **GetValue(DependencyProperty)**: Retrieves effective property value
- **SetValue(DependencyProperty, object)**: Sets local property value
- **ClearValue(DependencyProperty)**: Clears local value, restores inherited/default
- **Value Resolution**: Local > Style > Default > Inherited

---

### 1.2 Avalonia AvaloniaProperty System

**Location**: `extern/Avalonia/src/Avalonia.Base/`

**Architecture Overview**:
- **Base Classes**:
  - `AvaloniaProperty` (abstract): Base for all Avalonia properties
  - `AvaloniaProperty<T>` (generic): Type-safe property wrapper
  - `StyledProperty<T>`: Supports styling, inheritance, animations
  - `DirectProperty<T>`: Direct backing field access (lighter-weight)
  - `AttachedProperty<T>`: Attached properties without styling
  - `AvaloniaObject`: Base class for property support

**Two-Tier Property System**:

1. **StyledProperty** (Heavy-weight):
   - Supports styling, binding, inheritance
   - Priority system for value resolution
   - Change notification via observables
   - Metadata overrides per type
   
2. **DirectProperty** (Light-weight):
   - Direct CLR property with change notification
   - Lower memory overhead
   - Better performance for simple properties
   - Uses delegates for get/set access

**Registration Syntax**:
```csharp
// StyledProperty - for styled/bindable properties
public static readonly StyledProperty<string> TextProperty =
    AvaloniaProperty.Register<MyControl, string>(
        nameof(Text),
        defaultValue: "",
        inherits: true);  // Optional inheritance

// DirectProperty - for computed/simple properties
public static readonly DirectProperty<MyControl, int> WidthProperty =
    AvaloniaProperty.RegisterDirect<MyControl, int>(
        nameof(Width),
        o => o.Width,              // Getter
        (o, v) => o.Width = v);    // Setter (optional for read-only)

// AttachedProperty - for attached behavior
public static readonly AttachedProperty<double> MarginProperty =
    AvaloniaProperty.RegisterAttached<MyControl, Visual, double>(
        nameof(Margin),
        inherits: false);
```

**Priority System** (vs WPF's cascading):
```
Local > Binding > DataContext > Style > Default > Inherited
```

**Property Metadata**:
```csharp
public class AvaloniaPropertyMetadata
{
    public object? DefaultValue { get; }
    public Action<AvaloniaObject, bool>? Notifying { get; }  // Before/after notification
    public bool Inherits { get; }
    public List<(Type, AvaloniaPropertyMetadata)> Metadata { get; }  // Per-type overrides
}
```

---

### 1.3 Compatibility Analysis: Property System

**Key Differences**:

| Aspect | WPF | Avalonia | Compatibility |
|--------|-----|---------|---|
| Property Base | Single `DependencyProperty` | `StyledProperty` / `DirectProperty` duality | Need transformation logic |
| Change Callbacks | PropertyChangedCallback in metadata | Observable/reactive pattern | Wrap WPF callbacks in observables |
| Property Inheritance | Automatic for marked properties | Opt-in via `inherits: true` | Add metadata flag |
| Attached Properties | Full support with styling | `AttachedProperty<T>` separate type | Direct mapping possible |
| Validation | `ValidateValueCallback` + `CoerceValueCallback` | Validation in observable chain | Wrap callbacks |
| Freezable Pattern | Built-in for immutable objects | Not core framework | Custom implementation needed |
| Read-Only Properties | `DependencyPropertyKey` pattern | Separate registration method | Transformation rule |
| Value Priority | Local > Animation > Style > Default | Local > Binding > Style > Default | Re-prioritize values |

**Mapping Strategy**:
1. **StyledProperty**: For properties that support styling, binding, and inheritance
2. **DirectProperty**: For read-only computed properties or high-performance properties
3. **Metadata Transformation**: Convert PropertyMetadata callbacks to observable chain
4. **Value Priority**: Maintain equivalent resolution order (may require shim layer)

---

## 2. VISUAL TREE AND CONTROL HIERARCHY

### 2.1 WPF Visual Tree Architecture

**Location**: `extern/wpf/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/`

**Hierarchy**:
```
DependencyObject (Property support)
  ↓
DispatcherObject (Thread affinity)
  ↓
Visual (Rendering, transforms, clipping)
  ↓
UIElement (Layout, events, input)
  ↓
FrameworkElement (XAML, styles, data binding)
  ↓
Control (Templating, resources)
  ↓
ContentControl (Single content container)
  Button, Label, GroupBox, etc.
```

**Key Visual Tree Concepts**:
- **Visual Tree**: All visuals (renders to screen)
- **Logical Tree**: Templated structure visible to code
- **Two-Tree System**: Separates rendering from structure
- **Visual3D Support**: Full 3D element support
- **Custom Rendering**: DrawingVisual for custom content

**UIElement Features**:
- Measure/Arrange layout (two-pass system)
- Event routing (Bubble, Tunnel, Direct)
- Input handling (mouse, keyboard, touch)
- Hit testing support
- Opacity and transforms
- Clipping and effects

**FrameworkElement Features**:
- Styles and templates
- Data binding and context
- Resource dictionaries
- Margin and alignment
- Named elements and namescopes

---

### 2.2 Avalonia Visual Tree Architecture

**Location**: `extern/Avalonia/src/Avalonia.Base/`

**Hierarchy**:
```
AvaloniaObject (Property support)
  ↓
StyledElement (Styling support)
  ↓
Visual (Rendering properties)
  ├─ Bounds, Opacity, RenderTransform
  ├─ Clip, Effect, Transform
  ├─ Visual parent/child relationships
  ↓
Layoutable (Layout calculation)
  ├─ Measure/Arrange two-pass system
  ├─ Width, Height, Margin, Alignment
  ├─ Min/Max constraints
  ↓
Control (User interaction)
  ├─ Focus management
  ├─ Template support
  ├─ Input handling
  ↓
ContentControl
  Button, TextBlock, Border, etc.
```

**Key Visual Tree Concepts**:
- **Unified Visual Tree**: Single tree for rendering and logic
- **Visual Parent**: Direct parent relationship
- **Logical Children**: Template and content children
- **No 3D Support**: 2D-only framework
- **Composition Model**: GPU-accelerated rendering pipeline

**Layoutable Features** (vs UIElement):
- **Measure(Size)**: Calculate desired size
- **Arrange(Rect)**: Position element
- **DesiredSize**: Calculated from Measure
- **Bounds**: Actual rendered bounds
- **EffectiveViewport**: Viewport within parent container
- **HorizontalAlignment/VerticalAlignment**: Alignment within parent

**Visual Features**:
- **Bounds**: Calculated render bounds
- **ClipToBounds**: Automatic clipping
- **Clip**: Geometry-based clipping
- **RenderTransform/RenderTransformOrigin**: Visual transformations
- **Opacity/OpacityMask**: Transparency effects
- **Effect**: Post-processing effects
- **ZIndex**: Depth ordering
- **IsVisible**: Visibility flag

---

### 2.3 Compatibility Analysis: Visual Tree

**Key Differences**:

| Aspect | WPF | Avalonia | Compatibility |
|--------|-----|---------|---|
| Tree Structure | Visual + Logical dual tree | Unified visual tree | Logical tree via templates |
| 3D Support | Full Visual3D hierarchy | 2D only | Exclude 3D transformations |
| UIElement Base | Combines layout + events | Split: Visual + Layoutable | Map to Layoutable |
| Measure/Arrange | `MeasureOverride`/`ArrangeOverride` virtual | Virtual methods on Layoutable | Direct mapping |
| Transform Support | `RenderTransform` + `LayoutTransform` | `RenderTransform` only | Remove LayoutTransform |
| Clipping | `ClipToBounds` + custom `Clip` | `ClipToBounds` + `Clip` geometry | Direct mapping |
| Custom Drawing | `DrawingVisual` + `OnRender` | Custom render overrides | Implement via overrides |
| Visual Finding | `VisualTreeHelper` static methods | Extension methods in `VisualExtensions` | Create adapter layer |
| Hit Testing | `VisualTreeHelper.HitTest()` | Visual hit test callbacks | Implement via interface |
| Layout Rounding | `UseLayoutRounding` property | Same property on `Layoutable` | Direct mapping |
| DesiredSize | `MeasureCore` returns | Automatic from `Measure()` | Automatic calculation |

**Mapping Strategy**:
1. **UIElement → Layoutable**: Map layout-related functionality
2. **Visual → Visual**: Direct mapping for rendering properties
3. **FrameworkElement → Control**: Map to Control class
4. **Visual3D**: Skip or mark as unsupported
5. **Logical Tree**: Implement via control templates
6. **Layout overrides**: Use `MeasureOverride`/`ArrangeOverride` pattern
7. **Custom rendering**: Implement custom visual classes

---

## 3. LAYOUT SYSTEM

### 3.1 WPF Layout Architecture

**Location**: `extern/wpf/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/`

**Two-Pass Layout System**:

1. **Measure Pass**: Determine desired size
   ```csharp
   Size MeasureCore(Size availableSize)
   {
       // Calculate minimum/desired size based on content
       // Apply MinWidth, MaxWidth, Height constraints
       // Return desired size
   }
   ```

2. **Arrange Pass**: Position element
   ```csharp
   Rect ArrangeCore(Rect finalRect)
   {
       // Position element within provided rectangle
       // Apply alignment (HorizontalAlignment, VerticalAlignment)
       // Apply margins
       // Return actual bounds
   }
   ```

**Layout Properties** (FrameworkElement):
- `Width`, `Height`: Explicit sizes (can be NaN for auto)
- `MinWidth`, `MaxWidth`, `MinHeight`, `MaxHeight`: Constraints
- `Margin`: External spacing
- `HorizontalAlignment`, `VerticalAlignment`: Alignment within parent
- `UseLayoutRounding`: Pixel-perfect rendering

**Layout Panels**:
- `Panel` (abstract): Base for layout containers
- `Canvas`: Absolute positioning
- `StackPanel`: Linear stacking (horizontal/vertical)
- `Grid`: Table-based layout
- `WrapPanel`: Wrapping layout
- `DockPanel`: Docking layout
- `UniformGrid`: Equal-sized grid cells

**Layout Invalidation**:
- **InvalidateMeasure()**: Force re-measure
- **InvalidateArrange()**: Force re-arrange
- **UpdateLayout()**: Force layout calculation

---

### 3.2 Avalonia Layout Architecture

**Location**: `extern/Avalonia/src/Avalonia.Base/Layout/`

**Two-Pass Layout System** (Similar to WPF):

1. **Measure Pass**: `Measure(Size availableSize)`
   ```csharp
   public Size Measure(Size availableSize)
   {
       // Calculate desired size
       // Returns DesiredSize property
   }
   ```

2. **Arrange Pass**: `Arrange(Rect rect)`
   ```csharp
   public Size Arrange(Rect rect)
   {
       // Position and size element
       // Returns actual size used
   }
   ```

**Layout Properties** (Layoutable):
- `Width`, `Height`: Double (NaN for auto)
- `MinWidth`, `MaxWidth`, `MinHeight`, `MaxHeight`: Constraints
- `Margin`: `Thickness` struct
- `HorizontalAlignment`, `VerticalAlignment`: Same enum as WPF
- `DesiredSize`: Read-only calculated size
- `Bounds`: Read-only actual bounds (includes transforms)
- `UseLayoutRounding`: Same as WPF

**Layout Panels**:
- `Panel` (abstract): Base for layout containers
- `Canvas`: Absolute positioning
- `StackPanel`: Linear stacking
- `Grid`: Table-based layout
- `WrapPanel`: Wrapping layout
- `DockPanel`: Docking layout
- `UniformGrid`: Equal-sized cells
- `RelativePanel**: Relative positioning

**Layout System** (`LayoutManager`):
- Queues layout changes
- Executes measure/arrange passes
- Handles layout cycles
- Supports embedded layout roots

**Key Differences**:
- No `LayoutTransform` (only `RenderTransform`)
- `EffectiveViewport` property for viewport-relative layout
- Automatic invalidation tracking
- Layout rounding integrated
- `ILayoutRoot` interface for custom layout roots

---

### 3.3 Compatibility Analysis: Layout System

**Key Similarities**:
- Both use two-pass layout (Measure/Arrange)
- Same property names (Width, Height, Margin, Alignment)
- Same constraint model (Min/Max)
- Same layout panels
- Layout rounding support

**Differences**:
- Avalonia: `Bounds` is read-only, includes transforms
- WPF: `RenderSize` is actual arranged size
- Avalonia: No `LayoutTransform`
- WPF: Separate layout and render transforms
- Avalonia: `EffectiveViewport` for scrolling/clipping
- Avalonia: Automatic measure/arrange on property changes

**Mapping Strategy**:
1. **Direct Mapping**: Width, Height, Margin, Alignment, Min/Max constraints
2. **MeasureOverride/ArrangeOverride**: Direct virtual method mapping
3. **Remove LayoutTransform**: Use only RenderTransform
4. **Measure/Arrange**: Transform method signatures if needed
5. **InvalidateMeasure/Arrange**: Same method names available
6. **UseLayoutRounding**: Direct property mapping
7. **Layout Panels**: Direct type mapping

---

## 4. EVENT ROUTING SYSTEMS

### 4.1 WPF RoutedEvent System

**Location**: `extern/wpf/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/`

**Architecture**:
- **RoutedEvent**: Event identifier with metadata
- **RoutedEventArgs**: Base event arguments with routing information
- **RoutingStrategy**: Bubble, Tunnel, or Direct
- **EventRoute**: Path through visual tree

**Routing Strategies**:

1. **Bubble**: Event rises from source to root
   ```
   Button (source) → Panel → Window (root)
   Handlers: Button → Panel → Window
   ```

2. **Tunnel**: Event tunnels from root to source (Preview prefix)
   ```
   Window (root) → Panel → Button (source)
   Handlers: Window → Panel → Button
   ```

3. **Direct**: Event doesn't route (traditional event)
   ```
   Button (source only)
   Handlers: Button only
   ```

**Event Registration**:
```csharp
public static readonly RoutedEvent ClickEvent = 
    EventManager.RegisterRoutedEvent(
        name: "Click",
        routingStrategy: RoutingStrategy.Bubble,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(Button));
```

**Event Handling**:
```csharp
// Add handler
button.AddHandler(Button.ClickEvent, handler);
button.Click += handler;  // Also supports CLR events

// Raise event
RaiseEvent(new RoutedEventArgs(ClickEvent, this));

// Mark as handled
e.Handled = true;  // Stops routing
```

**Key Features**:
- **Attached Event Handlers**: Can add handlers to elements in code-behind or XAML
- **Class Handlers**: Register at class level for all instances
- **Handled Events**: Can specify `handledEventsToo=true` to receive handled events
- **Event Owner Addition**: `RoutedEvent.AddOwner()` to add handling to other types

---

### 4.2 Avalonia RoutedEvent System

**Location**: `extern/Avalonia/src/Avalonia.Base/Interactivity/`

**Architecture**:
- **RoutedEvent<T>**: Generic event with event args type
- **RoutedEventArgs**: Base for routed event arguments
- **RoutingStrategies**: Flags enum (Direct, Tunnel, Bubble combinations)
- **EventRoute**: Path through visual tree
- **Observable-based**: Uses Rx.NET internally

**Routing Strategies** (Flags):
```csharp
[Flags]
public enum RoutingStrategies
{
    Direct = 0x01,
    Tunnel = 0x02,
    Bubble = 0x04,
}

// Can combine: Tunnel | Bubble
```

**Event Registration**:
```csharp
public static readonly RoutedEvent<TappedEventArgs> TappedEvent =
    RoutedEvent.Register<MyControl, TappedEventArgs>(
        name: "Tapped",
        routingStrategy: RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
        ownerType: typeof(MyControl));
```

**Event Handling**:
```csharp
// Add handler (via Interactive class)
Interactive.AddHandler(control, Button.ClickEvent, (s, e) => { });

// Raise event (via Interactive class)
control.RaiseEvent(new RoutedEventArgs(ClickEvent) { Route = RoutingStrategies.Bubble });

// Mark as handled
e.Handled = true;

// Observable-based subscription
ClickEvent.Raised.Subscribe(args => { });
```

**Key Features**:
- **Reactive Pattern**: Events are observables
- **LightweightSubject**: Efficient event broadcasting
- **Class Handlers**: Via `RoutedEvent<T>.AddClassHandler()`
- **Observable Integration**: Full Rx.NET support
- **Route Parameter**: Specifies routing direction

**Major Differences**:
- Avalonia uses observables internally (reactive pattern)
- No separate CLR event wrapper (uses Interactive methods)
- Combines tunnel and bubble in single event
- Observable subscription pattern

---

### 4.3 Compatibility Analysis: Event Routing

**Key Differences**:

| Aspect | WPF | Avalonia | Compatibility |
|--------|-----|---------|---|
| Event Identifier | `RoutedEvent` class | `RoutedEvent<T>` generic | Implement both patterns |
| Strategy | Separate bubbling/tunneling | Combined in flags | Map strategies bidirectionally |
| Handler Registration | `AddHandler()` / `+=` operator | `Interactive.AddHandler()` | Create wrapper methods |
| Event Raising | `RaiseEvent()` on object | `Interactive.RaiseEvent()` | Wrapper pattern |
| Class Handlers | `EventManager.RegisterClassHandler()` | `RoutedEvent.AddClassHandler()` | Map registration |
| Observable Support | Built-in events | First-class via observables | Expose both APIs |
| Handled Events | `Handled` flag on args | Same `Handled` flag | Direct mapping |
| Preview Events | Separate tunnel events | Tunnel in same event | Transform naming |
| Owner Type Addition | `AddOwner()` method | Registry-based | Implement registry |
| Weak Event Pattern | `WeakEventManager` | Observable unsubscribe | Use IDisposable |

**Mapping Strategy**:
1. **RoutedEvent → RoutedEvent<T>**: Create generic wrapper
2. **Strategy Mapping**: Transform routing strategies bidirectionally
3. **Handler Methods**: Create `AddHandler()`/`RemoveHandler()` wrappers
4. **CLR Events**: Implement as wrapper over routed events
5. **Class Handlers**: Map to `AddClassHandler()` pattern
6. **Preview Events**: Transform naming (e.g., `PreviewMouseDown` → with Tunnel strategy)
7. **Observable Exposure**: Expose routed events as IObservable

---

## 5. DATA BINDING INFRASTRUCTURE

### 5.1 WPF Data Binding System

**Location**: `extern/wpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Data/`

**Architecture**:
- **Binding**: Markup extension describing binding
- **BindingExpression**: Runtime evaluation of binding
- **ValueConverter**: Type conversion between source and target
- **ValidationRule**: Value validation
- **Binding Modes**: OneWay, TwoWay, OneWayToSource, OneTime

**Binding Declaration**:
```xml
<TextBlock Text="{Binding UserName, Mode=TwoWay, StringFormat='Hello {0}'}" />
<TextBox Text="{Binding Price, Converter={StaticResource CurrencyConverter}}" />
```

**Binding Modes**:
- **OneWay**: Source → Target (read-only)
- **TwoWay**: Source ↔ Target (bidirectional)
- **OneWayToSource**: Source ← Target (write-only)
- **OneTime**: Source → Target (once, then no updates)

**Value Converters**:
```csharp
public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Convert source value to target type
        return $"${(decimal)value:N2}";
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Convert target value back to source type
        return decimal.Parse(value.ToString()!);
    }
}
```

**Validation Rules**:
```csharp
public class RangeValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (int.TryParse(value?.ToString(), out var num) && num >= 0 && num <= 100)
            return ValidationResult.ValidResult;
        return new ValidationResult(false, "Must be 0-100");
    }
}
```

**Update Triggers**:
- `PropertyChanged`: On every property change
- `LostFocus`: When control loses focus
- `Explicit`: Only via `BindingExpression.UpdateSource()`

**Binding Paths**:
```
{Binding Path=Property}              // Simple property
{Binding Path=Parent.Name}           // Property navigation
{Binding Path=Items/0}               // Collection indexing
{Binding Path=Items[0]}              // Alternative indexing
{Binding /}                          // Current item (collections)
{Binding}                            // Data context itself
```

**Attached Binding**:
```xml
<Window DataContext="{Binding MyViewModel}">
    <TextBlock Text="{Binding FirstName}" />
</Window>
```

---

### 5.2 Avalonia Data Binding System

**Location**: `extern/Avalonia/src/Avalonia.Base/Data/`

**Architecture**:
- **Binding**: Markup extension describing binding
- **IBinding**: Interface for bindings
- **InstancedBinding**: Runtime binding instance
- **IValueConverter**: Type conversion
- **BindingOperations**: Helper methods
- **Binding Modes**: OneWay, TwoWay, OneWayToSource, Default

**Binding Declaration**:
```xml
<TextBlock Text="{Binding UserName, Mode=TwoWay}" />
<TextBox Text="{Binding Price, Converter={StaticResource CurrencyConverter}}" />
```

**Binding Interfaces**:
```csharp
public interface IBinding
{
    IObservable<object?> Initiate(
        AvaloniaObject target,
        AvaloniaProperty property,
        object? anchor = null);
}
```

**Value Converters** (Similar to WPF):
```csharp
public class CurrencyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is decimal d ? $"${d:N2}" : null;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var str = value?.ToString() ?? "";
        return decimal.TryParse(str, out var d) ? d : null;
    }
}
```

**Data Context**:
```xml
<Window DataContext="{Binding MyViewModel}">
    <TextBlock Text="{Binding FirstName}" />
</Window>
```

**Observable-Based Binding**:
```csharp
// Binding to observable property
public IObservable<string> UserName { get; }

// Binding to regular property (via reflection)
<TextBlock Text="{Binding UserName}" />
```

**Binding Priority**:
```
Local > Binding > DataContext > Style > Default > Inherited
```

**Key Differences**:
- Observable-first design
- No explicit validation rules (use IDataErrorInfo instead)
- No separate Update Triggers (always reactive)
- More flexible observable integration
- No OneWayToSource (emulated via TwoWay)

---

### 5.3 Compatibility Analysis: Data Binding

**Key Similarities**:
- Both support OneWay and TwoWay modes
- Both use IValueConverter interface
- Both support property navigation paths
- Both support binding to properties, collections, and objects
- Both support DataContext inheritance

**Differences**:

| Aspect | WPF | Avalonia | Compatibility |
|--------|-----|---------|---|
| Binding Interface | Binding markup extension | IBinding interface | Adapter pattern |
| Reactivity | Event-based + INotifyPropertyChanged | Observable-first (Rx.NET) | Wrap events in observables |
| Update Trigger | UpdateSourceTrigger enum | Always reactive | Remove trigger parameter |
| Validation | ValidationRule classes | IDataErrorInfo pattern | Transform to IDataErrorInfo |
| Binding Modes | 4 modes (OneWayToSource) | 3 modes (TwoWay for both-ways) | Map OneWayToSource to TwoWay |
| String Format | StringFormat parameter | Custom converter | Move to converter |
| Relative Source | RelativeSource.FindAncestor | Binding to parent | Create adapter |
| Element Binding | ElementName binding | Explicit element binding | Map to element binding |
| Async Binding | PriorityBinding, MultiBinding | Separate constructs | Custom implementation |
| Converter Culture | CultureInfo parameter | Same | Direct mapping |

**Mapping Strategy**:
1. **Binding Syntax**: Transform XAML binding expressions (UpdateSourceTrigger removal)
2. **ValueConverters**: Direct interface mapping (IValueConverter)
3. **Data Context**: Direct inheritance model
4. **Observable Integration**: Expose INPC properties as observables
5. **Validation**: Transform ValidationRule to IDataErrorInfo
6. **String Format**: Create implicit converters
7. **Relative Source**: Implement via binding extensions
8. **Update Triggers**: Remove from binding declaration

---

## 6. RENDERING PIPELINE AND COMPOSITOR

### 6.1 WPF Rendering Architecture

**Location**: `extern/wpf/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/`

**Rendering Stack**:
```
DrawingVisual / UIElement (Logical)
         ↓
    OnRender() override
         ↓
  DrawingContext
         ↓
   GDI+ / Direct3D
         ↓
    Display
```

**Key Components**:

1. **Visual Rendering**:
   - `OnRender(DrawingContext)`: Virtual method for custom drawing
   - `DrawingContext`: Provides drawing primitives (lines, shapes, text, images)
   - `DrawingVisual`: Non-layout-related visual for custom rendering
   - `RenderTargetBitmap`: Off-screen rendering

2. **Brushes and Pens**:
   - `SolidColorBrush`: Solid color
   - `LinearGradientBrush`: Linear gradient
   - `RadialGradientBrush`: Radial gradient
   - `ImageBrush`: Image filling
   - `VisualBrush`: Visual tree as brush
   - `Pen`: Stroke properties

3. **Geometries**:
   - `Geometry`: Abstract base
   - `RectangleGeometry`, `EllipseGeometry`: Simple shapes
   - `PathGeometry`: Complex paths
   - `StreamGeometry`: Optimized path creation
   - `CombinedGeometry`: Boolean operations

4. **Rendering Options**:
   - `RenderingTier`: Capability levels
   - Bitmap caching
   - Automatic rendering optimization
   - Software vs Hardware rendering

5. **Visual Tree Rendering**:
   - **Dirty Rectangle Tracking**: Only re-render changed areas
   - **Visual Caching**: Cache rendered visuals
   - **Effect Application**: Drop shadows, blur, etc.
   - **Composition**: WPF composition (software + hardware)

---

### 6.2 Avalonia Rendering Architecture

**Location**: `extern/Avalonia/src/Avalonia.Base/Rendering/`

**Rendering Stack**:
```
Visual / Control (Logical)
         ↓
    Render() method
         ↓
DrawingContext / SceneGraph
         ↓
Composition Engine
         ↓
Skia / Direct3D / OpenGL
         ↓
    Display
```

**Key Components**:

1. **Composition Model** (`Rendering/Composition/`):
   - **Server-side composition**: GPU-accelerated rendering
   - **Visual tree composition**: Build scene graph
   - **Render target**: Framebuffer management
   - **Compositor**: Orchestrates rendering pipeline

2. **Drawing primitives**:
   - `DrawingContext`: Graphics context (similar to WPF)
   - `IBrush`: Brush interface (SolidColorBrush, GradientBrush, etc.)
   - `Pen`: Stroke definition
   - `Geometry`: Path and shape definitions

3. **Render Pipeline**:
   - `IRenderer`: Abstract renderer interface
   - `ImmediateRenderer`: Immediate mode rendering
   - `RenderLoop`: Manages rendering frame timing
   - `IRenderTimer`: Frame timing control

4. **Rendering Backend**:
   - **Skia Backend**: Cross-platform (Android, Desktop)
   - **Direct3D Backend**: Windows-specific
   - **OpenGL Backend**: Linux, macOS, Web
   - **Software Rendering**: Fallback

5. **Performance Features**:
   - **GPU Acceleration**: Hardware-accelerated rendering
   - **Composition Batching**: Batch operations
   - **Dirty Region Tracking**: Only re-render changed areas
   - **Effect Support**: Built-in effects (blur, drop shadow, etc.)

**Key Differences**:
- Avalonia: Unified composition engine (GPU-first)
- WPF: Separate software + hardware paths
- Avalonia: Skia as primary backend (cross-platform)
- WPF: DirectX for modern rendering
- Avalonia: Server-side composition with GPU
- WPF: Mixed software/hardware composition

---

### 6.3 Compatibility Analysis: Rendering

**Key Similarities**:
- Both use DrawingContext for rendering primitives
- Both support brushes (solid, gradient, image)
- Both support geometry operations
- Both support effects and transformations
- Both track dirty regions for optimization

**Differences**:

| Aspect | WPF | Avalonia | Compatibility |
|--------|-----|---------|---|
| Default Backend | DirectX / GDI+ | Skia | Platform-specific |
| Cross-Platform | Windows-only | All platforms | Target-specific builds |
| Rendering Model | Mixed software/hardware | GPU-first | Rewrite for GPU |
| DrawingContext | Available in OnRender | Available in Render | Method naming |
| Visual Caching | Bitmap caching property | Composition caching | Reimplement caching |
| 3D Support | Full 3D transformations | 2D only | Remove 3D features |
| Effects | Limited built-in effects | Rich effect library | Use Avalonia effects |
| RenderTargetBitmap | Supported | Similar functionality | Map to equivalent |
| Rendering Options | RenderOptions class | Composition settings | Transform settings |
| Composition Engine | Software mixing | GPU mixing | Use composition API |
| Dirty Rectangle | WPF manages | Composition manages | Automatic |
| Batch Operations | Implicit | Explicit composition | Use composition API |

**Mapping Strategy**:
1. **OnRender → Render**: Rename method (same logic)
2. **DrawingContext**: Direct interface mapping
3. **Brushes**: Type-for-type mapping available
4. **Geometry**: Direct mapping with same interface
5. **Rendering Backend**: Choose Skia or Direct3D
6. **Effects**: Map WPF effects to Avalonia equivalents
7. **Caching**: Use composition-based caching
8. **Visual Tree Rendering**: Use composition engine
9. **Performance**: Leverage GPU acceleration
10. **3D Support**: Exclude or mark as unsupported

---

## 7. XAML LOADING INFRASTRUCTURE

### 7.1 WPF XAML System

**Location**: `extern/wpf/src/Microsoft.DotNet.Wpf/src/System.Xaml/` and `PresentationFramework/`

**XAML Processing Pipeline**:
```
XAML File
   ↓
XamlReader.Load()
   ↓
XamlXmlReader (XML parsing)
   ↓
XamlObjectWriter (Object creation)
   ↓
Type Resolution
   ↓
PropertyMetadata Lookup
   ↓
Value Creation
   ↓
Object Instance
```

**Key Components**:

1. **Type System**:
   - `XamlType`: Metadata about a XAML-enabled type
   - `XamlMember`: Metadata about properties/events
   - `TypeConverter`: String-to-type conversion
   - `MarkupExtension`: {Binding}, {StaticResource}, etc.

2. **Namespace Management**:
   ```xml
   xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
   xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
   xmlns:local="clr-namespace:MyApp"
   ```

3. **Type Resolution**:
   - CLR namespace mapping to XML namespace
   - Assembly scanning for types
   - Custom type resolver support

4. **Property Setting**:
   - Attached properties in XAML
   - Property element syntax
   - Type conversion
   - Markup extensions

5. **Event Wiring**:
   ```xml
   <Button Click="Button_Click" />
   ```
   Connects XAML event name to code-behind handler

6. **Compilation**:
   - **Loose XAML**: Loaded at runtime (XamlReader)
   - **Compiled XAML**: Compiled to code-behind (x:Class)
   - **Baml**: Binary XAML format (compiled)

---

### 7.2 Avalonia XAML System

**Location**: `extern/Avalonia/src/Avalonia.Base/Markup/` and `Avalonia.Controls/`

**XAML Processing Pipeline**:
```
XAML File
   ↓
AvaloniaXamlLoader
   ↓
XamlX Reader
   ↓
Type Resolver
   ↓
Property Setter
   ↓
Object Instance
```

**Key Components**:

1. **Loader** (`AvaloniaXamlLoader`):
   - `Load(Uri)`: Load from URI
   - `Load(Stream)`: Load from stream
   - `Load(string)`: Load from string
   - Async loading support

2. **Type System**:
   - Property/Event metadata
   - Custom type resolvers
   - Assembly scanning

3. **Namespace Management**:
   ```xml
   xmlns="https://github.com/avaloniaui"
   xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
   ```

4. **Markup Extensions**:
   - `{Binding}`: Data binding
   - `{StaticResource}`: Resource lookup
   - `{DynamicResource}`: Dynamic resources
   - Custom extensions

5. **Property Setting**:
   - Simple properties: `<TextBlock Text="Hello" />`
   - Attached properties: `<Grid Row="1" Column="2" />`
   - Content property: `<Button>Click Me</Button>`
   - Property elements: `<Button><Button.Content>...</Button.Content></Button>`

6. **Event Wiring**:
   ```xml
   <Button Click="Button_Click" />
   ```
   Connects to code-behind handler (when x:Class is used)

**Key Differences**:
- Avalonia doesn't compile XAML to IL (runtime loading only)
- Uses XamlX compiler for type information
- More flexible runtime type resolution
- No binary XAML format (like WPF's Baml)
- Direct string-to-XAML loading in many scenarios

---

### 7.3 Compatibility Analysis: XAML Loading

**Key Similarities**:
- Both use XAML XML format
- Both support attached properties
- Both support markup extensions
- Both support event wiring
- Both support namespaces and type resolution

**Differences**:

| Aspect | WPF | Avalonia | Compatibility |
|--------|-----|---------|---|
| Compilation | Pre-compiled to BAML/IL | Runtime loaded | Runtime transformation |
| x:Class | Generates code-behind partial | Manual partial class | Handle separately |
| Namespace URL | `schemas.microsoft.com/*` | `avaloniaui` | Transform in XAML |
| Type Resolver | Assembly scanning + cache | Dynamic resolution | Similar process |
| Markup Extensions | WPF-specific | Avalonia-specific | Map extensions |
| Default Namespace | PresentationFramework | Avalonia.Controls | Change XML namespace |
| Event Binding | Via x:Class code-behind | Via code-behind partial | Same mechanism |
| Implicit Styles | Implicit DataType key | Implicit datatype | Direct mapping |
| DynamicResource | Full dynamic change | Style/theme changes | More limited |
| Freezable Support | Freezable x:Freeze | No freezing support | Skip or implement |
| Type Conversion | StringValueSerializer | String conversion | Implement converters |
| Resource Dictionaries | Merged, inherited | Theme-based | Different mechanism |

**Mapping Strategy**:
1. **Namespace Transformation**: Replace WPF namespace with Avalonia namespace
2. **Type Mapping**: Map WPF types to Avalonia types
3. **Markup Extensions**: Transform WPF markup to Avalonia markup
4. **Event Wiring**: Keep same mechanism with code-behind
5. **Property Setting**: Direct attribute-to-property mapping
6. **Attached Properties**: Map to Avalonia attached properties
7. **Type Resolution**: Use Avalonia resolver
8. **Resources**: Map to Avalonia resource dictionaries
9. **Themes**: Transform to Avalonia theme system
10. **Validation**: Implement semantic validation

---

## 8. PLATFORM ABSTRACTION LAYER

### 8.1 WPF Platform Abstraction

**Windows-Only Architecture**:
- Direct Windows API integration
- HwndHost for native control hosting
- PresentationHost.exe for browser hosting
- Platform-specific rendering backends

**Key Abstractions**:
- `DispatcherObject`: Thread affinity management
- `Dispatcher`: Message queue on UI thread
- `CompositionTarget`: Timing and composition

---

### 8.2 Avalonia Platform Abstraction

**Location**: `extern/Avalonia/src/Avalonia.Base/Platform/`

**Cross-Platform Architecture**:

**Platform Layer**:
- `IPlatformHandle`: Handle to native window
- `IRuntimePlatform`: Platform-specific services
- `IAssetLoader`: Resource loading
- `ICursorImpl`: Cursor implementation
- `IPlatformSettings`: Platform capabilities

**Windowing** (`Avalonia.*/`):
- **Windows** (`Avalonia.Win32`): Windows-specific windowing
- **X11** (`Avalonia.X11`): Linux X11 support
- **macOS** (`Avalonia.Native`): macOS support
- **Web** (`Avalonia.Web`): Web/WASM support
- **Android** (`Avalonia.Android`): Android support
- **iOS** (`Avalonia.iOS`): iOS support

**Rendering Backends**:
- **Skia**: Primary cross-platform backend
- **Direct3D**: Windows-specific accelerated
- **OpenGL**: Cross-platform accelerated
- **Software**: Fallback rasterizer

**Key Platform Interfaces**:
```csharp
public interface IRenderingPlatform
{
    IRenderer Create(IRenderTarget target);
    IFramebuffer CreateBackbuffer(ILockedFramebuffer size);
}

public interface IPlatformRenderInterfaceRegion
{
    void Dispose();
}
```

---

### 8.3 Compatibility Analysis: Platform Abstraction

**Key Differences**:
- WPF: Windows-only (now cross-platform via .NET)
- Avalonia: Native cross-platform from inception
- WPF: CompositionTarget for frame timing
- Avalonia: RenderLoop and IRenderTimer
- WPF: Direct Windows API
- Avalonia: Abstract platform services

**Mapping Strategy**:
1. **Dispatcher**: Map to Avalonia's Dispatcher
2. **Threading**: Use Avalonia's thread model
3. **Native Integration**: Use platform-specific modules
4. **Rendering**: Target specific rendering backend
5. **Windows API**: Use platform abstraction layer
6. **Async Operations**: Map to Avalonia async patterns

---

## 9. STYLING AND THEMING SYSTEMS

### 9.1 WPF Styling

**Location**: `extern/wpf/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/`

**Style System**:
- **Style**: Collection of setters for properties
- **Trigger**: Conditional property changes
- **DataTrigger**: Binding-based triggers
- **EventTrigger**: Event-based actions
- **MultiTrigger**: Multiple conditions

**Key Features**:
```xml
<Style TargetType="Button">
    <Setter Property="Background" Value="Blue" />
    <Setter Property="FontSize" Value="14" />
    <Trigger Property="IsMouseOver" Value="True">
        <Setter Property="Background" Value="DarkBlue" />
    </Trigger>
    <DataTrigger Binding="{Binding IsEnabled}" Value="False">
        <Setter Property="Opacity" Value="0.5" />
    </DataTrigger>
</Style>
```

**Control Templates**:
```xml
<ControlTemplate TargetType="Button">
    <Border Background="{TemplateBinding Background}">
        <ContentPresenter />
    </Border>
</ControlTemplate>
```

**Implicit Styling**:
```xml
<Style TargetType="Button">
    <!-- Applied to all Buttons -->
</Style>
```

---

### 9.2 Avalonia Styling

**Location**: `extern/Avalonia/src/Avalonia.Base/Styling/`

**Selector-Based System**:
- **Selector**: CSS-like selectors
- **Style**: Rules for matched elements
- **Setter**: Property assignments
- **No Triggers**: Logic handled via selectors and resources

**Key Features**:
```xaml
<Style Selector="Button">
    <Setter Property="Background" Value="Blue" />
    <Setter Property="FontSize" Value="14" />
</Style>

<Style Selector="Button:pointerover">
    <Setter Property="Background" Value="DarkBlue" />
</Style>

<Style Selector="Button:disabled">
    <Setter Property="Opacity" Value="0.5" />
</Style>
```

**Control Templates**:
```xaml
<ControlTemplate TargetType="Button">
    <Border Background="{TemplateBinding Background}">
        <ContentPresenter />
    </Border>
</ControlTemplate>
```

**Pseudo-Classes**:
- `:pointerover`: Mouse over
- `:pressed`: Button pressed
- `:focus`: Element has focus
- `:disabled`: Element disabled
- `:flyout-open`: Flyout open
- Custom via `PseudoClass.Set()`

**Key Differences**:
- Avalonia: CSS-like selectors (no WPF triggers)
- WPF: Imperative trigger system
- Avalonia: Pseudo-classes for states
- WPF: Property triggers for conditions
- Avalonia: More CSS-like styling
- WPF: More XAML-like with triggers

---

## 10. COMPATIBILITY LAYER DESIGN RECOMMENDATIONS

### 10.1 Core Strategy

**Three-Layer Approach**:

1. **Syntax Transformation Layer**:
   - XAML namespace transformation
   - Type name mapping
   - Property binding syntax updates
   - Event handler signature preservation

2. **Semantic Transformation Layer**:
   - DependencyProperty → StyledProperty/DirectProperty
   - Visual tree restructuring (if needed)
   - Event routing adaptation
   - Data binding mechanism translation

3. **Runtime Compatibility Layer**:
   - Adapter classes for API compatibility
   - Shim layers for behavior matching
   - Performance optimization wrappers
   - Fallback implementations

### 10.2 Priority Mapping

**Phase 1 - Core Property System** (Essential):
- DependencyProperty → StyledProperty transformation
- PropertyMetadata translation
- GetValue/SetValue method mapping
- CLR property wrapper detection and transformation

**Phase 2 - Visual Tree** (Essential):
- UIElement → Layoutable mapping
- Control hierarchy preservation
- Layout property mapping
- Measure/Arrange method transformation

**Phase 3 - Event System** (Important):
- RoutedEvent → RoutedEvent<T> mapping
- Event routing strategy translation
- CLR event wrapper generation
- Event handler signature preservation

**Phase 4 - Data Binding** (Important):
- Binding syntax transformation
- ValueConverter mapping
- Data context preservation
- UpdateSourceTrigger removal

**Phase 5 - Layout System** (Important):
- Panel mapping (Canvas, StackPanel, Grid, etc.)
- Layout property transformation
- Arrange/Measure override pattern
- Custom layout implementation

**Phase 6 - Rendering** (Nice-to-have):
- DrawingContext → DrawingContext (same)
- Brush type mapping
- Geometry operations
- Effect translation

**Phase 7 - XAML Loading** (Essential):
- Namespace transformation
- Type resolution
- Markup extension mapping
- Event binding preservation

---

## 11. KNOWN CHALLENGES AND MITIGATION

### 11.1 Architectural Gaps

**Challenge 1: Dual Property Types in Avalonia**
- WPF has single DependencyProperty
- Avalonia has StyledProperty and DirectProperty
- **Mitigation**: Implement heuristic to choose appropriate type based on property characteristics

**Challenge 2: Event Routing Differences**
- WPF has separate tunnel/bubble strategies
- Avalonia combines in flags
- **Mitigation**: Create translation layer to map strategies bidirectionally

**Challenge 3: No Layout Transform in Avalonia**
- WPF supports LayoutTransform (layout-affecting)
- Avalonia only has RenderTransform
- **Mitigation**: Document limitation, detect and warn on usage

**Challenge 4: Avalonia's GPU-First Rendering**
- WPF has software + hardware composition
- Avalonia is GPU-optimized
- **Mitigation**: Design assuming GPU acceleration, provide fallbacks

**Challenge 5: Property Priority Differences**
- WPF: Local > Animation > Style > Default
- Avalonia: Local > Binding > DataContext > Style > Default
- **Mitigation**: Document priority order differences

### 11.2 Feature Gaps

| Feature | WPF | Avalonia | Mitigation |
|---------|-----|---------|---|
| 3D Support | Full | 2D only | Document as unsupported |
| Visual3D | Available | N/A | Skip transformation |
| LayoutTransform | Yes | Only RenderTransform | Warn on usage |
| Freezable Pattern | Built-in | Custom implementation | Implement if needed |
| WeakEvent Pattern | Standard | Observable-based | Use observable disposal |
| ValidationRule | Classes | IDataErrorInfo | Transform pattern |
| Async Binding | MultiBinding, PriorityBinding | Observable composition | Implement wrappers |
| Compiled XAML | BAML | Runtime-only | Accept runtime loading |
| x:Freeze | Supported | N/A | Skip directive |
| Markup Extension Binding | Available | Similar | Direct mapping |

---

## 12. ARCHITECTURE DIAGRAM

```
WPF Application
    ↓
[Syntax Transformation]
    ├─ XAML Namespace Update
    ├─ Type Name Mapping
    ├─ Property Syntax Update
    └─ Event Handler Preservation
    ↓
[Semantic Transformation]
    ├─ DependencyProperty → StyledProperty/DirectProperty
    ├─ Event Routing Strategy Translation
    ├─ Data Binding Mechanism
    ├─ Visual Tree Restructuring (if needed)
    └─ Layout System Transformation
    ↓
[Code Generation / Transformation]
    ├─ CLR Property Wrappers
    ├─ Event Handler Registration
    ├─ Binding Expression Evaluation
    └─ Adapter Class Generation
    ↓
[Runtime Compatibility Layer]
    ├─ Avalonia Property System
    ├─ Avalonia Event System
    ├─ Avalonia Binding Engine
    ├─ Avalonia Layout System
    └─ Avalonia Rendering Pipeline
    ↓
Avalonia Application
```

---

## 13. SUMMARY AND RECOMMENDATIONS

### 13.1 Feasibility Assessment

**High Feasibility**:
- Property system transformation (DependencyProperty → StyledProperty)
- Visual tree and control hierarchy mapping
- Layout system (Measure/Arrange pattern identical)
- XAML syntax transformation
- Basic event routing
- Data binding basic scenarios

**Medium Feasibility**:
- Complex event routing scenarios
- Advanced data binding (validation, multi-binding)
- Style/trigger transformation
- Custom rendering
- Performance optimization

**Low Feasibility**:
- 3D transformations (not in Avalonia)
- Layout transforms (not in Avalonia)
- Full freezable pattern emulation
- Exact performance parity
- Complex composition scenarios

### 13.2 Recommended Approach

1. **Start with Property System**: Foundation for everything
2. **Build Visual Hierarchy**: Essential for control mapping
3. **Implement Layout**: Straightforward mapping
4. **Add XAML Loading**: Runtime transformation pipeline
5. **Event System**: Necessary for interactivity
6. **Data Binding**: For MVVMscenarios
7. **Styling**: For theming support
8. **Rendering**: For custom visuals (if needed)

### 13.3 Tools and Techniques

**Recommended Tools**:
- **Roslyn**: For C# code transformation
- **XDocument**: For XAML parsing and transformation
- **Semantic Analysis**: For accurate type and method detection
- **Code Generation**: For creating compatibility adapters

**Techniques**:
- **Syntax Rewriting**: Transform C# code patterns
- **XML Transformation**: Update XAML namespace and elements
- **Reflection**: Detect WPF-specific patterns
- **IL Weaving**: Optional for runtime interception (if needed)

### 13.4 Success Metrics

1. **Coverage**: % of WPF features supported
2. **Correctness**: Functional equivalence after transformation
3. **Performance**: Relative to native Avalonia
4. **Developer Experience**: Ease of using compatibility layer
5. **Maintenance**: Long-term sustainability

---

## Conclusion

A WPF compatibility layer for Avalonia is **technically feasible** with strategic focus on high-priority areas. The architectural similarity between the two frameworks (property systems, visual trees, layout) provides a solid foundation for transformation, while key differences (dual properties, event strategies, 3D support) require careful design decisions. A phased approach starting with the property system, visual hierarchy, and layout will provide the most value with manageable complexity.

