namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Provisions new database resources without assuming a local data directory layout.
/// </summary>
public interface IDatabaseProvisioner
{
    /// <summary>
    /// Creates a new database resource from the supplied provisioning request.
    /// </summary>
    /// <param name="request">The provider, location, and optional provisioning settings.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The provisioned database resource.</returns>
    Task<IDatabase> CreateAsync(
        DatabaseProvisioningRequest request,
        CancellationToken cancellationToken = default);
}
