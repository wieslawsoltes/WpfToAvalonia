using System.CommandLine;
using System.CommandLine.Invocation;
using System.Xml.Linq;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser;
using WpfToAvalonia.XamlParser.Transformers;

namespace WpfToAvalonia.CLI.Commands;

public static class AnalyzeCommand
{
    public static Command Create()
    {
        var command = new Command("analyze", "Analyze WPF XAML files and report transformation details");

        // Options
        var inputOption = new Option<string>(
            name: "--input",
            description: "Input file or directory path")
        {
            IsRequired = true
        };
        inputOption.AddAlias("-i");

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

        command.AddOption(inputOption);
        command.AddOption(patternOption);
        command.AddOption(recursiveOption);

        command.SetHandler(async (context) =>
        {
            var input = context.ParseResult.GetValueForOption(inputOption)!;
            var pattern = context.ParseResult.GetValueForOption(patternOption)!;
            var recursive = context.ParseResult.GetValueForOption(recursiveOption);

            await ExecuteAnalyzeAsync(input, pattern, recursive, context.GetCancellationToken());
        });

        return command;
    }

    private static async Task ExecuteAnalyzeAsync(
        string input,
        string pattern,
        bool recursive,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("WPF to Avalonia XAML Analyzer");
        Console.WriteLine("=============================");
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

            // Collect files to analyze
            var filesToAnalyze = new List<string>();

            if (isFile)
            {
                filesToAnalyze.Add(input);
            }
            else
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                filesToAnalyze.AddRange(Directory.GetFiles(input, pattern, searchOption));
            }

            if (filesToAnalyze.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: No files found matching pattern: {pattern}");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"Analyzing {filesToAnalyze.Count} file(s)...");
            Console.WriteLine();

            // Analysis statistics
            var totalErrors = 0;
            var totalWarnings = 0;
            var totalInfos = 0;
            var transformationCounts = new Dictionary<string, int>();

            for (var i = 0; i < filesToAnalyze.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Analysis cancelled by user");
                    Console.ResetColor();
                    break;
                }

                var file = filesToAnalyze[i];
                var fileName = Path.GetFileName(file);

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

                    // Collect statistics
                    var errors = diagnostics.ErrorCount;
                    var warnings = diagnostics.WarningCount;
                    var infos = diagnostics.InfoCount;

                    totalErrors += errors;
                    totalWarnings += warnings;
                    totalInfos += infos;

                    // Collect transformation counts
                    foreach (var (key, value) in context.Statistics.TransformationCounts)
                    {
                        if (!transformationCounts.ContainsKey(key))
                        {
                            transformationCounts[key] = 0;
                        }
                        transformationCounts[key] += value;
                    }

                    // Show file summary
                    Console.Write($"[{i + 1}/{filesToAnalyze.Count}] {fileName}: ");

                    if (errors > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"{errors} errors ");
                        Console.ResetColor();
                    }

                    if (warnings > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"{warnings} warnings ");
                        Console.ResetColor();
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{i + 1}/{filesToAnalyze.Count}] {fileName}: ✗ Error: {ex.Message}");
                    Console.ResetColor();
                    totalErrors++;
                }
            }

            // Summary
            Console.WriteLine();
            Console.WriteLine("Analysis Summary");
            Console.WriteLine("================");
            Console.WriteLine($"Files analyzed: {filesToAnalyze.Count}");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Total Errors: {totalErrors}");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Total Warnings: {totalWarnings}");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Total Info: {totalInfos}");
            Console.ResetColor();

            if (transformationCounts.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Transformations Applied:");
                foreach (var (type, count) in transformationCounts.OrderByDescending(x => x.Value).Take(15))
                {
                    Console.WriteLine($"  {type}: {count}");
                }
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
