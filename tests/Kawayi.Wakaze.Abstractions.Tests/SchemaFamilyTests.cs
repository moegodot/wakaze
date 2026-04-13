using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Abstractions.Schema;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class SchemaFamilyTests
{
    [Test]
    public async Task Constructor_Accepts_ValidFamilyUris()
    {
        var semantic = new SchemaFamily("semantic://wakaze.dev/tag");
        var nested = new SchemaFamily("semantic://wakaze.dev/semantic/tag");
        var database = new SchemaFamily("database://wakaze.dev/postgresql");

        await Assert.That(semantic.ToUri()).IsEqualTo(new Uri("semantic://wakaze.dev/tag"));
        await Assert.That(nested.ToUri()).IsEqualTo(new Uri("semantic://wakaze.dev/semantic/tag"));
        await Assert.That(database.ToUri()).IsEqualTo(new Uri("database://wakaze.dev/postgresql"));
        await Assert.That(database.ToString()).IsEqualTo("database://wakaze.dev/postgresql");
    }

    [Test]
    public async Task Equality_UsesUriObjectSemantics()
    {
        var left = new SchemaFamily("semantic://WAKAZE.dev/tag");
        var right = new SchemaFamily("SEMANTIC://wakaze.dev/tag");

        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left.Equals((object)right)).IsTrue();
        await Assert.That(left == right).IsTrue();
        await Assert.That(left != right).IsFalse();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
    }

    [Test]
    public void Constructor_Rejects_MissingHost()
    {
        AssertThrows<ArgumentException>(() => new SchemaFamily("semantic:///tag"));
    }

    [Test]
    public void Constructor_Rejects_MissingPath()
    {
        AssertThrows<ArgumentException>(() => new SchemaFamily("semantic://wakaze.dev"));
    }

    [Test]
    public void Constructor_Rejects_Query_Fragment_Port_AndUserInfo()
    {
        AssertThrows<ArgumentException>(() => new SchemaFamily("semantic://wakaze.dev/tag?x=1"));
        AssertThrows<ArgumentException>(() => new SchemaFamily("semantic://wakaze.dev/tag#fragment"));
        AssertThrows<ArgumentException>(() => new SchemaFamily("semantic://wakaze.dev:1234/tag"));
        AssertThrows<ArgumentException>(() => new SchemaFamily("semantic://user@wakaze.dev/tag"));
    }

    [Test]
    public void Constructor_Rejects_TrailingSlash()
    {
        AssertThrows<ArgumentException>(() => new SchemaFamily("semantic://wakaze.dev/tag/"));
    }

    [Test]
    public void Constructor_Rejects_NullInputs()
    {
        AssertThrows<ArgumentNullException>(() => new SchemaFamily((string)null!));
    }

    [Test]
    public async Task TryParse_Returns_ParsedValue_For_ValidFamilyUri()
    {
        var result = SchemaFamily.TryParse("database://wakaze.dev/postgresql", null, out var typeUri);

        await Assert.That(result).IsTrue();
        await Assert.That(typeUri).IsEqualTo(new SchemaFamily("database://wakaze.dev/postgresql"));
    }

    [Test]
    public async Task TryParse_Returns_False_For_InvalidValue()
    {
        var result = SchemaFamily.TryParse("semantic://wakaze.dev", null, out _);
        var nullResult = SchemaFamily.TryParse(null, null, out _);

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
