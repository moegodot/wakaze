namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Describes how an entity reference participates in reachability-aware operations.
/// </summary>
public enum RefKind : byte
{
    /// <summary>
    /// Identifies a weak reference that does not keep the target entity reachable on its own.
    /// </summary>
    Weak = 0,

    /// <summary>
    /// Identifies a strong reference that contributes to keeping the target entity reachable.
    /// </summary>
    Strong = 1,
}
