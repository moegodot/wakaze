using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Entity.Abstractions;
using Kawayi.Wakaze.Semantics.Abstractions;

namespace Kawayi.Wakaze.Semantics.Abstractions.Tests;

public class SemanticSchemaTests
{
    [Test]
    public async Task SemanticPayload_Equality_Uses_ExactSchema()
    {
        var content = new byte[] { 1, 2, 3 };
        var left = new SemanticPayload(
            new UriTypeSchema("type://wakaze.dev/tag/v2"),
            "json",
            content);
        var same = new SemanticPayload(
            new UriTypeSchema("type://wakaze.dev/tag/v2"),
            "json",
            content);
        var otherSchema = new SemanticPayload(
            new UriTypeSchema("type://wakaze.dev/tag/v1"),
            "json",
            content);

        await Assert.That(left == same).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(same.GetHashCode());
        await Assert.That(left != otherSchema).IsTrue();
    }

    [Test]
    public async Task SemanticClaim_Accepts_OneExtensionPerFamily()
    {
        var primary = new FakeSemanticValue(new UriTypeSchema("type://wakaze.dev/primary/v1"), "primary");
        var extension = new FakeSemanticValue(new UriTypeSchema("type://wakaze.dev/extension/v2"), "extension");

        var claim = new SemanticClaim(
            CreateRevision(),
            primary,
            ImmutableDictionary<TypeUri, ISemanticValue>.Empty.Add(extension.TypeSchema.TypeUri, extension));

        await Assert.That(claim.Extensions.Count).IsEqualTo(1);
        await Assert.That(ReferenceEquals(claim.Extensions[extension.TypeSchema.TypeUri], extension)).IsTrue();
    }

    [Test]
    public void SemanticClaim_Rejects_ExtensionKey_ThatDoesNotMatch_ValueFamily()
    {
        var primary = new FakeSemanticValue(new UriTypeSchema("type://wakaze.dev/primary/v1"), "primary");
        var extension = new FakeSemanticValue(new UriTypeSchema("type://wakaze.dev/extension/v2"), "extension");
        var wrongFamily = new TypeUri("type://wakaze.dev/other");

        AssertThrows<ArgumentException>(() => new SemanticClaim(
            CreateRevision(),
            primary,
            ImmutableDictionary<TypeUri, ISemanticValue>.Empty.Add(wrongFamily, extension)));
    }

    [Test]
    public void SemanticClaim_Rejects_PrimaryFamily_In_Extensions_EvenWhenVersionDiffers()
    {
        var primary = new FakeSemanticValue(new UriTypeSchema("type://wakaze.dev/tag/v1"), "primary");
        var extension = new FakeSemanticValue(new UriTypeSchema("type://wakaze.dev/tag/v2"), "extension");

        AssertThrows<ArgumentException>(() => new SemanticClaim(
            CreateRevision(),
            primary,
            ImmutableDictionary<TypeUri, ISemanticValue>.Empty.Add(extension.TypeSchema.TypeUri, extension)));
    }

    [Test]
    public async Task SessionFacingApis_Compile_Against_Family_And_ExactSchema_Split()
    {
        var primary = new FakeSemanticValue(new UriTypeSchema("type://wakaze.dev/tag/v2"), "primary");
        var session = new FakeSemanticSession(primary);

        var byFamily = session.TryGet(primary.TypeSchema.TypeUri, out ISemanticValue? familyValue);
        var bySchema = session.TryGetCompatible(primary.TypeSchema, out ISemanticValue? schemaValue);
        var byGenericSchema = session.TryGetCompatible(primary.TypeSchema, out FakeSemanticValue? typedValue);

        await Assert.That(byFamily).IsTrue();
        await Assert.That(bySchema).IsTrue();
        await Assert.That(byGenericSchema).IsTrue();
        await Assert.That(ReferenceEquals(familyValue, primary)).IsTrue();
        await Assert.That(ReferenceEquals(schemaValue, primary)).IsTrue();
        await Assert.That(ReferenceEquals(typedValue, primary)).IsTrue();
    }

    private static EntityRevision CreateRevision()
    {
        return new EntityRevision(
            EntityId.GenerateNew(),
            new Revision(Guid.CreateVersion7(), 1, 1));
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

    private sealed record FakeSemanticValue(UriTypeSchema TypeSchema, string Value) : ISemanticValue;

    private sealed class FakeSemanticSession : ISemanticSession
    {
        private ISemanticValue _primary;
        private ImmutableDictionary<TypeUri, ISemanticValue> _extensions;

        public FakeSemanticSession(ISemanticValue primary)
        {
            _primary = primary;
            _extensions = ImmutableDictionary<TypeUri, ISemanticValue>.Empty;
            EntityId = EntityId.GenerateNew();
            BasisRevision = CreateRevision();
        }

        public EntityId EntityId { get; }

        public EntityRevision BasisRevision { get; }

        public SemanticClaim Current => new(BasisRevision, _primary, _extensions);

        public void Apply(ISemanticCommand command)
        {
        }

        public bool RemoveExtension(TypeUri family)
        {
            var removed = _extensions.ContainsKey(family);
            _extensions = _extensions.Remove(family);
            return removed;
        }

        public void SetExtension(ISemanticValue value)
        {
            _extensions = _extensions.SetItem(value.TypeSchema.TypeUri, value);
        }

        public void SetPrimary(ISemanticValue value)
        {
            _primary = value;
        }

        public bool TryGet(TypeUri family, [NotNullWhen(true)] out ISemanticValue? value)
        {
            if (_primary.TypeSchema.TypeUri == family)
            {
                value = _primary;
                return true;
            }

            return _extensions.TryGetValue(family, out value);
        }

        public bool TryGet<TValue>(TypeUri family, [NotNullWhen(true)] out TValue? value)
            where TValue : class, ISemanticValue
        {
            if (TryGet(family, out var untyped) && untyped is TValue typed)
            {
                value = typed;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetCompatible(UriTypeSchema targetSchema, [NotNullWhen(true)] out ISemanticValue? value)
        {
            if (_primary.TypeSchema == targetSchema)
            {
                value = _primary;
                return true;
            }

            if (_extensions.TryGetValue(targetSchema.TypeUri, out var extension) && extension.TypeSchema == targetSchema)
            {
                value = extension;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetCompatible<TValue>(UriTypeSchema targetSchema, [NotNullWhen(true)] out TValue? value)
            where TValue : class, ISemanticValue
        {
            if (TryGetCompatible(targetSchema, out var untyped) && untyped is TValue typed)
            {
                value = typed;
                return true;
            }

            value = null;
            return false;
        }
    }
}
