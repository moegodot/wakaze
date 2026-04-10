using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class SchemaDefinitionModelTests
{
    [Test]
    public async Task ThreeLayerDefinitions_Expose_ExpectedUris()
    {
        await Assert.That(SemanticScheme.UriScheme).IsEqualTo("semantic");
        await Assert.That(TagFamily.Family).IsEqualTo(new SchemaFamily("semantic://wakaze.dev/tag"));
        await Assert.That(TagV2Schema.Schema).IsEqualTo(new SchemaId("semantic://wakaze.dev/tag/v2"));
        await Assert.That(TagV2Schema.Schema.Family).IsEqualTo(TagFamily.Family);
    }

    [Test]
    public async Task GenericTypedObject_Preserves_ErasedRuntimeSurface()
    {
        ITypedObject erased = new FakeTypedObject(TagV2Schema.Schema, "tag");
        var typed = (ITypedObject<TagV2Schema>)erased;

        await Assert.That(erased.SchemaId).IsEqualTo(TagV2Schema.Schema);
        await Assert.That(typed.SchemaId).IsEqualTo(TagV2Schema.Schema);
    }

    private sealed record FakeTypedObject(SchemaId SchemaId, string Value) : ITypedObject<TagV2Schema>;
}
