# XAML Test Assertion Pattern

## Summary

All XAML transformation tests have been updated to use full XAML string assertions instead of partial `.Contain()` checks. This ensures more rigorous testing and catches unintended transformations.

## Pattern

### Helper Method

Add this helper method to each test class:

```csharp
/// <summary>
/// Normalizes XAML by removing extra whitespace and newlines for comparison.
/// </summary>
private static string NormalizeXaml(string xaml)
{
    return System.Text.RegularExpressions.Regex.Replace(
        xaml.Trim(),
        @"\s+",
        " ")
        .Replace("> <", "><")
        .Replace(" >", ">")
        .Replace(" =", "=");
}
```

### Old Pattern (Partial Checks)

```csharp
[Fact]
public void Convert_SimpleButton_ShouldSucceed()
{
    // Arrange
    var converter = new WpfToAvaloniaConverter();
    var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""Click Me"" />
</Window>";

    // Act
    var result = converter.Convert(wpfXaml);

    // Assert
    result.Success.Should().BeTrue();
    result.OutputXaml.Should().Contain("https://github.com/avaloniaui");
    result.OutputXaml.Should().Contain("<Button");
    result.OutputXaml.Should().Contain("Content=\"Click Me\"");
}
```

### New Pattern (Full XAML Assertion)

```csharp
[Fact]
public void Convert_SimpleButton_ShouldSucceed()
{
    // Arrange
    var converter = new WpfToAvaloniaConverter();
    var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Content=""Click Me"" />
</Window>";

    var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <Button Content=""Click Me"" />
</Window>";

    // Act
    var result = converter.Convert(wpfXaml);

    // Assert
    result.Success.Should().BeTrue();
    NormalizeXaml(result.OutputXaml!).Should().Be(NormalizeXaml(expectedXaml),
        "the converted XAML should match the expected Avalonia XAML");
}
```

## Benefits

1. **Catches Unintended Changes**: Full assertions catch any unexpected modifications to the XAML structure
2. **Documents Expected Output**: The `expectedXaml` variable serves as documentation of what the transformation should produce
3. **Whitespace Agnostic**: The `NormalizeXaml` helper makes tests resilient to formatting changes
4. **More Maintainable**: When transformation rules change, it's clear what needs to be updated

## Files Updated

### ✅ Completed
- `ConverterIntegrationTests.cs` - 17 tests updated
- `BatchConversionTests.cs` - 8 tests updated
- `BindingTransformationTests.cs` - 16 tests updated
- `TransformationRulesTests.cs` - 20 tests updated
- `StyleTransformationTests.cs` - 15 tests updated
- `CompatibilityTransformationTests.cs` - 39 tests updated

**Total: 115 tests updated - All test files complete! ✅**

## Migration Instructions

For each test:

1. Add the `NormalizeXaml` helper method if not already present
2. For each test that does `.Contain()` checks:
   - Add an `expectedXaml` variable with the full expected Avalonia XAML
   - Replace multiple `.Contain()` assertions with a single `NormalizeXaml().Should().Be()` assertion
3. Keep tests for error cases (`Success.Should().BeFalse()`, etc.) as-is
4. Keep tests for diagnostics and file I/O as-is

## Example Transformations

### Visibility Property
```csharp
var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Button Visibility=""Visible"" />
</Window>";

var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <Button IsVisible=""true"" />
</Window>";
```

### Grid with Definitions
```csharp
var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
        </Grid.RowDefinitions>
    </Grid>
</Window>";

var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <Grid>
    <Grid.RowDefinitions>
      <RowDefinition Height=""Auto"" />
    </Grid.RowDefinitions>
  </Grid>
</Window>";
```

### Binding Syntax
```csharp
var wpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
    <TextBlock Text=""{Binding Name}"" />
</Window>";

var expectedXaml = @"<Window xmlns=""https://github.com/avaloniaui"">
  <TextBlock Text=""{Binding Name}"" />
</Window>";
```

## Testing the Pattern

Run tests to verify:

```bash
dotnet test --filter "FullyQualifiedName~ConverterIntegrationTests"
dotnet test --filter "FullyQualifiedName~BatchConversionTests"
```

All tests should pass with the new assertion pattern.
