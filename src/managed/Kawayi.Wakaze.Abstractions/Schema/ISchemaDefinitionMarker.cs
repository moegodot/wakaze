namespace Kawayi.Wakaze.Abstractions.Schema;

/// <summary>
/// Marks a compile-time schema definition.
/// </summary>
public interface ISchemaDefinitionMarker
{
    /// <summary>
    /// Gets the exact schema identifier.
    /// </summary>
    static abstract SchemaId Schema { get; }
}
