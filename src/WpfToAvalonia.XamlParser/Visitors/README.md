# Unified XAML Visitor Pattern

This directory contains the unified visitor pattern implementation for traversing and transforming the WpfToAvalonia Unified AST.

## Overview

The visitor pattern is fully unified and provides multiple specialized base classes for different use cases:

```
IUnifiedXamlVisitor (void return)
    ↓
UnifiedXamlVisitorBase
    ├── TypeResolutionEnricher
    ├── ResourceResolutionEnricher
    └── BindingAnalysisEnricher

IUnifiedXamlVisitor<T> (generic return)
    ↓
UnifiedXamlCollectorVisitor<T>
    ├── DiagnosticCollectorVisitor
    └── NamedElementCollectorVisitor

IUnifiedXamlTransformVisitor (transformation return)
    ↓
UnifiedXamlTransformVisitorBase
    └── (Used by transformation rules)
```

## Base Classes

### 1. UnifiedXamlVisitorBase

**Use Case**: Traversing the AST without returning values (analysis, enrichment, validation)

**Features**:
- Depth-first traversal by default
- Control over property/child visiting with flags
- Virtual methods for all node types

**Example**:
```csharp
public class MyAnalyzer : UnifiedXamlVisitorBase
{
    public override void VisitElement(UnifiedXamlElement element)
    {
        // Analyze element
        Console.WriteLine($"Found element: {element.TypeReference?.LocalName}");

        // Continue traversal
        base.VisitElement(element);
    }
}

// Usage
var analyzer = new MyAnalyzer();
analyzer.VisitDocument(document);
```

### 2. UnifiedXamlCollectorVisitor<T>

**Use Case**: Collecting information from the AST (diagnostics, named elements, statistics)

**Features**:
- Built-in `Results` list for accumulation
- Automatic result clearing on document visit
- Returns collected results

**Example**:
```csharp
public class NamedElementCollector : UnifiedXamlCollectorVisitor<UnifiedXamlElement>
{
    public override List<UnifiedXamlElement> VisitElement(UnifiedXamlElement element)
    {
        // Collect elements with x:Name
        if (!string.IsNullOrEmpty(element.XName))
        {
            Results.Add(element);
        }

        // Continue traversal
        return base.VisitElement(element);
    }
}

// Usage
var collector = new NamedElementCollector();
var namedElements = collector.VisitDocument(document);
Console.WriteLine($"Found {namedElements.Count} named elements");
```

### 3. UnifiedXamlTransformVisitorBase

**Use Case**: Transforming the AST (creating new/modified nodes)

**Features**:
- Returns transformed nodes
- Supports null returns for node removal
- Automatic tree reconstruction

**Example**:
```csharp
public class MyTransformer : UnifiedXamlTransformVisitorBase
{
    public override UnifiedXamlElement? TransformElement(UnifiedXamlElement element)
    {
        // Transform WPF Button to Avalonia Button
        if (element.TypeReference?.Matches("Button") == true)
        {
            var transformed = CloneElement(element);
            transformed.TypeReference = QualifiedTypeName.ForAvaloniaType("Button");
            return transformed;
        }

        // Default: no transformation
        return base.TransformElement(element);
    }
}
```

## Visitor Control Flags

All visitor base classes support control flags:

```csharp
public class MyVisitor : UnifiedXamlVisitorBase
{
    public MyVisitor()
    {
        VisitChildren = true;   // Visit child elements (default: true)
        VisitProperties = true;  // Visit properties (default: true)
    }
}
```

**Use Case**: Skip property/child traversal for performance or specific analysis needs.

## Best Practices

### 1. Always Call Base Implementation

Unless you're completely replacing traversal logic, always call `base.VisitXxx()`:

```csharp
public override void VisitElement(UnifiedXamlElement element)
{
    // Your logic
    DoSomething(element);

    // IMPORTANT: Continue traversal
    base.VisitElement(element);
}
```

### 2. Use Type-Safe Properties

Prefer new type-safe properties over obsolete ones:

```csharp
// ✅ GOOD: Type-safe
if (element.TypeReference?.Matches("Button") == true)

// ❌ BAD: String-based (obsolete)
if (element.TypeName == "Button")
```

### 3. Handle Null Values

Check for null when accessing optional properties:

```csharp
public override void VisitElement(UnifiedXamlElement element)
{
    // Check before using ElementType
    if (element.ElementType != null)
    {
        Console.WriteLine($"Type: {element.ElementType.FullName}");
    }

    // Or use ElementTypeOrThrow if type is required
    try
    {
        var type = element.ElementTypeOrThrow;  // Throws if not resolved
        Console.WriteLine($"Type: {type.FullName}");
    }
    catch (InvalidOperationException)
    {
        // Handle unresolved type
    }

    base.VisitElement(element);
}
```

### 4. Use PropertyValue and MarkupExtensionParameter

Access property values in a type-safe way:

```csharp
public override void VisitProperty(UnifiedXamlProperty property)
{
    // ✅ GOOD: Type-safe discriminated union
    if (property.ValueTyped != null)
    {
        property.ValueTyped.Switch(
            onString: value => Console.WriteLine($"String value: {value}"),
            onElement: element => Console.WriteLine($"Element: {element.TypeReference?.LocalName}"),
            onMarkupExtension: ext => Console.WriteLine($"Extension: {ext.ExtensionType}")
        );
    }

    // ❌ BAD: Unsafe casting (obsolete)
    if (property.Value is string str)
    {
        // Works but loses type safety
    }

    base.VisitProperty(property);
}
```

## Common Patterns

### Pattern 1: Collecting Specific Elements

```csharp
public class ButtonCollector : UnifiedXamlCollectorVisitor<UnifiedXamlElement>
{
    public override List<UnifiedXamlElement> VisitElement(UnifiedXamlElement element)
    {
        if (element.TypeReference?.Matches("Button") == true)
        {
            Results.Add(element);
        }

        return base.VisitElement(element);
    }
}
```

### Pattern 2: Validation

```csharp
public class DeprecatedPropertyValidator : UnifiedXamlVisitorBase
{
    private readonly List<string> _deprecatedProperties = new() { "Foo", "Bar" };

    public override void VisitProperty(UnifiedXamlProperty property)
    {
        if (_deprecatedProperties.Contains(property.PropertyName))
        {
            property.AddDiagnostic(
                "DEPRECATED_PROPERTY",
                $"Property '{property.PropertyName}' is deprecated",
                DiagnosticSeverity.Warning
            );
        }

        base.VisitProperty(property);
    }
}
```

### Pattern 3: Statistics Collection

```csharp
public class AstStatistics : UnifiedXamlVisitorBase
{
    public int ElementCount { get; private set; }
    public int PropertyCount { get; private set; }
    public int MarkupExtensionCount { get; private set; }

    public override void VisitElement(UnifiedXamlElement element)
    {
        ElementCount++;
        base.VisitElement(element);
    }

    public override void VisitProperty(UnifiedXamlProperty property)
    {
        PropertyCount++;
        base.VisitProperty(property);
    }

    public override void VisitMarkupExtension(UnifiedXamlMarkupExtension extension)
    {
        MarkupExtensionCount++;
        base.VisitMarkupExtension(extension);
    }
}
```

### Pattern 4: Conditional Traversal

```csharp
public class ConditionalVisitor : UnifiedXamlVisitorBase
{
    public override void VisitElement(UnifiedXamlElement element)
    {
        // Only visit Button elements and their children
        if (element.TypeReference?.Matches("Button") == true)
        {
            base.VisitElement(element);  // Traverse children
        }
        else
        {
            // Skip this element and its children
        }
    }
}
```

## Integration with Enrichment Pipeline

Enrichers implement `IEnricher` and typically extend `UnifiedXamlVisitorBase`:

```csharp
public sealed class MyEnricher : UnifiedXamlVisitorBase, IEnricher
{
    public void Enrich(UnifiedXamlDocument document)
    {
        // Entry point from enrichment pipeline
        VisitDocument(document);
    }

    public override void VisitElement(UnifiedXamlElement element)
    {
        // Enrichment logic
        EnrichElement(element);

        // Continue traversal
        base.VisitElement(element);
    }
}
```

## Performance Considerations

1. **Skip Unnecessary Traversal**: Use `VisitChildren = false` or `VisitProperties = false` if not needed
2. **Early Return**: Return early from visit methods when possible
3. **Avoid Repeated Lookups**: Cache frequently accessed properties
4. **Use Appropriate Visitor Type**: Don't use transform visitor if you only need to read

## Migration from Old Patterns

If you have code using old visitor patterns:

```csharp
// OLD: Manual traversal
foreach (var element in document.Root.Descendants())
{
    if (element.TypeName == "Button")  // String-based
    {
        // Process
    }
}

// NEW: Visitor pattern with type safety
public class ButtonProcessor : UnifiedXamlVisitorBase
{
    public override void VisitElement(UnifiedXamlElement element)
    {
        if (element.TypeReference?.Matches("Button") == true)  // Type-safe
        {
            // Process
        }
        base.VisitElement(element);
    }
}

var processor = new ButtonProcessor();
processor.VisitDocument(document);
```

## Summary

The unified visitor pattern provides:

✅ **Consistent Traversal**: All visitors use the same traversal logic
✅ **Type Safety**: Integration with discriminated unions and typed properties
✅ **Flexibility**: Multiple base classes for different use cases
✅ **Performance**: Control flags for skipping unnecessary traversal
✅ **Maintainability**: Single place to update traversal logic

For questions or issues, see the main project documentation.
