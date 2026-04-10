namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Describes the overall health state of a database resource.
/// </summary>
public enum DatabaseHealthState
{
    /// <summary>
    /// The health state is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The resource is healthy.
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// The resource is available but degraded.
    /// </summary>
    Degraded = 2,

    /// <summary>
    /// The resource is unhealthy.
    /// </summary>
    Unhealthy = 3
}
