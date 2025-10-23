using System.CommandLine;
using System.CommandLine.Invocation;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Services;
using WpfToAvalonia.Mappings;

namespace WpfToAvalonia.CLI.Commands;

public static class TransformCSharpCommand
{
    public static Command Create()
    {
        var command = new Command("transform-csharp", "Transform WPF C# files to Avalonia format");

        // Options
        var inputOption = new Option<string>(
            name: "--input",
            description: "Input file or directory path")
        {
            IsRequired = true
        };
        inputOption.AddAlias("-i");

        var outputOption = new Option<string?>(
            name: "--output",
            description: "Output directory path (defaults to input directory with '.avalonia' suffix)");
        outputOption.AddAlias("-o");

        var patternOption = new Option<string>(
            name: "--pattern",
            getDefaultValue: () => "*.cs",
            description: "File pattern to match (e.g., *.cs)");
        patternOption.AddAlias("-p");

        var recursiveOption = new Option<bool>(
            name: "--recursive",
            getDefaultValue: () => true,
            description: "Search directories recursively");
        recursiveOption.AddAlias("-r");

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
            getDefaultValue: () => new[] { "obj", "bin", ".vs", ".git" },
            description: "Directory patterns to exclude from search");
        excludeOption.AddAlias("-e");

        command.AddOption(inputOption);
        command.AddOption(outputOption);
        command.AddOption(patternOption);
        command.AddOption(recursiveOption);
        command.AddOption(dryRunOption);
        command.AddOption(verboseOption);
        command.AddOption(excludeOption);

        command.SetHandler(async (context) =>
        {
            var input = context.ParseResult.GetValueForOption(inputOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption);
            var pattern = context.ParseResult.GetValueForOption(patternOption)!;
            var recursive = context.ParseResult.GetValueForOption(recursiveOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);
            var exclude = context.ParseResult.GetValueForOption(excludeOption)!;

            await ExecuteTransformAsync(input, output, pattern, recursive, dryRun, verbose, exclude, context.GetCancellationToken());
        });

        return command;
    }

    private static async Task ExecuteTransformAsync(
        string input,
        string? output,
        string pattern,
        bool recursive,
        bool dryRun,
        bool verbose,
        string[] excludePatterns,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("WPF to Avalonia C# Transformer");
        Console.WriteLine("==============================");
        Console.WriteLine();

        try
        {
            // Determine if input is file or directory
            var isFile = File.Exists(input);
            var isDirectory = Directory.Exists(input);

            if (!isFile && !isDirectory)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Input path not found: {input}");
                Console.ResetColor();
                return;
            }

            // Collect files to transform
            var filesToTransform = new List<string>();

            if (isFile)
            {
                filesToTransform.Add(input);
            }
            else
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var allFiles = Directory.GetFiles(input, pattern, searchOption);

                // Filter out excluded directories
                filesToTransform.AddRange(allFiles.Where(file =>
                {
                    var relativePath = Path.GetRelativePath(input, file);
                    return !excludePatterns.Any(pattern =>
                        relativePath.Contains(Path.DirectorySeparatorChar + pattern + Path.DirectorySeparatorChar) ||
                        relativePath.StartsWith(pattern + Path.DirectorySeparatorChar));
                }));
            }

            if (filesToTransform.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: No files found matching pattern: {pattern}");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"Found {filesToTransform.Count} file(s) to transform");
            Console.WriteLine();

            if (dryRun)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("DRY RUN MODE - No files will be written");
                Console.ResetColor();
                Console.WriteLine();
            }

            // Initialize C# converter service
            var mappingsPath = Path.Combine(
                AppContext.BaseDirectory,
                "Data", "core-mappings.json");

            var mappingRepository = new JsonMappingRepository(mappingsPath);
            await mappingRepository.LoadAsync();

            var converterService = new CSharpConverterService(mappingRepository);

            // Transform files
            var successCount = 0;
            var failureCount = 0;
            var totalErrors = 0;
            var totalWarnings = 0;
            var totalInfo = 0;
            var transformationStats = new Dictionary<string, int>();

            for (var i = 0; i < filesToTransform.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Transformation cancelled by user");
                    Console.ResetColor();
                    break;
                }

                var file = filesToTransform[i];
                var progress = $"[{i + 1}/{filesToTransform.Count}]";

                Console.Write($"{progress} Processing: {Path.GetFileName(file)}");

                try
                {
                    // Read C# file
                    var sourceCode = await File.ReadAllTextAsync(file, cancellationToken);

                    // Transform
                    var diagnostics = new DiagnosticCollector();
                    var transformedCode = await converterService.ConvertAsync(sourceCode, diagnostics, cancellationToken);

                    // Determine output path
                    string outputPath;
                    if (output != null)
                    {
                        var relativePath = isDirectory
                            ? Path.GetRelativePath(input, file)
                            : Path.GetFileName(file);
                        outputPath = Path.Combine(output, relativePath);
                    }
                    else if (isDirectory)
                    {
                        var outputDir = input + ".avalonia";
                        var relativePath = Path.GetRelativePath(input, file);
                        outputPath = Path.Combine(outputDir, relativePath);
                    }
                    else
                    {
                        var dir = Path.GetDirectoryName(file) ?? ".";
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var ext = Path.GetExtension(file);
                        outputPath = Path.Combine(dir, $"{fileName}.avalonia{ext}");
                    }

                    // Write file
                    if (!dryRun)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                        await File.WriteAllTextAsync(outputPath, transformedCode, cancellationToken);
                    }

                    // Report success
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(" ✓");
                    Console.ResetColor();

                    successCount++;

                    // Collect statistics
                    totalErrors += diagnostics.ErrorCount;
                    totalWarnings += diagnostics.WarningCount;
                    totalInfo += diagnostics.InfoCount;

                    foreach (var diag in diagnostics.Diagnostics)
                    {
                        if (!string.IsNullOrEmpty(diag.Code))
                        {
                            if (transformationStats.ContainsKey(diag.Code))
                                transformationStats[diag.Code]++;
                            else
                                transformationStats[diag.Code] = 1;
                        }
                    }

                    // Show diagnostics if verbose
                    if (verbose)
                    {
                        Console.WriteLine($"  Diagnostics: {diagnostics.ErrorCount} errors, {diagnostics.WarningCount} warnings, {diagnostics.InfoCount} info");
                        Console.WriteLine($"  Output: {outputPath}");

                        if (diagnostics.ErrorCount > 0)
                        {
                            foreach (var error in diagnostics.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"    Error: {error.Message}");
                                Console.ResetColor();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(" ✗");
                    Console.WriteLine($"  Error: {ex.Message}");
                    Console.ResetColor();
                    failureCount++;

                    if (verbose)
                    {
                        Console.WriteLine($"  Stack trace: {ex.StackTrace}");
                    }
                }
            }

            // Summary
            Console.WriteLine();
            Console.WriteLine("Transformation Summary");
            Console.WriteLine("=====================");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Successful: {successCount}");
            Console.ResetColor();

            if (failureCount > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed: {failureCount}");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.WriteLine($"Total Errors: {totalErrors}");
            Console.WriteLine($"Total Warnings: {totalWarnings}");
            Console.WriteLine($"Total Info: {totalInfo}");

            if (transformationStats.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Transformations Applied:");
                var topTransforms = transformationStats
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(15);

                foreach (var (code, count) in topTransforms)
                {
                    Console.WriteLine($"  {code}: {count}");
                }
            }

            if (dryRun)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("DRY RUN COMPLETE - No files were written");
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
}
