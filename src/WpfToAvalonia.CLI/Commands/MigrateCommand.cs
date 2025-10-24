using System.CommandLine;
using WpfToAvalonia.CLI.Configuration;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Project;
using WpfToAvalonia.Core.Workspace;
using WpfToAvalonia.Mappings;
using WpfToAvalonia.XamlParser;

namespace WpfToAvalonia.CLI.Commands;

/// <summary>
/// CLI command for end-to-end WPF project migration using MigrationOrchestrator.
/// This command orchestrates the complete 7-stage migration pipeline:
/// 1. Analysis - Detect WPF usage and analyze project structure
/// 2. Backup - Create backup of original files (optional)
/// 3. Transform Project File - Convert .csproj to Avalonia
/// 4. Transform XAML Files - Convert all XAML files
/// 5. Transform C# Files - Convert C# code-behind and classes
/// 6. Validation - Verify transformed output
/// 7. Write Files - Write transformed files to disk (unless dry-run)
/// </summary>
public static class MigrateCommand
{
    public static Command Create()
    {
        var command = new Command("migrate", "Migrate entire WPF project to Avalonia using end-to-end orchestration")
        {
            Description = "Orchestrates complete migration with analysis, backup, transformation, and validation stages"
        };

        // Required options
        var projectOption = new Option<string>(
            name: "--project",
            description: "Path to WPF project file (.csproj) to migrate")
        {
            IsRequired = true
        };
        projectOption.AddAlias("-p");

        // Output options
        var outputOption = new Option<string?>(
            name: "--output",
            description: "Output project path (defaults to <project>.Avalonia.csproj)");
        outputOption.AddAlias("-o");

        // Migration behavior options
        var dryRunOption = new Option<bool>(
            name: "--dry-run",
            getDefaultValue: () => false,
            description: "Perform migration analysis without writing files");
        dryRunOption.AddAlias("-d");

        var createBackupOption = new Option<bool>(
            name: "--backup",
            getDefaultValue: () => true,
            description: "Create backup of original files before migration");
        createBackupOption.AddAlias("-b");

        var backupDirOption = new Option<string>(
            name: "--backup-dir",
            getDefaultValue: () => ".migration-backup",
            description: "Backup directory name (relative to project directory)");

        var renameXamlOption = new Option<bool>(
            name: "--rename-xaml",
            getDefaultValue: () => true,
            description: "Rename .xaml files to .axaml");

        var avaloniaVersionOption = new Option<string>(
            name: "--avalonia-version",
            getDefaultValue: () => "11.2.2",
            description: "Avalonia version to use for package references");

        var targetFrameworkOption = new Option<string?>(
            name: "--target-framework",
            description: "Target framework (e.g., net8.0) - leave empty to keep existing");

        var enableCompiledBindingsOption = new Option<bool>(
            name: "--compiled-bindings",
            getDefaultValue: () => true,
            description: "Enable compiled bindings in project file");

        // Display options
        var verboseOption = new Option<bool>(
            name: "--verbose",
            getDefaultValue: () => false,
            description: "Enable verbose output with detailed diagnostics");
        verboseOption.AddAlias("-v");

        var configOption = new Option<string?>(
            name: "--config",
            description: "Configuration file path (auto-detects wpf2avalonia.json if not specified)");
        configOption.AddAlias("-c");

        // Add all options to command
        command.AddOption(projectOption);
        command.AddOption(outputOption);
        command.AddOption(dryRunOption);
        command.AddOption(createBackupOption);
        command.AddOption(backupDirOption);
        command.AddOption(renameXamlOption);
        command.AddOption(avaloniaVersionOption);
        command.AddOption(targetFrameworkOption);
        command.AddOption(enableCompiledBindingsOption);
        command.AddOption(verboseOption);
        command.AddOption(configOption);

        command.SetHandler(async (context) =>
        {
            // Load configuration file if specified
            var configFile = context.ParseResult.GetValueForOption(configOption);
            MigrationConfig? config = null;

            if (configFile != null)
            {
                try
                {
                    config = await ConfigLoader.LoadAsync(configFile);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Loaded configuration from: {configFile}");
                    Console.ResetColor();
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error loading configuration file: {ex.Message}");
                    Console.ResetColor();
                    return;
                }
            }
            else
            {
                // Auto-detect configuration file
                var foundConfig = ConfigLoader.FindConfigFile();
                if (foundConfig != null)
                {
                    try
                    {
                        config = await ConfigLoader.LoadAsync(foundConfig);
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"Found and loaded configuration: {foundConfig}");
                        Console.ResetColor();
                        Console.WriteLine();
                    }
                    catch
                    {
                        // Ignore errors in auto-detected config
                    }
                }
            }

            // Get option values (command-line overrides config file)
            var projectPath = context.ParseResult.GetValueForOption(projectOption)!;
            var outputPath = context.ParseResult.GetValueForOption(outputOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var createBackup = context.ParseResult.GetValueForOption(createBackupOption);
            var backupDir = context.ParseResult.GetValueForOption(backupDirOption)!;
            var renameXaml = context.ParseResult.GetValueForOption(renameXamlOption);
            var avaloniaVersion = context.ParseResult.GetValueForOption(avaloniaVersionOption)!;
            var targetFramework = context.ParseResult.GetValueForOption(targetFrameworkOption);
            var enableCompiledBindings = context.ParseResult.GetValueForOption(enableCompiledBindingsOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            await ExecuteMigrationAsync(
                projectPath,
                outputPath,
                dryRun,
                createBackup,
                backupDir,
                renameXaml,
                avaloniaVersion,
                targetFramework,
                enableCompiledBindings,
                verbose,
                context.GetCancellationToken());
        });

        return command;
    }

    private static async Task ExecuteMigrationAsync(
        string projectPath,
        string? outputPath,
        bool dryRun,
        bool createBackup,
        string backupDir,
        bool renameXaml,
        string avaloniaVersion,
        string? targetFramework,
        bool enableCompiledBindings,
        bool verbose,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("WPF to Avalonia Migration Tool");
        Console.WriteLine("================================");
        Console.WriteLine();

        try
        {
            // Validate project path
            if (!File.Exists(projectPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Project file not found: {projectPath}");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"Project: {projectPath}");
            if (outputPath != null)
            {
                Console.WriteLine($"Output:  {outputPath}");
            }
            Console.WriteLine();

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("DRY RUN MODE - No files will be written");
                Console.ResetColor();
                Console.WriteLine();
            }

            // Initialize services
            var mappingsPath = Path.Combine(
                AppContext.BaseDirectory,
                "Data", "core-mappings.json");

            if (!File.Exists(mappingsPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Mappings file not found: {mappingsPath}");
                Console.ResetColor();
                return;
            }

            var mappingRepository = new JsonMappingRepository(mappingsPath);
            await mappingRepository.LoadAsync();

            var workspaceManager = new MSBuildWorkspaceManager();
            var xamlConverter = new WpfToAvaloniaConverter();
            var projectParser = new ProjectFileParser();

            // Create migration orchestrator
            var orchestrator = new MigrationOrchestrator(
                workspaceManager,
                mappingRepository,
                xamlConverter,
                projectParser);

            // Configure migration options
            var options = new MigrationOptions
            {
                DryRun = dryRun,
                CreateBackups = createBackup && !dryRun, // No backups in dry-run mode
                BackupDirectory = backupDir,
                AvaloniaVersion = avaloniaVersion,
                UpdateTargetFramework = targetFramework != null,
                TargetFramework = targetFramework,
                RenameXamlToAxaml = renameXaml,
                EnableCompiledBindings = enableCompiledBindings,
                OutputProjectPath = outputPath
            };

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Starting migration...");
            Console.ResetColor();
            Console.WriteLine();

            // Execute migration
            var result = await orchestrator.MigrateProjectAsync(projectPath, options, cancellationToken);

            // Display results
            Console.WriteLine();
            DisplayMigrationResult(result, verbose);

            // Display summary
            Console.WriteLine();
            Console.WriteLine("Migration Summary");
            Console.WriteLine("=================");

            var stats = result.GetStatistics();
            Console.WriteLine($"XAML Files:   {stats.SuccessfulXamlFiles}/{stats.TotalXamlFiles} successful");
            Console.WriteLine($"C# Files:     {stats.TotalCSharpFiles} processed");
            Console.WriteLine($"Elapsed Time: {stats.ElapsedTime.TotalSeconds:F1}s");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Warnings: {stats.TotalWarnings}");
            Console.ResetColor();

            if (stats.TotalErrors > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Errors:   {stats.TotalErrors}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Errors:   {stats.TotalErrors}");
                Console.ResetColor();
            }

            Console.WriteLine();

            if (result.Success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                if (dryRun)
                {
                    Console.WriteLine("✓ Migration analysis complete - no errors detected");
                }
                else
                {
                    Console.WriteLine($"✓ Migration complete! Output: {result.TransformedProjectPath}");
                }
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Migration failed - see errors above");
                Console.ResetColor();

                if (result.Exception != null && verbose)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Exception: {result.Exception.Message}");
                    Console.WriteLine($"Stack trace: {result.Exception.StackTrace}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Fatal error: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            Console.ResetColor();
        }
    }

    private static void DisplayMigrationResult(MigrationResult result, bool verbose)
    {
        // Display stages
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Migration Stages");
        Console.WriteLine("================");
        Console.ResetColor();

        foreach (var stage in result.Stages)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("✓ ");
            Console.ResetColor();
            Console.Write($"{stage.Name,-15} ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"({stage.ElapsedTime.TotalMilliseconds:F0}ms) {stage.Description}");
            Console.ResetColor();
        }

        // Display diagnostics if verbose
        if (verbose && result.Diagnostics.Diagnostics.Any())
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Diagnostics");
            Console.WriteLine("===========");
            Console.ResetColor();

            var diagnosticsByFile = result.Diagnostics.Diagnostics
                .GroupBy(d => d.FilePath ?? "(no file)")
                .OrderBy(g => g.Key);

            foreach (var fileGroup in diagnosticsByFile)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"\n{fileGroup.Key}:");
                Console.ResetColor();

                foreach (var diagnostic in fileGroup.Take(10)) // Limit to 10 per file
                {
                    var color = diagnostic.Severity switch
                    {
                        DiagnosticSeverity.Error => ConsoleColor.Red,
                        DiagnosticSeverity.Warning => ConsoleColor.Yellow,
                        DiagnosticSeverity.Info => ConsoleColor.Cyan,
                        _ => ConsoleColor.Gray
                    };

                    Console.ForegroundColor = color;
                    Console.Write($"  [{diagnostic.Severity}] ");
                    Console.ResetColor();
                    Console.Write($"{diagnostic.Code}: {diagnostic.Message}");

                    if (diagnostic.Line > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write($" (Line {diagnostic.Line})");
                        Console.ResetColor();
                    }

                    Console.WriteLine();
                }

                if (fileGroup.Count() > 10)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"  ... and {fileGroup.Count() - 10} more diagnostics");
                    Console.ResetColor();
                }
            }
        }
        else if (result.Diagnostics.ErrorCount > 0 || result.Diagnostics.WarningCount > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Total diagnostics: {result.Diagnostics.ErrorCount} errors, {result.Diagnostics.WarningCount} warnings");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("(Use --verbose to see detailed diagnostics)");
            Console.ResetColor();
        }
    }
}
