namespace Kawayi.Wakaze.Entity.Abstractions;

public interface IEntityHistoryReader
{
    ValueTask<Entity?> GetByRevisionAsync(
        EntityId id,
        EntityRevision revision,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<EntityRevision> ListRevisionsAsync(
        EntityId id,
        CancellationToken cancellationToken = default);
}
