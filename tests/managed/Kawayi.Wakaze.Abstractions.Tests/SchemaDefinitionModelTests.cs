using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class SchemaDefinitionModelTests
{
    [Test]
    public async Task ThreeLayerDefinitions_Expose_ExpectedUris()
    {
        await Assert.That(SemanticScheme.UriScheme).IsEqualTo("semantic");
        await Assert.That(TagFamily.TypeUri).IsEqualTo(new TypeUri("semantic://wakaze.dev/tag"));
        await Assert.That(TagV2Schema.Schema).IsEqualTo(new UriTypeSchema("semantic://wakaze.dev/tag/v2"));
        await Assert.That(TagV2Schema.Schema.TypeUri).IsEqualTo(TagFamily.TypeUri);
    }

    [Test]
    public async Task GenericTypedObject_Preserves_ErasedRuntimeSurface()
    {
        ITypedObject erased = new FakeTypedObject(TagV2Schema.Schema, "tag");
        var typed = (ITypedObject<TagV2Schema>)erased;

        await Assert.That(erased.TypeSchema).IsEqualTo(TagV2Schema.Schema);
        await Assert.That(typed.TypeSchema).IsEqualTo(TagV2Schema.Schema);
    }

    private sealed record FakeTypedObject(UriTypeSchema TypeSchema, string Value) : ITypedObject<TagV2Schema>;
}
