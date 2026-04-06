using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kawayi.Wakaze.Digest;

/// <summary>
/// Represents a fixed 32-byte BLAKE3 digest value.
/// </summary>
/// <remarks>
/// This type models the digest bytes and their value semantics. It does not compute
/// BLAKE3 hashes by itself.
/// Default string formatting returns 64 lowercase hexadecimal characters, for example
/// <c>0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef</c>.
/// Use format <c>x</c> for lowercase hexadecimal and format <c>X</c> for uppercase
/// hexadecimal, for example <c>digest.ToString("X", null)</c>.
/// </remarks>
[InlineArray(32)]
public struct Blake3 : IEquatable<Blake3>, ISpanFormattable, IUtf8SpanFormattable, IParsable<Blake3>,
    IUtf8SpanParsable<Blake3>
{
    private const int ByteLength = 32;
    private const int HexLength = ByteLength * 2;

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

    /// <summary>
    /// Returns the digest as 64 lowercase hexadecimal characters with no prefix.
    /// </summary>
    /// <returns>A lowercase hexadecimal string such as <c>0123456789abcdef...</c>.</returns>
    public readonly override string ToString()
    {
        return ToString(null, null);
    }

    /// <summary>
    /// Formats the digest as hexadecimal text.
    /// </summary>
    /// <param name="format">
    /// The format specifier.
    /// Use <see langword="null"/>, an empty string, or <c>x</c> for lowercase hexadecimal.
    /// Use <c>X</c> for uppercase hexadecimal.
    /// </param>
    /// <param name="formatProvider">The format provider. This value is ignored.</param>
    /// <returns>A hexadecimal string representation of the digest.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format"/> is not supported.</exception>
    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        _ = formatProvider;

        var uppercase = ParseHexFormat(format);
        return string.Create(HexLength, (Digest: this, Uppercase: uppercase),
            static (destination, state) => { state.Digest.WriteHex(destination, state.Uppercase); });
    }

    /// <summary>
    /// Formats the digest into a character span.
    /// </summary>
    /// <param name="destination">The destination buffer that receives 64 hexadecimal characters.</param>
    /// <param name="charsWritten">The number of characters written to <paramref name="destination"/>.</param>
    /// <param name="format">The format specifier. Supported values are <c></c>, <c>x</c>, and <c>X</c>.</param>
    /// <param name="formatProvider">The format provider. This value is ignored.</param>
    /// <returns><see langword="true"/> when formatting succeeds; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format"/> is not supported.</exception>
    public readonly bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? formatProvider)
    {
        _ = formatProvider;

        var uppercase = ParseHexFormat(format);
        if (destination.Length < HexLength)
        {
            charsWritten = 0;
            return false;
        }

        WriteHex(destination, uppercase);
        charsWritten = HexLength;
        return true;
    }

    /// <summary>
    /// Formats the digest into a UTF-8 span.
    /// </summary>
    /// <param name="utf8Destination">The destination buffer that receives 64 ASCII hexadecimal bytes.</param>
    /// <param name="bytesWritten">The number of bytes written to <paramref name="utf8Destination"/>.</param>
    /// <param name="format">The format specifier. Supported values are <c></c>, <c>x</c>, and <c>X</c>.</param>
    /// <param name="formatProvider">The format provider. This value is ignored.</param>
    /// <returns><see langword="true"/> when formatting succeeds; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format"/> is not supported.</exception>
    public readonly bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? formatProvider)
    {
        _ = formatProvider;

        var uppercase = ParseHexFormat(format);
        if (utf8Destination.Length < HexLength)
        {
            bytesWritten = 0;
            return false;
        }

        WriteHexUtf8(utf8Destination, uppercase);
        bytesWritten = HexLength;
        return true;
    }

    /// <summary>
    /// Parses a hexadecimal digest string.
    /// </summary>
    /// <param name="s">A 64-character hexadecimal digest string.</param>
    /// <param name="provider">The format provider. This value is ignored.</param>
    /// <returns>The parsed digest value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="s"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="s"/> is not a valid 64-character hexadecimal digest.</exception>
    public static Blake3 Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);
        _ = provider;

        if (!TryParse(s, provider, out var result))
            throw new FormatException("The value is not a valid 64-character BLAKE3 digest.");

        return result;
    }

    /// <summary>
    /// Parses a UTF-8 hexadecimal digest span.
    /// </summary>
    /// <param name="utf8Text">A 64-byte ASCII hexadecimal digest span.</param>
    /// <param name="provider">The format provider. This value is ignored.</param>
    /// <returns>The parsed digest value.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="utf8Text"/> is not a valid 64-byte hexadecimal digest.</exception>
    public static Blake3 Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
    {
        _ = provider;

        if (!TryParse(utf8Text, provider, out var result))
            throw new FormatException("The value is not a valid 64-character BLAKE3 digest.");

        return result;
    }

    /// <summary>
    /// Attempts to parse a hexadecimal digest string.
    /// </summary>
    /// <param name="s">A 64-character hexadecimal digest string.</param>
    /// <param name="provider">The format provider. This value is ignored.</param>
    /// <param name="result">The parsed digest value when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out Blake3 result)
    {
        _ = provider;
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParseHex(s.AsSpan(), out result);
    }

    /// <summary>
    /// Attempts to parse a UTF-8 hexadecimal digest span.
    /// </summary>
    /// <param name="utf8Text">A 64-byte ASCII hexadecimal digest span.</param>
    /// <param name="provider">The format provider. This value is ignored.</param>
    /// <param name="result">The parsed digest value when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Blake3 result)
    {
        _ = provider;
        return TryParseHex(utf8Text, out result);
    }

    private readonly void WriteHex(Span<char> destination, bool uppercase)
    {
        ReadOnlySpan<byte> bytes = this;
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            destination[i * 2] = ToHexChar(b >> 4, uppercase);
            destination[i * 2 + 1] = ToHexChar(b & 0xF, uppercase);
        }
    }

    private readonly void WriteHexUtf8(Span<byte> destination, bool uppercase)
    {
        ReadOnlySpan<byte> bytes = this;
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            destination[i * 2] = (byte)ToHexChar(b >> 4, uppercase);
            destination[i * 2 + 1] = (byte)ToHexChar(b & 0xF, uppercase);
        }
    }

    private static bool ParseHexFormat(string? format)
    {
        if (string.IsNullOrEmpty(format)) return false;
        return ParseHexFormat(format.AsSpan());
    }

    private static bool ParseHexFormat(ReadOnlySpan<char> format)
    {
        if (format.IsEmpty) return false;

        if (format.Length == 1)
            return format[0] switch
            {
                'x' => false,
                'X' => true,
                _ => throw new FormatException("The format specifier is not supported.")
            };

        throw new FormatException("The format specifier is not supported.");
    }

    private static bool TryParseHex(ReadOnlySpan<char> text, out Blake3 result)
    {
        if (text.Length != HexLength)
        {
            result = default;
            return false;
        }

        result = default;
        Span<byte> bytes = result;

        for (var i = 0; i < bytes.Length; i++)
            if (!TryParseHexByte(text[i * 2], text[i * 2 + 1], out bytes[i]))
            {
                result = default;
                return false;
            }

        return true;
    }

    private static bool TryParseHex(ReadOnlySpan<byte> text, out Blake3 result)
    {
        if (text.Length != HexLength)
        {
            result = default;
            return false;
        }

        result = default;
        Span<byte> bytes = result;

        for (var i = 0; i < bytes.Length; i++)
            if (!TryParseHexByte(text[i * 2], text[i * 2 + 1], out bytes[i]))
            {
                result = default;
                return false;
            }

        return true;
    }

    private static bool TryParseHexByte(char high, char low, out byte value)
    {
        if (TryGetHexValue(high, out var highValue) && TryGetHexValue(low, out var lowValue))
        {
            value = (byte)((highValue << 4) | lowValue);
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryParseHexByte(byte high, byte low, out byte value)
    {
        if (TryGetHexValue(high, out var highValue) && TryGetHexValue(low, out var lowValue))
        {
            value = (byte)((highValue << 4) | lowValue);
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryGetHexValue(char value, out int hexValue)
    {
        hexValue = value switch
        {
            >= '0' and <= '9' => value - '0',
            >= 'a' and <= 'f' => value - 'a' + 10,
            >= 'A' and <= 'F' => value - 'A' + 10,
            _ => -1
        };

        return hexValue >= 0;
    }

    private static bool TryGetHexValue(byte value, out int hexValue)
    {
        hexValue = value switch
        {
            >= (byte)'0' and <= (byte)'9' => value - '0',
            >= (byte)'a' and <= (byte)'f' => value - 'a' + 10,
            >= (byte)'A' and <= (byte)'F' => value - 'A' + 10,
            _ => -1
        };

        return hexValue >= 0;
    }

    private static char ToHexChar(int value, bool uppercase)
    {
        return (char)(value < 10
            ? '0' + value
            : (uppercase ? 'A' : 'a') + (value - 10));
    }
}
