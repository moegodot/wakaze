using Kawayi.Wakaze.Entity.Abstractions;

namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Reads semantic state for entities.
/// </summary>
public interface ISemanticReader
{
    /// <summary>
    /// Retrieves the semantic state associated with the current entity view.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// A task that resolves to the current semantic state,
    /// or <see langword="null"/> when no semantic state is available for the requested entity view.
    /// </returns>
    ValueTask<SemanticClaim?> GetAsync(
        EntityId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the semantic state associated with a specific entity revision.
    /// </summary>
    /// <param name="revision">The revision token to read.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// A task that resolves to the semantic state for the requested revision,
    /// or <see langword="null"/> when the revision is not available.
    /// </returns>
    ValueTask<SemanticClaim?> GetByRevisionAsync(
        EntityRevision revision,
        CancellationToken cancellationToken = default);
}
