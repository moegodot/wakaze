using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Abstractions.Schema;
using Kawayi.Wakaze.Db.Abstractions;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class DatabaseSchemaIdentifierTests
{
    [Test]
    public async Task DatabaseIdentifiers_Accept_DatabaseSchemeUris()
    {
        var providerScheme = new SchemaId<DatabaseProviderScheme>("database-provider://wakaze.dev/postgresql/v1");
        var schema = new SchemaId<Kawayi.Wakaze.Db.Abstractions.DatabaseScheme>("database://wakaze.dev/postgresql/v1");
        var descriptor = new DatabaseDescriptor(
            providerScheme,
            schema,
            new DatabaseFileLocation("/tmp/wakaze.db"),
            "postgresql");

        await Assert.That(descriptor.ProviderId).IsEqualTo(providerScheme);
        await Assert.That(descriptor.Engine).IsEqualTo(schema);
        await Assert.That(descriptor.ProviderId.ToString()).IsEqualTo("database-provider://wakaze.dev/postgresql/v1");
        await Assert.That(descriptor.Engine.ToString()).IsEqualTo("database://wakaze.dev/postgresql/v1");
    }

    [Test]
    public async Task DatabaseOpaqueLocation_Preserves_StronglyTyped_Schema_And_Format()
    {
        var schema = new SchemaId<Kawayi.Wakaze.Db.Abstractions.DatabaseScheme>("database://wakaze.dev/location/v1");
        var format =
            new SchemaId<Kawayi.Wakaze.Db.Abstractions.DatabaseScheme>("database://wakaze.dev/location-format/v2");
        var payload = new byte[] { 1, 2, 3 };
        var location = new DatabaseOpaqueLocation(schema, format, payload, "opaque");

        await Assert.That(location.Schema).IsEqualTo(schema);
        await Assert.That(location.Format).IsEqualTo(format);
        await Assert.That(location.Payload.ToArray()).IsEquivalentTo(payload);
        await Assert.That(location.Description).IsEqualTo("opaque");
    }

    [Test]
    public void DatabaseIdentifiers_Reject_NonDatabaseSchemeUris()
    {
        AssertThrows<ArgumentException>(() =>
            _ = new SchemaId<Kawayi.Wakaze.Db.Abstractions.DatabaseScheme>("semantic://wakaze.dev/tag/v1"));
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
