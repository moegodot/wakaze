using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Entity.Abstractions;

namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Represents a semantic reference edge extracted for indexing.
/// </summary>
public readonly struct SemanticEdge : IEquatable<SemanticEdge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticEdge"/> struct.
    /// </summary>
    /// <param name="sourceEntityId">The source entity identifier.</param>
    /// <param name="sourceRevision">The source entity revision.</param>
    /// <param name="ownerSchema">The exact semantic schema that owns the relation.</param>
    /// <param name="relationKey">The relation key within the owning semantic value.</param>
    /// <param name="targetEntityId">The referenced entity identifier.</param>
    /// <param name="reachability">The reachability kind projected for the relation.</param>
    /// <param name="ordinal">The zero-based ordinal within the relation.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="relationKey"/> is empty or whitespace,
    /// when <paramref name="ordinal"/> is negative,
    /// or when <paramref name="sourceRevision"/> does not belong to <paramref name="sourceEntityId"/>.
    /// </exception>
    public SemanticEdge(
        EntityId sourceEntityId,
        EntityRevision sourceRevision,
        SchemaId ownerSchema,
        string relationKey,
        EntityId targetEntityId,
        RefKind reachability,
        int ordinal)
    {
        if (sourceRevision.EntityId != sourceEntityId)
        {
            throw new ArgumentException(
                "The source revision must belong to the source entity.",
                nameof(sourceRevision));
        }

        if (string.IsNullOrWhiteSpace(relationKey))
        {
            throw new ArgumentException("The relation key cannot be empty.", nameof(relationKey));
        }

        if (ordinal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, "The ordinal cannot be negative.");
        }

        SourceEntityId = sourceEntityId;
        SourceRevision = sourceRevision;
        OwnerSchema = ownerSchema;
        RelationKey = relationKey;
        TargetEntityId = targetEntityId;
        Reachability = reachability;
        Ordinal = ordinal;
    }

    /// <summary>
    /// Gets the source entity identifier.
    /// </summary>
    public EntityId SourceEntityId { get; }

    /// <summary>
    /// Gets the source entity revision.
    /// </summary>
    public EntityRevision SourceRevision { get; }

    /// <summary>
    /// Gets the exact semantic schema that owns the relation.
    /// </summary>
    public SchemaId OwnerSchema { get; }

    /// <summary>
    /// Gets the relation key within the owning semantic value.
    /// </summary>
    public string RelationKey { get; }

    /// <summary>
    /// Gets the referenced entity identifier.
    /// </summary>
    public EntityId TargetEntityId { get; }

    /// <summary>
    /// Gets the reachability kind projected for the relation.
    /// </summary>
    public RefKind Reachability { get; }

    /// <summary>
    /// Gets the zero-based ordinal within the relation.
    /// </summary>
    public int Ordinal { get; }

    /// <summary>
    /// Determines whether the current edge is equal to another edge.
    /// </summary>
    /// <param name="other">The edge to compare with the current value.</param>
    /// <returns><see langword="true"/> when the edges are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(SemanticEdge other)
    {
        return SourceEntityId.Equals(other.SourceEntityId)
               && SourceRevision.Equals(other.SourceRevision)
               && OwnerSchema.Equals(other.OwnerSchema)
               && string.Equals(RelationKey, other.RelationKey, StringComparison.Ordinal)
               && TargetEntityId.Equals(other.TargetEntityId)
               && Reachability == other.Reachability
               && Ordinal == other.Ordinal;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current edge.
    /// </summary>
    /// <param name="obj">The object to compare with the current edge.</param>
    /// <returns><see langword="true"/> when the specified object is an equal <see cref="SemanticEdge"/>; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is SemanticEdge other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current edge.
    /// </summary>
    /// <returns>A hash code for the current edge.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            SourceEntityId,
            SourceRevision,
            OwnerSchema,
            StringComparer.Ordinal.GetHashCode(RelationKey),
            TargetEntityId,
            Reachability,
            Ordinal);
    }

    /// <summary>
    /// Compares two semantic edges for equality.
    /// </summary>
    /// <param name="left">The first edge.</param>
    /// <param name="right">The second edge.</param>
    /// <returns><see langword="true"/> when the edges are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(SemanticEdge left, SemanticEdge right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two semantic edges for inequality.
    /// </summary>
    /// <param name="left">The first edge.</param>
    /// <param name="right">The second edge.</param>
    /// <returns><see langword="true"/> when the edges are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(SemanticEdge left, SemanticEdge right)
    {
        return !left.Equals(right);
    }
}
