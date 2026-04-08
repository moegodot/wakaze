namespace Kawayi.Wakaze.Entity.Abstractions;

public interface IEntityWriteContext : IEntityReader
{
    ValueTask PutAsync(
        Entity entity,
        CancellationToken cancellationToken = default);

    ValueTask DeleteAsync(
        EntityId id,
        CancellationToken cancellationToken = default);

    ValueTask<bool> TryPutAsync(
        Entity entity,
        EntityRevision expectedRevision,
        CancellationToken cancellationToken = default);

    ValueTask<bool> TryDeleteAsync(
        EntityId id,
        EntityRevision expectedRevision,
        CancellationToken cancellationToken = default);
}
