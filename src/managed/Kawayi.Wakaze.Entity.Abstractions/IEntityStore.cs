namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Represents a composite entity store surface that combines reading, snapshots, atomic execution,
/// history access, and graph-oriented writes.
/// </summary>
public interface IEntityStore :
    IEntityReader,
    IEntitySnapshotSource,
    IEntityAtomicExecutor,
    IEntityHistoryReader,
    IEntityGraphWriter
{
}
