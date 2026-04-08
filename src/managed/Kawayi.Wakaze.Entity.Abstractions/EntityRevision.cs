
namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Represents a revision of an Entity, identified by a unique revision ID.
/// </summary>
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

    public bool Equals(EntityRevision other)
    {
        return _entityId == other._entityId && _revision == other._revision;
    }

    public override bool Equals(object? obj)
    {
        return obj is EntityRevision other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_entityId, _revision);
    }

    public override string ToString()
    {
        return $"EntityRevision({_entityId}, {_revision})";
    }
}
