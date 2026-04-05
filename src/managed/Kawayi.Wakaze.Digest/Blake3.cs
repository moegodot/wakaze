using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kawayi.Wakaze.Digest;

/// <summary>
/// The blake3 hash.
/// </summary>
[InlineArray(32)]
public struct Blake3 : IEquatable<Blake3>
{
    private byte _element0;

    /// <summary>
    /// Equals two blake3 hash.
    /// </summary>
    /// <param name="other">the other blake3 hash</param>
    /// <returns>true if the two hash equals</returns>
    public readonly bool Equals(Blake3 other)
    {
        ReadOnlySpan<byte> left = this;
        ReadOnlySpan<byte> right = other;
        return left.SequenceEqual(right);
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Blake3 other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        ReadOnlySpan<byte> bytes = this;
        HashCode hc = new();
        hc.AddBytes(bytes);
        return hc.ToHashCode();
    }

    public static bool operator ==(Blake3 left, Blake3 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Blake3 left, Blake3 right)
    {
        return !left.Equals(right);
    }
}
