using Kawayi.Wakaze.Entity.Sqlite;

namespace Kawayi.Wakaze.Entity.Sqlite.Tests;

internal sealed class TestSqliteEntityStoreScope : IAsyncDisposable
{
    private TestSqliteEntityStoreScope(string rootPath, string databasePath)
    {
        RootPath = rootPath;
        DatabasePath = databasePath;
        Options = new SqliteEntityStoreOptions
        {
            DatabasePath = databasePath
        };
        Store = new SqliteEntityStore(Options);
        Migrator = new SqliteEntityStoreMigrator(Options);
    }

    public string RootPath { get; }

    public string DatabasePath { get; }

    public SqliteEntityStoreOptions Options { get; }

    public SqliteEntityStore Store { get; }

    public SqliteEntityStoreMigrator Migrator { get; }

    public static async ValueTask<TestSqliteEntityStoreScope> CreateAsync(bool migrate = true)
    {
        var rootPath = Path.Combine(
            Path.GetTempPath(),
            "wakaze-entity-sqlite-tests",
            Guid.NewGuid().ToString("N"));

        var databasePath = Path.Combine(rootPath, "entity-store.db");
        var scope = new TestSqliteEntityStoreScope(rootPath, databasePath);

        if (migrate)
        {
            await scope.Migrator.MigrateAsync();
        }

        return scope;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, true);
            }
        }
        catch
        {
        }

        await ValueTask.CompletedTask;
    }
}
