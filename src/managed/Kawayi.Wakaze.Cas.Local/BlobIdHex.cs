using Kawayi.Wakaze.Cas.Abstractions;

namespace Kawayi.Wakaze.Cas.Local;

internal static class BlobIdHex
{
    public static string Format(BlobId id)
    {
        return string.Create(64, id.Blake3, static (chars, digest) =>
        {
            ReadOnlySpan<byte> bytes = digest;

            for (var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                chars[i * 2] = ToHexChar(b >> 4);
                chars[i * 2 + 1] = ToHexChar(b & 0xF);
            }
        });
    }

    private static char ToHexChar(int value)
    {
        return (char)(value < 10 ? '0' + value : 'a' + (value - 10));
    }
}
