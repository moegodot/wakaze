using System.Collections.Immutable;
using Kawayi.Wakaze.Cas.Abstractions;

namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Represents the current content of an entity.
/// </summary>
public sealed class Entity : IEquatable<Entity>
{
    /// <summary>
    /// Gets the stable identity of the entity.
    /// </summary>
    public EntityId Id { get; }

    /// <summary>
    /// Gets the entity references held by this entity.
    /// </summary>
    public ImmutableArray<EntityRef> Refs { get; }

    /// <summary>
    /// Gets the blob references held by this entity.
    /// </summary>
    public ImmutableArray<BlobId> BlobRefs { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity"/> class.
    /// </summary>
    /// <param name="id">The stable identity of the entity.</param>
    /// <param name="refs">The entity references held by the entity.</param>
    /// <param name="blobRefs">The blob references held by the entity.</param>
    public Entity(EntityId id, ImmutableArray<EntityRef> refs, ImmutableArray<BlobId> blobRefs)
    {
        Id = id;
        Refs = refs.Sort();
        BlobRefs = blobRefs.Sort();
    }

    /// <summary>
    /// Determines whether this entity has the same identity and references as another entity.
    /// </summary>
    /// <param name="other">The entity to compare with the current value.</param>
    /// <returns><see langword="true"/> when the entities are equal; otherwise, <see langword="false"/>.</returns>
    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id) && Refs.SequenceEqual(other.Refs) && BlobRefs.SequenceEqual(other.BlobRefs);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns><see langword="true"/> when the specified object is an equal <see cref="Entity"/>; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is Entity other && Equals(other));
    }

    /// <summary>
    /// Returns a hash code for the current entity.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in Refs)
        {
            hash.Add(item);
        }
        foreach (var item in BlobRefs)
        {
            hash.Add(item);
        }
        hash.Add(Id);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Compares two entities for equality.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns><see langword="true"/> when the entities are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Entity? left, Entity? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Compares two entities for inequality.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns><see langword="true"/> when the entities are different; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Entity? left, Entity? right)
    {
        return !Equals(left, right);
    }
}
