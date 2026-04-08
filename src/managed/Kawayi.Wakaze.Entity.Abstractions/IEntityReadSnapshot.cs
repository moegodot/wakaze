namespace Kawayi.Wakaze.Entity.Abstractions;

/// <summary>
/// Represents a stable point-in-time snapshot for entity reads.
/// </summary>
/// <remarks>
/// Reads performed through the same snapshot observe a self-consistent view that is not affected by later commits.
/// Dispose the snapshot when it is no longer needed.
/// </remarks>
public interface IEntityReadSnapshot : IEntityReader, IAsyncDisposable
{
}
