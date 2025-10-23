namespace WpfToAvalonia.Core.Pipeline;

/// <summary>
/// Defines a transformer that performs a specific transformation step.
/// </summary>
public interface ITransformer
{
    /// <summary>
    /// Gets the name of this transformer.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the order priority for this transformer (lower numbers run first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Determines whether this transformer can handle the given context.
    /// </summary>
    /// <param name="context">The transformation context.</param>
    /// <returns>True if this transformer can process the context, otherwise false.</returns>
    bool CanTransform(TransformationContext context);

    /// <summary>
    /// Executes the transformation on the given context.
    /// </summary>
    /// <param name="context">The transformation context to modify.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the asynchronous transformation operation.</returns>
    Task TransformAsync(TransformationContext context, CancellationToken cancellationToken = default);
}
