using System.Text;
using Kawayi.Wakaze.Cas.Abstractions;

namespace Kawayi.Wakaze.Cas.Local.Tests;

public class BlobIdTests
{
    [Test]
    public async Task EqualBlobIds_AreEqualAcrossAllEqualityEntryPoints()
    {
        var left = CreateBlobId(0x10);
        var right = CreateBlobId(0x10);

        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left.Equals((object)right)).IsTrue();
        await Assert.That(left == right).IsTrue();
        await Assert.That(left != right).IsFalse();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
    }

    [Test]
    public async Task DifferentBlobIds_AreNotEqualAcrossAllEqualityEntryPoints()
    {
        var left = CreateBlobId(0x10);
        var right = CreateBlobId(0x10, (7, 0xFF));

        await Assert.That(left.Equals(right)).IsFalse();
        await Assert.That(left.Equals((object)right)).IsFalse();
        await Assert.That(left == right).IsFalse();
        await Assert.That(left != right).IsTrue();
    }

    [Test]
    public async Task ToString_DefaultAndRawFormats_UseExpectedRepresentations()
    {
        var id = CreateBlobId(0x00);
        var rawLower = id.Blake3.ToString();
        var rawUpper = id.Blake3.ToString("X", null);
        var wrapped = $"BlobId[blake3({rawLower})]";

        await Assert.That(id.ToString()).IsEqualTo(wrapped);
        await Assert.That(id.ToString(string.Empty, null)).IsEqualTo(wrapped);
        await Assert.That(id.ToString("R", null)).IsEqualTo(rawLower);
        await Assert.That(id.ToString("Rx", null)).IsEqualTo(rawLower);
        await Assert.That(id.ToString("RX", null)).IsEqualTo(rawUpper);
    }

    [Test]
    public async Task TryFormat_WritesExpectedCharacters_AndReportsBufferSizeFailures()
    {
        var id = CreateBlobId(0x01);
        var wrapped = id.ToString();
        var raw = id.ToString("R", null);
        var chars = new char[80];
        var tooSmallChars = new char[79];
        var utf8 = new byte[64];
        var tooSmallUtf8 = new byte[63];

        var charFormatted = id.TryFormat(chars, out var charsWritten, default, null);
        var charFailed = id.TryFormat(tooSmallChars, out var failedCharsWritten, default, null);
        var utf8Formatted = id.TryFormat(utf8, out var bytesWritten, "RX", null);
        var utf8Failed = id.TryFormat(tooSmallUtf8, out var failedBytesWritten, "R", null);

        await Assert.That(charFormatted).IsTrue();
        await Assert.That(charsWritten).IsEqualTo(80);
        await Assert.That(new string(chars)).IsEqualTo(wrapped);

        await Assert.That(charFailed).IsFalse();
        await Assert.That(failedCharsWritten).IsEqualTo(0);

        await Assert.That(utf8Formatted).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(64);
        await Assert.That(Encoding.UTF8.GetString(utf8)).IsEqualTo(raw.ToUpperInvariant());

        await Assert.That(utf8Failed).IsFalse();
        await Assert.That(failedBytesWritten).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_AndTryParse_RoundTrip_Wrapped_String_And_Utf8_Input()
    {
        var id = CreateBlobId(0x11, (3, 0xFE), (31, 0xA5));
        var canonical = id.ToString();
        var mixedCase = $"BLOBID[BLAKE3({id.ToString("RX", null)})]";
        var utf8Text = Encoding.UTF8.GetBytes(mixedCase);

        await Assert.That(BlobId.Parse(canonical, null)).IsEqualTo(id);
        await Assert.That(BlobId.Parse(utf8Text, null)).IsEqualTo(id);
        await Assert.That(BlobId.TryParse(mixedCase, null, out var fromString)).IsTrue();
        await Assert.That(fromString).IsEqualTo(id);
        await Assert.That(BlobId.TryParse(utf8Text, null, out var fromUtf8)).IsTrue();
        await Assert.That(fromUtf8).IsEqualTo(id);
    }

    [Test]
    public async Task TryParse_InvalidInput_ReturnsFalse()
    {
        var raw = CreateBlobId(0x01).ToString("R", null);

        await Assert.That(BlobId.TryParse(raw, null, out _)).IsFalse();
        await Assert.That(BlobId.TryParse("BlobId[blake3(abc)]", null, out _)).IsFalse();
        await Assert.That(BlobId.TryParse(Encoding.UTF8.GetBytes("BlobId[blake3(abc)]"), null, out _)).IsFalse();
    }

    [Test]
    public void Parse_OrFormat_InvalidInput_ThrowsFormatException()
    {
        var id = CreateBlobId(0x22);
        var chars = new char[80];
        var utf8 = new byte[80];

        AssertThrows<ArgumentNullException>(() => BlobId.Parse((string)null!, null));
        AssertThrows<FormatException>(() => BlobId.Parse(id.ToString("R", null), null));
        AssertThrows<FormatException>(() => BlobId.Parse(Encoding.UTF8.GetBytes(id.ToString("R", null)), null));
        AssertThrows<FormatException>(() => id.ToString("x", null));
        AssertThrows<FormatException>(() => id.TryFormat(chars, out _, "x", null));
        AssertThrows<FormatException>(() => id.TryFormat(utf8, out _, "x", null));
    }

    private static BlobId CreateBlobId(byte seed, params (int Index, byte Value)[] overrides)
    {
        Kawayi.Wakaze.Digest.Blake3 digest = default;
        Span<byte> bytes = digest;

        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = unchecked((byte)(seed + i));
        }

        foreach (var (index, value) in overrides)
        {
            bytes[index] = value;
        }

        return new BlobId(digest);
    }

    private static void AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
            throw new Exception($"Expected {typeof(TException).Name}.");
        }
        catch (TException)
        {
        }
    }
}
