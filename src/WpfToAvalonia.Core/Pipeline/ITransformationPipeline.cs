namespace WpfToAvalonia.Core.Pipeline;

/// <summary>
/// Defines the main transformation pipeline for migrating WPF code to Avalonia.
/// </summary>
public interface ITransformationPipeline
{
    /// <summary>
    /// Executes the transformation pipeline on the specified input.
    /// </summary>
    /// <param name="context">The transformation context containing input and configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The transformation result.</returns>
    Task<TransformationResult> ExecuteAsync(TransformationContext context, CancellationToken cancellationToken = default);
}
