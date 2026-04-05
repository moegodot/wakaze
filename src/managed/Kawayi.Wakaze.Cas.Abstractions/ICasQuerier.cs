namespace Kawayi.Wakaze.Cas.Abstractions;

public interface ICasQuerier
{
    bool Exists(BlobId id);
    ulong GetLength(BlobId id);
}
