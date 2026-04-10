using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class UriTypeSchemaTests
{
    [Test]
    public async Task Constructor_Parses_VersionedSchemaUri()
    {
        var schema = new UriTypeSchema("semantic://wakaze.dev/tag/v2");

        await Assert.That(schema.TypeUri).IsEqualTo(new TypeUri("semantic://wakaze.dev/tag"));
        await Assert.That(schema.Version).IsEqualTo(new TypeVersion(2));
        await Assert.That(schema.ToString()).IsEqualTo("semantic://wakaze.dev/tag/v2");
    }

    [Test]
    public async Task Equality_Uses_Family_AndVersion()
    {
        var left = new UriTypeSchema("semantic://WAKAZE.dev/tag/v2");
        var right = new UriTypeSchema("SEMANTIC://wakaze.dev/tag/v2");
        var otherVersion = new UriTypeSchema("semantic://wakaze.dev/tag/v3");

        await Assert.That(left == right).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
        await Assert.That(left != otherVersion).IsTrue();
    }

    [Test]
    public void Constructor_Rejects_FamilyOnlyUri()
    {
        AssertThrows<ArgumentException>(() => new UriTypeSchema("semantic://wakaze.dev/tag"));
    }

    [Test]
    public void Constructor_Rejects_InvalidVersionSegment()
    {
        AssertThrows<ArgumentException>(() => new UriTypeSchema("semantic://wakaze.dev/tag/version2"));
        AssertThrows<ArgumentException>(() => new UriTypeSchema("semantic://wakaze.dev/tag/v0"));
        AssertThrows<ArgumentException>(() => new UriTypeSchema("semantic://wakaze.dev/tag/v01"));
    }

    [Test]
    public async Task TryParse_Returns_ParsedValue_For_ValidSchemaUri()
    {
        var result = UriTypeSchema.TryParse("semantic://wakaze.dev/tag/v2", out var schema);

        await Assert.That(result).IsTrue();
        await Assert.That(schema).IsEqualTo(new UriTypeSchema("semantic://wakaze.dev/tag/v2"));
    }

    [Test]
    public async Task TryParse_Returns_False_For_InvalidValue()
    {
        var result = UriTypeSchema.TryParse("semantic://wakaze.dev/tag", out _);
        var nullResult = UriTypeSchema.TryParse(null, out _);

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
