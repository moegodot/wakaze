using System.Collections.Immutable;

namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Represents a loaded current-state entity aggregate together with requested logical edges.
/// </summary>
public sealed record class EntityLoadResult
{
    /// <summary>
    /// Initializes a new <see cref="EntityLoadResult"/>.
    /// </summary>
    /// <param name="entity">The currently visible entity state.</param>
    /// <param name="outgoingRefs">The requested outgoing logical references.</param>
    /// <param name="incomingRefs">The requested incoming logical references.</param>
    public EntityLoadResult(
        Entity entity,
        ImmutableArray<EntityRef> outgoingRefs,
        ImmutableArray<EntityRef> incomingRefs)
    {
        Entity = entity;
        OutgoingRefs = outgoingRefs;
        IncomingRefs = incomingRefs;
    }

    /// <summary>
    /// Gets the currently visible entity state.
    /// </summary>
    /// <value>The currently visible entity state.</value>
    public Entity Entity { get; }

    /// <summary>
    /// Gets the requested outgoing logical references.
    /// </summary>
    /// <value>The requested outgoing logical references.</value>
    public ImmutableArray<EntityRef> OutgoingRefs { get; }

    /// <summary>
    /// Gets the requested incoming logical references.
    /// </summary>
    /// <value>The requested incoming logical references.</value>
    public ImmutableArray<EntityRef> IncomingRefs { get; }
}
