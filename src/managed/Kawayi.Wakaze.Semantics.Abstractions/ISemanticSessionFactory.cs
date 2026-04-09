using Kawayi.Wakaze.Entity.Abstractions;

namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Opens semantic sessions for single-entity edits.
/// </summary>
public interface ISemanticSessionFactory
{
    /// <summary>
    /// Opens a semantic session for the current visible state of an entity.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// A task that resolves to a semantic session,
    /// or <see langword="null"/> when the entity does not currently expose editable semantic state.
    /// </returns>
    ValueTask<ISemanticSession?> OpenAsync(
        EntityId entityId,
        CancellationToken cancellationToken = default);
}
