namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Represents a precondition evaluated against the current visible state inside an atomic mutation boundary.
/// </summary>
public abstract record EntityOpPrecondition;

/// <summary>
/// Requires the target entity to be absent from the current visible state.
/// </summary>
public sealed record MustNotExist : EntityOpPrecondition;

/// <summary>
/// Requires the target entity to match a specific current visible revision.
/// </summary>
public sealed record MustHaveRevision : EntityOpPrecondition
{
    /// <summary>
    /// Initializes a new <see cref="MustHaveRevision"/>.
    /// </summary>
    /// <param name="revision">The revision that the target entity must currently expose.</param>
    public MustHaveRevision(EntityRevision revision)
    {
        Revision = revision;
    }

    /// <summary>
    /// Gets the revision that the target entity must currently expose.
    /// </summary>
    /// <value>The revision that the target entity must currently expose.</value>
    public EntityRevision Revision { get; }
}

/// <summary>
/// Requires the target entity to be present in the current visible state.
/// </summary>
public sealed record MustExist : EntityOpPrecondition;
