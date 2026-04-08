
namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Represents a reference from one entity to another entity.
/// </summary>
/// <param name="Target">The referenced entity identifier.</param>
/// <param name="Kind">The reference kind used by reachability-aware operations.</param>
public readonly record struct EntityRef(EntityId Target, RefKind Kind) : IComparable<EntityRef>
{
    /// <summary>
    /// Compares the current reference with another reference.
    /// </summary>
    /// <param name="other">The reference to compare with the current value.</param>
    /// <returns>
    /// A value less than zero when the current reference precedes <paramref name="other"/>,
    /// zero when they are equal,
    /// and a value greater than zero when the current reference follows <paramref name="other"/>.
    /// </returns>
    public int CompareTo(EntityRef other)
    {
        var l = Target.Id.CompareTo(other.Target.Id);
        if (l == 0)
        {
            return Kind.CompareTo(other.Kind);
        }
        return l;
    }
}
