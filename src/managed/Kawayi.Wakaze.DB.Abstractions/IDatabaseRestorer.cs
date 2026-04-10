namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Restores a dumped database into a newly provisioned database resource.
/// </summary>
public interface IDatabaseRestorer
{
    /// <summary>
    /// Restores the contents of <paramref name="dumpDirectory"/> into a target database resource.
    /// </summary>
    /// <param name="dumpDirectory">The source directory that contains the dump contents.</param>
    /// <param name="target">The target database resource to provision and restore into.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The restored database resource.</returns>
    Task<IDatabase> RestoreDumpAsync(
        string dumpDirectory,
        DatabaseProvisioningRequest target,
        CancellationToken cancellationToken = default);
}
