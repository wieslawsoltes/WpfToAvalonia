using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CodeAnalysis;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.Core.Services;
using WpfToAvalonia.Mappings;
using WpfToAvalonia.Playground.Services;
using WpfToAvalonia.XamlParser;
using TextDocument = AvaloniaEdit.Document.TextDocument;

namespace WpfToAvalonia.Playground.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly XamlConverterService _xamlConverter;
    private CSharpConverterService? _csharpConverter;
    private bool _isInitialized;

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
        _xamlConverter = new XamlConverterService();

        InputDocument = new TextDocument(SampleWpfXaml);
        OutputDocument = new TextDocument();

        // Initialize async (don't block constructor)
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            StatusText = "Initializing...";

            // Load mapping repository asynchronously
            var mappingsPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data", "core-mappings.json");

            var mappingRepository = new JsonMappingRepository(mappingsPath);
            await mappingRepository.LoadAsync();

            // Initialize C# converter service
            _csharpConverter = new CSharpConverterService(mappingRepository);
            _isInitialized = true;

            StatusText = "Ready";

            // Perform initial conversion
            ConvertCommand.Execute(null);
        }
        catch (Exception ex)
        {
            StatusText = $"Initialization failed: {ex.Message}";
            ErrorCount++;
        }
    }

    partial void OnAutoConvertChanged(bool value)
    {
        if (value)
        {
            ConvertCommand.Execute(null);
        }
    }

    partial void OnSelectedModeChanged(string value)
    {
        // Update input with appropriate sample code
        if (value == "C#")
        {
            InputDocument = new TextDocument(SampleWpfCSharp);
        }
        else
        {
            InputDocument = new TextDocument(SampleWpfXaml);
        }

        // Trigger conversion if auto-convert is enabled
        if (AutoConvert)
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

            // Check which mode we're in
            if (SelectedMode == "C#")
            {
                ConvertCSharp(input, stopwatch);
            }
            else
            {
                ConvertXaml(input, stopwatch);
            }
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

    private void ConvertXaml(string input, Stopwatch stopwatch)
    {
        var options = new ConversionOptions
        {
            PreserveFormatting = PreserveFormatting,
            PreserveComments = true,
            AddTransformationComments = false
        };

        var result = _xamlConverter.Convert(input, options);

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
        TotalTransformations = result.Diagnostics.Count(d => d.Severity == Core.Diagnostics.DiagnosticSeverity.Info);
        ErrorCount = result.Diagnostics.Count(d => d.Severity == Core.Diagnostics.DiagnosticSeverity.Error);
        WarningCount = result.Diagnostics.Count(d => d.Severity == Core.Diagnostics.DiagnosticSeverity.Warning);
        InfoCount = result.Diagnostics.Count(d => d.Severity == Core.Diagnostics.DiagnosticSeverity.Info);

        OnPropertyChanged(nameof(OutputDocument));
    }

    private void ConvertCSharp(string input, Stopwatch stopwatch)
    {
        // Check if C# converter is initialized
        if (_csharpConverter == null || !_isInitialized)
        {
            stopwatch.Stop();
            ConversionTime = stopwatch.Elapsed.TotalMilliseconds;
            OutputDocument = new TextDocument("C# converter is initializing. Please wait...");
            StatusText = "Initializing...";
            return;
        }

        var diagnostics = new DiagnosticCollector();

        try
        {
            // Use the C# converter service
            var output = _csharpConverter.Convert(input, diagnostics);

            stopwatch.Stop();
            ConversionTime = stopwatch.Elapsed.TotalMilliseconds;

            OutputDocument = new TextDocument(output);
            StatusText = "C# conversion completed successfully";

            // Update diagnostics
            foreach (var diagnostic in diagnostics.Diagnostics)
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
            TotalTransformations = diagnostics.Diagnostics.Count(d => d.Severity == Core.Diagnostics.DiagnosticSeverity.Info);
            ErrorCount = diagnostics.Diagnostics.Count(d => d.Severity == Core.Diagnostics.DiagnosticSeverity.Error);
            WarningCount = diagnostics.Diagnostics.Count(d => d.Severity == Core.Diagnostics.DiagnosticSeverity.Warning);
            InfoCount = diagnostics.Diagnostics.Count(d => d.Severity == Core.Diagnostics.DiagnosticSeverity.Info);

            OnPropertyChanged(nameof(OutputDocument));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            ConversionTime = stopwatch.Elapsed.TotalMilliseconds;

            OutputDocument = new TextDocument($"C# conversion failed: {ex.Message}\n\nSee diagnostics for details.");
            StatusText = "C# conversion failed";
            ErrorCount++;

            Diagnostics.Add(new DiagnosticItemViewModel
            {
                Code = "CSHARP_CONVERSION_ERROR",
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

    private const string SampleWpfCSharp = @"using System.Windows;
using System.Windows.Controls;

namespace SampleWpfApp
{
    public class MyControl : Control
    {
        // Simple DependencyProperty
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(""Title"", typeof(string), typeof(MyControl));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        // DependencyProperty with metadata
        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register(""Count"", typeof(int), typeof(MyControl),
                new PropertyMetadata(0, OnCountChanged));

        private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MyControl)d;
            // Handle count change
        }

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }
    }
}";
}

public class DiagnosticItemViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}
