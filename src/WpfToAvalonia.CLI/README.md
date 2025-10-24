# WPF to Avalonia CLI Tool

A command-line tool for transforming WPF XAML files to Avalonia UI format.

## Installation

```bash
dotnet build src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj
```

## Usage

The CLI provides six main commands:

1. **migrate** - **NEW!** End-to-end project migration with 7-stage orchestration
2. **transform** - Transform XAML files only
3. **transform-csharp** - Transform C# files only
4. **transform-project** - Transform entire project (XAML + C#)
5. **analyze** - Analyze XAML files without transformation
6. **config** - Manage migration configuration files

### Migrate Command (Recommended)

**NEW!** Orchestrates complete end-to-end WPF project migration using the 7-stage pipeline:

1. **Analysis** - Detect WPF usage and analyze project structure
2. **Backup** - Create backup of original files (optional)
3. **Transform Project File** - Convert .csproj to Avalonia
4. **Transform XAML Files** - Convert all XAML files
5. **Transform C# Files** - Convert C# code-behind and classes
6. **Validation** - Verify transformed output
7. **Write Files** - Write transformed files to disk (unless dry-run)

```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- migrate [options]
```

**Options:**
- `-p, --project <path>` (required) - Path to WPF project file (.csproj) to migrate
- `-o, --output <path>` - Output project path (defaults to `<project>.Avalonia.csproj`)
- `-d, --dry-run` - Perform migration analysis without writing files (default: `false`)
- `-b, --backup` - Create backup of original files before migration (default: `true`)
- `--backup-dir <dir>` - Backup directory name (default: `.migration-backup`)
- `--rename-xaml` - Rename .xaml files to .axaml (default: `true`)
- `--avalonia-version <version>` - Avalonia version to use (default: `11.2.2`)
- `--target-framework <framework>` - Target framework (e.g., `net8.0`)
- `--compiled-bindings` - Enable compiled bindings (default: `true`)
- `-v, --verbose` - Enable verbose output with detailed diagnostics
- `-c, --config <path>` - Configuration file path

**Examples:**

Migrate a WPF project (recommended approach):
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- migrate -p ./MyWpfApp/MyWpfApp.csproj
```

Dry run to preview migration:
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- migrate -p ./MyWpfApp/MyWpfApp.csproj --dry-run -v
```

Migrate without creating backup (not recommended):
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- migrate -p ./MyWpfApp/MyWpfApp.csproj --backup false
```

Migrate with custom Avalonia version and target framework:
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- migrate -p ./MyWpfApp/MyWpfApp.csproj --avalonia-version 11.1.0 --target-framework net7.0
```

**Output:**
```
WPF to Avalonia Migration Tool
================================

Project: ./MyWpfApp/MyWpfApp.csproj

Starting migration...

Migration Stages
================
✓ Analysis       (42ms) Analyzing WPF project...
✓ Backup         (15ms) Creating backup of original files...
✓ ProjectFile    (23ms) Transforming project file...
✓ XAML           (156ms) Transforming XAML files...
✓ CSharp         (89ms) Transforming C# files...
✓ Validation     (12ms) Validating transformed output...
✓ Writing        (34ms) Writing transformed files to disk...

Migration Summary
=================
XAML Files:   5/5 successful
C# Files:     12 processed
Elapsed Time: 0.4s

Warnings: 3
Errors:   0

✓ Migration complete! Output: ./MyWpfApp/MyWpfApp.Avalonia.csproj
```

### Transform Command

Transform WPF XAML files to Avalonia format with batch processing support.

```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform [options]
```

**Options:**
- `-i, --input <path>` (required) - Input file or directory path
- `-o, --output <path>` - Output directory path (defaults to input directory with '.avalonia' suffix)
- `-p, --pattern <pattern>` - File pattern to match (default: `*.xaml`)
- `-r, --recursive` - Search directories recursively (default: `true`)
- `-d, --dry-run` - Perform a dry run without writing files (default: `false`)
- `-v, --verbose` - Enable verbose output with detailed diagnostics (default: `false`)

**Examples:**

Transform a single file:
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform -i MainWindow.xaml
```

Transform all XAML files in a directory:
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform -i ./Views -o ./ViewsAvalonia
```

Dry run with verbose output:
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform -i ./Views -d -v
```

Transform with custom pattern (non-recursive):
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform -i ./Views -p "Main*.xaml" -r false
```

### Transform C# Command

Transform WPF C# code files to Avalonia format.

```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform-csharp [options]
```

**Options:**
- `-i, --input <path>` (required) - Input file or directory path
- `-o, --output <path>` - Output directory path (defaults to input directory with '.avalonia' suffix)
- `-p, --pattern <pattern>` - File pattern to match (default: `*.cs`)
- `-r, --recursive` - Search directories recursively (default: `true`)
- `-d, --dry-run` - Perform a dry run without writing files (default: `false`)
- `-v, --verbose` - Enable verbose output with detailed diagnostics (default: `false`)
- `-e, --exclude <patterns>` - Directory patterns to exclude (default: `obj`, `bin`, `.vs`, `.git`)

**Examples:**

Transform a single C# file:
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform-csharp -i MainWindow.xaml.cs
```

Transform all C# files in a directory:
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform-csharp -i ./src -o ./output
```

Dry run with verbose output:
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform-csharp -i ./src -d -v
```

**C# Transformations:**
- `using System.Windows.*` → `using Avalonia.*`
- `DependencyProperty` → `StyledProperty`
- `ReadOnlyDependencyProperty` → `DirectProperty`
- `GetValue()/SetValue()` → Direct property access
- `Visibility` property → `IsVisible`
- Event handler transformations
- Routed event mappings

### Transform Project Command

Transform an entire WPF project including both XAML and C# files.

```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform-project [options]
```

**Options:**
- `-i, --input <path>` (required) - Input directory path (project root)
- `-o, --output <path>` - Output directory path (defaults to input directory with '.avalonia' suffix)
- `--xaml-pattern <pattern>` - XAML file pattern to match (default: `*.xaml`)
- `--csharp-pattern <pattern>` - C# file pattern to match (default: `*.cs`)
- `-d, --dry-run` - Perform a dry run without writing files (default: `false`)
- `-v, --verbose` - Enable verbose output (default: `false`)
- `-e, --exclude <patterns>` - Directory patterns to exclude (default: `obj`, `bin`, `.vs`, `.git`, `packages`)
- `--skip-csharp` - Skip C# file transformation (XAML only)
- `--skip-xaml` - Skip XAML file transformation (C# only)

**Examples:**

Transform entire project (recommended):
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform-project -i ./MyWpfProject -o ./MyAvaloniaProject
```

Transform only XAML files:
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform-project -i ./MyProject --skip-csharp
```

Transform only C# files:
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform-project -i ./MyProject --skip-xaml
```

Dry run to preview all changes:
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform-project -i ./MyProject -d -v
```

### Config Command

Manage migration configuration files for simplified workflows.

```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- config [subcommand] [options]
```

**Subcommands:**

#### config init
Create a new migration configuration file.

**Options:**
- `-o, --output <path>` - Output file path (default: `wpf2avalonia.json`)
- `-t, --template <name>` - Template (default, xaml-only, csharp-only, incremental)
- `-f, --force` - Overwrite existing file

**Examples:**
```bash
# Create default configuration
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- config init

# Create with template
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- config init --template incremental -o my-config.json

# Force overwrite
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- config init --force
```

#### config show
Display current configuration.

**Options:**
- `-f, --file <path>` - Configuration file path (auto-detects if not specified)

**Example:**
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- config show
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- config show --file custom-config.json
```

#### config validate
Validate a configuration file.

**Options:**
- `-f, --file <path>` - Configuration file path (auto-detects if not specified)

**Example:**
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- config validate
```

**Using Configuration Files:**

The `transform-project` command supports configuration files:

```bash
# Auto-detect wpf2avalonia.json
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform-project

# Specify configuration file
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform-project --config my-config.json

# Command-line options override config file
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- transform-project --config wpf2avalonia.json --dry-run
```

See [CONFIGURATION.md](../../docs/CONFIGURATION.md) for complete configuration file reference.

### Analyze Command

Analyze WPF XAML files and report transformation details without modifying files.

```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- analyze [options]
```

**Options:**
- `-i, --input <path>` (required) - Input file or directory path
- `-p, --pattern <pattern>` - File pattern to match (default: `*.xaml`)
- `-r, --recursive` - Search directories recursively (default: `true`)

**Examples:**

Analyze a single file:
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- analyze -i MainWindow.xaml
```

Analyze all XAML files in a directory:
```bash
dotnet run --project src/WpfToAvalonia.CLI/WpfToAvalonia.CLI.csproj -- analyze -i ./Views
```

## Features

### Batch Processing
- Process multiple XAML files in a single command
- Pattern matching for selective file processing
- Recursive directory search
- Individual file error handling (failures don't stop batch processing)

### Progress Reporting
- Real-time colored console output
- Progress indicators `[1/10]` for batch operations
- Color-coded status: ✓ (green), ✗ (red), warnings (yellow)
- Summary statistics after completion

### Transformation Statistics
- Total errors, warnings, and info diagnostics
- Top 15 transformation types applied
- Per-file diagnostic counts (in verbose mode)

### Error Handling
- Graceful handling of individual file failures
- Cancellation support (Ctrl+C)
- Detailed error messages with file paths
- Stack traces in verbose mode

## Output

### Transform Command Output

```
WPF to Avalonia XAML Transformer
================================

Found 3 file(s) to transform

[1/3] Processing: MainWindow.xaml ✓
[2/3] Processing: UserControl1.xaml ✓
[3/3] Processing: DataTemplate.xaml ✓

Transformation Summary
=====================
Successful: 3
```

### Analyze Command Output

```
WPF to Avalonia XAML Analyzer
=============================

Analyzing 3 file(s)...

[1/3] MainWindow.xaml: ✓
[2/3] UserControl1.xaml: ✓
[3/3] DataTemplate.xaml: 1 warnings ✓

Analysis Summary
================
Files analyzed: 3

Total Errors: 0
Total Warnings: 1
Total Info: 45

Transformations Applied:
  Namespace: 9
  TypeMapping: 6
  PropertyMapping: 3
```

## Architecture

The CLI tool uses the WpfToAvalonia transformation pipeline:

1. **Parse** - Load XAML files and parse to UnifiedAST
2. **Transform** - Apply 8 transformer groups in priority order:
   - NamespaceTransformer (Priority 10)
   - TypeTransformer (Priority 20)
   - PropertyTransformer (Priority 30)
   - BindingTransformations (Priority 40)
   - ResourceTransformer (Priority 45)
   - StyleTransformations (Priority 50)
   - TemplateTransformer (Priority 55)
   - ControlTransformations (Priority 60)
3. **Serialize** - Convert transformed UnifiedAST back to XAML
4. **Write** - Save transformed files to output directory

## Dependencies

- System.CommandLine 2.0.0-beta4.22272.1
- WpfToAvalonia.Core
- WpfToAvalonia.XamlParser

## Testing

All transformation features are covered by 380 passing unit and integration tests.

## Future Enhancements

- Configuration file support (JSON/YAML)
- Rollback mechanism
- Interactive mode with project selection
- Additional output formats (JSON reports)
- Quiet mode for CI/CD integration
