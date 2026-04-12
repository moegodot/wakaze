using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Represents the provider-neutral location of a database resource.
/// </summary>
/// <param name="Kind">The shape of the location model.</param>
public abstract record DatabaseLocation(DatabaseLocationKind Kind);

/// <summary>
/// Describes a database resource addressed by a local file path.
/// </summary>
/// <param name="FilePath">The database file path.</param>
public sealed record DatabaseFileLocation(string FilePath)
    : DatabaseLocation(DatabaseLocationKind.File);

/// <summary>
/// Describes a database resource addressed by a host, optional port, and optional database name.
/// </summary>
/// <param name="Host">The database host or endpoint name.</param>
/// <param name="Port">The optional TCP port.</param>
/// <param name="DatabaseName">The optional database name or catalog.</param>
public sealed record DatabaseEndpointLocation(
    string Host,
    int? Port = null,
    string? DatabaseName = null)
    : DatabaseLocation(DatabaseLocationKind.Endpoint);

/// <summary>
/// Describes a database resource whose location must be carried as a provider-specific document.
/// </summary>
public sealed record DatabaseOpaqueLocation : DatabaseLocation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseOpaqueLocation"/> class.
    /// </summary>
    /// <param name="schema">The exact <c>database://</c> schema identifier of the opaque location document.</param>
    /// <param name="format">The exact <c>database://</c> schema identifier of the serialized payload format.</param>
    /// <param name="payload">The serialized opaque location payload.</param>
    /// <param name="description">An optional human-readable description of the location.</param>
    public DatabaseOpaqueLocation(
        SchemaId<DatabaseScheme> schema,
        SchemaId<DatabaseScheme> format,
        ReadOnlyMemory<byte> payload,
        string? description = null)
        : base(DatabaseLocationKind.Opaque)
    {
        Schema = schema;
        Format = format;
        Payload = payload;
        Description = description;
    }

    /// <summary>
    /// Gets the exact <c>database://</c> schema identifier of the opaque location document.
    /// </summary>
    public SchemaId<DatabaseScheme> Schema { get; }

    /// <summary>
    /// Gets the exact <c>database://</c> schema identifier of the serialized payload format.
    /// </summary>
    public SchemaId<DatabaseScheme> Format { get; }

    /// <summary>
    /// Gets the serialized opaque location payload.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>
    /// Gets an optional human-readable description of the location.
    /// </summary>
    public string? Description { get; }
}
