using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Abstractions.Schema;
using Kawayi.Wakaze.Cas.Abstractions;

namespace Kawayi.Wakaze.Entity.Abstractions;

public sealed record BlobRef
{
    public EntityId From { get; init; }

    public BlobId To { get; init; }

    public SchemaId<PluginSchema> Creator { get; init; }

    public string Description { get; init; }

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
