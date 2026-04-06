namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Represents the content-derived identifier of a blob in a content-addressed storage system.
/// </summary>
/// <param name="Blake3">The BLAKE3 digest value that identifies the blob content.</param>
public record struct BlobId(Digest.Blake3 Blake3)
{
}
