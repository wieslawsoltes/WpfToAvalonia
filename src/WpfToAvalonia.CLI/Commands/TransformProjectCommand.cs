using System.CommandLine;
using System.CommandLine.Invocation;
using System.Xml.Linq;
using WpfToAvalonia.CLI.Configuration;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Services;
using WpfToAvalonia.Mappings;
using WpfToAvalonia.XamlParser;
using WpfToAvalonia.XamlParser.Serialization;
using WpfToAvalonia.XamlParser.Transformers;

namespace WpfToAvalonia.CLI.Commands;

public static class TransformProjectCommand
{
    public static Command Create()
    {
        var command = new Command("transform-project", "Transform entire WPF project (XAML + C#) to Avalonia format");

        // Options
        var inputOption = new Option<string>(
            name: "--input",
            description: "Input directory path (project root)")
        {
            IsRequired = true
        };
        inputOption.AddAlias("-i");

        var outputOption = new Option<string?>(
            name: "--output",
            description: "Output directory path (defaults to input directory with '.avalonia' suffix)");
        outputOption.AddAlias("-o");

        var xamlPatternOption = new Option<string>(
            name: "--xaml-pattern",
            getDefaultValue: () => "*.xaml",
            description: "XAML file pattern to match");

        var csharpPatternOption = new Option<string>(
            name: "--csharp-pattern",
            getDefaultValue: () => "*.cs",
            description: "C# file pattern to match");

        var dryRunOption = new Option<bool>(
            name: "--dry-run",
            getDefaultValue: () => false,
            description: "Perform a dry run without writing files");
        dryRunOption.AddAlias("-d");

        var verboseOption = new Option<bool>(
            name: "--verbose",
            getDefaultValue: () => false,
            description: "Enable verbose output");
        verboseOption.AddAlias("-v");

        var excludeOption = new Option<string[]>(
            name: "--exclude",
            getDefaultValue: () => new[] { "obj", "bin", ".vs", ".git", "packages" },
            description: "Directory patterns to exclude from search");
        excludeOption.AddAlias("-e");

        var skipCSharpOption = new Option<bool>(
            name: "--skip-csharp",
            getDefaultValue: () => false,
            description: "Skip C# file transformation (XAML only)");

        var skipXamlOption = new Option<bool>(
            name: "--skip-xaml",
            getDefaultValue: () => false,
            description: "Skip XAML file transformation (C# only)");

        var configOption = new Option<string?>(
            name: "--config",
            description: "Configuration file path (auto-detects wpf2avalonia.json if not specified)");
        configOption.AddAlias("-c");

        command.AddOption(inputOption);
        command.AddOption(outputOption);
        command.AddOption(xamlPatternOption);
        command.AddOption(csharpPatternOption);
        command.AddOption(dryRunOption);
        command.AddOption(verboseOption);
        command.AddOption(excludeOption);
        command.AddOption(skipCSharpOption);
        command.AddOption(skipXamlOption);
        command.AddOption(configOption);

        command.SetHandler(async (context) =>
        {
            var configFile = context.ParseResult.GetValueForOption(configOption);
            MigrationConfig? config = null;

            // Try to load configuration file
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

            // Command-line options override config file
            var input = context.ParseResult.GetValueForOption(inputOption) ?? config?.Input;
            var output = context.ParseResult.GetValueForOption(outputOption) ?? config?.Output;
            var xamlPattern = context.ParseResult.GetValueForOption(xamlPatternOption) ?? config?.XamlPattern ?? "*.xaml";
            var csharpPattern = context.ParseResult.GetValueForOption(csharpPatternOption) ?? config?.CSharpPattern ?? "*.cs";
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption) || (config?.DryRun ?? false);
            var verbose = context.ParseResult.GetValueForOption(verboseOption) || (config?.Verbose ?? false);
            var exclude = context.ParseResult.GetValueForOption(excludeOption) ?? config?.Exclude ?? new[] { "obj", "bin", ".vs", ".git", "packages" };
            var skipCSharp = context.ParseResult.GetValueForOption(skipCSharpOption) || (config?.SkipCSharp ?? false);
            var skipXaml = context.ParseResult.GetValueForOption(skipXamlOption) || (config?.SkipXaml ?? false);

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Input path is required (use --input or set in configuration file)");
                Console.ResetColor();
                return;
            }

            await ExecuteTransformAsync(
                input!, output, xamlPattern!, csharpPattern!,
                dryRun, verbose, exclude!, skipCSharp, skipXaml,
                context.GetCancellationToken());
        });

        return command;
    }

    private static async Task ExecuteTransformAsync(
        string input,
        string? output,
        string xamlPattern,
        string csharpPattern,
        bool dryRun,
        bool verbose,
        string[] excludePatterns,
        bool skipCSharp,
        bool skipXaml,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("WPF to Avalonia Project Transformer");
        Console.WriteLine("===================================");
        Console.WriteLine();

        try
        {
            if (!Directory.Exists(input))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Input directory not found: {input}");
                Console.ResetColor();
                return;
            }

            // Determine output directory
            var outputDir = output ?? input + ".avalonia";

            Console.WriteLine($"Input:  {input}");
            Console.WriteLine($"Output: {outputDir}");
            Console.WriteLine();

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("DRY RUN MODE - No files will be written");
                Console.ResetColor();
                Console.WriteLine();
            }

            // Statistics
            var totalSuccess = 0;
            var totalFailure = 0;

            // Initialize services
            var mappingsPath = Path.Combine(
                AppContext.BaseDirectory,
                "Data", "core-mappings.json");

            var mappingRepository = new JsonMappingRepository(mappingsPath);
            await mappingRepository.LoadAsync();

            var csharpConverter = new CSharpConverterService(mappingRepository);

            // Transform XAML files
            if (!skipXaml)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Phase 1: Transforming XAML Files");
                Console.WriteLine("=================================");
                Console.ResetColor();
                Console.WriteLine();

                var (success, failure) = await TransformXamlFilesAsync(
                    input, outputDir, xamlPattern, excludePatterns,
                    dryRun, verbose, cancellationToken);

                totalSuccess += success;
                totalFailure += failure;

                Console.WriteLine();
            }

            // Transform C# files
            if (!skipCSharp)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Phase 2: Transforming C# Files");
                Console.WriteLine("===============================");
                Console.ResetColor();
                Console.WriteLine();

                var (success, failure) = await TransformCSharpFilesAsync(
                    input, outputDir, csharpPattern, excludePatterns,
                    csharpConverter, dryRun, verbose, cancellationToken);

                totalSuccess += success;
                totalFailure += failure;

                Console.WriteLine();
            }

            // Overall summary
            Console.WriteLine("Overall Transformation Summary");
            Console.WriteLine("==============================");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Total Successful: {totalSuccess}");
            Console.ResetColor();

            if (totalFailure > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Total Failed: {totalFailure}");
                Console.ResetColor();
            }

            if (dryRun)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("DRY RUN COMPLETE - No files were written");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Project transformation complete! Output: {outputDir}");
                Console.ResetColor();
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

    private static async Task<(int success, int failure)> TransformXamlFilesAsync(
        string input,
        string output,
        string pattern,
        string[] excludePatterns,
        bool dryRun,
        bool verbose,
        CancellationToken cancellationToken)
    {
        var files = GetFilteredFiles(input, pattern, excludePatterns);

        if (files.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"No XAML files found matching pattern: {pattern}");
            Console.ResetColor();
            return (0, 0);
        }

        Console.WriteLine($"Found {files.Count} XAML file(s)");
        Console.WriteLine();

        var successCount = 0;
        var failureCount = 0;

        for (var i = 0; i < files.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var file = files[i];
            var progress = $"[{i + 1}/{files.Count}]";
            Console.Write($"{progress} {Path.GetFileName(file)}");

            try
            {
                var xamlContent = await File.ReadAllTextAsync(file, cancellationToken);
                var diagnostics = new DiagnosticCollector();
                var parser = new UnifiedXamlParser(diagnostics);
                var document = parser.Parse(xamlContent, file);

                var pipeline = TransformationPipeline.CreateDefault();
                pipeline.Transform(document, diagnostics);

                var serializer = new UnifiedAstSerializer(diagnostics, new SerializationOptions
                {
                    IncludeDiagnosticComments = verbose
                });
                var transformedXaml = serializer.SerializeToString(document);

                var relativePath = Path.GetRelativePath(input, file);
                var outputPath = Path.Combine(output, relativePath);

                if (!dryRun)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                    await File.WriteAllTextAsync(outputPath, transformedXaml, cancellationToken);
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" ✓");
                Console.ResetColor();

                if (verbose)
                {
                    Console.WriteLine($"  Diagnostics: {diagnostics.ErrorCount} errors, {diagnostics.WarningCount} warnings, {diagnostics.InfoCount} info");
                }

                successCount++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" ✗");
                Console.WriteLine($"  Error: {ex.Message}");
                Console.ResetColor();
                failureCount++;
            }
        }

        return (successCount, failureCount);
    }

    private static async Task<(int success, int failure)> TransformCSharpFilesAsync(
        string input,
        string output,
        string pattern,
        string[] excludePatterns,
        CSharpConverterService converter,
        bool dryRun,
        bool verbose,
        CancellationToken cancellationToken)
    {
        var files = GetFilteredFiles(input, pattern, excludePatterns);

        if (files.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"No C# files found matching pattern: {pattern}");
            Console.ResetColor();
            return (0, 0);
        }

        Console.WriteLine($"Found {files.Count} C# file(s)");
        Console.WriteLine();

        var successCount = 0;
        var failureCount = 0;

        for (var i = 0; i < files.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var file = files[i];
            var progress = $"[{i + 1}/{files.Count}]";
            Console.Write($"{progress} {Path.GetFileName(file)}");

            try
            {
                var sourceCode = await File.ReadAllTextAsync(file, cancellationToken);
                var diagnostics = new DiagnosticCollector();
                var transformedCode = await converter.ConvertAsync(sourceCode, diagnostics, cancellationToken);

                var relativePath = Path.GetRelativePath(input, file);
                var outputPath = Path.Combine(output, relativePath);

                if (!dryRun)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                    await File.WriteAllTextAsync(outputPath, transformedCode, cancellationToken);
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(" ✓");
                Console.ResetColor();

                if (verbose)
                {
                    Console.WriteLine($"  Diagnostics: {diagnostics.ErrorCount} errors, {diagnostics.WarningCount} warnings, {diagnostics.InfoCount} info");
                }

                successCount++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" ✗");
                Console.WriteLine($"  Error: {ex.Message}");
                Console.ResetColor();
                failureCount++;
            }
        }

        return (successCount, failureCount);
    }

    private static List<string> GetFilteredFiles(string input, string pattern, string[] excludePatterns)
    {
        var allFiles = Directory.GetFiles(input, pattern, SearchOption.AllDirectories);
        return allFiles.Where(file =>
        {
            var relativePath = Path.GetRelativePath(input, file);
            return !excludePatterns.Any(excludePattern =>
                relativePath.Contains(Path.DirectorySeparatorChar + excludePattern + Path.DirectorySeparatorChar) ||
                relativePath.StartsWith(excludePattern + Path.DirectorySeparatorChar));
        }).ToList();
    }
}
