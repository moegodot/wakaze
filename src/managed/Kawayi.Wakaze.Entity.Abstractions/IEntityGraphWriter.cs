namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Writes multiple entities as a graph-oriented batch.
/// </summary>
public interface IEntityGraphWriter
{
    /// <summary>
    /// Writes a batch of entities.
    /// </summary>
    /// <param name="entities">The entities to write.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the write operation finishes.</returns>
    /// <remarks>
    /// Implementations may optimize this method for graph-oriented writes.
    /// This operation may partially succeed and does not imply all-or-nothing semantics.
    /// </remarks>
    ValueTask PutGraphAsync(
        IReadOnlyCollection<Entity> entities,
        CancellationToken cancellationToken = default);
}
