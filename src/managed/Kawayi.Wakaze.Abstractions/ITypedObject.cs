using Kawayi.Wakaze.Abstractions.Schema;

namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Represents an object bound to an exact schema identifier.
/// </summary>
public interface ITypedObject
{
    /// <summary>
    /// Gets the exact schema identity of the object.
    /// </summary>
    SchemaId SchemaId { get; }
}

/// <summary>
/// Represents an object bound to a compile-time schema definition.
/// </summary>
/// <typeparam name="TSchema">The exact schema definition.</typeparam>
public interface ITypedObject<TSchema> : ITypedObject
    where TSchema : ISchemaDefinitionMarker
{
}
