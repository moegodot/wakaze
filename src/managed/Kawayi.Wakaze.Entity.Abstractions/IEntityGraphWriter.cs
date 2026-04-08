namespace Kawayi.Wakaze.Entity.Abstractions;

public interface IEntityGraphWriter
{
    ValueTask PutGraphAsync(
        IReadOnlyCollection<Entity> entities,
        CancellationToken cancellationToken = default);
}
