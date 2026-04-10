using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class TypeUriTests
{
    [Test]
    public async Task Constructor_Accepts_ValidFamilyUris()
    {
        var single = new TypeUri("type://wakaze.dev/tag");
        var nested = new TypeUri("type://wakaze.dev/semantic/tag");
        var versionLike = new TypeUri("type://wakaze.dev/tag/v1");

        await Assert.That(single.Value).IsEqualTo(new Uri("type://wakaze.dev/tag"));
        await Assert.That(nested.Value).IsEqualTo(new Uri("type://wakaze.dev/semantic/tag"));
        await Assert.That(versionLike.Value).IsEqualTo(new Uri("type://wakaze.dev/tag/v1"));
        await Assert.That(versionLike.ToString()).IsEqualTo("type://wakaze.dev/tag/v1");
    }

    [Test]
    public async Task Equality_UsesUriObjectSemantics()
    {
        var left = new TypeUri("type://WAKAZE.dev/tag");
        var right = new TypeUri("TYPE://wakaze.dev/tag");

        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left.Equals((object)right)).IsTrue();
        await Assert.That(left == right).IsTrue();
        await Assert.That(left != right).IsFalse();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
    }

    [Test]
    public void Constructor_Rejects_NonTypeScheme()
    {
        AssertThrows<ArgumentException>(() => new TypeUri("https://wakaze.dev/tag"));
    }

    [Test]
    public void Constructor_Rejects_MissingHost()
    {
        AssertThrows<ArgumentException>(() => new TypeUri("type:///tag"));
    }

    [Test]
    public void Constructor_Rejects_MissingPath()
    {
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev"));
    }

    [Test]
    public void Constructor_Rejects_Query_Fragment_Port_AndUserInfo()
    {
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev/tag?x=1"));
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev/tag#fragment"));
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev:1234/tag"));
        AssertThrows<ArgumentException>(() => new TypeUri("type://user@wakaze.dev/tag"));
    }

    [Test]
    public void Constructor_Rejects_TrailingSlash()
    {
        AssertThrows<ArgumentException>(() => new TypeUri("type://wakaze.dev/tag/"));
    }

    [Test]
    public void Constructor_Rejects_NullInputs()
    {
        AssertThrows<ArgumentNullException>(() => new TypeUri((string)null!));
    }

    [Test]
    public async Task TryParse_Returns_ParsedValue_For_ValidFamilyUri()
    {
        var result = TypeUri.TryParse("type://wakaze.dev/tag", out var typeUri);

        await Assert.That(result).IsTrue();
        await Assert.That(typeUri).IsEqualTo(new TypeUri("type://wakaze.dev/tag"));
    }

    [Test]
    public async Task TryParse_Returns_False_For_InvalidValue()
    {
        var result = TypeUri.TryParse("https://wakaze.dev/tag", out _);
        var nullResult = TypeUri.TryParse(null, out _);

        await Assert.That(result).IsFalse();
        await Assert.That(nullResult).IsFalse();
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
