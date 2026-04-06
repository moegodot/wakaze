namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Represents the result of storing content in a content-addressed storage system.
/// </summary>
/// <param name="Id">The identifier assigned to the stored content.</param>
/// <param name="Length">The length of the stored content in bytes.</param>
public readonly record struct PutResult(
    BlobId Id,
    ulong Length);
