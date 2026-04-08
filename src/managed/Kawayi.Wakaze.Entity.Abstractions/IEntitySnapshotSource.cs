namespace Kawayi.Wakaze.Entity.Abstractions;

public interface IEntitySnapshotSource
{
    ValueTask<IEntityReadSnapshot> OpenSnapshotAsync(
        CancellationToken cancellationToken = default);
}
