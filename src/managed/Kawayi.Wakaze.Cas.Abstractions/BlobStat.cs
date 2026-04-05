namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Status of a blob
/// </summary>
/// <param name="Id">The id of blob</param>
/// <param name="Size">The size of the blob</param>
public record struct BlobStat(
    BlobId Id,
    ulong Size
);
