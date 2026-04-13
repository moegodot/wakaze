namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Represents a composite entity store surface that combines current-state reads, aggregate loading,
/// stable snapshots, atomic execution, and historical reads.
/// </summary>
public interface IEntityStore :
    IEntityReader,
    IEntitySnapshotSource,
    IEntityAtomicExecutor,
    IEntityHistoryReader
{
}
