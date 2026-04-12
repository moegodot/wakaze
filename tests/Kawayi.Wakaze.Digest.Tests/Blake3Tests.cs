using Kawayi.Wakaze.Digest;
using System.Text;

namespace Kawayi.Wakaze.Digest.Tests;

public class Blake3Tests
{
    [Test]
    public async Task EqualDigests_AreEqualAcrossAllEqualityEntryPoints()
    {
        var left = CreateDigest(0x10);
        var right = CreateDigest(0x10);

        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left.Equals((object)right)).IsTrue();
        await Assert.That(left == right).IsTrue();
        await Assert.That(left != right).IsFalse();
    }

    [Test]
    public async Task DifferentDigests_AreNotEqualAcrossAllEqualityEntryPoints()
    {
        var left = CreateDigest(0x10);
        var right = CreateDigest(0x10, (15, 0xFF));

        await Assert.That(left.Equals(right)).IsFalse();
        await Assert.That(left.Equals((object)right)).IsFalse();
        await Assert.That(left == right).IsFalse();
        await Assert.That(left != right).IsTrue();
    }

    [Test]
    public async Task EqualDigests_ProduceTheSameHashCode()
    {
        var left = CreateDigest(0x20);
        var right = CreateDigest(0x20);

        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
    }

    [Test]
    public async Task DifferentDigests_ProduceDifferentHashCodesForTheCoveredSamples()
    {
        var left = CreateDigest(0x20);
        var right = CreateDigest(0x20, (31, 0x7E));

        await Assert.That(left.GetHashCode()).IsNotEqualTo(right.GetHashCode());
    }

    [Test]
    public async Task DefaultDigest_EqualsAnExplicitlyZeroedDigest()
    {
        Blake3 left = default;
        var right = CreateZeroDigest();

        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left == right).IsTrue();
    }

    [Test]
    public async Task ToString_DefaultAndExplicitFormats_UseExpectedHexCasing()
    {
        var digest = CreateDigest(0x00);
        var lower = "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f";
        var upper = lower.ToUpperInvariant();

        await Assert.That(digest.ToString()).IsEqualTo(lower);
        await Assert.That(digest.ToString(string.Empty, null)).IsEqualTo(lower);
        await Assert.That(digest.ToString("x", null)).IsEqualTo(lower);
        await Assert.That(digest.ToString("X", null)).IsEqualTo(upper);
    }

    [Test]
    public async Task TryFormat_WritesExpectedCharacters_AndReportsBufferSizeFailures()
    {
        var digest = CreateDigest(0x00);
        var chars = new char[64];
        var tooSmallChars = new char[63];
        var utf8 = new byte[64];
        var tooSmallUtf8 = new byte[63];

        var charFormatted = digest.TryFormat(chars, out var charsWritten, default, null);
        var charFailed = digest.TryFormat(tooSmallChars, out var failedCharsWritten, default, null);
        var utf8Formatted = digest.TryFormat(utf8, out var bytesWritten, "X", null);
        var utf8Failed = digest.TryFormat(tooSmallUtf8, out var failedBytesWritten, default, null);

        await Assert.That(charFormatted).IsTrue();
        await Assert.That(charsWritten).IsEqualTo(64);
        await Assert.That(new string(chars)).IsEqualTo(digest.ToString());

        await Assert.That(charFailed).IsFalse();
        await Assert.That(failedCharsWritten).IsEqualTo(0);

        await Assert.That(utf8Formatted).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(64);
        await Assert.That(Encoding.UTF8.GetString(utf8)).IsEqualTo(digest.ToString("X", null));

        await Assert.That(utf8Failed).IsFalse();
        await Assert.That(failedBytesWritten).IsEqualTo(0);
    }

    [Test]
    public async Task Parse_AndTryParse_RoundTrip_String_And_Utf8_Input()
    {
        var digest = CreateDigest(0x11, (3, 0xFE), (31, 0xA5));
        var lower = digest.ToString();
        var upper = digest.ToString("X", null);
        var utf8Text = Encoding.UTF8.GetBytes(upper);

        await Assert.That(Blake3.Parse(lower, null)).IsEqualTo(digest);
        await Assert.That(Blake3.Parse(utf8Text, null)).IsEqualTo(digest);
        await Assert.That(Blake3.TryParse(upper, null, out var fromUpper)).IsTrue();
        await Assert.That(fromUpper).IsEqualTo(digest);
        await Assert.That(Blake3.TryParse(utf8Text, null, out var fromUtf8)).IsTrue();
        await Assert.That(fromUtf8).IsEqualTo(digest);
    }

    [Test]
    public async Task TryParse_InvalidInput_ReturnsFalse()
    {
        await Assert.That(Blake3.TryParse("abc", null, out _)).IsFalse();
        await Assert.That(Blake3.TryParse(new string('g', 64), null, out _)).IsFalse();
        await Assert.That(Blake3.TryParse(Encoding.UTF8.GetBytes(new string('z', 64)), null, out _)).IsFalse();
    }

    [Test]
    public void Parse_InvalidInput_ThrowsFormatException()
    {
        AssertThrows<ArgumentNullException>(() => Blake3.Parse((string)null!, null));
        AssertThrows<FormatException>(() => Blake3.Parse("abc", null));
        AssertThrows<FormatException>(() => Blake3.Parse(Encoding.UTF8.GetBytes("abc"), null));
    }

    [Test]
    public void Formatting_InvalidFormatSpecifier_ThrowsFormatException()
    {
        var digest = CreateDigest(0x22);
        var chars = new char[64];
        var utf8 = new byte[64];

        AssertThrows<FormatException>(() => digest.ToString("R", null));
        AssertThrows<FormatException>(() => digest.TryFormat(chars, out _, "R", null));
        AssertThrows<FormatException>(() => digest.TryFormat(utf8, out _, "R", null));
    }

    private static Blake3 CreateDigest(byte seed, params (int Index, byte Value)[] overrides)
    {
        Blake3 digest = default;
        Span<byte> bytes = digest;

        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = unchecked((byte)(seed + i));
        }

        foreach (var (index, value) in overrides)
        {
            bytes[index] = value;
        }

        return digest;
    }

    private static Blake3 CreateZeroDigest()
    {
        return default;
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
