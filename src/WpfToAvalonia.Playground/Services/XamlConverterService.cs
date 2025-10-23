using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WpfToAvalonia.Core.Diagnostics;
using WpfToAvalonia.XamlParser;

namespace WpfToAvalonia.Playground.Services;

/// <summary>
/// Service for converting XAML from WPF to Avalonia.
/// Encapsulates XAML transformation functionality.
/// </summary>
public sealed class XamlConverterService
{
    private readonly WpfToAvaloniaConverter _converter;

    /// <summary>
    /// Initializes a new instance of the <see cref="XamlConverterService"/> class.
    /// </summary>
    public XamlConverterService()
    {
        _converter = new WpfToAvaloniaConverter();
    }

    /// <summary>
    /// Converts XAML from WPF to Avalonia.
    /// </summary>
    /// <param name="xamlContent">The WPF XAML content to convert.</param>
    /// <param name="options">Optional conversion options.</param>
    /// <returns>The conversion result containing the converted XAML and diagnostics.</returns>
    public ConversionResult Convert(string xamlContent, ConversionOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(xamlContent))
        {
            return new ConversionResult
            {
                Success = false,
                OutputXaml = xamlContent,
                Diagnostics = new List<TransformationDiagnostic>
                {
                    new TransformationDiagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        Code = "EMPTY_INPUT",
                        Message = "No XAML content to convert",
                        FilePath = null
                    }
                }
            };
        }

        options ??= new ConversionOptions
        {
            PreserveFormatting = true,
            PreserveComments = true,
            AddTransformationComments = false
        };

        return _converter.Convert(xamlContent, null, options);
    }

    /// <summary>
    /// Converts XAML from WPF to Avalonia asynchronously.
    /// </summary>
    /// <param name="xamlContent">The WPF XAML content to convert.</param>
    /// <param name="options">Optional conversion options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conversion result containing the converted XAML and diagnostics.</returns>
    public Task<ConversionResult> ConvertAsync(
        string xamlContent,
        ConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Convert(xamlContent, options), cancellationToken);
    }
}
