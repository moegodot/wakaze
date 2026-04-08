namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Provides read and write operations within an atomic execution boundary.
/// </summary>
public interface IEntityWriteContext : IEntityReader
{
    /// <summary>
    /// Writes the supplied entity content.
    /// </summary>
    /// <param name="entity">The entity content to store.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the write operation finishes.</returns>
    ValueTask PutAsync(
        Entity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified entity.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the delete operation finishes.</returns>
    ValueTask DeleteAsync(
        EntityId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the supplied entity content when the entity currently has the expected revision.
    /// </summary>
    /// <param name="entity">The entity content to store.</param>
    /// <param name="expectedRevision">The expected current revision of the target entity.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// A task that resolves to <see langword="true"/> when the write succeeds;
    /// otherwise, <see langword="false"/> when the target entity does not currently match <paramref name="expectedRevision"/>.
    /// </returns>
    /// <remarks>
    /// The condition only validates the current revision of the target entity.
    /// It does not imply any additional checks against referrers or other global state.
    /// </remarks>
    ValueTask<bool> TryPutAsync(
        Entity entity,
        EntityRevision expectedRevision,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified entity when the entity currently has the expected revision.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="expectedRevision">The expected current revision of the target entity.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// A task that resolves to <see langword="true"/> when the delete succeeds;
    /// otherwise, <see langword="false"/> when the target entity does not currently match <paramref name="expectedRevision"/>.
    /// </returns>
    /// <remarks>
    /// The condition only validates the current revision of the target entity.
    /// It does not imply any additional checks against referrers or other global state.
    /// </remarks>
    ValueTask<bool> TryDeleteAsync(
        EntityId id,
        EntityRevision expectedRevision,
        CancellationToken cancellationToken = default);
}
