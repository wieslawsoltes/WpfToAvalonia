using System.Collections.Concurrent;

namespace WpfToAvalonia.Core.Diagnostics;

/// <summary>
/// Collects diagnostics (errors, warnings, information) during transformation.
/// </summary>
public sealed class DiagnosticCollector
{
    private readonly ConcurrentBag<TransformationDiagnostic> _diagnostics = new();

    /// <summary>
    /// Gets all collected diagnostics.
    /// </summary>
    public IReadOnlyList<TransformationDiagnostic> Diagnostics => _diagnostics.ToList();

    /// <summary>
    /// Gets the count of diagnostics.
    /// </summary>
    public int Count => _diagnostics.Count;

    /// <summary>
    /// Gets the count of errors.
    /// </summary>
    public int ErrorCount => _diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Gets the count of warnings.
    /// </summary>
    public int WarningCount => _diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);

    /// <summary>
    /// Gets the count of information diagnostics.
    /// </summary>
    public int InfoCount => _diagnostics.Count(d => d.Severity == DiagnosticSeverity.Info);

    /// <summary>
    /// Adds an error diagnostic.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="filePath">The file path where the diagnostic occurred.</param>
    /// <param name="line">The line number (optional).</param>
    /// <param name="column">The column number (optional).</param>
    public void AddError(string code, string message, string? filePath = null, int? line = null, int? column = null)
    {
        Add(new TransformationDiagnostic
        {
            Severity = DiagnosticSeverity.Error,
            Code = code,
            Message = message,
            FilePath = filePath,
            Line = line,
            Column = column
        });
    }

    /// <summary>
    /// Adds a warning diagnostic.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="filePath">The file path where the diagnostic occurred.</param>
    /// <param name="line">The line number (optional).</param>
    /// <param name="column">The column number (optional).</param>
    public void AddWarning(string code, string message, string? filePath = null, int? line = null, int? column = null)
    {
        Add(new TransformationDiagnostic
        {
            Severity = DiagnosticSeverity.Warning,
            Code = code,
            Message = message,
            FilePath = filePath,
            Line = line,
            Column = column
        });
    }

    /// <summary>
    /// Adds an information diagnostic.
    /// </summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="filePath">The file path where the diagnostic occurred.</param>
    /// <param name="line">The line number (optional).</param>
    /// <param name="column">The column number (optional).</param>
    public void AddInfo(string code, string message, string? filePath = null, int? line = null, int? column = null)
    {
        Add(new TransformationDiagnostic
        {
            Severity = DiagnosticSeverity.Info,
            Code = code,
            Message = message,
            FilePath = filePath,
            Line = line,
            Column = column
        });
    }

    /// <summary>
    /// Adds a diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    public void Add(TransformationDiagnostic diagnostic)
    {
        _diagnostics.Add(diagnostic);
    }

    /// <summary>
    /// Adds multiple diagnostics.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to add.</param>
    public void AddRange(IEnumerable<TransformationDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            _diagnostics.Add(diagnostic);
        }
    }

    /// <summary>
    /// Clears all diagnostics.
    /// </summary>
    public void Clear()
    {
        _diagnostics.Clear();
    }

    /// <summary>
    /// Gets diagnostics filtered by severity.
    /// </summary>
    /// <param name="severity">The severity to filter by.</param>
    /// <returns>Diagnostics with the specified severity.</returns>
    public IReadOnlyList<TransformationDiagnostic> GetBySeverity(DiagnosticSeverity severity)
    {
        return _diagnostics.Where(d => d.Severity == severity).ToList();
    }

    /// <summary>
    /// Gets diagnostics for a specific file.
    /// </summary>
    /// <param name="filePath">The file path to filter by.</param>
    /// <returns>Diagnostics for the specified file.</returns>
    public IReadOnlyList<TransformationDiagnostic> GetByFile(string filePath)
    {
        return _diagnostics.Where(d => d.FilePath == filePath).ToList();
    }
}
