namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Reads the current visible state of entities and their reverse references.
/// </summary>
public interface IEntityReader
{
    /// <summary>
    /// Retrieves the current content of an entity.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="options">Optional behaviors for the read operation.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// A task that resolves to the current entity content,
    /// or <see langword="null"/> when the entity is not currently visible.
    /// Deleted entities are hidden unless <see cref="EntityReadOptions.IncludeDeleted"/> is <see langword="true"/>.
    /// </returns>
    ValueTask<Entity?> GetAsync(
        EntityId id,
        EntityReadOptions options = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an entity is currently visible.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// A task that resolves to <see langword="true"/> when the entity is currently visible;
    /// otherwise, <see langword="false"/>.
    /// Deleted entities are reported as not existing.
    /// </returns>
    ValueTask<bool> ExistsAsync(
        EntityId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current visible revision token for an entity.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// A task that resolves to the current revision token,
    /// or <see langword="null"/> when the entity is not currently visible.
    /// Deleted entities return <see langword="null"/>.
    /// </returns>
    ValueTask<EntityRevision?> GetRevisionAsync(
        EntityId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates the currently visible entities that reference the specified target.
    /// </summary>
    /// <param name="target">The referenced entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>An asynchronous sequence of current referrer entities.</returns>
    /// <remarks>
    /// This method returns current referrers without filtering by <see cref="RefKind"/>.
    /// Callers must inspect the returned entities to determine whether the reference is strong or weak.
    /// </remarks>
    IAsyncEnumerable<Entity> GetReferrersAsync(
        EntityId target,
        CancellationToken cancellationToken = default);
}
