# WPF to Avalonia CLI - Usage Examples

This document provides comprehensive examples of using the WPF to Avalonia CLI tool for various migration scenarios.

## Table of Contents

1. [Basic Usage](#basic-usage)
2. [Batch Processing](#batch-processing)
3. [C# Code Transformation](#c-code-transformation)
4. [Full Project Migration](#full-project-migration)
5. [Advanced Scenarios](#advanced-scenarios)
6. [Workflow Examples](#workflow-examples)
7. [Troubleshooting](#troubleshooting)

## Basic Usage

### Analyze a Single File

Preview what transformations would be applied without modifying any files:

```bash
dotnet run --project src/WpfToAvalonia.CLI -- analyze -i MainWindow.xaml
```

**Output:**
```
WPF to Avalonia XAML Analyzer
=============================

Analyzing 1 file(s)...

[1/1] MainWindow.xaml: ✓

Analysis Summary
================
Files analyzed: 1
Total Errors: 0
Total Warnings: 0
Total Info: 45

Transformations Applied:
  Namespace: 12
  Type:ListView→ListBox: 1
```

### Transform a Single File

Transform a single WPF XAML file to Avalonia format:

```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform -i MainWindow.xaml
```

This creates `MainWindow.avalonia.xaml` in the same directory.

**Specify custom output:**
```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform -i MainWindow.xaml -o ./output
```

## Batch Processing

### Transform an Entire Directory

Transform all XAML files in a directory structure:

```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./Views -o ./ViewsAvalonia
```

**With recursive search (default):**
```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./MyWpfProject -o ./MyAvaloniaProject -r
```

**Non-recursive (top-level only):**
```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./Views -o ./ViewsAvalonia -r false
```

### Pattern Matching

Transform only specific files using patterns:

**Main windows only:**
```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -p "*Window.xaml"
```

**User controls only:**
```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -p "*UserControl*.xaml"
```

**Multiple patterns (run separately):**
```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -p "Main*.xaml"
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -p "App.xaml"
```

## C# Code Transformation

### Transform a Single C# File

Transform WPF C# code to Avalonia:

```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform-csharp -i MainWindow.xaml.cs
```

This creates `MainWindow.xaml.avalonia.cs` in the same directory.

**What gets transformed:**
- `using System.Windows` → `using Avalonia`
- `DependencyProperty` → `StyledProperty`
- `GetValue/SetValue` → Direct property access
- `Visibility` property → `IsVisible`
- Event handlers and routed events

### Transform All C# Files in a Directory

Transform all C# files in your project:

```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform-csharp -i ./src -o ./output
```

**Exclude build artifacts:**
```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform-csharp -i ./MyProject -e obj bin .vs
```

### Preview C# Transformations

Dry run to see what would be changed:

```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform-csharp -i ./src -d -v
```

**Output:**
```
WPF to Avalonia C# Transformer
==============================

Found 25 file(s) to transform

DRY RUN MODE - No files will be written

[1/25] Processing: MainWindow.xaml.cs ✓
  Diagnostics: 0 errors, 0 warnings, 12 info
  Output: ./output/MainWindow.xaml.cs
[2/25] Processing: CustomControl.cs ✓
  Diagnostics: 0 errors, 1 warnings, 8 info
...

Transformation Summary
=====================
Successful: 25

Transformations Applied:
  NAMESPACE_MAPPING: 45
  DEPENDENCY_PROPERTY_TO_STYLED: 12
  PROPERTY_ACCESS_MAPPING: 28
```

## Full Project Migration

### Migrate Entire Project (XAML + C#)

The most powerful option - transform both XAML and C# files together:

```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform-project -i ./MyWpfProject -o ./MyAvaloniaProject
```

**Output:**
```
WPF to Avalonia Project Transformer
===================================

Input:  ./MyWpfProject
Output: ./MyAvaloniaProject

Phase 1: Transforming XAML Files
=================================

Found 15 XAML file(s)

[1/15] MainWindow.xaml ✓
[2/15] UserControl1.xaml ✓
...

Phase 2: Transforming C# Files
===============================

Found 25 C# file(s)

[1/25] MainWindow.xaml.cs ✓
[2/25] App.xaml.cs ✓
...

Overall Transformation Summary
==============================
Total Successful: 40
Project transformation complete! Output: ./MyAvaloniaProject
```

### XAML-Only Project Migration

Transform only XAML files, skip C#:

```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform-project -i ./MyProject --skip-csharp
```

### C#-Only Project Migration

Transform only C# files, skip XAML:

```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform-project -i ./MyProject --skip-xaml
```

### Custom File Patterns

Specify which files to transform:

```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform-project \
  -i ./MyProject \
  --xaml-pattern "*.xaml" \
  --csharp-pattern "*.cs" \
  -e obj bin packages
```

## Advanced Scenarios

### Dry Run Mode

Preview transformations without writing any files:

```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./Views -d
```

**Output:**
```
WPF to Avalonia XAML Transformer
================================

Found 15 file(s) to transform

DRY RUN MODE - No files will be written

[1/15] Processing: MainWindow.xaml ✓
[2/15] Processing: UserControl1.xaml ✓
...

Transformation Summary
=====================
Successful: 15

DRY RUN COMPLETE - No files were written
```

### Verbose Mode

Get detailed diagnostic information during transformation:

```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./Views -o ./output -v
```

**Output includes:**
```
[1/3] Processing: MainWindow.xaml ✓
  Diagnostics: 0 errors, 1 warnings, 45 info
  Output: ./output/MainWindow.xaml
```

### Combine Options

Dry run + verbose + custom pattern:

```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -p "*.xaml" -d -v
```

## Workflow Examples

### Scenario 1: Incremental Migration

Migrate one module at a time to minimize risk:

```bash
# Step 1: Analyze the Views module (XAML only)
dotnet run --project src/WpfToAvalonia.CLI -- analyze -i ./WpfApp/Views

# Step 2: Dry run to preview XAML changes
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./WpfApp/Views -d -v

# Step 3: Transform Views module XAML
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./WpfApp/Views -o ./AvaloniaApp/Views

# Step 4: Transform corresponding C# code-behind files
dotnet run --project src/WpfToAvalonia.CLI -- transform-csharp -i ./WpfApp/Views -o ./AvaloniaApp/Views

# Step 5: Repeat for other modules
dotnet run --project src/WpfToAvalonia.CLI -- transform-project -i ./WpfApp/Controls -o ./AvaloniaApp/Controls
```

### Scenario 2: Full Project Migration

Migrate an entire WPF project (recommended approach):

```bash
# Step 1: Create backup
cp -r ./WpfApp ./WpfApp.backup

# Step 2: Analyze entire project (XAML)
dotnet run --project src/WpfToAvalonia.CLI -- analyze -i ./WpfApp

# Step 3: Dry run entire project transformation
dotnet run --project src/WpfToAvalonia.CLI -- transform-project -i ./WpfApp -d -v

# Step 4: Transform entire project (XAML + C#)
dotnet run --project src/WpfToAvalonia.CLI -- transform-project -i ./WpfApp -o ./AvaloniaApp

# Step 5: Review generated files
ls -R ./AvaloniaApp
```

### Scenario 3: Selective Migration

Migrate specific file types or patterns:

```bash
# Transform only main application files
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -p "Main*.xaml" -o ./output

# Transform only resource dictionaries
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -p "*Resources*.xaml" -o ./output

# Transform only user controls
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -p "*Control.xaml" -o ./output
```

### Scenario 4: Quality Assurance Workflow

Use analysis mode for code review before transformation:

```bash
# Step 1: Generate analysis report for review
dotnet run --project src/WpfToAvalonia.CLI -- analyze -i ./src > migration-analysis.txt

# Step 2: Review the report
cat migration-analysis.txt

# Step 3: Transform with verbose output for tracking
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -o ./output -v > migration-log.txt

# Step 4: Review transformation log
cat migration-log.txt
```

### Scenario 5: Continuous Integration

Automate migration checks in CI/CD pipeline:

```bash
#!/bin/bash
# CI script example

# Run XAML analysis (exit code 0 if successful)
dotnet run --project src/WpfToAvalonia.CLI -- analyze -i ./src

if [ $? -eq 0 ]; then
    echo "XAML analysis completed successfully"

    # Perform dry run to validate transformations (both XAML and C#)
    dotnet run --project src/WpfToAvalonia.CLI -- transform-project -i ./src -d -v

    if [ $? -eq 0 ]; then
        echo "Dry run completed - ready for migration"

        # Generate migration report
        dotnet run --project src/WpfToAvalonia.CLI -- transform-project -i ./src -d -v > migration-report.txt
        echo "Migration report saved to migration-report.txt"
    fi
fi
```

### Scenario 6: Code-Behind Only Migration

Migrate only C# code-behind files (useful when XAML is already migrated):

```bash
# Step 1: Preview C# transformations
dotnet run --project src/WpfToAvalonia.CLI -- transform-csharp -i ./WpfApp -d -v

# Step 2: Transform all C# files
dotnet run --project src/WpfToAvalonia.CLI -- transform-csharp -i ./WpfApp -o ./AvaloniaApp

# Step 3: Review transformation statistics
# The output will show:
# - DependencyProperty → StyledProperty conversions
# - Namespace mappings
# - Property access updates
# - Event handler transformations
```

### Scenario 7: Mixed Approach Migration

Combine different commands for fine-grained control:

```bash
# Step 1: Transform XAML files only for Views
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src/Views -o ./output/Views

# Step 2: Transform both XAML and C# for Controls
dotnet run --project src/WpfToAvalonia.CLI -- transform-project -i ./src/Controls -o ./output/Controls

# Step 3: Transform only C# for ViewModels (no XAML)
dotnet run --project src/WpfToAvalonia.CLI -- transform-csharp -i ./src/ViewModels -o ./output/ViewModels

# Step 4: Review each module separately
```

## Troubleshooting

### Issue: No Files Found

**Problem:**
```
Warning: No files found matching pattern: *.xaml
```

**Solution:**
```bash
# Check if you're in the correct directory
ls *.xaml

# Use absolute path
dotnet run --project src/WpfToAvalonia.CLI -- transform -i /absolute/path/to/files

# Check pattern syntax
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -p "*.xaml"
```

### Issue: Permission Denied

**Problem:**
```
Error: Permission denied writing to output directory
```

**Solution:**
```bash
# Check output directory permissions
ls -ld ./output

# Create output directory first
mkdir -p ./output

# Use a different output location
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -o /tmp/avalonia-output
```

### Issue: File Processing Errors

**Problem:**
```
[1/5] file1.xaml ✗
  Error: Failed to parse XAML
```

**Solution:**
```bash
# Use verbose mode to get detailed error information
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -v

# Individual file errors don't stop batch processing
# Review error messages and fix source files if needed
```

### Issue: Unexpected Transformations

**Problem:**
Some transformations produce unexpected results.

**Solution:**
```bash
# Use analyze mode first to understand changes
dotnet run --project src/WpfToAvalonia.CLI -- analyze -i ./src

# Use dry run with verbose to preview exact changes
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -d -v

# Transform incrementally, file by file
dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./MainWindow.xaml
# Review output, then continue with next file
```

## Tips and Best Practices

1. **Always backup first**: Create a backup before transforming files
   ```bash
   cp -r ./WpfProject ./WpfProject.backup
   ```

2. **Use analyze before transform**: Understand what changes will be made
   ```bash
   dotnet run --project src/WpfToAvalonia.CLI -- analyze -i ./src
   ```

3. **Start with dry runs**: Validate transformations without modifying files
   ```bash
   dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -d
   ```

4. **Use verbose for debugging**: Get detailed information about the transformation process
   ```bash
   dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -v
   ```

5. **Migrate incrementally**: Transform one module at a time for easier review
   ```bash
   # Good: Module-by-module
   dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./Views -o ./output/Views
   dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./Controls -o ./output/Controls

   # Risky: Everything at once
   dotnet run --project src/WpfToAvalonia.CLI -- transform -i ./src -o ./output
   ```

6. **Version control**: Commit transformed files separately for easier review
   ```bash
   git add -p ./output/  # Review each change
   git commit -m "Migrate Views module to Avalonia"
   ```

7. **Review diagnostics**: Pay attention to warnings in the analysis output
   - Warnings indicate potential issues that may need manual review
   - Info diagnostics show what transformations were applied

8. **Test after transformation**: Always test the transformed XAML with Avalonia
   - Some WPF features may not have direct Avalonia equivalents
   - Manual adjustments may be needed for edge cases

## Additional Resources

- [CLI README](../src/WpfToAvalonia.CLI/README.md) - Complete CLI reference
- [Migration Plan](./MIGRATION_PLAN.md) - Project roadmap and features
- [Implementation Status](./IMPLEMENTATION_STATUS.md) - Current implementation status

## Getting Help

For issues or questions:
1. Check the troubleshooting section above
2. Review the CLI README for detailed option descriptions
3. Run with `-v` verbose mode for more diagnostic information
4. Review the migration plan for understanding transformations
