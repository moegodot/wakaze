using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class SchemaProjectorRegistryTests
{
    [Test]
    public async Task TryProject_Uses_DirectProjector()
    {
        var registry = new SchemaProjectorRegistry();
        var sourceSchema = TagV2Schema.Schema;
        var targetSchema = TagV1Schema.Schema;

        registry.Register<TagV2Schema, TagFamily, SemanticScheme, TagV1Schema, TagFamily, SemanticScheme>(
            source => new FakeTypedObject(targetSchema, $"{((FakeTypedObject)source).Value}:v1"));

        var source = new FakeTypedObject(sourceSchema, "tag");
        var result = registry.TryProject(source, targetSchema, out var projected);

        await Assert.That(result).IsTrue();
        await Assert.That(projected).IsNotNull();
        await Assert.That(projected!.SchemaId).IsEqualTo(targetSchema);
    }

    [Test]
    public async Task TryProject_Chains_MultipleProjectors()
    {
        var registry = new SchemaProjectorRegistry();
        var v3 = TagV3Schema.Schema;
        var v2 = TagV2Schema.Schema;
        var v1 = TagV1Schema.Schema;

        registry.Register<TagV3Schema, TagFamily, SemanticScheme, TagV2Schema, TagFamily, SemanticScheme>(
            source => new FakeTypedObject(v2, $"{((FakeTypedObject)source).Value}:v2"));
        registry.Register<TagV2Schema, TagFamily, SemanticScheme, TagV1Schema, TagFamily, SemanticScheme>(
            source => new FakeTypedObject(v1, $"{((FakeTypedObject)source).Value}:v1"));

        var result = registry.TryProject(new FakeTypedObject(v3, "tag"), v1, out var projected);

        await Assert.That(result).IsTrue();
        await Assert.That(projected).IsNotNull();
        await Assert.That(projected!.SchemaId).IsEqualTo(v1);
        await Assert.That(((FakeTypedObject)projected).Value).IsEqualTo("tag:v2:v1");
    }

    [Test]
    public async Task TryProject_Returns_False_When_NoPathExists()
    {
        var registry = new SchemaProjectorRegistry();
        var source = new FakeTypedObject(TagV2Schema.Schema, "tag");
        var target = TagV1Schema.Schema;

        var result = registry.TryProject(source, target, out var projected);

        await Assert.That(result).IsFalse();
        await Assert.That(projected).IsNull();
    }

    [Test]
    public void Register_Rejects_CrossFamilyProjectors()
    {
        var registry = new SchemaProjectorRegistry();
        var source = TagV2Schema.Schema;
        var target = OtherV1Schema.Schema;

        AssertThrows<ArgumentException>(() => registry.Register(source, target, value => value));
    }

    [Test]
    public void Register_Rejects_Target_NotDeclared_In_SourceSchemaMetadata()
    {
        var registry = new SchemaProjectorRegistry();

        registry.RegisterSchema<TagV1Schema, TagFamily, SemanticScheme>();

        AssertThrows<ArgumentException>(() => registry.Register(TagV1Schema.Schema, TagV2Schema.Schema, value => value));
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

    private sealed record FakeTypedObject(SchemaId SchemaId, string Value) : ITypedObject;
}
