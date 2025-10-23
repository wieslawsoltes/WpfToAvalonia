using System.CommandLine;
using WpfToAvalonia.CLI.Configuration;

namespace WpfToAvalonia.CLI.Commands;

public static class ConfigCommand
{
    public static Command Create()
    {
        var command = new Command("config", "Manage migration configuration files");

        // Add subcommands
        command.AddCommand(CreateInitCommand());
        command.AddCommand(CreateShowCommand());
        command.AddCommand(CreateValidateCommand());

        return command;
    }

    private static Command CreateInitCommand()
    {
        var command = new Command("init", "Create a new migration configuration file");

        var outputOption = new Option<string>(
            name: "--output",
            getDefaultValue: () => "wpf2avalonia.json",
            description: "Output file path for the configuration");
        outputOption.AddAlias("-o");

        var templateOption = new Option<string>(
            name: "--template",
            getDefaultValue: () => "default",
            description: "Configuration template (default, xaml-only, csharp-only, incremental)");
        templateOption.AddAlias("-t");

        var forceOption = new Option<bool>(
            name: "--force",
            getDefaultValue: () => false,
            description: "Overwrite existing configuration file");
        forceOption.AddAlias("-f");

        command.AddOption(outputOption);
        command.AddOption(templateOption);
        command.AddOption(forceOption);

        command.SetHandler(async (context) =>
        {
            var output = context.ParseResult.GetValueForOption(outputOption)!;
            var template = context.ParseResult.GetValueForOption(templateOption)!;
            var force = context.ParseResult.GetValueForOption(forceOption);

            await ExecuteInitAsync(output, template, force, context.GetCancellationToken());
        });

        return command;
    }

    private static Command CreateShowCommand()
    {
        var command = new Command("show", "Display current configuration");

        var fileOption = new Option<string?>(
            name: "--file",
            description: "Configuration file path (auto-detects if not specified)");
        fileOption.AddAlias("-f");

        command.AddOption(fileOption);

        command.SetHandler((context) =>
        {
            var file = context.ParseResult.GetValueForOption(fileOption);
            ExecuteShow(file);
        });

        return command;
    }

    private static Command CreateValidateCommand()
    {
        var command = new Command("validate", "Validate a configuration file");

        var fileOption = new Option<string?>(
            name: "--file",
            description: "Configuration file path (auto-detects if not specified)");
        fileOption.AddAlias("-f");

        command.AddOption(fileOption);

        command.SetHandler(async (context) =>
        {
            var file = context.ParseResult.GetValueForOption(fileOption);
            await ExecuteValidateAsync(file, context.GetCancellationToken());
        });

        return command;
    }

    private static async Task ExecuteInitAsync(
        string output,
        string template,
        bool force,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("WPF to Avalonia - Configuration Generator");
        Console.WriteLine("=========================================");
        Console.WriteLine();

        // Check if file exists
        if (File.Exists(output) && !force)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Configuration file already exists: {output}");
            Console.ResetColor();
            Console.WriteLine("Use --force to overwrite.");
            return;
        }

        // Create configuration from template
        MigrationConfig config = template.ToLowerInvariant() switch
        {
            "xaml-only" => MigrationConfig.CreateXamlOnly(),
            "csharp-only" => MigrationConfig.CreateCSharpOnly(),
            "incremental" => MigrationConfig.CreateIncremental(),
            _ => MigrationConfig.CreateDefault()
        };

        // Save configuration
        try
        {
            await ConfigLoader.SaveAsync(config, output);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Configuration file created: {output}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine($"Template: {template}");
            Console.WriteLine();
            Console.WriteLine("Edit the configuration file to customize your migration settings.");
            Console.WriteLine("Then run: dotnet run --project src/WpfToAvalonia.CLI -- transform-project --config " + output);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error creating configuration file: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void ExecuteShow(string? file)
    {
        Console.WriteLine("WPF to Avalonia - Configuration Viewer");
        Console.WriteLine("======================================");
        Console.WriteLine();

        try
        {
            // Find or load configuration
            var configPath = file ?? ConfigLoader.FindConfigFile();

            if (configPath == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No configuration file found.");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Searched for:");
                Console.WriteLine("  - wpf2avalonia.json");
                Console.WriteLine("  - .wpf2avalonia.json");
                Console.WriteLine("  - wpf-to-avalonia.json");
                Console.WriteLine("  - migration.config.json");
                Console.WriteLine();
                Console.WriteLine("Run 'config init' to create a new configuration file.");
                return;
            }

            var config = ConfigLoader.Load(configPath);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Configuration File: {configPath}");
            Console.ResetColor();
            Console.WriteLine();

            // Display configuration
            Console.WriteLine("Settings:");
            Console.WriteLine($"  Input:                {config.Input ?? "(not set)"}");
            Console.WriteLine($"  Output:               {config.Output ?? "(not set)"}");
            Console.WriteLine($"  XAML Pattern:         {config.XamlPattern}");
            Console.WriteLine($"  C# Pattern:           {config.CSharpPattern}");
            Console.WriteLine($"  Recursive:            {config.Recursive}");
            Console.WriteLine($"  Dry Run:              {config.DryRun}");
            Console.WriteLine($"  Verbose:              {config.Verbose}");
            Console.WriteLine($"  Skip C#:              {config.SkipCSharp}");
            Console.WriteLine($"  Skip XAML:            {config.SkipXaml}");
            Console.WriteLine($"  Diagnostic Comments:  {config.IncludeDiagnosticComments}");
            Console.WriteLine($"  Exclude:              {string.Join(", ", config.Exclude)}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error loading configuration: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static async Task ExecuteValidateAsync(string? file, CancellationToken cancellationToken)
    {
        Console.WriteLine("WPF to Avalonia - Configuration Validator");
        Console.WriteLine("=========================================");
        Console.WriteLine();

        try
        {
            // Find or load configuration
            var configPath = file ?? ConfigLoader.FindConfigFile();

            if (configPath == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No configuration file found.");
                Console.ResetColor();
                return;
            }

            Console.WriteLine($"Validating: {configPath}");
            Console.WriteLine();

            var config = await ConfigLoader.LoadAsync(configPath);

            // Validate configuration
            var errors = new List<string>();
            var warnings = new List<string>();

            // Check for required fields
            if (string.IsNullOrWhiteSpace(config.Input))
            {
                warnings.Add("Input path is not set - will need to be specified via command line");
            }

            // Check if input exists (if specified)
            if (!string.IsNullOrWhiteSpace(config.Input))
            {
                if (!File.Exists(config.Input) && !Directory.Exists(config.Input))
                {
                    warnings.Add($"Input path does not exist: {config.Input}");
                }
            }

            // Check for conflicting options
            if (config.SkipCSharp && config.SkipXaml)
            {
                errors.Add("Cannot skip both C# and XAML transformation");
            }

            // Display results
            if (errors.Count == 0 && warnings.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ Configuration is valid");
                Console.ResetColor();
            }
            else
            {
                if (errors.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Errors ({errors.Count}):");
                    Console.ResetColor();
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"  - {error}");
                    }
                    Console.WriteLine();
                }

                if (warnings.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Warnings ({warnings.Count}):");
                    Console.ResetColor();
                    foreach (var warning in warnings)
                    {
                        Console.WriteLine($"  - {warning}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error validating configuration: {ex.Message}");
            Console.ResetColor();
        }
    }
}
