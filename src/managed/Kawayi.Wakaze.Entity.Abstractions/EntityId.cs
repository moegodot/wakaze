namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// global unique identity on behalf of an entity.
/// </summary>
/// <param name="Id">The unique id</param>
public readonly record struct EntityId(Guid Id) : IComparable<EntityId>
{
    /// <summary>
    /// Generate a new <see cref="EntityId"/>.
    ///
    /// This is the recommended way to generate a new <see cref="EntityId"/>.
    /// </summary>
    /// <returns>A new unique <see cref="EntityId"/></returns>
    public static EntityId GenerateNew()
    {
        return new EntityId(Guid.CreateVersion7());
    }

    public int CompareTo(EntityId other)
    {
        return Id.CompareTo(other.Id);
    }
}
