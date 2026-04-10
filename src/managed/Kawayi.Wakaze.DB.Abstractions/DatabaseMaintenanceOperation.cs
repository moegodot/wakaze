namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Identifies a portable maintenance intent for a database resource.
/// </summary>
public enum DatabaseMaintenanceOperation
{
    /// <summary>
    /// Refreshes optimizer statistics or equivalent metadata.
    /// </summary>
    UpdateStatistics = 0,

    /// <summary>
    /// Compacts storage or performs an equivalent space-reclaiming operation.
    /// </summary>
    CompactStorage = 1,

    /// <summary>
    /// Rebuilds indexes or performs an equivalent index-maintenance operation.
    /// </summary>
    RebuildIndexes = 2
}
