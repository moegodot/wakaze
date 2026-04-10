using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class TypeVersionTests
{
    [Test]
    public async Task Constructor_Accepts_PositiveVersion_AndFormatsCanonicalSegment()
    {
        var version = new TypeVersion(2);

        await Assert.That(version.Value).IsEqualTo(2U);
        await Assert.That(version.ToString()).IsEqualTo("v2");
    }

    [Test]
    public async Task Ordering_AndEquality_Work_AsExpected()
    {
        var left = new TypeVersion(1);
        var right = new TypeVersion(2);
        var same = new TypeVersion(1);

        await Assert.That(left < right).IsTrue();
        await Assert.That(right > left).IsTrue();
        await Assert.That(left == same).IsTrue();
        await Assert.That(left != right).IsTrue();
        await Assert.That(left.CompareTo(right) < 0).IsTrue();
    }

    [Test]
    public void Constructor_Rejects_Zero()
    {
        AssertThrows<ArgumentOutOfRangeException>(() => new TypeVersion(0));
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
