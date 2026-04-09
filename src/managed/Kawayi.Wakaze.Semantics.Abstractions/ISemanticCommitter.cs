using Kawayi.Wakaze.Entity.Abstractions;

namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Persists semantic session changes and returns the resulting entity revision.
/// </summary>
public interface ISemanticCommitter
{
    /// <summary>
    /// Commits the changes tracked by a semantic session.
    /// </summary>
    /// <param name="session">The session to commit.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The resulting entity revision.</returns>
    /// <remarks>
    /// Implementations are expected to validate optimistic concurrency against the session basis revision.
    /// </remarks>
    ValueTask<EntityRevision> CommitAsync(
        ISemanticSession session,
        CancellationToken cancellationToken = default);
}
