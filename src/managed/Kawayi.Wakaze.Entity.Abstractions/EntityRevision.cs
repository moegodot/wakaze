namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Represents an opaque revision token for a specific entity revision.
/// </summary>
/// <remarks>
/// Callers should treat this type as a value used for optimistic concurrency and historical lookup.
/// The public contract does not define any ordering or monotonicity semantics.
/// </remarks>
public readonly record struct EntityRevision
{
    /// <summary>
    /// Initializes a new <see cref="EntityRevision"/>.
    /// </summary>
    /// <param name="entityId">The entity that owns the revision.</param>
    /// <param name="token">The opaque token that identifies the revision.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="token"/> is empty.</exception>
    public EntityRevision(EntityId entityId, Guid token)
    {
        if (token == Guid.Empty)
        {
            throw new ArgumentException("The revision token must not be empty.", nameof(token));
        }

        EntityId = entityId;
        Token = token;
    }

    /// <summary>
    /// Gets the entity that owns the revision.
    /// </summary>
    /// <value>The entity that owns the revision.</value>
    public EntityId EntityId { get; }

    /// <summary>
    /// Gets the opaque token that identifies the revision.
    /// </summary>
    /// <value>The opaque token that identifies the revision.</value>
    public Guid Token { get; }

    /// <summary>
    /// Creates a new revision token for the specified entity.
    /// </summary>
    /// <param name="entityId">The entity that owns the revision.</param>
    /// <returns>A newly generated entity revision token.</returns>
    public static EntityRevision GenerateNew(EntityId entityId)
    {
        return new EntityRevision(entityId, Guid.CreateVersion7());
    }
}
