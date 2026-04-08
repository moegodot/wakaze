namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Executes entity store operations within a single atomic boundary.
/// </summary>
public interface IEntityAtomicExecutor
{
    /// <summary>
    /// Executes the supplied operation within a single atomic boundary.
    /// </summary>
    /// <param name="action">The operation to execute.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the operation finishes.</returns>
    ValueTask ExecuteAsync(
        Func<IEntityWriteContext, CancellationToken, ValueTask> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the supplied operation within a single atomic boundary and returns a result.
    /// </summary>
    /// <typeparam name="TResult">The result type produced by the operation.</typeparam>
    /// <param name="action">The operation to execute.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to the value produced by <paramref name="action"/>.</returns>
    ValueTask<TResult> ExecuteAsync<TResult>(
        Func<IEntityWriteContext, CancellationToken, ValueTask<TResult>> action,
        CancellationToken cancellationToken = default);
}
