namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Represents the summarized outcome of a database health check.
/// </summary>
/// <param name="State">The overall health state.</param>
/// <param name="Summary">A concise human-readable summary of the health check result.</param>
public readonly record struct DatabaseHealthCheckResult(
    DatabaseHealthState State,
    string Summary);
