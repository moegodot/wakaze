using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Abstractions.Schema;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class SchemaCompatibilityGraphTests
{
    [Test]
    public async Task CanReadAs_Returns_True_For_ExactSchema_WithoutRegistration()
    {
        var graph = new SchemaCompatibilityGraph();
        var schema = TagV1Schema.Schema;

        await Assert.That(graph.CanReadAs(schema, schema)).IsTrue();
    }

    [Test]
    public async Task CanReadAs_Registers_Declared_And_TransitiveEdges()
    {
        var graph = new SchemaCompatibilityGraph();
        var v3 = TagV3Schema.Schema;
        var v2 = TagV2Schema.Schema;
        var v1 = TagV1Schema.Schema;

        graph.Register<TagV3Schema, TagFamily, SemanticScheme>();
        graph.Register<TagV2Schema, TagFamily, SemanticScheme>();

        await Assert.That(graph.CanReadAs(v3, v2)).IsTrue();
        await Assert.That(graph.CanReadAs(v3, v1)).IsTrue();
        await Assert.That(graph.CanReadAs(v1, v3)).IsFalse();
    }

    [Test]
    public async Task CanReadAs_Returns_False_When_NoPathExists_OrFamiliesDiffer()
    {
        var graph = new SchemaCompatibilityGraph();
        var source = TagV2Schema.Schema;
        var missing = TagV1Schema.Schema;
        var otherFamily = OtherV1Schema.Schema;

        await Assert.That(graph.CanReadAs(source, missing)).IsFalse();
        await Assert.That(graph.CanReadAs(source, otherFamily)).IsFalse();
    }

    [Test]
    public void Register_Rejects_CrossFamilyEdges()
    {
        var graph = new SchemaCompatibilityGraph();
        var source = TagV2Schema.Schema;
        var target = OtherV1Schema.Schema;

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
