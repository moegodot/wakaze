using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class GenericSchemaIdTests
{
    [Test]
    public async Task VersionConstructor_Creates_FamilyBoundSchema()
    {
        var schema = new SchemaId<SemanticScheme, TagFamily>(new SchemaVersion(2));
        SchemaId erased = schema;

        await Assert.That(schema.Family).IsEqualTo(TagFamily.Family);
        await Assert.That(schema.Version).IsEqualTo(new SchemaVersion(2));
        await Assert.That(schema.ToString()).IsEqualTo("semantic://wakaze.dev/tag/v2");
        await Assert.That(erased).IsEqualTo(new SchemaId("semantic://wakaze.dev/tag/v2"));
    }

    [Test]
    public async Task UntypedConstructor_Accepts_MatchingFamily()
    {
        var schema = new SchemaId<SemanticScheme, TagFamily>(new SchemaId("semantic://wakaze.dev/tag/v3"));

        await Assert.That(schema.Family).IsEqualTo(TagFamily.Family);
        await Assert.That(schema.Version).IsEqualTo(new SchemaVersion(3));
    }

    [Test]
    public void UntypedConstructor_Rejects_DifferentFamily()
    {
        AssertThrows<ArgumentException>(() =>
            _ = new SchemaId<SemanticScheme, TagFamily>(new SchemaId("semantic://wakaze.dev/other/v1")));
    }

    [Test]
    public void UntypedConstructor_Rejects_DifferentScheme()
    {
        AssertThrows<ArgumentException>(() =>
            _ = new SchemaId<SemanticScheme, TagFamily>(new SchemaId("notsemantic://wakaze.dev/tag/v3")));
    }

    [Test]
    public async Task TryParse_Returns_False_When_Family_Does_Not_Match()
    {
        var matches = SchemaId<SemanticScheme, TagFamily>.TryParse("semantic://wakaze.dev/tag/v2", out var matching);
        var mismatched = SchemaId<SemanticScheme, TagFamily>.TryParse("semantic://wakaze.dev/other/v1", out _);

        await Assert.That(matches).IsTrue();
        await Assert.That(matching)
            .IsEqualTo(new SchemaId<SemanticScheme, TagFamily>(new SchemaId("semantic://wakaze.dev/tag/v2")));
        await Assert.That(mismatched).IsFalse();
    }

    [Test]
    public async Task ExplicitConversion_FromUntyped_Preserves_Value()
    {
        var schema = (SchemaId<SemanticScheme, TagFamily>)new SchemaId("semantic://wakaze.dev/tag/v1");

        await Assert.That(schema).IsEqualTo(new SchemaId<SemanticScheme, TagFamily>(new SchemaVersion(1)));
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
