namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Describes a database resource that should be provisioned by a provider-specific implementation.
/// </summary>
/// <param name="ProviderId">The provider to provision with.</param>
/// <param name="Location">
/// The target location for the new resource. Opaque locations carry a versioned provider-specific document.
/// </param>
/// <param name="DisplayName">An optional human-readable label for the new resource.</param>
/// <param name="AdministrativeConnection">
/// Optional administrative connection settings used to provision the resource.
/// </param>
/// <param name="Properties">Optional provider-specific provisioning properties.</param>
public sealed record DatabaseProvisioningRequest(
    DatabaseProviderId ProviderId,
    DatabaseLocation Location,
    string? DisplayName = null,
    DatabaseConnectionRequest AdministrativeConnection = default,
    IReadOnlyDictionary<string, string?>? Properties = null);
