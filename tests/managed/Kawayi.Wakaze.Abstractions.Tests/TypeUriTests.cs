using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class TypeUriTests
{
    [Test]
    public async Task Constructor_Accepts_ValidSingleSegmentTypeUri()
    {
        var value = new TypeUri("type://wakaze.dev/tag/v1");

        await Assert.That(value.Value).IsEqualTo(new Uri("type://wakaze.dev/tag/v1"));
        await Assert.That(value.ToString()).IsEqualTo("type://wakaze.dev/tag/v1");
    }

    [Test]
    public async Task Constructor_Accepts_MultiplePathSegments_AndVersionZero()
    {
        var nested = new TypeUri("type://wakaze.dev/semantic/tag/v1");
        var zero = new TypeUri("type://wakaze.dev/tag/v0");

        await Assert.That(nested.Value).IsEqualTo(new Uri("type://wakaze.dev/semantic/tag/v1"));
        await Assert.That(zero.Value).IsEqualTo(new Uri("type://wakaze.dev/tag/v0"));
    }

    [Test]
    public async Task Equality_UsesUriObjectSemantics()
    {
        var left = new TypeUri(new Uri("type://WAKAZE.dev/tag/v1"));
        var right = new TypeUri(new Uri("TYPE://wakaze.dev/tag/v1"));

        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left.Equals((object)right)).IsTrue();
        await Assert.That(left == right).IsTrue();
        await Assert.That(left != right).IsFalse();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
    }

    [Test]
    public void Constructor_Rejects_NonTypeScheme()
    {
        AssertThrows<ArgumentException>(() => new TypeUri("https://wakaze.dev/tag/v1"));
    }

    [Test]
    public void Constructor_Rejects_MissingHost()
    {
        AssertThrows<ArgumentException>(() => new TypeUri("type:///tag/v1"));
    }

    [Test]
    public void Constructor_Rejects_MissingPath()
    {
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev"));
    }

    [Test]
    public void Constructor_Rejects_PathWithoutVersionSegment()
    {
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev/tag"));
    }

    [Test]
    public void Constructor_Rejects_FinalSegmentThatIsNotAValidVersion()
    {
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev/tag/version1"));
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev/tag/v-1"));
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev/tag/v4294967296"));
    }

    [Test]
    public void Constructor_Rejects_LeadingZeroVersions()
    {
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev/tag/v01"));
    }

    [Test]
    public void Constructor_Rejects_Query_Fragment_Port_AndUserInfo()
    {
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev/tag/v1?x=1"));
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev/tag/v1#fragment"));
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev:1234/tag/v1"));
        AssertThrows<ArgumentException>(() => new TypeUri("type://user@wakaze.dev/tag/v1"));
    }

    [Test]
    public void Constructor_Rejects_TrailingSlash()
    {
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev/tag/v1/"));
    }

    [Test]
    public void Constructor_Rejects_NullInputs()
    {
        AssertThrows<ArgumentNullException>(() => new TypeUri((string)null!));
        AssertThrows<ArgumentNullException>(() => new TypeUri((Uri)null!));
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
