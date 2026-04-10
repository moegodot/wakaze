namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Identifies the shape used to describe a database location.
/// </summary>
public enum DatabaseLocationKind
{
    /// <summary>
    /// The location cannot be expressed through the built-in location models.
    /// </summary>
    Opaque = 0,

    /// <summary>
    /// The resource is addressed by a local file path.
    /// </summary>
    File = 1,

    /// <summary>
    /// The resource is addressed by a network endpoint.
    /// </summary>
    Endpoint = 2
}
