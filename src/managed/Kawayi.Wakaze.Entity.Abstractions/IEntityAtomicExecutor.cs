namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Executes entity store operations within a single atomic boundary.
/// </summary>
/// <remarks>
/// The atomic boundary commits only when the supplied delegate completes successfully.
/// If the delegate throws or cancellation is observed before commit completes,
/// the implementation must roll back the mutation set and expose no partial commit.
/// Nested atomic executions are not supported and must throw <see cref="InvalidOperationException"/>.
/// </remarks>
public interface IEntityAtomicExecutor
{
    /// <summary>
    /// Executes the supplied operation within a single atomic boundary.
    /// </summary>
    /// <param name="action">The operation to execute.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the operation commits successfully.</returns>
    ValueTask ExecuteAsync(
        Func<IEntityWriteContext, CancellationToken, ValueTask> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the supplied operation within a single atomic boundary and returns a result.
    /// </summary>
    /// <typeparam name="TResult">The result type produced by the operation.</typeparam>
    /// <param name="action">The operation to execute.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to the value produced by <paramref name="action"/> when the operation commits successfully.</returns>
    ValueTask<TResult> ExecuteAsync<TResult>(
        Func<IEntityWriteContext, CancellationToken, ValueTask<TResult>> action,
        CancellationToken cancellationToken = default);
}
