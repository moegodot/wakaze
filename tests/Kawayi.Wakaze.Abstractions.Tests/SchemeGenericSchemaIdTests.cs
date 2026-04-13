using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Abstractions.Schema;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class SchemeGenericSchemaIdTests
{
    [Test]
    public async Task FamilyAndVersionConstructor_Creates_SchemeBoundSchema()
    {
        var schema = new SchemaId<SemanticScheme>(TagFamily.Family, new SchemaVersion(2));
        var erased = schema;

        await Assert.That(schema.Id.Family).IsEqualTo(TagFamily.Family);
        await Assert.That(schema.Id.Version).IsEqualTo(new SchemaVersion(2));
        await Assert.That(schema.ToString()).IsEqualTo("semantic://wakaze.dev/tag/v2");
        //await Assert.That(erased).IsEqualTo(new SchemaId("semantic://wakaze.dev/tag/v2"));
    }

    [Test]
    public async Task UntypedConstructor_Accepts_MatchingScheme_With_AnyFamily()
    {
        var schema = new SchemaId<SemanticScheme>(new SchemaId("semantic://wakaze.dev/other/v3"));

        await Assert.That(schema.Family).IsEqualTo(OtherFamily.Family);
        await Assert.That(schema.Version).IsEqualTo(new SchemaVersion(3));
    }

    [Test]
    public void FamilyAndVersionConstructor_Rejects_DifferentScheme()
    {
        AssertThrows<ArgumentException>(() =>
            _ = new SchemaId<SemanticScheme>(PostgreSqlFamily.Family, new SchemaVersion(1)));
    }

    [Test]
    public void UntypedConstructor_Rejects_DifferentScheme()
    {
        AssertThrows<ArgumentException>(() =>
            _ = new SchemaId<SemanticScheme>(new SchemaId("database://wakaze.dev/postgresql/v1")));
    }

    [Test]
    public async Task TryParse_Returns_False_When_Scheme_Does_Not_Match()
    {
        var matches = SchemaId<SemanticScheme>.TryParse("semantic://wakaze.dev/tag/v2", null, out var matching);
        var mismatched = SchemaId<SemanticScheme>.TryParse("database://wakaze.dev/postgresql/v1", null, out _);

        await Assert.That(matches).IsTrue();
        await Assert.That(matching)
            .IsEqualTo(new SchemaId<SemanticScheme>(new SchemaId("semantic://wakaze.dev/tag/v2")));
        await Assert.That(mismatched).IsFalse();
    }

    [Test]
    public async Task ExplicitConversion_FromUntyped_Preserves_Value()
    {
        var schema = (SchemaId<DatabaseScheme>)new SchemaId("database://wakaze.dev/postgresql/v1");

        await Assert.That(schema)
            .IsEqualTo(new SchemaId<DatabaseScheme>(PostgreSqlFamily.Family, new SchemaVersion(1)));
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
