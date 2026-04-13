namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Reads historical revisions of entities.
/// </summary>
public interface IEntityHistoryReader
{
    /// <summary>
    /// Retrieves the entity content associated with a specific revision token.
    /// </summary>
    /// <param name="revision">The historical revision token to read.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// A task that resolves to the entity content for the requested revision,
    /// or <see langword="null"/> when the requested revision is not available.
    /// Historical reads are the explicit surface where retained tombstones and deleted revisions may appear.
    /// </returns>
    ValueTask<Entity?> GetByRevisionAsync(
        EntityRevision revision,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the revisions that are available for an entity.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>An asynchronous sequence of available revision tokens.</returns>
    /// <remarks>
    /// The public contract does not guarantee the enumeration order.
    /// </remarks>
    IAsyncEnumerable<EntityRevision> ListRevisionsAsync(
        EntityId id,
        CancellationToken cancellationToken = default);
}
