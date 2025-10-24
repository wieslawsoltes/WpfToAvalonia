using System.CommandLine;
using WpfToAvalonia.CLI.Commands;

namespace WpfToAvalonia.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("WPF to Avalonia Migration Tool")
        {
            Description = "Transforms WPF projects to Avalonia UI projects"
        };

        // Add commands
        rootCommand.AddCommand(MigrateCommand.Create());
        rootCommand.AddCommand(TransformCommand.Create());
        rootCommand.AddCommand(TransformCSharpCommand.Create());
        rootCommand.AddCommand(TransformProjectCommand.Create());
        rootCommand.AddCommand(AnalyzeCommand.Create());
        rootCommand.AddCommand(ConfigCommand.Create());

        return await rootCommand.InvokeAsync(args);
    }
}
