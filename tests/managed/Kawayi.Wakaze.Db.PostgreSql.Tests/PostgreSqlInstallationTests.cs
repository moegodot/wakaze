using Kawayi.Wakaze.Process;
using TUnit.Core;

namespace Kawayi.Wakaze.Db.PostgreSql.Tests;

public sealed class PostgreSqlInstallationTests
{
    [ClassDataSource<TestPostgreSqlDatabaseScope>(Shared = SharedType.PerClass)]
    public required TestPostgreSqlDatabaseScope Scope { get; init; }

    [Test]
    public async Task ServerLifecycle_CanStartRestartAndQueryServer()
    {
        await using var installation = await Scope.CreateInstalledEnvironmentAsync("server-lifecycle");

        await installation.InitializeDatabaseAsync();

        var postgresVersion = await installation.RunInstalledCaptureAsync("postgres", ["--version"]);
        var psqlVersion = await installation.RunInstalledCaptureAsync("psql", ["--version"]);
        var initdbVersion = await installation.RunInstalledCaptureAsync("initdb", ["--version"]);

        await AssertContainsAsync(postgresVersion.StandardOutput, "PostgreSQL", "postgres --version");
        await AssertContainsAsync(psqlVersion.StandardOutput, "PostgreSQL", "psql --version");
        await AssertContainsAsync(initdbVersion.StandardOutput, "PostgreSQL", "initdb --version");

        await installation.StartServerAsync();

        var version = await Scope.ExecuteInstalledScalarAsync(installation, "postgres", "SELECT version();");
        await AssertContainsAsync(version, "PostgreSQL 18.3-wakaze", "SELECT version()");

        await installation.StopServerAsync();
        await installation.StartServerAsync();

        var ping = await Scope.ExecuteInstalledScalarAsync(installation, "postgres", "SELECT 1;");
        await AssertEqualAsync("1", ping, "SELECT 1 after restart");
    }

    [Test]
    public async Task RoleLifecycle_CanCreateUseAndDropRole()
    {
        await using var installation = await Scope.CreateInstalledEnvironmentAsync("role-lifecycle", initializeDatabase: true, startServer: true);

        await installation.RunInstalledAsync("createuser", ["-h", installation.SocketDirectory, "-s", "portable_user"]);
        await installation.RunInstalledAsync("createdb", ["-h", installation.SocketDirectory, "-O", "portable_user", "portable_role_db"]);

        var currentUser = await Scope.ExecuteInstalledScalarAsync(
            installation,
            "portable_role_db",
            "SELECT current_user;",
            "portable_user");
        await AssertEqualAsync("portable_user", currentUser, "current_user");

        await installation.RunInstalledAsync("dropdb", ["-h", installation.SocketDirectory, "portable_role_db"]);
        await installation.RunInstalledAsync("dropuser", ["-h", installation.SocketDirectory, "portable_user"]);

        var roleCount = await Scope.ExecuteInstalledScalarAsync(
            installation,
            "postgres",
            "SELECT count(*) FROM pg_roles WHERE rolname = 'portable_user';");
        await AssertEqualAsync("0", roleCount, "portable_user removal");
    }

    [Test]
    public async Task TableLifecycle_CanCreateQueryAndDropTable()
    {
        await using var installation = await Scope.CreateInstalledEnvironmentAsync("table-lifecycle", initializeDatabase: true, startServer: true);

        await installation.RunInstalledAsync("createdb", ["-h", installation.SocketDirectory, "portable_table_db"]);
        await Scope.ExecuteInstalledSqlAsync(
            installation,
            "portable_table_db",
            "CREATE TABLE portability_check(id integer primary key, payload text not null); INSERT INTO portability_check(id, payload) VALUES (1, 'portable'), (2, 'durable');");

        var rowCount = await Scope.ExecuteInstalledScalarAsync(
            installation,
            "portable_table_db",
            "SELECT count(*) FROM portability_check;");
        await AssertEqualAsync("2", rowCount, "portability_check row count");

        var payloads = await Scope.ExecuteInstalledScalarAsync(
            installation,
            "portable_table_db",
            "SELECT string_agg(payload, ',' ORDER BY id) FROM portability_check;");
        await AssertEqualAsync("portable,durable", payloads, "payload aggregation");

        await Scope.ExecuteInstalledSqlAsync(installation, "portable_table_db", "DROP TABLE portability_check;");

        var tableCount = await Scope.ExecuteInstalledScalarAsync(
            installation,
            "portable_table_db",
            "SELECT count(*) FROM pg_tables WHERE schemaname = 'public' AND tablename = 'portability_check';");
        await AssertEqualAsync("0", tableCount, "portability_check removal");

        await installation.RunInstalledAsync("dropdb", ["-h", installation.SocketDirectory, "portable_table_db"]);
    }

    [Test]
    public async Task BackupAndRestore_RoundTripsDataAndExtensions()
    {
        await using var installation = await Scope.CreateInstalledEnvironmentAsync("backup-restore", initializeDatabase: true, startServer: true);

        var dumpPath = Path.Combine(installation.RootDirectory, "portable_backup.dump");

        await installation.RunInstalledAsync("createdb", ["-h", installation.SocketDirectory, "portable_backup_db"]);
        await Scope.ExecuteInstalledSqlAsync(
            installation,
            "portable_backup_db",
            "CREATE EXTENSION pgcrypto; CREATE TABLE backup_check(id integer primary key, note text not null); INSERT INTO backup_check(id, note) VALUES (1, 'row1'), (2, 'row2');");

        var digest = await Scope.ExecuteInstalledScalarAsync(
            installation,
            "portable_backup_db",
            "SELECT encode(digest('wakaze', 'sha256'), 'hex');");
        await AssertEqualAsync(
            "516ca6ffab783d491b532d85a66e80a54a11655992295318ce2c137d8a599576",
            digest,
            "pgcrypto digest");

        await installation.RunInstalledAsync(
            "pg_dump",
            ["-h", installation.SocketDirectory, "-d", "portable_backup_db", "-Fc", "-f", dumpPath]);

        await installation.RunInstalledAsync("createdb", ["-h", installation.SocketDirectory, "portable_restore_db"]);
        await installation.RunInstalledAsync(
            "pg_restore",
            ["-h", installation.SocketDirectory, "-d", "portable_restore_db", dumpPath]);

        var restoredRows = await Scope.ExecuteInstalledScalarAsync(
            installation,
            "portable_restore_db",
            "SELECT string_agg(note, ',' ORDER BY id) FROM backup_check;");
        await AssertEqualAsync("row1,row2", restoredRows, "restored rows");

        await installation.RunInstalledAsync("dropdb", ["-h", installation.SocketDirectory, "portable_restore_db"]);
        await installation.RunInstalledAsync("dropdb", ["-h", installation.SocketDirectory, "portable_backup_db"]);
    }

    [Test]
    [MacOsOnly]
    public async Task PortableDependencies_UseRelocatableInstallNames()
    {
        await using var installation = await Scope.CreateInstalledEnvironmentAsync("portable-dependencies");

        var postgresPath = Path.Combine(installation.InstallDirectory, "bin", "postgres");
        var psqlPath = Path.Combine(installation.InstallDirectory, "bin", "psql");
        var initdbPath = Path.Combine(installation.InstallDirectory, "bin", "initdb");
        var libpqPath = Path.Combine(installation.InstallDirectory, "lib", "libpq.5.dylib");

        var postgresDependencies = await RunProcessCaptureAsync("otool", ["-L", postgresPath], installation.RootDirectory);
        var psqlDependencies = await RunProcessCaptureAsync("otool", ["-L", psqlPath], installation.RootDirectory);
        var initdbDependencies = await RunProcessCaptureAsync("otool", ["-L", initdbPath], installation.RootDirectory);
        var libpqDependencies = await RunProcessCaptureAsync("otool", ["-L", libpqPath], installation.RootDirectory);
        var libpqInstallName = await RunProcessCaptureAsync("otool", ["-D", libpqPath], installation.RootDirectory);

        await AssertNoForbiddenPathsAsync(postgresDependencies.StandardOutput, Scope.RepositoryRoot, "postgres dependencies");
        await AssertNoForbiddenPathsAsync(psqlDependencies.StandardOutput, Scope.RepositoryRoot, "psql dependencies");
        await AssertNoForbiddenPathsAsync(initdbDependencies.StandardOutput, Scope.RepositoryRoot, "initdb dependencies");
        await AssertNoForbiddenPathsAsync(libpqDependencies.StandardOutput, Scope.RepositoryRoot, "libpq dependencies");

        await AssertContainsAsync(postgresDependencies.StandardOutput, "@executable_path/../lib/libssl.3.dylib", "postgres dependency relocation");
        await AssertContainsAsync(psqlDependencies.StandardOutput, "@executable_path/../lib/libpq.5.dylib", "psql dependency relocation");
        await AssertContainsAsync(initdbDependencies.StandardOutput, "@executable_path/../lib/libpq.5.dylib", "initdb dependency relocation");
        await AssertContainsAsync(libpqDependencies.StandardOutput, "@loader_path/libssl.3.dylib", "libpq dependency relocation");
        await AssertContainsAsync(libpqInstallName.StandardOutput, "@loader_path/libpq.5.dylib", "libpq install name");
    }

    private static async Task<ProcessCommandResult> RunProcessCaptureAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory)
    {
        return await ProcessCommandRunner.RunAsync(
            new ProcessCommandRequest(
                fileName,
                arguments,
                workingDirectory,
                CaptureOutput: true,
                EnvironmentVariables: null,
                ThrowOnNonZeroExit: true));
    }

    private static Task AssertEqualAsync(string expected, string actual, string operation)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected '{operation}' to return '{expected}', but got '{actual}'.");
        }

        return Task.CompletedTask;
    }

    private static Task AssertContainsAsync(string actual, string expectedSubstring, string operation)
    {
        if (!actual.Contains(expectedSubstring, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected '{operation}' to contain '{expectedSubstring}', but got '{actual.Trim()}'.");
        }

        return Task.CompletedTask;
    }

    private static Task AssertNoForbiddenPathsAsync(string output, string repositoryRoot, string operation)
    {
        if (output.Contains(repositoryRoot, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"'{operation}' still references the repository root '{repositoryRoot}'.");
        }

        if (output.Contains("/opt/homebrew/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"'{operation}' still references a Homebrew absolute path.");
        }

        return Task.CompletedTask;
    }
}
