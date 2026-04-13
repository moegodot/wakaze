namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Provides read and write operations within an atomic execution boundary.
/// Reads performed through the same write context observe writes
/// and deletes that were previously accepted by that context,
/// even before the atomic boundary commits.
/// </summary>
public interface IEntityWriteContext : IEntityReader
{
    /// <summary>
    /// Writes the supplied entity content.
    /// Performs a full replacement upsert for the current visible state.
    /// </summary>
    /// <param name="entity">The entity content to store.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the write operation finishes.</returns>
    ValueTask PutAsync(
        Entity entity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified entity.
    /// Removes the entity from the current visible state without implying physical erasure of retained history.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the delete operation finishes.</returns>
    ValueTask DeleteAsync(
        EntityId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a logical reference edge within the current atomic boundary.
    /// </summary>
    /// <param name="entityRef">The logical reference to add.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the logical reference has been accepted.</returns>
    ValueTask AddRefAsync(EntityRef entityRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a logical reference edge within the current atomic boundary.
    /// </summary>
    /// <param name="from">The source entity identifier.</param>
    /// <param name="to">The target entity identifier.</param>
    /// <param name="refKind">The reachability semantics of the reference to remove.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when the logical reference has been removed.</returns>
    ValueTask RemoveRefAsync(
        EntityId from,
        EntityId to,
        RefKind refKind,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the supplied entity content when all supplied preconditions match the current visible state.
    /// </summary>
    /// <param name="entity">The entity content to store.</param>
    /// <param name="preconditions">The conditions that must match inside the same atomic boundary before the write is accepted.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to the conditional mutation result.</returns>
    /// <remarks>
    /// Preconditions are evaluated against the current visible state inside the same atomic boundary.
    /// Implementations must throw <see cref="ArgumentException"/> when a <see cref="MustHaveRevision"/>
    /// targets a revision token that belongs to a different entity than <paramref name="entity"/>.
    /// </remarks>
    ValueTask<EntityOpResult> TryPutAsync(
        Entity entity,
        ReadOnlyMemory<EntityOpPrecondition> preconditions = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified entity when all supplied preconditions match the current visible state.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="preconditions">The conditions that must match inside the same atomic boundary before the delete is accepted.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that resolves to the conditional mutation result.</returns>
    /// <remarks>
    /// Preconditions are evaluated against the current visible state inside the same atomic boundary.
    /// Implementations must throw <see cref="ArgumentException"/> when a <see cref="MustHaveRevision"/>
    /// targets a revision token that belongs to a different entity than <paramref name="id"/>.
    /// </remarks>
    ValueTask<EntityOpResult> TryDeleteAsync(
        EntityId id,
        ReadOnlyMemory<EntityOpPrecondition> preconditions = default,
        CancellationToken cancellationToken = default);
}
