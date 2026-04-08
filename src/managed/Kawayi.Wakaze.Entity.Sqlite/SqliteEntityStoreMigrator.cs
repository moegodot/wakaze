using Microsoft.EntityFrameworkCore;

namespace Kawayi.Wakaze.Entity.Sqlite;

/// <summary>
/// Applies EF Core migrations for the SQLite-backed entity store.
/// </summary>
public sealed class SqliteEntityStoreMigrator
{
    private const int StoreMetadataKey = 1;

    private readonly SqliteEntityStoreInfrastructure _infrastructure;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteEntityStoreMigrator"/> class.
    /// </summary>
    /// <param name="options">The SQLite store options.</param>
    public SqliteEntityStoreMigrator(SqliteEntityStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _infrastructure = new SqliteEntityStoreInfrastructure(options);
    }

    /// <summary>
    /// Applies all pending migrations and ensures that store metadata exists.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when migration finishes.</returns>
    public async ValueTask MigrateAsync(CancellationToken cancellationToken = default)
    {
        var (connection, context) = await _infrastructure.OpenContextAsync(true, cancellationToken);
        await using (context)
        await using (connection)
        {
            await context.Database.MigrateAsync(cancellationToken);

            var metadata = await context.StoreMetadata.SingleOrDefaultAsync(
                x => x.StoreMetadataId == StoreMetadataKey,
                cancellationToken);

            if (metadata is null)
            {
                context.StoreMetadata.Add(new StoreMetadataRow
                {
                    StoreMetadataId = StoreMetadataKey,
                    EntityStoreId = Guid.CreateVersion7(),
                    EpochId = 0,
                    NextRevisionId = 1
                });

                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
