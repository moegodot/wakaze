namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Represents the stable identity of an entity.
/// </summary>
/// <param name="Id">The globally unique identifier value.</param>
public readonly record struct EntityId(Guid Id) : IComparable<EntityId>
{
    /// <summary>
    /// Creates a new <see cref="EntityId"/>.
    /// </summary>
    /// <returns>A newly generated <see cref="EntityId"/>.</returns>
    public static EntityId GenerateNew()
    {
        return new EntityId(Guid.CreateVersion7());
    }

    /// <summary>
    /// Compares the current identifier with another identifier.
    /// </summary>
    /// <param name="other">The identifier to compare with the current value.</param>
    /// <returns>
    /// A value less than zero when the current identifier precedes <paramref name="other"/>,
    /// zero when they are equal,
    /// and a value greater than zero when the current identifier follows <paramref name="other"/>.
    /// </returns>
    public int CompareTo(EntityId other)
    {
        return Id.CompareTo(other.Id);
    }
}
