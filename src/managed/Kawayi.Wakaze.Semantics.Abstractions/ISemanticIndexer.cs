using System.Collections.Immutable;
using Kawayi.Wakaze.Entity.Abstractions;

namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Extracts indexable edges and properties from semantic state.
/// </summary>
public interface ISemanticIndexer
{
    /// <summary>
    /// Extracts semantic edges from a semantic claim.
    /// </summary>
    /// <param name="entityId">The entity identifier that owns the semantic claim.</param>
    /// <param name="revision">The entity revision that produced the semantic claim.</param>
    /// <param name="claim">The semantic claim to inspect.</param>
    /// <returns>The extracted semantic edges.</returns>
    ImmutableArray<SemanticEdge> ExtractEdges(
        EntityId entityId,
        EntityRevision revision,
        SemanticClaim claim);

    /// <summary>
    /// Extracts semantic properties from a semantic claim.
    /// </summary>
    /// <param name="entityId">The entity identifier that owns the semantic claim.</param>
    /// <param name="revision">The entity revision that produced the semantic claim.</param>
    /// <param name="claim">The semantic claim to inspect.</param>
    /// <returns>The extracted semantic properties.</returns>
    ImmutableArray<SemanticProperty> ExtractProperties(
        EntityId entityId,
        EntityRevision revision,
        SemanticClaim claim);
}
