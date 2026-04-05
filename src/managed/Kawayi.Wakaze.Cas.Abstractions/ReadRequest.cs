namespace Kawayi.Wakaze.Cas.Abstractions;

public record struct ReadRequest(BlobId Id, BlobRange Range)
{
}
