using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class SchemaIdTests
{
    [Test]
    public async Task Constructor_Parses_VersionedSchemaUri()
    {
        var schema = new SchemaId("semantic://wakaze.dev/tag/v2");

        await Assert.That(schema.Family).IsEqualTo(new SchemaFamily("semantic://wakaze.dev/tag"));
        await Assert.That(schema.Version).IsEqualTo(new SchemaVersion(2));
        await Assert.That(schema.ToString()).IsEqualTo("semantic://wakaze.dev/tag/v2");
    }

    [Test]
    public async Task Equality_Uses_Family_AndVersion()
    {
        var left = new SchemaId("semantic://WAKAZE.dev/tag/v2");
        var right = new SchemaId("SEMANTIC://wakaze.dev/tag/v2");
        var otherVersion = new SchemaId("semantic://wakaze.dev/tag/v3");

        await Assert.That(left == right).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
        await Assert.That(left != otherVersion).IsTrue();
    }

    [Test]
    public void Constructor_Rejects_FamilyOnlyUri()
    {
        AssertThrows<ArgumentException>(() => new SchemaId("semantic://wakaze.dev/tag"));
    }

    [Test]
    public void Constructor_Rejects_InvalidVersionSegment()
    {
        AssertThrows<ArgumentException>(() => new SchemaId("semantic://wakaze.dev/tag/version2"));
        AssertThrows<ArgumentException>(() => new SchemaId("semantic://wakaze.dev/tag/v0"));
        AssertThrows<ArgumentException>(() => new SchemaId("semantic://wakaze.dev/tag/v01"));
    }

    [Test]
    public async Task TryParse_Returns_ParsedValue_For_ValidSchemaUri()
    {
        var result = SchemaId.TryParse("semantic://wakaze.dev/tag/v2", out var schema);

        await Assert.That(result).IsTrue();
        await Assert.That(schema).IsEqualTo(new SchemaId("semantic://wakaze.dev/tag/v2"));
    }

    [Test]
    public async Task TryParse_Returns_False_For_InvalidValue()
    {
        var result = SchemaId.TryParse("semantic://wakaze.dev/tag", out _);
        var nullResult = SchemaId.TryParse(null, out _);

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
