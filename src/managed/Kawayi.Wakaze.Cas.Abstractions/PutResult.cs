namespace Kawayi.Wakaze.Cas.Abstractions;

public readonly record struct PutResult(
    BlobId Id,
    ulong Length);
