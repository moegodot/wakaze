namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Represents the content-derived identifier of a blob in a content-addressed storage system.
/// </summary>
/// <remarks>
/// Default string formatting returns <c>BlobId[blake3(0123456789abcdef...)]</c>.
/// Use format <c>R</c> to emit only the digest, for example
/// <c>0123456789abcdef...</c>.
/// Use format <c>Rx</c> or <c>RX</c> to forward the suffix format to
/// <see cref="Digest.Blake3.ToString(string?, IFormatProvider?)"/>, for example
/// <c>id.ToString("RX", null)</c>.
/// </remarks>
public readonly struct BlobId : IEquatable<BlobId>, IComparable<BlobId>, ISpanFormattable, IUtf8SpanFormattable, IParsable<BlobId>,
    IUtf8SpanParsable<BlobId>
{
    private const int DigestHexLength = 64;
    private const int PrefixLength = 14;
    private const int SuffixLength = 2;
    private const string Prefix = "BlobId[blake3(";
    private const string Suffix = ")]";
    private const int WrappedLength = PrefixLength + DigestHexLength + SuffixLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobId"/> struct.
    /// </summary>
    /// <param name="blake3">The BLAKE3 digest value that identifies the blob content.</param>
    public BlobId(Digest.Blake3 blake3)
    {
        Blake3 = blake3;
    }

    /// <summary>
    /// Gets the BLAKE3 digest value that identifies the blob content.
    /// </summary>
    public Digest.Blake3 Blake3 { get; }

    /// <summary>
    /// Determines whether two blob identifiers contain the same digest value.
    /// </summary>
    /// <param name="other">The other blob identifier to compare with.</param>
    /// <returns><see langword="true"/> when both identifiers contain the same digest; otherwise, <see langword="false"/>.</returns>
    public bool Equals(BlobId other)
    {
        return Blake3.Equals(other.Blake3);
    }

    /// <summary>
    /// Determines whether the specified object is a <see cref="BlobId"/> with the same digest value.
    /// </summary>
    /// <param name="obj">The object to compare with this blob identifier.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is a matching <see cref="BlobId"/>; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        return obj is BlobId other && Equals(other);
    }

    /// <summary>
    /// Returns a hash code derived from the digest value.
    /// </summary>
    /// <returns>A hash code for the current blob identifier.</returns>
    public override int GetHashCode()
    {
        return Blake3.GetHashCode();
    }

    /// <summary>
    /// Compares the current blob identifier with another identifier.
    /// </summary>
    /// <param name="other">The other blob identifier to compare with.</param>
    /// <returns>
    /// A value less than zero when the current identifier precedes <paramref name="other"/>,
    /// zero when they are equal,
    /// and a value greater than zero when the current identifier follows <paramref name="other"/>.
    /// </returns>
    public int CompareTo(BlobId other)
    {
        var leftDigest = Blake3;
        var rightDigest = other.Blake3;
        ReadOnlySpan<byte> left = leftDigest;
        ReadOnlySpan<byte> right = rightDigest;
        return left.SequenceCompareTo(right);
    }

    /// <summary>
    /// Compares two blob identifiers for equality.
    /// </summary>
    /// <param name="left">The first blob identifier.</param>
    /// <param name="right">The second blob identifier.</param>
    /// <returns><see langword="true"/> when both identifiers contain the same digest; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(BlobId left, BlobId right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares two blob identifiers for inequality.
    /// </summary>
    /// <param name="left">The first blob identifier.</param>
    /// <param name="right">The second blob identifier.</param>
    /// <returns><see langword="true"/> when the identifiers differ; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(BlobId left, BlobId right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Returns the blob identifier in its default wrapped form.
    /// </summary>
    /// <returns>A string such as <c>BlobId[blake3(0123456789abcdef...)]</c>.</returns>
    public override string ToString()
    {
        return ToString(null, null);
    }

    /// <summary>
    /// Formats the blob identifier.
    /// </summary>
    /// <param name="format">
    /// The format specifier.
    /// Use <see langword="null"/> or an empty string for <c>BlobId[blake3(...)]</c>.
    /// Use <c>R</c> for the raw digest.
    /// Use <c>Rx</c> or <c>RX</c> to forward <c>x</c> or <c>X</c> to the underlying digest formatter.
    /// </param>
    /// <param name="formatProvider">The format provider. This value is ignored.</param>
    /// <returns>The formatted blob identifier.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format"/> is not supported.</exception>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        _ = formatProvider;

        if (string.IsNullOrEmpty(format))
            return string.Create(WrappedLength, this, static (destination, value) =>
            {
                Prefix.AsSpan().CopyTo(destination);
                value.Blake3.TryFormat(destination.Slice(PrefixLength, DigestHexLength), out _, default, null);
                Suffix.AsSpan().CopyTo(destination[^SuffixLength..]);
            });

        if (format[0] != 'R') throw new FormatException("The format specifier is not supported.");

        var digestFormat = format.Length == 1 ? string.Empty : format[1..];
        return Blake3.ToString(digestFormat, formatProvider);
    }

    /// <summary>
    /// Formats the blob identifier into a character span.
    /// </summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">The number of characters written to <paramref name="destination"/>.</param>
    /// <param name="format">The format specifier. Supported values are <c></c>, <c>R</c>, <c>Rx</c>, and <c>RX</c>.</param>
    /// <param name="formatProvider">The format provider. This value is ignored.</param>
    /// <returns><see langword="true"/> when formatting succeeds; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format"/> is not supported.</exception>
    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? formatProvider)
    {
        _ = formatProvider;

        if (format.IsEmpty)
        {
            if (destination.Length < WrappedLength)
            {
                charsWritten = 0;
                return false;
            }

            Prefix.AsSpan().CopyTo(destination);
            Blake3.TryFormat(destination.Slice(PrefixLength, DigestHexLength), out _, default, null);
            Suffix.AsSpan().CopyTo(destination[^SuffixLength..]);
            charsWritten = WrappedLength;
            return true;
        }

        if (format[0] != 'R') throw new FormatException("The format specifier is not supported.");

        var digestFormat = format[1..];
        return Blake3.TryFormat(destination, out charsWritten, digestFormat, formatProvider);
    }

    /// <summary>
    /// Formats the blob identifier into a UTF-8 span.
    /// </summary>
    /// <param name="utf8Destination">The destination buffer.</param>
    /// <param name="bytesWritten">The number of bytes written to <paramref name="utf8Destination"/>.</param>
    /// <param name="format">The format specifier. Supported values are <c></c>, <c>R</c>, <c>Rx</c>, and <c>RX</c>.</param>
    /// <param name="formatProvider">The format provider. This value is ignored.</param>
    /// <returns><see langword="true"/> when formatting succeeds; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="format"/> is not supported.</exception>
    public bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? formatProvider)
    {
        _ = formatProvider;

        if (format.IsEmpty)
        {
            var utf8Prefix = "BlobId[blake3("u8;
            var utf8Suffix = ")]"u8;

            if (utf8Destination.Length < WrappedLength)
            {
                bytesWritten = 0;
                return false;
            }

            utf8Prefix.CopyTo(utf8Destination);
            Blake3.TryFormat(utf8Destination.Slice(utf8Prefix.Length, DigestHexLength), out _, default, null);
            utf8Suffix.CopyTo(utf8Destination[^utf8Suffix.Length..]);
            bytesWritten = WrappedLength;
            return true;
        }

        if (format[0] != 'R') throw new FormatException("The format specifier is not supported.");

        var digestFormat = format[1..];
        return Blake3.TryFormat(utf8Destination, out bytesWritten, digestFormat, formatProvider);
    }

    /// <summary>
    /// Parses a blob identifier string in wrapped form.
    /// </summary>
    /// <param name="s">A string such as <c>BlobId[blake3(0123456789abcdef...)]</c>.</param>
    /// <param name="provider">The format provider. This value is ignored.</param>
    /// <returns>The parsed blob identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="s"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="s"/> is not a valid wrapped blob identifier.</exception>
    public static BlobId Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);
        _ = provider;

        if (!TryParse(s, provider, out var result)) throw new FormatException("The value is not a valid BlobId.");

        return result;
    }

    /// <summary>
    /// Parses a UTF-8 blob identifier span in wrapped form.
    /// </summary>
    /// <param name="utf8Text">A UTF-8 span such as <c>BlobId[blake3(0123456789abcdef...)]</c>.</param>
    /// <param name="provider">The format provider. This value is ignored.</param>
    /// <returns>The parsed blob identifier.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="utf8Text"/> is not a valid wrapped blob identifier.</exception>
    public static BlobId Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider)
    {
        _ = provider;

        if (!TryParse(utf8Text, provider, out var result))
            throw new FormatException("The value is not a valid BlobId.");

        return result;
    }

    /// <summary>
    /// Attempts to parse a blob identifier string in wrapped form.
    /// </summary>
    /// <param name="s">A string such as <c>BlobId[blake3(0123456789abcdef...)]</c>.</param>
    /// <param name="provider">The format provider. This value is ignored.</param>
    /// <param name="result">The parsed blob identifier when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out BlobId result)
    {
        _ = provider;
        if (s is null)
        {
            result = default;
            return false;
        }

        return TryParseCore(s.AsSpan(), out result);
    }

    /// <summary>
    /// Attempts to parse a UTF-8 blob identifier span in wrapped form.
    /// </summary>
    /// <param name="utf8Text">A UTF-8 span such as <c>BlobId[blake3(0123456789abcdef...)]</c>.</param>
    /// <param name="provider">The format provider. This value is ignored.</param>
    /// <param name="result">The parsed blob identifier when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out BlobId result)
    {
        _ = provider;
        return TryParseCore(utf8Text, out result);
    }

    private static bool TryParseCore(ReadOnlySpan<char> text, out BlobId result)
    {
        if (text.Length != WrappedLength
            || !text[..PrefixLength].Equals(Prefix.AsSpan(), StringComparison.OrdinalIgnoreCase)
            || !text[^SuffixLength..].SequenceEqual(Suffix))
        {
            result = default;
            return false;
        }

        if (!Digest.Blake3.TryParse(text.Slice(PrefixLength, DigestHexLength).ToString(), null, out var digest))
        {
            result = default;
            return false;
        }

        result = new BlobId(digest);
        return true;
    }

    private static bool TryParseCore(ReadOnlySpan<byte> utf8Text, out BlobId result)
    {
        var utf8Prefix = "BlobId[blake3("u8;
        var utf8Suffix = ")]"u8;

        if (utf8Text.Length != WrappedLength
            || !EqualsAsciiIgnoreCase(utf8Text[..utf8Prefix.Length], utf8Prefix)
            || !utf8Text[^utf8Suffix.Length..].SequenceEqual(utf8Suffix))
        {
            result = default;
            return false;
        }

        if (!Digest.Blake3.TryParse(utf8Text.Slice(utf8Prefix.Length, DigestHexLength), null, out var digest))
        {
            result = default;
            return false;
        }

        result = new BlobId(digest);
        return true;
    }

    private static bool EqualsAsciiIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        if (left.Length != right.Length) return false;

        for (var i = 0; i < left.Length; i++)
        {
            var leftValue = left[i];
            var rightValue = right[i];

            if (leftValue == rightValue) continue;

            if (ToAsciiLower(leftValue) != ToAsciiLower(rightValue)) return false;
        }

        return true;
    }

    private static byte ToAsciiLower(byte value)
    {
        return value is >= (byte)'A' and <= (byte)'Z'
            ? (byte)(value + 32)
            : value;
    }
}
