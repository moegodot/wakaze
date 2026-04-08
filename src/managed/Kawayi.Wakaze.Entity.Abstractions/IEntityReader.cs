namespace Kawayi.Wakaze.Entity.Abstractions;

public interface IEntityReader
{
    ValueTask<Entity?> GetAsync(
        EntityId id,
        EntityReadOptions options = default,
        CancellationToken cancellationToken = default);

    ValueTask<bool> ExistsAsync(
        EntityId id,
        CancellationToken cancellationToken = default);

    ValueTask<EntityRevision?> GetRevisionAsync(
        EntityId id,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<EntityId> GetReferrersAsync(
        EntityId target,
        CancellationToken cancellationToken = default);
}
