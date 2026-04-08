
namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Represents an opaque revision token for an entity.
/// </summary>
/// <remarks>
/// Callers should treat this type as a value used for optimistic concurrency and historical lookup.
/// Its internal structure is not part of the public contract.
/// </remarks>
public readonly struct EntityRevision : IEquatable<EntityRevision>
{
    private readonly Revision _revision;
    private readonly EntityId _entityId;

    internal EntityRevision(EntityId entityId, Revision revision)
    {
        _entityId = entityId;
        _revision = revision;
    }

    internal Revision Revision => _revision;
    internal EntityId EntityId => _entityId;

    /// <summary>
    /// Determines whether the current revision token is equal to another revision token.
    /// </summary>
    /// <param name="other">The revision token to compare with the current value.</param>
    /// <returns><see langword="true"/> when the tokens are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(EntityRevision other)
    {
        return _entityId == other._entityId && _revision == other._revision;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current revision token.
    /// </summary>
    /// <param name="obj">The object to compare with the current revision token.</param>
    /// <returns><see langword="true"/> when the specified object is an equal <see cref="EntityRevision"/>; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is EntityRevision other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code for the current revision token.
    /// </summary>
    /// <returns>A hash code for the current revision token.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(_entityId, _revision);
    }

    /// <summary>
    /// Returns a string representation of the current revision token.
    /// </summary>
    /// <returns>A string representation of the current revision token.</returns>
    public override string ToString()
    {
        return $"EntityRevision({_entityId}, {_revision})";
    }
}
