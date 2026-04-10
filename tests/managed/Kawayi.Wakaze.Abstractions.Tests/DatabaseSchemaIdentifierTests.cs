using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Db.Abstractions;

namespace Kawayi.Wakaze.Abstractions.Tests;

public class DatabaseSchemaIdentifierTests
{
    [Test]
    public async Task DatabaseIdentifiers_Accept_DatabaseSchemeUris()
    {
        var schema = new SchemaId("database://wakaze.dev/postgresql/v1");
        var providerId = new DatabaseProviderId(schema);
        var engine = new DatabaseEngine("database://wakaze.dev/postgresql/v1");

        await Assert.That(providerId.Value).IsEqualTo(schema);
        await Assert.That(engine.Value).IsEqualTo(schema);
        await Assert.That(providerId.ToString()).IsEqualTo("database://wakaze.dev/postgresql/v1");
        await Assert.That(engine.ToString()).IsEqualTo("database://wakaze.dev/postgresql/v1");
    }
}
