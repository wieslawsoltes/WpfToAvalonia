using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace WpfToAvalonia.Playground.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetupSyntaxHighlighting();
    }

    private void SetupSyntaxHighlighting()
    {
        var inputEditor = this.FindControl<TextEditor>("InputEditor");
        var outputEditor = this.FindControl<TextEditor>("OutputEditor");

        if (inputEditor != null && outputEditor != null)
        {
            // Setup TextMate syntax highlighting with Dark+ theme
            var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
            var textMateInstallation = inputEditor.InstallTextMate(registryOptions);
            var outputTextMateInstallation = outputEditor.InstallTextMate(registryOptions);

            // Set XML syntax for both editors
            textMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".xml").Id));
            outputTextMateInstallation.SetGrammar(registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".xml").Id));

            // Configure selection colors
            if (inputEditor.TextArea != null)
            {
                inputEditor.TextArea.SelectionBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#264F78"));
                inputEditor.TextArea.SelectionForeground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            }

            if (outputEditor.TextArea != null)
            {
                outputEditor.TextArea.SelectionBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#264F78"));
                outputEditor.TextArea.SelectionForeground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            }
        }
    }
}