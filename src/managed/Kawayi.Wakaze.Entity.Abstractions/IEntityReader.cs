using System.Collections.Immutable;

namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Reads the current visible state of entities and their logical references.
/// </summary>
public interface IEntityReader
{
    /// <summary>
    /// Retrieves the current visible state of an entity.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// A task that resolves to the current visible entity state,
    /// or <see langword="null"/> when the entity is not currently visible.
    /// </returns>
    ValueTask<Entity?> GetAsync(
        EntityId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an entity is currently visible.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// A task that resolves to <see langword="true"/> when the entity is currently visible;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    ValueTask<bool> ExistsAsync(
        EntityId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the currently visible outgoing references from an entity.
    /// </summary>
    /// <param name="id">The source entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to the current visible outgoing references.</returns>
    ValueTask<ImmutableArray<EntityRef>> GetOutgoingRefsAsync(
        EntityId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the currently visible incoming references into an entity.
    /// </summary>
    /// <param name="id">The target entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to the current visible incoming references.</returns>
    ValueTask<ImmutableArray<EntityRef>> GetIncomingRefsAsync(
        EntityId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the current visible entity state together with requested logical edges.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="options">The optional related data to include in the load result.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// A task that resolves to the requested current visible entity aggregate,
    /// or <see langword="null"/> when the entity is not currently visible.
    /// </returns>
    ValueTask<EntityLoadResult?> LoadAsync(
        EntityId id,
        EntityLoadOptions options = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current visible revision token for an entity.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// A task that resolves to the current revision token,
    /// or <see langword="null"/> when the entity is not currently visible.
    /// </returns>
    ValueTask<EntityRevision?> GetRevisionAsync(
        EntityId id,
        CancellationToken cancellationToken = default);
}
