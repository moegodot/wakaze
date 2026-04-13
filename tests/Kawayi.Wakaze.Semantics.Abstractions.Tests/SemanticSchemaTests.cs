using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Abstractions.Schema;
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
            new SchemaId("semantic://wakaze.dev/tag/v2"),
            "json",
            content);
        var same = new SemanticPayload(
            new SchemaId("semantic://wakaze.dev/tag/v2"),
            "json",
            content);
        var otherSchema = new SemanticPayload(
            new SchemaId("semantic://wakaze.dev/tag/v1"),
            "json",
            content);

        await Assert.That(left == same).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(same.GetHashCode());
        await Assert.That(left != otherSchema).IsTrue();
    }

    [Test]
    public async Task SemanticClaim_Accepts_OneExtensionPerFamily()
    {
        var primary = new FakeSemanticValue(new SchemaId("semantic://wakaze.dev/primary/v1"), "primary");
        var extension = new FakeSemanticValue(new SchemaId("semantic://wakaze.dev/extension/v2"), "extension");

        var claim = new SemanticClaim(
            CreateRevision(),
            primary,
            ImmutableDictionary<SchemaFamily, ISemanticValue>.Empty.Add(extension.SchemaId.Family, extension));

        await Assert.That(claim.Extensions.Count).IsEqualTo(1);
        await Assert.That(ReferenceEquals(claim.Extensions[extension.SchemaId.Family], extension)).IsTrue();
    }

    [Test]
    public void SemanticClaim_Rejects_ExtensionKey_ThatDoesNotMatch_ValueFamily()
    {
        var primary = new FakeSemanticValue(new SchemaId("semantic://wakaze.dev/primary/v1"), "primary");
        var extension = new FakeSemanticValue(new SchemaId("semantic://wakaze.dev/extension/v2"), "extension");
        var wrongFamily = new SchemaFamily("semantic://wakaze.dev/other");

        AssertThrows<ArgumentException>(() => new SemanticClaim(
            CreateRevision(),
            primary,
            ImmutableDictionary<SchemaFamily, ISemanticValue>.Empty.Add(wrongFamily, extension)));
    }

    [Test]
    public void SemanticClaim_Rejects_PrimaryFamily_In_Extensions_EvenWhenVersionDiffers()
    {
        var primary = new FakeSemanticValue(new SchemaId("semantic://wakaze.dev/tag/v1"), "primary");
        var extension = new FakeSemanticValue(new SchemaId("semantic://wakaze.dev/tag/v2"), "extension");

        AssertThrows<ArgumentException>(() => new SemanticClaim(
            CreateRevision(),
            primary,
            ImmutableDictionary<SchemaFamily, ISemanticValue>.Empty.Add(extension.SchemaId.Family, extension)));
    }

    [Test]
    public async Task SessionFacingApis_Compile_Against_Family_And_ExactSchema_Split()
    {
        var primary = new FakeSemanticValue(new SchemaId("semantic://wakaze.dev/tag/v2"), "primary");
        var session = new FakeSemanticSession(primary);

        var byFamily = session.TryGet(primary.SchemaId.Family, out var familyValue);
        var bySchema = session.TryGetCompatible(primary.SchemaId, out var schemaValue);
        var byGenericSchema = session.TryGetCompatible(primary.SchemaId, out FakeSemanticValue? typedValue);

        await Assert.That(byFamily).IsTrue();
        await Assert.That(bySchema).IsTrue();
        await Assert.That(byGenericSchema).IsTrue();
        await Assert.That(ReferenceEquals(familyValue, primary)).IsTrue();
        await Assert.That(ReferenceEquals(schemaValue, primary)).IsTrue();
        await Assert.That(ReferenceEquals(typedValue, primary)).IsTrue();
    }

    private static EntityRevision CreateRevision()
    {
        return EntityRevision.GenerateNew(EntityId.GenerateNew());
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

    private sealed record FakeSemanticValue(SchemaId SchemaId, string Value) : ISemanticValue;

    private sealed class FakeSemanticSession : ISemanticSession
    {
        private ISemanticValue _primary;
        private ImmutableDictionary<SchemaFamily, ISemanticValue> _extensions;

        public FakeSemanticSession(ISemanticValue primary)
        {
            _primary = primary;
            _extensions = ImmutableDictionary<SchemaFamily, ISemanticValue>.Empty;
            EntityId = EntityId.GenerateNew();
            BasisRevision = CreateRevision();
        }

        public EntityId EntityId { get; }

        public EntityRevision BasisRevision { get; }

        public SemanticClaim Current => new(BasisRevision, _primary, _extensions);

        public void Apply(ISemanticCommand command)
        {
        }

        public bool RemoveExtension(SchemaFamily family)
        {
            var removed = _extensions.ContainsKey(family);
            _extensions = _extensions.Remove(family);
            return removed;
        }

        public void SetExtension(ISemanticValue value)
        {
            _extensions = _extensions.SetItem(value.SchemaId.Family, value);
        }

        public void SetPrimary(ISemanticValue value)
        {
            _primary = value;
        }

        public bool TryGet(SchemaFamily family, [NotNullWhen(true)] out ISemanticValue? value)
        {
            if (_primary.SchemaId.Family == family)
            {
                value = _primary;
                return true;
            }

            return _extensions.TryGetValue(family, out value);
        }

        public bool TryGet<TValue>(SchemaFamily family, [NotNullWhen(true)] out TValue? value)
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

        public bool TryGetCompatible(SchemaId targetSchema, [NotNullWhen(true)] out ISemanticValue? value)
        {
            if (_primary.SchemaId == targetSchema)
            {
                value = _primary;
                return true;
            }

            if (_extensions.TryGetValue(targetSchema.Family, out var extension) && extension.SchemaId == targetSchema)
            {
                value = extension;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetCompatible<TValue>(SchemaId targetSchema, [NotNullWhen(true)] out TValue? value)
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
