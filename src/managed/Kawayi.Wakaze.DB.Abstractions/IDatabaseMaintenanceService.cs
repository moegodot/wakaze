namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Executes optional health and maintenance operations for a database resource.
/// </summary>
public interface IDatabaseMaintenanceService
{
    /// <summary>
    /// Runs a health check against the specified database resource.
    /// </summary>
    /// <param name="database">The database resource to inspect.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A summarized health check result.</returns>
    Task<DatabaseHealthCheckResult> CheckHealthAsync(
        IDatabase database,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a provider-specific maintenance operation for the specified database resource.
    /// </summary>
    /// <param name="database">The database resource to maintain.</param>
    /// <param name="operation">The maintenance operation to execute.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    Task ExecuteAsync(
        IDatabase database,
        DatabaseMaintenanceOperation operation,
        CancellationToken cancellationToken = default);
}
