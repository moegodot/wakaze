namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Exposes write operations for a content-addressed storage system.
/// </summary>
public interface ICasWriter
{
    /// <summary>
    /// Stores the content from the provided stream and returns its identifier.
    /// </summary>
    /// <param name="content">The readable stream that provides the blob content.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A value that resolves to the identifier and length of the stored content.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is not readable.</exception>
    ValueTask<PutResult> PutAsync(
        Stream content,
        CancellationToken cancellationToken = default);
}
