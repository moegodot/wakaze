namespace Kawayi.Wakaze.Abstractions;

public readonly record struct EntityRef(EntityId Target, RefKind Kind)
{
}
