namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Access cas system with readonly permission
/// </summary>
public interface ICasReader
{
    Stream Get(BlobId id, BlobRange range);
}
