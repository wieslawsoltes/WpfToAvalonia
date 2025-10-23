using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser;

namespace WpfToAvalonia.Playground.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly WpfToAvaloniaConverter _converter;

    [ObservableProperty]
    private TextDocument? _inputDocument;

    [ObservableProperty]
    private TextDocument? _outputDocument;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private double _conversionTime;

    [ObservableProperty]
    private int _totalTransformations;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private int _warningCount;

    [ObservableProperty]
    private int _infoCount;

    [ObservableProperty]
    private bool _autoConvert;

    [ObservableProperty]
    private bool _preserveFormatting = true;

    [ObservableProperty]
    private string _selectedMode = "XAML";

    public ObservableCollection<string> ConversionModes { get; } = new() { "XAML", "C#" };
    public ObservableCollection<DiagnosticItemViewModel> Diagnostics { get; } = new();

    public MainWindowViewModel()
    {
        _converter = new WpfToAvaloniaConverter();

        InputDocument = new TextDocument(SampleWpfXaml);
        OutputDocument = new TextDocument();

        // Initial conversion
        ConvertCommand.Execute(null);
    }

    partial void OnAutoConvertChanged(bool value)
    {
        if (value)
        {
            ConvertCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task OpenFile()
    {
        try
        {
            // Note: This needs to be called from UI thread with proper StorageProvider
            StatusText = "Open file functionality requires UI integration";
        }
        catch (Exception ex)
        {
            StatusText = $"Error opening file: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Convert()
    {
        try
        {
            StatusText = "Converting...";
            Diagnostics.Clear();

            var input = InputDocument?.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                StatusText = "No input to convert";
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            var options = new ConversionOptions
            {
                PreserveFormatting = PreserveFormatting,
                PreserveComments = true,
                AddTransformationComments = false
            };

            var result = _converter.Convert(input, null, options);

            stopwatch.Stop();
            ConversionTime = stopwatch.Elapsed.TotalMilliseconds;

            if (result.Success && !string.IsNullOrEmpty(result.OutputXaml))
            {
                OutputDocument = new TextDocument(result.OutputXaml);
                StatusText = "Conversion completed successfully";
            }
            else
            {
                OutputDocument = new TextDocument("Conversion failed. See diagnostics.");
                StatusText = "Conversion failed";
            }

            // Update diagnostics
            foreach (var diagnostic in result.Diagnostics)
            {
                Diagnostics.Add(new DiagnosticItemViewModel
                {
                    Code = diagnostic.Code,
                    Message = diagnostic.Message,
                    Location = diagnostic.FilePath ?? string.Empty,
                    Severity = diagnostic.Severity.ToString()
                });
            }

            // Update statistics
            TotalTransformations = result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Info);
            ErrorCount = result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
            WarningCount = result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
            InfoCount = result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Info);

            OnPropertyChanged(nameof(OutputDocument));
        }
        catch (Exception ex)
        {
            StatusText = $"Error during conversion: {ex.Message}";
            ErrorCount++;
            Diagnostics.Add(new DiagnosticItemViewModel
            {
                Code = "EXCEPTION",
                Message = ex.ToString(),
                Location = string.Empty,
                Severity = "Error"
            });
        }
    }

    [RelayCommand]
    private async Task SaveOutput()
    {
        try
        {
            // Note: This needs to be called from UI thread with proper StorageProvider
            StatusText = "Save functionality requires UI integration";
        }
        catch (Exception ex)
        {
            StatusText = $"Error saving file: {ex.Message}";
        }
    }

    private const string SampleWpfXaml = @"<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        Title=""Sample WPF Window"" Height=""450"" Width=""800"">
    <Window.Resources>
        <Style TargetType=""Button"">
            <Setter Property=""Background"" Value=""White"" />
            <Setter Property=""Padding"" Value=""10,5"" />
            <Style.Triggers>
                <Trigger Property=""IsMouseOver"" Value=""True"">
                    <Setter Property=""Background"" Value=""LightBlue"" />
                </Trigger>
                <Trigger Property=""IsPressed"" Value=""True"">
                    <Setter Property=""Background"" Value=""DarkBlue"" />
                    <Setter Property=""Foreground"" Value=""White"" />
                </Trigger>
            </Style.Triggers>
        </Style>
    </Window.Resources>
    <Grid>
        <StackPanel Margin=""20"" VerticalAlignment=""Center"">
            <TextBlock Text=""WPF to Avalonia Converter""
                       FontSize=""24""
                       FontWeight=""Bold""
                       HorizontalAlignment=""Center""
                       Margin=""0,0,0,20"" />
            <Button Content=""Click Me"" HorizontalAlignment=""Center"" />
            <TextBox Text=""Sample Text"" Margin=""0,10,0,0"" />
            <CheckBox Content=""Enable Feature"" Margin=""0,10,0,0"" />
        </StackPanel>
    </Grid>
</Window>";
}

public class DiagnosticItemViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}
