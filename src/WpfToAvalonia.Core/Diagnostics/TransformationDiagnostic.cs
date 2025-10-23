namespace WpfToAvalonia.Core.Diagnostics;

/// <summary>
/// Represents a diagnostic message from the transformation process.
/// </summary>
public sealed class TransformationDiagnostic
{
    /// <summary>
    /// Gets or sets the severity of the diagnostic.
    /// </summary>
    public DiagnosticSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the diagnostic code (e.g., "WA001").
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the diagnostic message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Gets or sets the file path where the diagnostic occurred.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    public int? Line { get; set; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    public int? Column { get; set; }

    /// <summary>
    /// Gets or sets the category of the diagnostic.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for this diagnostic.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this diagnostic was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the formatted location string.
    /// </summary>
    public string Location
    {
        get
        {
            if (string.IsNullOrEmpty(FilePath))
                return string.Empty;

            if (Line.HasValue && Column.HasValue)
                return $"{FilePath}:{Line}:{Column}";

            if (Line.HasValue)
                return $"{FilePath}:{Line}";

            return FilePath;
        }
    }

    /// <summary>
    /// Returns a formatted string representation of this diagnostic.
    /// </summary>
    public override string ToString()
    {
        var location = !string.IsNullOrEmpty(Location) ? $"{Location}: " : string.Empty;
        var severity = Severity.ToString().ToLowerInvariant();
        return $"{location}{severity} {Code}: {Message}";
    }
}

/// <summary>
/// Defines diagnostic severity levels.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// Information message.
    /// </summary>
    Info,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning,

    /// <summary>
    /// Error message.
    /// </summary>
    Error
}
