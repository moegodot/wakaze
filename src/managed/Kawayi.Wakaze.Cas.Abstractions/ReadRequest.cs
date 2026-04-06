namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Represents a request to open a blob for reading.
/// </summary>
/// <param name="Id">The identifier of the blob to read.</param>
/// <param name="Range">The requested range within the blob.</param>
public record struct ReadRequest(BlobId Id, BlobRange Range)
{
}
