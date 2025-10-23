# Configuration File Reference

The WPF to Avalonia CLI tool supports configuration files to simplify repeated migrations and team workflows.

## Quick Start

### Create a Configuration File

```bash
# Create default configuration
dotnet run --project src/WpfToAvalonia.CLI -- config init

# Create with template
dotnet run --project src/WpfToAvalonia.CLI -- config init --template incremental
```

### Use Configuration File

```bash
# Auto-detect wpf2avalonia.json in current directory
dotnet run --project src/WpfToAvalonia.CLI -- transform-project

# Specify configuration file
dotnet run --project src/WpfToAvalonia.CLI -- transform-project --config my-config.json
```

## Configuration File Format

Configuration files use JSON format. Here's a complete example:

```json
{
  "input": "./MyWpfProject",
  "output": "./MyAvaloniaProject",
  "xamlPattern": "*.xaml",
  "csharpPattern": "*.cs",
  "recursive": true,
  "dryRun": false,
  "verbose": false,
  "exclude": [
    "obj",
    "bin",
    ".vs",
    ".git",
    "packages"
  ],
  "skipCSharp": false,
  "skipXaml": false,
  "includeDiagnosticComments": false
}
```

## Configuration Options

### `input` (string, optional)
Input file or directory path.

**Example:**
```json
"input": "./src/MyWpfApp"
```

**Note:** Can be overridden with `--input` command-line option.

### `output` (string, optional)
Output directory path.

**Example:**
```json
"output": "./output/MyAvaloniaApp"
```

**Default:** Input directory with `.avalonia` suffix

### `xamlPattern` (string, default: `"*.xaml"`)
File pattern for XAML files to transform.

**Examples:**
```json
"xamlPattern": "*.xaml"
"xamlPattern": "*Window.xaml"
"xamlPattern": "Main*.xaml"
```

### `csharpPattern` (string, default: `"*.cs"`)
File pattern for C# files to transform.

**Examples:**
```json
"csharpPattern": "*.cs"
"csharpPattern": "*.xaml.cs"
```

### `recursive` (boolean, default: `true`)
Search directories recursively.

**Example:**
```json
"recursive": true
```

### `dryRun` (boolean, default: `false`)
Perform transformation without writing files.

**Example:**
```json
"dryRun": true
```

**Use Case:** Preview transformations before applying changes.

### `verbose` (boolean, default: `false`)
Enable verbose output with detailed diagnostics.

**Example:**
```json
"verbose": true
```

### `exclude` (array of strings, default: `["obj", "bin", ".vs", ".git", "packages"]`)
Directory patterns to exclude from transformation.

**Example:**
```json
"exclude": [
  "obj",
  "bin",
  ".vs",
  ".git",
  "packages",
  "node_modules",
  "TestResults"
]
```

### `skipCSharp` (boolean, default: `false`)
Skip C# file transformation (XAML only).

**Example:**
```json
"skipCSharp": true
```

**Use Case:** When only XAML needs transformation.

### `skipXaml` (boolean, default: `false`)
Skip XAML file transformation (C# only).

**Example:**
```json
"skipXaml": true
```

**Use Case:** When only C# code-behind needs transformation.

### `includeDiagnosticComments` (boolean, default: `false`)
Include diagnostic comments in transformed XAML files.

**Example:**
```json
"includeDiagnosticComments": true
```

**Output Example:**
```xml
<!-- WPF to Avalonia Transformation Applied -->
<!-- 12 transformations: Namespace (5), TypeMapping (4), PropertyMapping (3) -->
<Window xmlns="https://github.com/avaloniaui">
```

## Configuration Templates

The `config init` command supports several templates:

### Default Template
```bash
dotnet run --project src/WpfToAvalonia.CLI -- config init
```

Standard configuration with all defaults.

### XAML-Only Template
```bash
dotnet run --project src/WpfToAvalonia.CLI -- config init --template xaml-only
```

Skips C# transformation:
```json
{
  "skipCSharp": true,
  "xamlPattern": "*.xaml"
}
```

### C#-Only Template
```bash
dotnet run --project src/WpfToAvalonia.CLI -- config init --template csharp-only
```

Skips XAML transformation:
```json
{
  "skipXaml": true,
  "csharpPattern": "*.cs"
}
```

### Incremental Template
```bash
dotnet run --project src/WpfToAvalonia.CLI -- config init --template incremental
```

For safe incremental migration:
```json
{
  "verbose": true,
  "dryRun": true
}
```

## Configuration File Locations

The CLI auto-detects configuration files in the following order:

1. `wpf2avalonia.json` (current directory)
2. `.wpf2avalonia.json` (current directory)
3. `wpf-to-avalonia.json` (current directory)
4. `migration.config.json` (current directory)
5. Parent directories (searches upward)

## Managing Configurations

### View Current Configuration

```bash
dotnet run --project src/WpfToAvalonia.CLI -- config show
```

**Output:**
```
Configuration File: ./wpf2avalonia.json

Settings:
  Input:                ./MyWpfProject
  Output:               ./MyAvaloniaProject
  XAML Pattern:         *.xaml
  C# Pattern:           *.cs
  Recursive:            true
  Dry Run:              false
  Verbose:              false
  Skip C#:              false
  Skip XAML:            false
  Diagnostic Comments:  false
  Exclude:              obj, bin, .vs, .git, packages
```

### Validate Configuration

```bash
dotnet run --project src/WpfToAvalonia.CLI -- config validate
```

**Output:**
```
Validating: ./wpf2avalonia.json

✓ Configuration is valid
```

Or with errors:
```
Errors (1):
  - Cannot skip both C# and XAML transformation

Warnings (1):
  - Input path does not exist: ./NonExistentProject
```

## Command-Line Override

Command-line options always override configuration file settings:

```bash
# Configuration file has dryRun: false
# This command still performs a dry run
dotnet run --project src/WpfToAvalonia.CLI -- transform-project --config wpf2avalonia.json --dry-run
```

**Priority:**
1. Command-line options (highest)
2. Configuration file
3. Default values (lowest)

## Example Workflows

### Team Collaboration

Create a shared configuration for your team:

**`wpf2avalonia.json`:**
```json
{
  "input": "./src",
  "output": "./migrated",
  "exclude": [
    "obj",
    "bin",
    ".vs",
    "TestResults"
  ],
  "verbose": true
}
```

Commit to repository:
```bash
git add wpf2avalonia.json
git commit -m "Add migration configuration"
```

Team members can now run:
```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform-project
```

### Multi-Stage Migration

**Stage 1 - Preview (incremental.json):**
```json
{
  "input": "./MyProject",
  "dryRun": true,
  "verbose": true
}
```

**Stage 2 - XAML Only (xaml.json):**
```json
{
  "input": "./MyProject",
  "output": "./Migrated",
  "skipCSharp": true
}
```

**Stage 3 - Full Migration (full.json):**
```json
{
  "input": "./MyProject",
  "output": "./Migrated"
}
```

Execute stages:
```bash
dotnet run --project src/WpfToAvalonia.CLI -- transform-project --config incremental.json
dotnet run --project src/WpfToAvalonia.CLI -- transform-project --config xaml.json
dotnet run --project src/WpfToAvalonia.CLI -- transform-project --config full.json
```

### CI/CD Integration

**`.github/workflows/migration.yml`:**
```yaml
name: Migration Validation
on: [push]

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Validate Migration Config
        run: |
          dotnet run --project src/WpfToAvalonia.CLI -- config validate
      - name: Dry Run Migration
        run: |
          dotnet run --project src/WpfToAvalonia.CLI -- transform-project --config wpf2avalonia.json
```

**`wpf2avalonia.json`:**
```json
{
  "input": "./src",
  "dryRun": true,
  "verbose": true
}
```

## Tips and Best Practices

1. **Start with Incremental Template:** Always begin with dry-run and verbose mode
   ```bash
   dotnet run --project src/WpfToAvalonia.CLI -- config init --template incremental
   ```

2. **Version Control:** Commit configuration files to your repository

3. **Multiple Configs:** Use different configs for different migration scenarios
   - `preview.json` - Dry run with verbose
   - `xaml-only.json` - XAML transformation
   - `full.json` - Complete migration

4. **Validate Before Migration:** Always validate configuration first
   ```bash
   dotnet run --project src/WpfToAvalonia.CLI -- config validate
   dotnet run --project src/WpfToAvalonia.CLI -- transform-project
   ```

5. **Override When Needed:** Use command-line options for one-off changes without modifying config

## See Also

- [CLI Examples](CLI_EXAMPLES.md) - Comprehensive usage examples
- [CLI README](../src/WpfToAvalonia.CLI/README.md) - Command reference
- [Migration Plan](MIGRATION_PLAN.md) - Project roadmap
