namespace Kawayi.Wakaze.Cas.Abstractions;

public interface ICasQuerier : IDisposable
{
    ValueTask<bool> ExistsAsync(
        BlobId id,
        CancellationToken cancellationToken = default);

    ValueTask<BlobStat?> StatAsync(
        BlobId id,
        CancellationToken cancellationToken = default);
}
