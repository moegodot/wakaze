using System.Runtime.CompilerServices;
using Kawayi.Wakaze.Entity.Abstractions;
using EntityModel = Kawayi.Wakaze.Entity.Abstractions.Entity;

namespace Kawayi.Wakaze.Entity.Sqlite;

/// <summary>
/// Stores entities in a SQLite database using EF Core migrations for schema management.
/// </summary>
/// <remarks>
/// This type only registers and executes entity store semantics. Call <see cref="SqliteEntityStoreMigrator"/>
/// before using a fresh database so the schema and metadata are initialized.
/// </remarks>
public sealed class SqliteEntityStore : IEntityStore
{
    private readonly SqliteEntityStoreInfrastructure _infrastructure;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteEntityStore"/> class.
    /// </summary>
    /// <param name="options">The SQLite store options.</param>
    public SqliteEntityStore(SqliteEntityStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _infrastructure = new SqliteEntityStoreInfrastructure(options);
    }

    /// <summary>
    /// Retrieves the current content of an entity.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="options">Optional behaviors for the read operation.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The current entity content, or <see langword="null"/> when the entity is not visible.</returns>
    public async ValueTask<EntityModel?> GetAsync(
        EntityId id,
        EntityReadOptions options = default,
        CancellationToken cancellationToken = default)
    {
        await using var scope = await _infrastructure.OpenReadScopeAsync(cancellationToken);
        return await SqliteEntityStoreOperations.GetAsync(scope.Context, scope.Identity, id, options, cancellationToken);
    }

    /// <summary>
    /// Determines whether an entity is currently visible.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns><see langword="true"/> when the entity is visible; otherwise, <see langword="false"/>.</returns>
    public async ValueTask<bool> ExistsAsync(
        EntityId id,
        CancellationToken cancellationToken = default)
    {
        await using var scope = await _infrastructure.OpenReadScopeAsync(cancellationToken);
        return await SqliteEntityStoreOperations.ExistsAsync(scope.Context, id, cancellationToken);
    }

    /// <summary>
    /// Retrieves the current visible revision token for an entity.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The current visible revision token, or <see langword="null"/> when the entity is not visible.</returns>
    public async ValueTask<EntityRevision?> GetRevisionAsync(
        EntityId id,
        CancellationToken cancellationToken = default)
    {
        await using var scope = await _infrastructure.OpenReadScopeAsync(cancellationToken);
        return await SqliteEntityStoreOperations.GetRevisionAsync(scope.Context, scope.Identity, id, cancellationToken);
    }

    /// <summary>
    /// Enumerates the currently visible entities that reference the specified target.
    /// </summary>
    /// <param name="target">The referenced entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the enumeration.</param>
    /// <returns>An asynchronous sequence of current referrer entities.</returns>
    public async IAsyncEnumerable<EntityModel> GetReferrersAsync(
        EntityId target,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var scope = await _infrastructure.OpenReadScopeAsync(cancellationToken);

        await foreach (var entity in SqliteEntityStoreOperations
                           .GetReferrersAsync(scope.Context, target, cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            yield return entity;
        }
    }

    /// <summary>
    /// Opens a stable point-in-time snapshot for entity reads.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A snapshot whose reads observe a stable view.</returns>
    public ValueTask<IEntityReadSnapshot> OpenSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return OpenSnapshotCoreAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves the entity content associated with a specific revision token.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="revision">The revision token to read.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The entity content for the requested revision, or <see langword="null"/> when the revision is unavailable.</returns>
    public async ValueTask<EntityModel?> GetByRevisionAsync(
        EntityId id,
        EntityRevision revision,
        CancellationToken cancellationToken = default)
    {
        await using var scope = await _infrastructure.OpenReadScopeAsync(cancellationToken);
        return await SqliteEntityStoreOperations.GetByRevisionAsync(
            scope.Context,
            scope.Identity,
            id,
            revision,
            cancellationToken);
    }

    /// <summary>
    /// Lists the content revisions that are available for an entity.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the enumeration.</param>
    /// <returns>An asynchronous sequence of available content revision tokens.</returns>
    public async IAsyncEnumerable<EntityRevision> ListRevisionsAsync(
        EntityId id,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var scope = await _infrastructure.OpenReadScopeAsync(cancellationToken);

        await foreach (var revision in SqliteEntityStoreOperations
                           .ListRevisionsAsync(scope.Context, scope.Identity, id, cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            yield return revision;
        }
    }

    /// <summary>
    /// Executes the supplied operation within a single atomic boundary.
    /// </summary>
    /// <param name="action">The operation to execute.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the operation finishes.</returns>
    public async ValueTask ExecuteAsync(
        Func<IEntityWriteContext, CancellationToken, ValueTask> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using var scope = await _infrastructure.OpenWriteScopeAsync(cancellationToken);
        var writeContext = new SqliteEntityWriteContext(scope.Context, scope.Metadata);

        await action(writeContext, cancellationToken);
        await scope.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Executes the supplied operation within a single atomic boundary and returns a result.
    /// </summary>
    /// <typeparam name="TResult">The result type produced by the operation.</typeparam>
    /// <param name="action">The operation to execute.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The result produced by <paramref name="action"/>.</returns>
    public async ValueTask<TResult> ExecuteAsync<TResult>(
        Func<IEntityWriteContext, CancellationToken, ValueTask<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await using var scope = await _infrastructure.OpenWriteScopeAsync(cancellationToken);
        var writeContext = new SqliteEntityWriteContext(scope.Context, scope.Metadata);

        var result = await action(writeContext, cancellationToken);
        await scope.CommitAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Writes a batch of entities in a single atomic transaction.
    /// </summary>
    /// <param name="entities">The entities to write.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the write operation finishes.</returns>
    public ValueTask PutGraphAsync(
        IReadOnlyCollection<EntityModel> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return ExecuteAsync(
            async (context, ct) =>
            {
                foreach (var entity in entities)
                {
                    await context.PutAsync(entity, ct);
                }
            },
            cancellationToken);
    }

    private async ValueTask<IEntityReadSnapshot> OpenSnapshotCoreAsync(CancellationToken cancellationToken)
    {
        return await _infrastructure.OpenSnapshotAsync(cancellationToken);
    }
}
