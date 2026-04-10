using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class TypeSchemaCompatibilityGraphTests
{
    [Test]
    public async Task CanReadAs_Returns_True_For_ExactSchema_WithoutRegistration()
    {
        var graph = new TypeSchemaCompatibilityGraph();
        var schema = new UriTypeSchema("type://wakaze.dev/tag/v1");

        await Assert.That(graph.CanReadAs(schema, schema)).IsTrue();
    }

    [Test]
    public async Task CanReadAs_Resolves_Explicit_And_TransitiveEdges()
    {
        var graph = new TypeSchemaCompatibilityGraph();
        var v3 = new UriTypeSchema("type://wakaze.dev/tag/v3");
        var v2 = new UriTypeSchema("type://wakaze.dev/tag/v2");
        var v1 = new UriTypeSchema("type://wakaze.dev/tag/v1");

        graph.Register(v3, v2);
        graph.Register(v2, v1);

        await Assert.That(graph.CanReadAs(v3, v2)).IsTrue();
        await Assert.That(graph.CanReadAs(v3, v1)).IsTrue();
        await Assert.That(graph.CanReadAs(v1, v3)).IsFalse();
    }

    [Test]
    public async Task CanReadAs_Returns_False_When_NoPathExists_OrFamiliesDiffer()
    {
        var graph = new TypeSchemaCompatibilityGraph();
        var source = new UriTypeSchema("type://wakaze.dev/tag/v2");
        var missing = new UriTypeSchema("type://wakaze.dev/tag/v1");
        var otherFamily = new UriTypeSchema("type://wakaze.dev/other/v1");

        await Assert.That(graph.CanReadAs(source, missing)).IsFalse();
        await Assert.That(graph.CanReadAs(source, otherFamily)).IsFalse();
    }

    [Test]
    public void Register_Rejects_CrossFamilyEdges()
    {
        var graph = new TypeSchemaCompatibilityGraph();
        var source = new UriTypeSchema("type://wakaze.dev/tag/v2");
        var target = new UriTypeSchema("type://wakaze.dev/other/v1");

        AssertThrows<ArgumentException>(() => graph.Register(source, target));
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
