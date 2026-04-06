namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Exposes read-only access to blobs stored in a content-addressed storage system.
/// </summary>
public interface ICasReader : ICasQuerier
{
    /// <summary>
    /// Opens a stream for reading the requested blob range.
    /// </summary>
    /// <param name="request">The blob and range to read.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A value that resolves to a readable stream for the requested blob range.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the requested blob does not exist.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the requested range exceeds the blob length.</exception>
    ValueTask<Stream> OpenReadAsync(
        ReadRequest request,
        CancellationToken cancellationToken = default);
}
