namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Represents an expected conditional mutation failure.
/// </summary>
public abstract record EntityOpFailure;

/// <summary>
/// Indicates that the target entity is not currently visible.
/// </summary>
public sealed record EntityNotFound() : EntityOpFailure;

/// <summary>
/// Indicates that the target entity is currently visible when it was expected to be absent.
/// </summary>
public sealed record EntityAlreadyExists() : EntityOpFailure;

/// <summary>
/// Indicates that the target entity does not currently match the required revision.
/// </summary>
public sealed record RevisionMismatch : EntityOpFailure
{
    /// <summary>
    /// Initializes a new <see cref="RevisionMismatch"/>.
    /// </summary>
    /// <param name="actualRevision">
    /// The current visible revision when one exists; otherwise, <see langword="null"/>.
    /// </param>
    public RevisionMismatch(EntityRevision? actualRevision)
    {
        ActualRevision = actualRevision;
    }

    /// <summary>
    /// Gets the current visible revision when one exists.
    /// </summary>
    /// <value>The current visible revision when one exists; otherwise, <see langword="null"/>.</value>
    public EntityRevision? ActualRevision { get; }
}
