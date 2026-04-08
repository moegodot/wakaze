using System.Collections.Immutable;

namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Present a readonly Entity in the system.
/// Built on the Blob,the Entity can holds Blob references and Entity references.
/// </summary>
public sealed class Entity : IEquatable<Entity>
{
    public EntityId Id { get; }

    public ImmutableArray<EntityRef> Refs { get; }

    public Entity(EntityId id, ImmutableArray<EntityRef> refs)
    {
        Id = id;
        Refs = refs.Sort();
    }

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id) && Refs.SequenceEqual(other.Refs);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is Entity other && Equals(other));
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in Refs)
        {
            hash.Add(item);
        }
        hash.Add(Id);
        return hash.ToHashCode();
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !Equals(left, right);
    }
}
