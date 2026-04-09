using Kawayi.Wakaze.Entity.Abstractions;
using EntityModel = Kawayi.Wakaze.Entity.Abstractions.Entity;

namespace Kawayi.Wakaze.Semantics.Abstractions;

/// <summary>
/// Represents the entity content projected from semantic state.
/// </summary>
public sealed class SemanticProjection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticProjection"/> class.
    /// </summary>
    /// <param name="entity">The projected entity content.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <see langword="null"/>.</exception>
    public SemanticProjection(EntityModel entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Entity = entity;
    }

    /// <summary>
    /// Gets the projected entity content.
    /// </summary>
    public EntityModel Entity { get; }
}
