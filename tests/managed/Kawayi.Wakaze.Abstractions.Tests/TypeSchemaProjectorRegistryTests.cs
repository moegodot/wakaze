using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class TypeSchemaProjectorRegistryTests
{
    [Test]
    public async Task TryProject_Uses_DirectProjector()
    {
        var registry = new TypeSchemaProjectorRegistry();
        var sourceSchema = new UriTypeSchema("type://wakaze.dev/tag/v2");
        var targetSchema = new UriTypeSchema("type://wakaze.dev/tag/v1");

        registry.Register(
            sourceSchema,
            targetSchema,
            source => new FakeTypedObject(targetSchema, $"{((FakeTypedObject)source).Value}:v1"));

        var source = new FakeTypedObject(sourceSchema, "tag");
        var result = registry.TryProject(source, targetSchema, out var projected);

        await Assert.That(result).IsTrue();
        await Assert.That(projected).IsNotNull();
        await Assert.That(projected!.TypeSchema).IsEqualTo(targetSchema);
    }

    [Test]
    public async Task TryProject_Chains_MultipleProjectors()
    {
        var registry = new TypeSchemaProjectorRegistry();
        var v3 = new UriTypeSchema("type://wakaze.dev/tag/v3");
        var v2 = new UriTypeSchema("type://wakaze.dev/tag/v2");
        var v1 = new UriTypeSchema("type://wakaze.dev/tag/v1");

        registry.Register(v3, v2, source => new FakeTypedObject(v2, $"{((FakeTypedObject)source).Value}:v2"));
        registry.Register(v2, v1, source => new FakeTypedObject(v1, $"{((FakeTypedObject)source).Value}:v1"));

        var result = registry.TryProject(new FakeTypedObject(v3, "tag"), v1, out var projected);

        await Assert.That(result).IsTrue();
        await Assert.That(projected).IsNotNull();
        await Assert.That(projected!.TypeSchema).IsEqualTo(v1);
        await Assert.That(((FakeTypedObject)projected).Value).IsEqualTo("tag:v2:v1");
    }

    [Test]
    public async Task TryProject_Returns_False_When_NoPathExists()
    {
        var registry = new TypeSchemaProjectorRegistry();
        var source = new FakeTypedObject(new UriTypeSchema("type://wakaze.dev/tag/v2"), "tag");
        var target = new UriTypeSchema("type://wakaze.dev/tag/v1");

        var result = registry.TryProject(source, target, out var projected);

        await Assert.That(result).IsFalse();
        await Assert.That(projected).IsNull();
    }

    [Test]
    public void Register_Rejects_CrossFamilyProjectors()
    {
        var registry = new TypeSchemaProjectorRegistry();
        var source = new UriTypeSchema("type://wakaze.dev/tag/v2");
        var target = new UriTypeSchema("type://wakaze.dev/other/v1");

        AssertThrows<ArgumentException>(() => registry.Register(source, target, value => value));
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

    private sealed record FakeTypedObject(UriTypeSchema TypeSchema, string Value) : ITypedObject;
}
