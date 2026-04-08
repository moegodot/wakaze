namespace Kawayi.Wakaze.Entity.Abstractions;

public interface IEntityStore :
    IEntityReader,
    IEntitySnapshotSource,
    IEntityAtomicExecutor,
    IEntityHistoryReader,
    IEntityGraphWriter
{
}
