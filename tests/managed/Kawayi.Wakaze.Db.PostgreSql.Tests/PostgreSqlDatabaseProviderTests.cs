using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Db.Abstractions;
using Kawayi.Wakaze.IO;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TUnit.Core;

namespace Kawayi.Wakaze.Db.PostgreSql.Tests;

public class PostgreSqlDatabaseProviderIntegrationTests
{
    [ClassDataSource<TestPostgreSqlDatabaseScope>(Shared = SharedType.PerClass)]
    public required TestPostgreSqlDatabaseScope Scope { get; init; }

    [Test]
    public async Task TryResolveAsync_ResolvesEndpointAndOpensConnection()
    {
        await using var database = await Scope.CreateDatabaseAsync("resolve_target");

        var resolved = await Scope.Provider.TryResolveAsync(
            new DatabaseResolutionRequest(
                Scope.CreateEndpoint("resolve_target"),
                Connection: Scope.CreateAdminConnection()));

        if (resolved is null)
        {
            throw new Exception("Expected the endpoint to resolve.");
        }

        await using (resolved)
        {
            var currentDatabase = await Scope.ExecuteScalarAsync<string>(resolved, "SELECT current_database();");

            await Assert.That(currentDatabase).IsEqualTo("resolve_target");
        }
    }

    [Test]
    public async Task TryResolveAsync_ReturnsNullWhenProviderOrEngineDoNotMatch()
    {
        var otherSchema = new SchemaId<DatabaseScheme>("database://wakaze.dev/other/v1");

        var providerMismatch = await Scope.Provider.TryResolveAsync(
            new DatabaseResolutionRequest(Scope.CreateEndpoint("provider_mismatch"), ProviderId: otherSchema));
        var engineMismatch = await Scope.Provider.TryResolveAsync(
            new DatabaseResolutionRequest(Scope.CreateEndpoint("engine_mismatch"), Engine: otherSchema));

        await Assert.That(providerMismatch).IsNull();
        await Assert.That(engineMismatch).IsNull();
    }

    [Test]
    public async Task CreateAsync_CreatesDatabaseAndReturnsUsableResource()
    {
        await using var database = await Scope.Provider.CreateAsync(
            new DatabaseProvisioningRequest(
                Scope.Provider.ProviderId,
                Scope.CreateEndpoint("created_database"),
                "created_database",
                Scope.CreateAdminConnection()));

        var currentDatabase = await Scope.ExecuteScalarAsync<string>(database, "SELECT current_database();");

        await Assert.That(currentDatabase).IsEqualTo("created_database");
        await Assert.That(database.Descriptor.ProviderId).IsEqualTo(Scope.Provider.ProviderId);
        await Assert.That(database.Descriptor.Engine).IsEqualTo(Scope.Provider.Engine);
    }

    [Test]
    public async Task GetConnectionStringAsync_MergesOverridesAndHints()
    {
        await using var database = await Scope.Provider.CreateAsync(
            new DatabaseProvisioningRequest(
                Scope.Provider.ProviderId,
                Scope.CreateEndpoint("connection_string_source"),
                "connection_string_source",
                Scope.CreateAdminConnection()));

        var connectionString = await database.GetConnectionStringAsync(new DatabaseConnectionRequest(
            new DatabaseCredential("override_user", "secret"),
            "connection_string_override",
            true,
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Application Name"] = "wakaze-tests"
            }));

        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        await Assert.That(builder.Host).IsEqualTo(Scope.SocketDirectory);
        await Assert.That(builder.Database).IsEqualTo("connection_string_override");
        await Assert.That(builder.Username).IsEqualTo("override_user");
        await Assert.That(builder.Password).IsEqualTo("secret");
        await Assert.That(builder.ApplicationName).IsEqualTo("wakaze-tests");
        await Assert.That(connectionString.Contains("default_transaction_read_only=on", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task CheckHealthAsync_ReturnsHealthy()
    {
        await using var database = await Scope.CreateDatabaseAsync("health_database");

        var result = await Scope.Provider.CheckHealthAsync(database);

        await Assert.That(result.State).IsEqualTo(DatabaseHealthState.Healthy);
    }

    [Test]
    public async Task ExecuteAsync_RunsAllMaintenanceOperations()
    {
        await using var database = await Scope.CreateDatabaseAsync("maintenance_database");

        await Scope.ExecuteScalarAsync<string>(database, "CREATE TABLE maintenance_check(id integer primary key);");

        await Scope.Provider.ExecuteAsync(database, DatabaseMaintenanceOperation.UpdateStatistics);
        await Scope.Provider.ExecuteAsync(database, DatabaseMaintenanceOperation.CompactStorage);
        await Scope.Provider.ExecuteAsync(database, DatabaseMaintenanceOperation.RebuildIndexes);
    }

    [Test]
    public async Task DumpToAsync_AndRestoreDumpAsync_RoundTripData()
    {
        await using var sourceDatabase = await Scope.CreateDatabaseAsync("dump_source");

        await Scope.ExecuteScalarAsync<string>(
            sourceDatabase,
            "CREATE TABLE dump_check(id integer primary key, note text not null); INSERT INTO dump_check(id, note) VALUES (1, 'alpha'), (2, 'beta');");

        var dumpDirectory = Path.Combine(Scope.RootDirectory, "dump");

        await Scope.Provider.DumpToAsync(dumpDirectory, sourceDatabase);

        await Assert.That(File.Exists(Path.Combine(dumpDirectory, "manifest.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(dumpDirectory, "database.dump"))).IsTrue();

        await using var restoredDatabase = await Scope.Provider.RestoreDumpAsync(
            dumpDirectory,
            new DatabaseProvisioningRequest(
                Scope.Provider.ProviderId,
                Scope.CreateEndpoint("dump_restored"),
                "dump_restored",
                Scope.CreateAdminConnection()));

        var restoredRows = await Scope.ExecuteScalarAsync<string>(
            restoredDatabase,
            "SELECT string_agg(note, ',' ORDER BY id) FROM dump_check;");

        await Assert.That(restoredRows).IsEqualTo("alpha,beta");
    }
}

public class PostgreSqlDatabaseProviderTests
{
    [Test]
    public void TryResolveAsync_ThrowsWhenDatabaseNameIsMissing()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        AssertThrows<ArgumentException>(() =>
            _ = new PostgreSqlDatabaseProvider(new PostgreSqlDatabaseProviderOptions
            {
                ToolBinaryDirectory = "/tmp"
            }).TryResolveAsync(
                new DatabaseResolutionRequest(
                    new DatabaseEndpointLocation("localhost"),
                    Connection: default),
                cancellation.Token));
    }

    [Test]
    public async Task AddPostgreSqlDatabaseProvider_RegistersSharedProviderInstance()
    {
        var services = new ServiceCollection();
        services.AddPostgreSqlDatabaseProvider(new PostgreSqlDatabaseProviderOptions
        {
            ToolBinaryDirectory = Path.Combine(
                GetRepositoryRoot(),
                "vendors",
                "install",
                "postgresql",
                "bin")
        });

        using var provider = services.BuildServiceProvider();
        var databaseProvider = provider.GetRequiredService<IDatabaseProvider>();
        var maintenance = provider.GetRequiredService<IDatabaseMaintenanceService>();
        var dumper = provider.GetRequiredService<IDatabaseDumper>();
        var restorer = provider.GetRequiredService<IDatabaseRestorer>();

        await Assert.That(ReferenceEquals(databaseProvider, maintenance)).IsTrue();
        await Assert.That(ReferenceEquals(databaseProvider, dumper)).IsTrue();
        await Assert.That(ReferenceEquals(databaseProvider, restorer)).IsTrue();
    }

    [Test]
    public void PostgreSqlDatabaseProviderOptions_ThrowsWhenToolDirectoryIsInvalid()
    {
        AssertThrows<InvalidOperationException>(() =>
            _ = new PostgreSqlDatabaseProvider(new PostgreSqlDatabaseProviderOptions
            {
                ToolBinaryDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
            }));
    }

    private static string GetRepositoryRoot()
    {
        return RepositoryRootLocator.FindContainingDirectory(AppContext.BaseDirectory, "wakaze.root");
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
