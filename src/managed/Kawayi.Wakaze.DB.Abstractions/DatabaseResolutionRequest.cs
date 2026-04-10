namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Describes a database resource that should be resolved from an existing location.
/// </summary>
/// <param name="Location">
/// The location to inspect. Opaque locations carry a versioned provider-specific document.
/// </param>
/// <param name="ProviderId">An optional preferred provider.</param>
/// <param name="Engine">An optional preferred engine family.</param>
/// <param name="Connection">
/// Optional connection settings used when the resolver must authenticate or probe the target.
/// </param>
/// <param name="Properties">Optional provider-specific resolution properties.</param>
public sealed record DatabaseResolutionRequest(
    DatabaseLocation Location,
    DatabaseProviderId? ProviderId = null,
    DatabaseEngine? Engine = null,
    DatabaseConnectionRequest Connection = default,
    IReadOnlyDictionary<string, string?>? Properties = null);
