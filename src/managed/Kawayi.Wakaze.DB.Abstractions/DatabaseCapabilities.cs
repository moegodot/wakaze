namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Describes optional capabilities exposed by a database resource or provider.
/// </summary>
[Flags]
public enum DatabaseCapabilities
{
    /// <summary>
    /// No optional capabilities are declared.
    /// </summary>
    None = 0,

    /// <summary>
    /// The database supports transactional write boundaries.
    /// </summary>
    Transactions = 1 << 0,

    /// <summary>
    /// The database supports stable point-in-time read snapshots.
    /// </summary>
    SnapshotReads = 1 << 1,

    /// <summary>
    /// The database supports schema initialization or migration workflows.
    /// </summary>
    SchemaManagement = 1 << 2,

    /// <summary>
    /// The database can be exported through a logical dump workflow.
    /// </summary>
    LogicalDump = 1 << 3,

    /// <summary>
    /// The database can be restored from a logical dump workflow.
    /// </summary>
    LogicalRestore = 1 << 4,

    /// <summary>
    /// The database exposes health check operations.
    /// </summary>
    HealthChecks = 1 << 5,

    /// <summary>
    /// The database exposes provider-specific maintenance operations.
    /// </summary>
    Maintenance = 1 << 6
}
