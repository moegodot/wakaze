namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Represents the stored metadata of a blob.
/// </summary>
/// <param name="Id">The identifier of the blob.</param>
/// <param name="Size">The size of the blob in bytes.</param>
public record struct BlobStat(
    BlobId Id,
    ulong Size
);
