namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Defines optional data to include when loading an entity aggregate from the current visible view.
/// </summary>
public readonly record struct EntityLoadOptions
{
    /// <summary>
    /// Initializes a new <see cref="EntityLoadOptions"/>.
    /// </summary>
    /// <param name="includeOutgoingRefs">
    /// <see langword="true"/> to include outgoing logical references from the loaded entity.
    /// </param>
    /// <param name="includeIncomingRefs">
    /// <see langword="true"/> to include incoming logical references into the loaded entity.
    /// </param>
    public EntityLoadOptions(bool includeOutgoingRefs = false, bool includeIncomingRefs = false)
    {
        IncludeOutgoingRefs = includeOutgoingRefs;
        IncludeIncomingRefs = includeIncomingRefs;
    }

    /// <summary>
    /// Gets a value indicating whether outgoing logical references should be included.
    /// </summary>
    /// <value><see langword="true"/> when outgoing logical references should be included.</value>
    public bool IncludeOutgoingRefs { get; }

    /// <summary>
    /// Gets a value indicating whether incoming logical references should be included.
    /// </summary>
    /// <value><see langword="true"/> when incoming logical references should be included.</value>
    public bool IncludeIncomingRefs { get; }
}
