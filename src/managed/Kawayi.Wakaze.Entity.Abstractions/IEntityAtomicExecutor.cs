namespace Kawayi.Wakaze.Entity.Abstractions;

public interface IEntityAtomicExecutor
{
    ValueTask ExecuteAsync(
        Func<IEntityWriteContext, CancellationToken, ValueTask> action,
        CancellationToken cancellationToken = default);

    ValueTask<TResult> ExecuteAsync<TResult>(
        Func<IEntityWriteContext, CancellationToken, ValueTask<TResult>> action,
        CancellationToken cancellationToken = default);
}
