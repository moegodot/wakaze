using Kawayi.Wakaze.Entity.Abstractions;
using Microsoft.Data.Sqlite;
using EntityModel = Kawayi.Wakaze.Entity.Abstractions.Entity;

namespace Kawayi.Wakaze.Entity.Sqlite;

internal sealed class SqliteEntityReadSnapshot : IEntityReadSnapshot
{
    private readonly SqliteConnection _connection;
    private readonly EntityStoreDbContext _context;
    private readonly SqliteTransaction _transaction;
    private readonly SqliteStoreIdentity _identity;

    public SqliteEntityReadSnapshot(
        SqliteConnection connection,
        EntityStoreDbContext context,
        SqliteTransaction transaction,
        SqliteStoreIdentity identity)
    {
        _connection = connection;
        _context = context;
        _transaction = transaction;
        _identity = identity;
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

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync();
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
