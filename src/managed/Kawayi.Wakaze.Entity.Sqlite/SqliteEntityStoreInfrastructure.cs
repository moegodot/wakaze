using Kawayi.Wakaze.Entity.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kawayi.Wakaze.Entity.Sqlite;

internal sealed class SqliteEntityStoreInfrastructure
{
    private readonly SqliteEntityStoreOptions _options;
    private readonly string _connectionString;

    public SqliteEntityStoreInfrastructure(SqliteEntityStoreOptions options)
    {
        _options = options.CloneValidated();
        _connectionString = _options.GetConnectionString();
    }

    public async ValueTask<SqliteReadScope> OpenReadScopeAsync(CancellationToken cancellationToken)
    {
        var connection = await OpenConnectionAsync(cancellationToken);
        var context = CreateDbContext(connection);

        try
        {
            var identity = await LoadStoreIdentityAsync(context, cancellationToken);
            return new SqliteReadScope(connection, context, identity);
        }
        catch
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
            throw;
        }
    }

    public async ValueTask<SqliteWriteScope> OpenWriteScopeAsync(CancellationToken cancellationToken)
    {
        var connection = await OpenConnectionAsync(cancellationToken);
        var context = CreateDbContext(connection);

        try
        {
            var transaction = connection.BeginTransaction(false);
            context.Database.UseTransaction(transaction);

            var metadata = await context.StoreMetadata.SingleAsync(cancellationToken);
            return new SqliteWriteScope(connection, context, transaction, metadata);
        }
        catch
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
            throw;
        }
    }

    public async ValueTask<SqliteEntityReadSnapshot> OpenSnapshotAsync(CancellationToken cancellationToken)
    {
        var connection = await OpenConnectionAsync(cancellationToken);
        var context = CreateDbContext(connection);

        try
        {
            var transaction = connection.BeginTransaction(true);
            context.Database.UseTransaction(transaction);

            var identity = await LoadStoreIdentityAsync(context, cancellationToken);
            return new SqliteEntityReadSnapshot(connection, context, transaction, identity);
        }
        catch
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
            throw;
        }
    }

    public async ValueTask<(SqliteConnection Connection, EntityStoreDbContext Context)> OpenContextAsync(
        bool writable,
        CancellationToken cancellationToken)
    {
        var connection = await OpenConnectionAsync(cancellationToken);
        var context = CreateDbContext(connection);

        try
        {
            if (writable) await ApplyWritePragmasAsync(connection, cancellationToken);

            return (connection, context);
        }
        catch
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
            throw;
        }
    }

    public async ValueTask<SqliteStoreIdentity> LoadStoreIdentityAsync(
        EntityStoreDbContext context,
        CancellationToken cancellationToken)
    {
        var metadata = await context.StoreMetadata
            .AsNoTracking()
            .SingleAsync(cancellationToken);

        return new SqliteStoreIdentity(
            metadata.EntityStoreId,
            checked((ulong)metadata.EpochId));
    }

    public EntityStoreDbContext CreateDbContext(SqliteConnection connection)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EntityStoreDbContext>();
        optionsBuilder.UseSqlite(connection,
            sqlite => { sqlite.MigrationsAssembly(typeof(SqliteEntityStore).Assembly.FullName); });

        optionsBuilder.EnableDetailedErrors();
        return new EntityStoreDbContext(optionsBuilder.Options);
    }

    public async ValueTask<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        _options.EnsureDatabaseDirectory();

        var connection = new SqliteConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async ValueTask ApplyWritePragmasAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        if (!_options.EnableWriteAheadLogging) return;

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = WAL;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

internal readonly record struct SqliteStoreIdentity(Guid EntityStoreId, ulong EpochId)
{
    public EntityRevision CreateEntityRevision(EntityId entityId, long revisionId)
    {
        return new EntityRevision(
            entityId,
            new Revision(EntityStoreId, EpochId, checked((ulong)revisionId)));
    }

    public bool Matches(EntityId entityId, EntityRevision revision, long currentRevisionId)
    {
        return revision.EntityId == entityId
               && revision.Revision.ContainerId == EntityStoreId
               && revision.Revision.EpochId == EpochId
               && revision.Revision.RevisionId == checked((ulong)currentRevisionId);
    }
}

internal sealed class SqliteReadScope : IAsyncDisposable
{
    public SqliteReadScope(
        SqliteConnection connection,
        EntityStoreDbContext context,
        SqliteStoreIdentity identity)
    {
        Connection = connection;
        Context = context;
        Identity = identity;
    }

    public SqliteConnection Connection { get; }

    public EntityStoreDbContext Context { get; }

    public SqliteStoreIdentity Identity { get; }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await Connection.DisposeAsync();
    }
}

internal sealed class SqliteWriteScope : IAsyncDisposable
{
    public SqliteWriteScope(
        SqliteConnection connection,
        EntityStoreDbContext context,
        SqliteTransaction transaction,
        StoreMetadataRow metadata)
    {
        Connection = connection;
        Context = context;
        Transaction = transaction;
        Metadata = metadata;
    }

    public SqliteConnection Connection { get; }

    public EntityStoreDbContext Context { get; }

    public SqliteTransaction Transaction { get; }

    public StoreMetadataRow Metadata { get; }

    public SqliteStoreIdentity Identity => new(Metadata.EntityStoreId, checked((ulong)Metadata.EpochId));

    public async ValueTask CommitAsync(CancellationToken cancellationToken)
    {
        await Transaction.CommitAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Transaction.DisposeAsync();
        await Context.DisposeAsync();
        await Connection.DisposeAsync();
    }
}
