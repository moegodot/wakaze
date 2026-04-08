namespace Kawayi.Wakaze.Cas.Abstractions.Admin;

/// <summary>
/// Exposes administrative operations for managing blobs in a content-addressed storage system.
/// </summary>
public interface ICasAdmin : IAsyncDisposable
{
    /// <summary>
    /// Attempts to delete the blob with the specified identifier.
    /// </summary>
    /// <param name="id">The identifier of the blob to delete.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A value that resolves to <see langword="true"/> when the blob existed and was deleted; otherwise, <see langword="false"/>.</returns>
    ValueTask<bool> TryDeleteAsync(
        BlobId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumerates the identifiers of blobs currently stored in the content-addressed storage system.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the enumeration.</param>
    /// <returns>An asynchronous sequence of blob identifiers present in the store.</returns>
    IAsyncEnumerable<BlobId> ScanBlobIdsAsync(
        CancellationToken cancellationToken = default);
}
