namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Exposes blob existence and metadata queries for a content-addressed storage system.
/// </summary>
public interface ICasQuerier : IDisposable
{
    /// <summary>
    /// Determines whether a blob with the specified identifier exists.
    /// </summary>
    /// <param name="id">The identifier of the blob to check.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A value that resolves to <see langword="true"/> when the blob exists; otherwise, <see langword="false"/>.</returns>
    ValueTask<bool> ExistsAsync(
        BlobId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves metadata for the specified blob.
    /// </summary>
    /// <param name="id">The identifier of the blob to inspect.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A value that resolves to the blob metadata, or <see langword="null"/> when the blob does not exist.</returns>
    ValueTask<BlobStat?> StatAsync(
        BlobId id,
        CancellationToken cancellationToken = default);
}
