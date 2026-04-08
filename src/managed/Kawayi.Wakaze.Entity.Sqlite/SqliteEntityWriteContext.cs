using Kawayi.Wakaze.Entity.Abstractions;
using EntityModel = Kawayi.Wakaze.Entity.Abstractions.Entity;

namespace Kawayi.Wakaze.Entity.Sqlite;

internal sealed class SqliteEntityWriteContext : IEntityWriteContext
{
    private readonly EntityStoreDbContext _context;
    private readonly StoreMetadataRow _metadata;
    private readonly SqliteStoreIdentity _identity;

    public SqliteEntityWriteContext(
        EntityStoreDbContext context,
        StoreMetadataRow metadata)
    {
        _context = context;
        _metadata = metadata;
        _identity = new SqliteStoreIdentity(metadata.EntityStoreId, checked((ulong)metadata.EpochId));
    }

    public ValueTask<EntityModel?> GetAsync(
        EntityId id,
        EntityReadOptions options = default,
        CancellationToken cancellationToken = default)
    {
        return SqliteEntityStoreOperations.GetAsync(_context, _identity, id, options, cancellationToken);
    }

    public ValueTask<bool> ExistsAsync(
        EntityId id,
        CancellationToken cancellationToken = default)
    {
        return SqliteEntityStoreOperations.ExistsAsync(_context, id, cancellationToken);
    }

    public ValueTask<EntityRevision?> GetRevisionAsync(
        EntityId id,
        CancellationToken cancellationToken = default)
    {
        return SqliteEntityStoreOperations.GetRevisionAsync(_context, _identity, id, cancellationToken);
    }

    public IAsyncEnumerable<EntityModel> GetReferrersAsync(
        EntityId target,
        CancellationToken cancellationToken = default)
    {
        return SqliteEntityStoreOperations.GetReferrersAsync(_context, target, cancellationToken);
    }

    public ValueTask PutAsync(
        EntityModel entity,
        CancellationToken cancellationToken = default)
    {
        return SqliteEntityStoreOperations.PutAsync(_context, _metadata, entity, cancellationToken);
    }

    public ValueTask DeleteAsync(
        EntityId id,
        CancellationToken cancellationToken = default)
    {
        return SqliteEntityStoreOperations.DeleteAsync(_context, _metadata, id, cancellationToken);
    }

    public ValueTask<bool> TryPutAsync(
        EntityModel entity,
        EntityRevision expectedRevision,
        CancellationToken cancellationToken = default)
    {
        return SqliteEntityStoreOperations.TryPutAsync(_context, _metadata, entity, expectedRevision, cancellationToken);
    }

    public ValueTask<bool> TryDeleteAsync(
        EntityId id,
        EntityRevision expectedRevision,
        CancellationToken cancellationToken = default)
    {
        return SqliteEntityStoreOperations.TryDeleteAsync(_context, _metadata, id, expectedRevision, cancellationToken);
    }
}
