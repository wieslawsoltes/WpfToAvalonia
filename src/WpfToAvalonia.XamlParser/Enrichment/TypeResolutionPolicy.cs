namespace WpfToAvalonia.XamlParser.Enrichment;

/// <summary>
/// Defines how strictly type resolution should be enforced during enrichment.
/// This policy controls whether unresolved types are errors, warnings, or acceptable.
/// </summary>
public enum TypeResolutionPolicy
{
    /// <summary>
    /// Type resolution is optional. Unresolved types are logged as warnings but don't prevent transformation.
    /// Use this for best-effort transformations where some types may not be available.
    /// This is the default for maximum compatibility.
    /// </summary>
    Optional,

    /// <summary>
    /// Type resolution is required. Unresolved types cause transformation to fail with an error.
    /// Use this when type information is critical for correct transformation.
    /// Ensures all types are fully resolved before proceeding.
    /// </summary>
    Required,

    /// <summary>
    /// Type resolution attempts best effort with fallbacks. Uses reflection or heuristics
    /// for unresolved types before marking them as failed.
    /// Use this when you want maximum resolution success with graceful degradation.
    /// </summary>
    BestEffort
}

/// <summary>
/// Options for type resolution enrichment.
/// </summary>
public sealed class TypeResolutionOptions
{
    /// <summary>
    /// Gets or sets the type resolution policy.
    /// Default is Optional for backward compatibility.
    /// </summary>
    public TypeResolutionPolicy Policy { get; set; } = TypeResolutionPolicy.Optional;

    /// <summary>
    /// Gets or sets whether to attempt reflection-based fallback for unresolved types.
    /// Only applies when Policy is BestEffort.
    /// Default is true.
    /// </summary>
    public bool UseReflectionFallback { get; set; } = true;

    /// <summary>
    /// Gets or sets the assemblies to search for reflection fallback.
    /// Only applies when UseReflectionFallback is true.
    /// </summary>
    public List<string> FallbackAssemblies { get; } = new();

    /// <summary>
    /// Gets or sets whether to fail fast on first unresolved type (when Policy is Required).
    /// If false, collects all unresolved types before failing.
    /// Default is false for better diagnostics.
    /// </summary>
    public bool FailFast { get; set; } = false;

    /// <summary>
    /// Creates default options with Optional policy.
    /// </summary>
    public static TypeResolutionOptions Default() => new TypeResolutionOptions();

    /// <summary>
    /// Creates options with Required policy (fail-fast type resolution).
    /// </summary>
    public static TypeResolutionOptions Strict() => new TypeResolutionOptions
    {
        Policy = TypeResolutionPolicy.Required,
        FailFast = true
    };

    /// <summary>
    /// Creates options with BestEffort policy and reflection fallback.
    /// </summary>
    public static TypeResolutionOptions BestEffort() => new TypeResolutionOptions
    {
        Policy = TypeResolutionPolicy.BestEffort,
        UseReflectionFallback = true
    };
}
