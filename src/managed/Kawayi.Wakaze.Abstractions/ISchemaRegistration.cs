namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Registers schema compatibility metadata and projector functions into runtime registries.
/// </summary>
public interface ISchemaRegistration
{
    /// <summary>
    /// Registers schemas and projectors into the supplied registries.
    /// </summary>
    /// <param name="compatibility">The schema compatibility graph to populate.</param>
    /// <param name="projector">The schema projector registry to populate.</param>
    void Register(SchemaCompatibilityGraph compatibility, SchemaProjectorRegistry projector);
}
