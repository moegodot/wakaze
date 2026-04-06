using Kawayi.Wakaze.Cas.Abstractions;

namespace Kawayi.Wakaze.Cas.Local;

internal sealed class BlobPathStrategy
{
    public BlobPathStrategy(string rootPath)
    {
        RootPath = rootPath;
        ContentRoot = Path.Combine(rootPath, "content");
        TempRoot = Path.Combine(rootPath, "temp");
    }

    public string RootPath { get; }

    public string ContentRoot { get; }

    public string TempRoot { get; }

    public string GetContentPath(BlobId id)
    {
        var hex = BlobIdHex.Format(id);
        return GetContentPath(hex);
    }

    public string GetContentPath(string hexDigest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hexDigest);

        return Path.Combine(
            ContentRoot,
            hexDigest[..2],
            hexDigest[2..4],
            hexDigest);
    }
}
