# Technical Implementation Recommendations

## 1. Property System Transformation

### Current State
The `DependencyPropertyTransformer` already handles basic transformation, but needs enhancement for complex scenarios.

### Recommended Enhancements

#### 1.1 PropertyMetadata Callback Detection
**Current**: Basic metadata detection
**Recommended**: Enhanced pattern matching for callbacks

```csharp
private bool DetectPropertyChangedCallback(ExpressionSyntax metadata)
{
    var metadataStr = metadata.ToString();
    return metadataStr.Contains("PropertyChangedCallback") ||
           metadataStr.Contains("OnPropertyChanged") ||
           metadataStr.Contains("new PropertyMetadata(");
}

private bool DetectValidationCallback(ExpressionSyntax metadata)
{
    var metadataStr = metadata.ToString();
    return metadataStr.Contains("ValidateValueCallback") ||
           metadataStr.Contains("OnValidate");
}

private bool DetectCoerceCallback(ExpressionSyntax metadata)
{
    var metadataStr = metadata.ToString();
    return metadataStr.Contains("CoerceValueCallback") ||
           metadataStr.Contains("OnCoerce");
}
```

#### 1.2 Inheritance Flag Recognition
**Current**: No inheritance tracking
**Recommended**: Detect inheritance patterns

```csharp
private bool ShouldInherit(DependencyPropertyAnalysis analysis)
{
    // Common inheritable properties
    var inheritablePatterns = new[]
    {
        "FontSize", "FontFamily", "FontWeight",
        "Foreground", "Background",
        "TextAlignment", "Language",
        "Padding", "Margin"
    };
    
    if (inheritablePatterns.Any(p => analysis.PropertyName.Contains(p)))
        return true;
    
    // Check metadata for inheritance hints
    if (analysis.Metadata?.ToString().Contains("Inherited") == true)
        return true;
    
    return false;
}
```

#### 1.3 StyledProperty vs DirectProperty Heuristic
**Current**: Based on read-only and metadata complexity
**Recommended**: Multi-factor analysis

```csharp
private bool ShouldUseDirectProperty(DependencyPropertyAnalysis analysis)
{
    // Factor 1: Read-only properties should use DirectProperty
    if (analysis.IsReadOnly)
        return true;
    
    // Factor 2: High-cardinality properties (many controls)
    if (IsHighCardinalityProperty(analysis))
        return false; // Use StyledProperty for styling support
    
    // Factor 3: Properties with complex metadata
    if (HasComplexMetadata(analysis))
        return false; // Use StyledProperty
    
    // Factor 4: Computed properties with getters
    if (IsComputedProperty(analysis))
        return true; // DirectProperty for computed values
    
    // Factor 5: Attached properties almost always StyledProperty
    if (analysis.IsAttached && !analysis.IsReadOnly)
        return false;
    
    // Default: StyledProperty for safety
    return false;
}

private bool IsHighCardinalityProperty(DependencyPropertyAnalysis analysis)
{
    // Properties used on many control types should support styling
    var highCardinalityProps = new[] { "Background", "Foreground", "Padding", 
                                       "Margin", "BorderBrush", "BorderThickness" };
    return highCardinalityProps.Contains(analysis.PropertyName);
}

private bool HasComplexMetadata(DependencyPropertyAnalysis analysis)
{
    if (analysis.Metadata == null)
        return false;
    
    var metadataStr = analysis.Metadata.ToString();
    return metadataStr.Contains("CoerceValueCallback") ||
           metadataStr.Contains("ValidateValueCallback") ||
           metadataStr.Contains("PropertyChangedCallback");
}
```

### 1.4 Property Registration Transformation
**Recommendation**: Generate both property field AND CLR property wrapper

```csharp
private void GeneratePropertyAndWrapper(
    FieldDeclarationSyntax fieldDeclaration,
    DependencyPropertyAnalysis analysis)
{
    // Generate: public static readonly StyledProperty<T> PropertyProperty = ...
    var propertyFieldCode = GenerateStyledProperty(fieldDeclaration, analysis);
    
    // Generate: public T Property { get; set; }
    var clrPropertyCode = GenerateCLRProperty(analysis);
    
    // Return both
    yield return propertyFieldCode;
    yield return clrPropertyCode;
}

private PropertyDeclarationSyntax GenerateCLRProperty(DependencyPropertyAnalysis analysis)
{
    var getterCode = $"GetValue({analysis.FieldName})";
    var setterCode = $"SetValue({analysis.FieldName}, value)";
    
    // Cast if needed
    if (!analysis.PropertyType.Equals("object", StringComparison.OrdinalIgnoreCase))
    {
        getterCode = $"({analysis.PropertyType}){getterCode}";
    }
    
    return SyntaxFactory.PropertyDeclaration(
        SyntaxFactory.ParseTypeName(analysis.PropertyType),
        analysis.PropertyName)
        .WithAccessorList(
            SyntaxFactory.AccessorList(
                SyntaxFactory.List(new[]
                {
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                            SyntaxFactory.ParseExpression(getterCode)))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                            SyntaxFactory.ParseExpression(setterCode)))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                })));
}
```

---

## 2. Type Mapping System

### Recommended Architecture

```csharp
public interface ITypeMapper
{
    /// <summary>
    /// Maps a WPF type name to its Avalonia equivalent.
    /// </summary>
    TypeMappingResult Map(string wpfTypeName);
    
    /// <summary>
    /// Gets all mapped types.
    /// </summary>
    IEnumerable<(string Wpf, string Avalonia)> GetMappings();
    
    /// <summary>
    /// Registers a custom type mapping.
    /// </summary>
    void RegisterMapping(string wpfType, string avaloniaType);
}

public class TypeMappingResult
{
    public string AvaloniaType { get; set; }
    public MappingLevel Level { get; set; }  // Complete, Partial, Unavailable
    public List<string> MissingFeatures { get; set; }
    public string Replacement { get; set; }  // Alternative type if direct mapping unavailable
}

public enum MappingLevel
{
    Complete = 0,      // 1:1 mapping, fully compatible
    Partial = 1,       // Mapped but some features missing
    Unavailable = 2,   // No equivalent type
    Deprecated = 3     // Type should not be used
}
```

### Type Mapping Database

```csharp
private static readonly Dictionary<string, (string Avalonia, MappingLevel Level)> TypeMappings = 
    new()
    {
        // Core Framework
        { "System.Windows.DependencyObject", ("Avalonia.AvaloniaObject", MappingLevel.Complete) },
        { "System.Windows.UIElement", ("Avalonia.Layout.Layoutable", MappingLevel.Partial) },
        { "System.Windows.FrameworkElement", ("Avalonia.Controls.Control", MappingLevel.Partial) },
        { "System.Windows.Controls.Control", ("Avalonia.Controls.Control", MappingLevel.Complete) },
        
        // Visual Elements
        { "System.Windows.Controls.Button", ("Avalonia.Controls.Button", MappingLevel.Complete) },
        { "System.Windows.Controls.TextBlock", ("Avalonia.Controls.TextBlock", MappingLevel.Complete) },
        { "System.Windows.Controls.TextBox", ("Avalonia.Controls.TextBox", MappingLevel.Complete) },
        { "System.Windows.Controls.Panel", ("Avalonia.Controls.Panel", MappingLevel.Complete) },
        { "System.Windows.Controls.Canvas", ("Avalonia.Controls.Canvas", MappingLevel.Complete) },
        { "System.Windows.Controls.StackPanel", ("Avalonia.Controls.StackPanel", MappingLevel.Complete) },
        { "System.Windows.Controls.Grid", ("Avalonia.Controls.Grid", MappingLevel.Complete) },
        { "System.Windows.Controls.WrapPanel", ("Avalonia.Controls.WrapPanel", MappingLevel.Complete) },
        { "System.Windows.Controls.DockPanel", ("Avalonia.Controls.DockPanel", MappingLevel.Complete) },
        
        // Not Supported
        { "System.Windows.Media.Visual3D", ("", MappingLevel.Unavailable) },
        { "System.Windows.Controls.Viewport3D", ("", MappingLevel.Unavailable) },
    };
```

---

## 3. Event System Transformation

### Recommended Implementation

#### 3.1 RoutedEvent Registration Mapper

```csharp
public class RoutedEventTransformer : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        if (!IsRoutedEventField(node))
            return base.VisitFieldDeclaration(node);
        
        var analysis = AnalyzeRoutedEvent(node);
        if (analysis == null)
            return base.VisitFieldDeclaration(node);
        
        // Transform: DependencyProperty.Register... → RoutedEvent.Register...
        var newField = TransformRoutedEvent(node, analysis);
        
        _diagnostics.AddInfo(
            "ROUTED_EVENT_TRANSFORMED",
            $"Transformed RoutedEvent '{analysis.EventName}' with strategy {analysis.Strategy}",
            node.GetLocation().GetLineSpan().Path,
            node.GetLocation().GetLineSpan().StartLinePosition.Line);
        
        return newField;
    }
    
    private RoutedEventAnalysis? AnalyzeRoutedEvent(FieldDeclarationSyntax field)
    {
        var initializer = field.Declaration.Variables[0].Initializer?.Value 
            as InvocationExpressionSyntax;
        if (initializer == null)
            return null;
        
        var methodName = ((MemberAccessExpressionSyntax)initializer.Expression)
            .Name.Identifier.Text;
        
        // Map WPF registration methods to Avalonia
        var strategy = methodName switch
        {
            "RegisterRoutedEvent" => ExtractRoutingStrategy(initializer),
            _ => null
        };
        
        return new RoutedEventAnalysis
        {
            FieldName = field.Declaration.Variables[0].Identifier.Text,
            EventName = ExtractStringLiteral(initializer.ArgumentList.Arguments[0].Expression),
            Strategy = strategy,
            HandlerType = ExtractTypeFromTypeOf(initializer.ArgumentList.Arguments[2].Expression),
            OwnerType = ExtractTypeFromTypeOf(initializer.ArgumentList.Arguments[3].Expression),
        };
    }
    
    private RoutingStrategies ExtractRoutingStrategy(InvocationExpressionSyntax invocation)
    {
        var args = invocation.ArgumentList.Arguments;
        var strategyArg = args.Skip(1).FirstOrDefault();
        if (strategyArg == null)
            return RoutingStrategies.Direct;
        
        var strategyStr = strategyArg.Expression.ToString();
        return strategyStr switch
        {
            "RoutingStrategy.Bubble" => RoutingStrategies.Bubble,
            "RoutingStrategy.Tunnel" => RoutingStrategies.Tunnel,
            "RoutingStrategy.Direct" => RoutingStrategies.Direct,
            _ => RoutingStrategies.Direct
        };
    }
}
```

#### 3.2 Preview Event Transformation
**Recommendation**: Detect and transform Preview* events

```csharp
private string TransformEventName(string wpfEventName, RoutingStrategies strategy)
{
    // Remove "Preview" prefix if present
    if (wpfEventName.StartsWith("Preview"))
    {
        var baseName = wpfEventName.Substring("Preview".Length);
        // Mark as tunnel strategy
        return baseName;
    }
    
    return wpfEventName;
}

private string GenerateEventRegistration(RoutedEventAnalysis analysis)
{
    // Get event args type
    var eventArgsType = analysis.EventArgsType ?? "RoutedEventArgs";
    
    return $@"RoutedEvent<{eventArgsType}>.Register<{analysis.OwnerType}, {eventArgsType}>(
    name: ""{analysis.EventName}"",
    routingStrategy: RoutingStrategies.{FlagsToString(analysis.Strategy)})";
}

private string FlagsToString(RoutingStrategies strategies)
{
    var parts = new List<string>();
    if ((strategies & RoutingStrategies.Direct) != 0)
        parts.Add("Direct");
    if ((strategies & RoutingStrategies.Tunnel) != 0)
        parts.Add("Tunnel");
    if ((strategies & RoutingStrategies.Bubble) != 0)
        parts.Add("Bubble");
    
    return string.Join(" | ", parts);
}
```

---

## 4. Data Binding Transformation

### Binding Expression Sanitizer

```csharp
public class BindingExpressionSanitizer
{
    private static readonly Regex UpdateSourceTriggerPattern = 
        new(@"UpdateSourceTrigger\s*=\s*\w+\s*,?", RegexOptions.IgnoreCase);
    
    private static readonly Regex StringFormatPattern = 
        new(@"StringFormat\s*=\s*'([^']*)'", RegexOptions.IgnoreCase);
    
    public string SanitizeBindingExpression(string bindingExpression)
    {
        var result = bindingExpression;
        
        // Remove UpdateSourceTrigger (always PropertyChanged in Avalonia)
        result = UpdateSourceTriggerPattern.Replace(result, "");
        
        // Note: StringFormat would need converter transformation
        if (StringFormatPattern.IsMatch(result))
        {
            _diagnostics.AddWarning(
                "BINDING_STRING_FORMAT",
                "StringFormat in bindings should be converted to custom IValueConverter");
        }
        
        // Clean up trailing commas
        result = Regex.Replace(result, @",\s*([\}\]])", "$1");
        
        return result.Trim();
    }
}
```

### Relative Source Mapping

```csharp
public class RelativeSourceTransformer
{
    /// <summary>
    /// Maps WPF RelativeSource.FindAncestor to Avalonia binding.
    /// </summary>
    public string TransformRelativeSource(string wpfBinding)
    {
        // WPF: {Binding DataContext, RelativeSource={RelativeSource FindAncestor, AncestorType=Window}}
        // Avalonia: {Binding DataContext, RelativeSource={RelativeSource FindAncestor, AncestorType=Window}}
        // (Same in Avalonia)
        
        return wpfBinding; // Direct mapping for now
    }
    
    /// <summary>
    /// Maps ElementName bindings.
    /// </summary>
    public string TransformElementNameBinding(string elementName)
    {
        // WPF: {Binding Text, ElementName=MyTextBox}
        // Avalonia: {Binding Text, ElementName=MyTextBox}
        // (Same in Avalonia)
        
        return elementName; // Direct mapping
    }
}
```

---

## 5. Layout System Enhancements

### Layout Transform Detection

```csharp
public class LayoutTransformDetector : CSharpSyntaxWalker
{
    private readonly DiagnosticCollector _diagnostics;
    
    public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        // Detect: element.LayoutTransform = ...
        if (node.Left is MemberAccessExpressionSyntax member &&
            member.Name.Identifier.Text == "LayoutTransform")
        {
            _diagnostics.AddWarning(
                "LAYOUT_TRANSFORM_NOT_SUPPORTED",
                "LayoutTransform is not supported in Avalonia. Use RenderTransform instead.",
                node.GetLocation().GetLineSpan().Path,
                node.GetLocation().GetLineSpan().StartLinePosition.Line);
        }
        
        base.VisitAssignmentExpression(node);
    }
    
    public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
    {
        // Detect: element["LayoutTransform"] = ...
        if (node.ToString().Contains("LayoutTransform"))
        {
            _diagnostics.AddWarning(
                "LAYOUT_TRANSFORM_NOT_SUPPORTED",
                "LayoutTransform is not supported in Avalonia.",
                node.GetLocation().GetLineSpan().Path,
                node.GetLocation().GetLineSpan().StartLinePosition.Line);
        }
        
        base.VisitElementAccessExpression(node);
    }
}
```

### MeasureOverride/ArrangeOverride Transformation

```csharp
public class LayoutOverrideTransformer : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (node.Identifier.Text == "MeasureCore")
        {
            // Transform: MeasureCore(Size availableSize) → Measure(Size availableSize)
            return node.WithIdentifier(
                SyntaxFactory.Identifier("Measure"));
        }
        
        if (node.Identifier.Text == "ArrangeCore")
        {
            // Transform: ArrangeCore(Rect finalRect) → Arrange(Rect finalRect)
            return node.WithIdentifier(
                SyntaxFactory.Identifier("Arrange"));
        }
        
        return base.VisitMethodDeclaration(node);
    }
}
```

---

## 6. XAML Namespace Transformation

### Namespace Mapping

```csharp
public class XamlNamespaceTransformer
{
    private static readonly Dictionary<string, string> NamespaceMappings = 
        new()
        {
            {
                "http://schemas.microsoft.com/winfx/2006/xaml/presentation",
                "https://github.com/avaloniaui"
            },
            {
                "http://schemas.microsoft.com/winfx/2006/xaml",
                "http://schemas.microsoft.com/winfx/2006/xaml"
            },
            {
                "http://schemas.microsoft.com/expression/blend/2008",
                "https://github.com/avaloniaui"
            }
        };
    
    public XDocument TransformDocument(XDocument wpfXaml)
    {
        var root = wpfXaml.Root;
        if (root == null)
            return wpfXaml;
        
        // Update namespace declarations
        var newAttributes = root.Attributes()
            .Select(attr => 
            {
                if (attr.Name.NamespaceName == XNamespace.Xmlns.NamespaceName ||
                    attr.Name.LocalName == "xmlns")
                {
                    var oldValue = attr.Value;
                    if (NamespaceMappings.TryGetValue(oldValue, out var newValue))
                    {
                        return new XAttribute(attr.Name, newValue);
                    }
                }
                return attr;
            })
            .ToList();
        
        root.Attributes().Remove();
        root.Add(newAttributes);
        
        return wpfXaml;
    }
}
```

---

## 7. Testing Recommendations

### Unit Test Structure

```csharp
public class DependencyPropertyTransformerTests
{
    [Fact]
    public void Transform_SimpleProperty_GeneratesStyledProperty()
    {
        // Arrange
        var source = @"
            public static readonly DependencyProperty TextProperty =
                DependencyProperty.Register(
                    nameof(Text),
                    typeof(string),
                    typeof(MyControl));";
        
        // Act
        var result = TransformCode(source);
        
        // Assert
        result.Should().Contain("StyledProperty<string>");
        result.Should().Contain("AvaloniaProperty.Register");
    }
    
    [Fact]
    public void Transform_ReadOnlyProperty_GeneratesDirectProperty()
    {
        // Arrange
        var source = @"
            public static readonly DependencyPropertyKey WidthPropertyKey =
                DependencyProperty.RegisterReadOnly(
                    nameof(Width),
                    typeof(double),
                    typeof(MyControl),
                    null);";
        
        // Act
        var result = TransformCode(source);
        
        // Assert
        result.Should().Contain("DirectProperty");
    }
}
```

---

## 8. Diagnostic Output Format

**Recommendation**: Standardize diagnostic format for clarity

```csharp
public class DiagnosticFormatter
{
    public string Format(DiagnosticInfo diagnostic)
    {
        return $@"[{diagnostic.Severity}] {diagnostic.Code}: {diagnostic.Message}
  File: {diagnostic.FilePath}:{diagnostic.Line}:{diagnostic.Column}
  Details: {diagnostic.Details}
  Recommendation: {diagnostic.Recommendation}";
    }
}

// Example output:
// [WARNING] LAYOUT_TRANSFORM_NOT_SUPPORTED: LayoutTransform is not supported in Avalonia
//   File: MyControl.cs:42:10
//   Details: LayoutTransform affects layout calculation, which is unavailable in Avalonia
//   Recommendation: Convert to RenderTransform if only visual rotation is needed
```

---

## Summary of Recommended Improvements

1. **Property System**: Enhanced heuristics, inheritance detection, callback wrapping
2. **Type Mapping**: Comprehensive database with mapping levels and fallbacks
3. **Event System**: Complete RoutedEvent transformation with strategy mapping
4. **Data Binding**: Expression sanitization, validation transformation
5. **Layout System**: LayoutTransform detection and warnings
6. **XAML**: Namespace and type mapping infrastructure
7. **Testing**: Comprehensive test coverage for each transformer
8. **Diagnostics**: Clear, actionable diagnostic messages

These recommendations provide a solid foundation for implementing a robust WPF to Avalonia compatibility layer.

