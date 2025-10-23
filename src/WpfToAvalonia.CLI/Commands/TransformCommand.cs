using System.CommandLine;
using System.CommandLine.Invocation;
using System.Xml.Linq;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser;
using WpfToAvalonia.XamlParser.Serialization;
using WpfToAvalonia.XamlParser.Transformers;

namespace WpfToAvalonia.CLI.Commands;

public static class TransformCommand
{
    public static Command Create()
    {
        var command = new Command("transform", "Transform WPF XAML files to Avalonia format");

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
            getDefaultValue: () => "*.xaml",
            description: "File pattern to match (e.g., *.xaml)");
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

        command.AddOption(inputOption);
        command.AddOption(outputOption);
        command.AddOption(patternOption);
        command.AddOption(recursiveOption);
        command.AddOption(dryRunOption);
        command.AddOption(verboseOption);

        command.SetHandler(async (context) =>
        {
            var input = context.ParseResult.GetValueForOption(inputOption)!;
            var output = context.ParseResult.GetValueForOption(outputOption);
            var pattern = context.ParseResult.GetValueForOption(patternOption)!;
            var recursive = context.ParseResult.GetValueForOption(recursiveOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            await ExecuteTransformAsync(input, output, pattern, recursive, dryRun, verbose, context.GetCancellationToken());
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
        CancellationToken cancellationToken)
    {
        Console.WriteLine("WPF to Avalonia XAML Transformer");
        Console.WriteLine("================================");
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
                filesToTransform.AddRange(Directory.GetFiles(input, pattern, searchOption));
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

            // Transform files
            var successCount = 0;
            var failureCount = 0;
            var warnings = new List<string>();

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
                    // Read XAML file
                    var xamlContent = await File.ReadAllTextAsync(file, cancellationToken);
                    var xdoc = XDocument.Parse(xamlContent);

                    // Parse to UnifiedAST
                    var diagnostics = new DiagnosticCollector();
                    var parser = new UnifiedXamlParser(diagnostics);
                    var document = parser.Parse(xamlContent, file);

                    // Transform
                    var pipeline = TransformationPipeline.CreateDefault();
                    var context = pipeline.Transform(document, diagnostics);

                    // Serialize
                    var serializer = new UnifiedAstSerializer(diagnostics, new SerializationOptions
                    {
                        IncludeDiagnosticComments = verbose
                    });
                    var transformedXaml = serializer.SerializeToString(document);

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
                        await File.WriteAllTextAsync(outputPath, transformedXaml, cancellationToken);
                    }

                    // Report success
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(" ✓");
                    Console.ResetColor();

                    successCount++;

                    // Show diagnostics if verbose
                    if (verbose)
                    {
                        var errors = diagnostics.ErrorCount;
                        var warns = diagnostics.WarningCount;
                        var infos = diagnostics.InfoCount;

                        Console.WriteLine($"  Diagnostics: {errors} errors, {warns} warnings, {infos} info");
                        Console.WriteLine($"  Output: {outputPath}");
                    }

                    // Collect warnings
                    foreach (var warning in diagnostics.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning))
                    {
                        warnings.Add($"{Path.GetFileName(file)}: {warning.Message}");
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

            if (warnings.Count > 0 && verbose)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warnings ({warnings.Count}):");
                Console.ResetColor();
                foreach (var warning in warnings.Take(10))
                {
                    Console.WriteLine($"  - {warning}");
                }
                if (warnings.Count > 10)
                {
                    Console.WriteLine($"  ... and {warnings.Count - 10} more");
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
            Console.ResetColor();
        }
    }
}
