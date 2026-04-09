using Kawayi.Wakaze.Entity.Abstractions;

namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Projects semantic state into entity content.
/// </summary>
public interface ISemanticProjector
{
    /// <summary>
    /// Projects semantic state for an entity.
    /// </summary>
    /// <param name="entityId">The entity identifier to project.</param>
    /// <param name="claim">The semantic state to project.</param>
    /// <returns>The resulting entity projection.</returns>
    SemanticProjection Project(
        EntityId entityId,
        SemanticClaim claim);
}
