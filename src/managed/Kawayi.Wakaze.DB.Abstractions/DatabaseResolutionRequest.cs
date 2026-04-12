using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Describes a database resource that should be resolved from an existing location.
/// </summary>
/// <param name="Location">
/// The location to inspect. Opaque locations carry a versioned provider-specific document.
/// </param>
/// <param name="ProviderId">An optional preferred provider identified by an exact <c>database://</c> schema identifier.</param>
/// <param name="Engine">An optional preferred engine identified by an exact <c>database://</c> schema identifier.</param>
/// <param name="Connection">
/// Optional connection settings used when the resolver must authenticate or probe the target.
/// </param>
/// <param name="Properties">Optional provider-specific resolution properties.</param>
public sealed record DatabaseResolutionRequest(
    DatabaseLocation Location,
    SchemaId<DatabaseScheme>? ProviderId = null,
    SchemaId<DatabaseScheme>? Engine = null,
    DatabaseConnectionRequest Connection = default,
    IReadOnlyDictionary<string, string?>? Properties = null);
