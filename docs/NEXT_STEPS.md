# WPF to Avalonia Migration Tool - Next Steps

**Last Updated**: October 23, 2025

## 🎯 Current Status

### What's Working ✅
- ✅ **XAML Transformation**: 117 tests passing, comprehensive WPF→Avalonia XAML conversion
- ✅ **Project File Transformation**: Complete .csproj transformation with MSBuild APIs
- ✅ **Playground App**: Interactive desktop application for testing transformations
- ✅ **Migration Orchestrator**: 70% complete, architecture and pipeline designed
- ✅ **Diagnostics System**: Comprehensive error/warning/info collection
- ✅ **Backup System**: Automatic file backup before transformation

### What Needs Work ⚠️
- ⚠️ **MigrationOrchestrator Compilation**: Several API mismatches need fixing
- ⚠️ **C# Transformation**: Currently stubbed, not implemented
- ⚠️ **CLI Integration**: Migration orchestrator not yet wired to CLI
- ⚠️ **Integration Tests**: End-to-end migration tests needed
- ⚠️ **Reporting**: No HTML/JSON report generation yet

## 📋 Immediate Next Steps (Priority Order)

### 1. Fix MigrationOrchestrator Compilation Errors ⭐ HIGH PRIORITY
**File**: `src/WpfToAvalonia.XamlParser/MigrationOrchestrator.cs`

**Issues to Fix**:
```csharp
// Line 110: DiagnosticCollector.HasErrors doesn't exist
result.Success = !result.Diagnostics.HasErrors;
// FIX: Use ErrorCount instead
result.Success = result.Diagnostics.ErrorCount == 0;

// Line 280: TransformedProjectInfo.Diagnostics doesn't exist
result.Diagnostics.AddRange(projectTransformResult.Diagnostics);
// FIX: Access diagnostics from the collector, not the result

// Line 288: Type mismatch - Core.Project.TransformedProjectInfo vs XamlParser.TransformedProjectInfo
// FIX: Map between the two types or use a common base

// Line 310-312: ConversionResult.TransformedXaml doesn't exist
TransformedContent = converted.TransformedXaml
// FIX: Check actual property name in ConversionResult class

// Lines 326, 353: DiagnosticSeverity ambiguity
// FIX: Fully qualify as Core.Diagnostics.DiagnosticSeverity
```

**Estimated Time**: 1-2 hours

---

### 2. Create Integration Test for End-to-End Migration ⭐ HIGH PRIORITY
**File**: `tests/WpfToAvalonia.Tests/IntegrationTests/EndToEndMigrationTests.cs` (NEW)

**Test Scenario**:
```csharp
[Fact]
public async Task MigrateSimpleWpfProject_ShouldSucceed()
{
    // Create sample WPF project
    var wpfProject = CreateSampleWpfProject();

    // Create orchestrator
    var orchestrator = new MigrationOrchestrator(...);

    // Migrate
    var result = await orchestrator.MigrateProjectAsync(
        wpfProject.Path,
        new MigrationOptions { DryRun = false }
    );

    // Assert
    result.Success.Should().BeTrue();
    result.XamlTransformationResults.Should().NotBeEmpty();
    File.Exists(result.TransformedProjectPath).Should().BeTrue();
}
```

**Estimated Time**: 2-3 hours

---

### 3. Wire MigrationOrchestrator to CLI ⭐ HIGH PRIORITY
**File**: `src/WpfToAvalonia.CLI/Commands/MigrateCommand.cs` (NEW)

**Implementation**:
```csharp
public class MigrateCommand
{
    [Command("migrate")]
    public async Task<int> ExecuteAsync(
        [Argument] string projectPath,
        [Option("--dry-run")] bool dryRun = false,
        [Option("--backup-dir")] string backupDir = ".migration-backup",
        [Option("--avalonia-version")] string avaloniaVersion = "11.2.2")
    {
        var orchestrator = CreateOrchestrator();
        var options = new MigrationOptions
        {
            DryRun = dryRun,
            BackupDirectory = backupDir,
            AvaloniaVersion = avaloniaVersion
        };

        var result = await orchestrator.MigrateProjectAsync(projectPath, options);

        PrintResults(result);
        return result.Success ? 0 : 1;
    }
}
```

**Estimated Time**: 2-3 hours

---

## 🚀 Short-Term Goals (This Week)

### 4. Add HTML Report Generation
**File**: `src/WpfToAvalonia.Core/Reporting/HtmlReportGenerator.cs` (NEW)

**Features**:
- Summary statistics (files processed, errors, warnings)
- File-by-file transformation details
- Diagnostic messages with severity colors
- Manual review checklist
- Timestamp and duration

**Estimated Time**: 4-6 hours

---

### 5. Add JSON Report Output
**File**: `src/WpfToAvalonia.Core/Reporting/JsonReportGenerator.cs` (NEW)

**Use Case**: CI/CD integration, automated testing

**Format**:
```json
{
  "success": true,
  "projectPath": "/path/to/project.csproj",
  "statistics": {
    "totalXamlFiles": 15,
    "successfulXamlFiles": 15,
    "errors": 0,
    "warnings": 3
  },
  "diagnostics": [...]
}
```

**Estimated Time**: 2-3 hours

---

### 6. Implement Git Integration
**File**: `src/WpfToAvalonia.Core/Git/GitManager.cs` (NEW)

**Features**:
- Auto-detect git repository (check for .git folder)
- Create migration branch (`git checkout -b wpf-to-avalonia-migration`)
- Stage changes (`git add .`)
- Generate commit message template

**Estimated Time**: 3-4 hours

---

## 🎓 Medium-Term Goals (Next 2 Weeks)

### 7. Implement C# Using Directive Transformation
**File**: `src/WpfToAvalonia.Core/Transformers/CSharp/UsingDirectiveRewriter.cs`

**Mappings**:
```
System.Windows → Avalonia
System.Windows.Controls → Avalonia.Controls
System.Windows.Data → Avalonia.Data
System.Windows.Input → Avalonia.Input
```

**Estimated Time**: 1-2 days

---

### 8. Add Progress Reporting
**Enhancement**: Real-time progress updates during migration

**Implementation**:
```csharp
public interface IProgress<T>
{
    void Report(T value);
}

// Usage
var progress = new Progress<MigrationProgress>(p => {
    Console.WriteLine($"[{p.Stage}] {p.Message} ({p.Percentage}%)");
});

await orchestrator.MigrateProjectAsync(path, options, progress);
```

**Estimated Time**: 1 day

---

### 9. Configuration File Support
**File**: `.wpf-to-avalonia.json`

**Format**:
```json
{
  "avaloniaVersion": "11.2.2",
  "targetFramework": "net8.0",
  "renameXamlToAxaml": true,
  "enableCompiledBindings": true,
  "customMappings": {
    "MyCustomControl": "MyAvaloniaControl"
  }
}
```

**Estimated Time**: 1 day

---

## 📊 Success Metrics

### Phase 1: Make It Work (This Week)
- [ ] MigrationOrchestrator compiles without errors
- [ ] At least 1 integration test passes
- [ ] CLI `migrate` command works for simple WPF project
- [ ] HTML report generates successfully

### Phase 2: Make It Better (Next 2 Weeks)
- [ ] 5+ integration tests covering various project types
- [ ] JSON report output working
- [ ] Git integration functional
- [ ] Basic C# transformation implemented
- [ ] Progress reporting in CLI

### Phase 3: Make It Production-Ready (Next Month)
- [ ] Handle 10+ real-world WPF projects successfully
- [ ] Comprehensive error handling and validation
- [ ] User documentation complete
- [ ] Performance optimization (parallel file processing)

---

## 🎯 Recommended Focus

**START HERE**: Fix compilation errors in MigrationOrchestrator

**WHY**: This unblocks everything else. Once the orchestrator compiles, you can:
1. Test it with real projects
2. Add it to the CLI
3. Iterate on improvements based on actual usage

**QUICK WIN**: After fixing compilation, create one simple integration test that migrates a basic WPF project (just Window + Button). This proves the end-to-end flow works.

**THEN**: Add CLI integration so you can actually use the tool from command line.

---

## 📞 Questions to Consider

1. **Should we support incremental migration?** (Migrate one file at a time vs all at once)
2. **How to handle third-party WPF controls?** (Custom mapping file? Plugin system?)
3. **Should we generate a migration report automatically?** (HTML by default?)
4. **Git integration: Auto-commit or just stage changes?**
5. **Should C# transformation be optional?** (Some users might want XAML-only)

---

## 🔗 Related Files

- Migration Plan: `docs/MIGRATION_PLAN.md`
- Status Document: `docs/STATUS.md`
- MigrationOrchestrator: `src/WpfToAvalonia.XamlParser/MigrationOrchestrator.cs`
- MigrationModels: `src/WpfToAvalonia.XamlParser/MigrationModels.cs`
- ProjectFileParser: `src/WpfToAvalonia.Core/Project/ProjectFileParser.cs`
- ProjectFileTransformer: `src/WpfToAvalonia.Core/Project/ProjectFileTransformer.cs`
