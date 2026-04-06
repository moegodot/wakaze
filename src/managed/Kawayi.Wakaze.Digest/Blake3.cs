using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kawayi.Wakaze.Digest;

/// <summary>
/// Represents a fixed 32-byte BLAKE3 digest value.
/// </summary>
/// <remarks>
/// This type models the digest bytes and their value semantics. It does not compute
/// BLAKE3 hashes by itself.
/// </remarks>
[InlineArray(32)]
public struct Blake3 : IEquatable<Blake3>
{
    private byte _element0;

    /// <summary>
    /// Determines whether two digest values contain the same 32 bytes.
    /// </summary>
    /// <param name="other">The other digest value to compare with.</param>
    /// <returns><see langword="true"/> when both values contain the same bytes; otherwise, <see langword="false"/>.</returns>
    public readonly bool Equals(Blake3 other)
    {
        ReadOnlySpan<byte> left = this;
        ReadOnlySpan<byte> right = other;
        return left.SequenceEqual(right);
    }

    /// <summary>
    /// Determines whether the specified object is a <see cref="Blake3"/> with the same byte content.
    /// </summary>
    /// <param name="obj">The object to compare with this digest value.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is a matching <see cref="Blake3"/>; otherwise, <see langword="false"/>.</returns>
    public readonly override bool Equals(object? obj)
    {
        return obj is Blake3 other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code derived from the digest bytes.
    /// </summary>
    /// <returns>A hash code for the current digest value.</returns>
    public readonly override int GetHashCode()
    {
        ReadOnlySpan<byte> bytes = this;
        HashCode hc = new();
        hc.AddBytes(bytes);
        return hc.ToHashCode();
    }

    /// <summary>
    /// Compares two digest values for byte-for-byte equality.
    /// </summary>
    /// <param name="left">The first digest value.</param>
    /// <param name="right">The second digest value.</param>
    /// <returns><see langword="true"/> when both digest values contain the same bytes; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Blake3 left, Blake3 right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two digest values for byte-for-byte inequality.
    /// </summary>
    /// <param name="left">The first digest value.</param>
    /// <param name="right">The second digest value.</param>
    /// <returns><see langword="true"/> when the digest values differ; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Blake3 left, Blake3 right)
    {
        return !left.Equals(right);
    }
}
