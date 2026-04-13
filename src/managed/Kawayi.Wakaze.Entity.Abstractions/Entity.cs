using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Abstractions.Schema;

namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Represents the current visible state of an entity.
/// </summary>
public sealed record Entity
{
    public SchemaId<PluginSchema> Owner { get; init; }

    public string Description { get; init; }

    /// <summary>
    /// Gets the stable identity of the entity.
    /// </summary>
    /// <value>The stable identity of the entity.</value>
    public EntityId Id { get; init; }

    /// <summary>
    /// Gets the current revision token for the visible state.
    /// </summary>
    /// <value>The current revision token for the visible state.</value>
    public EntityRevision Revision { get; init; }

    /// <summary>
    /// Gets the timestamp when the entity was first created.
    /// </summary>
    /// <value>The timestamp when the entity was first created.</value>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the visible state was last updated, if any.
    /// </summary>
    /// <value>The timestamp when the visible state was last updated, if any.</value>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the state was removed from the current visible view, if any.
    /// </summary>
    /// <value>The timestamp when the state was removed from the current visible view, if any.</value>
    public DateTime? DeletedAt { get; init; }
}
