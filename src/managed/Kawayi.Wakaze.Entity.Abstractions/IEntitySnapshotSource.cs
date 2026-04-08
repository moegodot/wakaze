namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Opens stable point-in-time snapshots for entity reads.
/// </summary>
public interface IEntitySnapshotSource
{
    /// <summary>
    /// Opens a stable point-in-time snapshot.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// A task that resolves to a snapshot whose reads observe a self-consistent view that is not affected by later commits.
    /// </returns>
    ValueTask<IEntityReadSnapshot> OpenSnapshotAsync(
        CancellationToken cancellationToken = default);
}
