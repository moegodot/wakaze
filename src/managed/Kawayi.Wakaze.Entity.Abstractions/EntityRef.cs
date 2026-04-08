
namespace Kawayi.Wakaze.Entity.Abstractions;

public readonly record struct EntityRef(EntityId Target, RefKind Kind) : IComparable<EntityRef>
{
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
