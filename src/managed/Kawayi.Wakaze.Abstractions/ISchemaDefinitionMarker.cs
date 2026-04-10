namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Marks a compile-time schema definition.
/// </summary>
public interface ISchemaDefinitionMarker
{
    /// <summary>
    /// Gets the exact schema URI.
    /// </summary>
    static abstract UriTypeSchema Schema { get; }
}
