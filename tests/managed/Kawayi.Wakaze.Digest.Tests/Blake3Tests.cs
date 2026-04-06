using Kawayi.Wakaze.Digest;

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
}
