namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// The unique id for a blob in the cas system.
/// </summary>
public record struct BlobId(Digest.Blake3 Blake3)
{
}
