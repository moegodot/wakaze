using System.Runtime.CompilerServices;
using Kawayi.Wakaze.Db.Abstractions;
using Kawayi.Wakaze.IO;
using Kawayi.Wakaze.Process;
using TUnit.Core.Interfaces;

namespace Kawayi.Wakaze.Db.PostgreSql.Tests;

public sealed class TestPostgreSqlDatabaseScope : IAsyncInitializer, IAsyncDisposable
{
    private string? _repositoryRoot;
    private string? _sourceInstallDirectory;
    private TestPostgreSqlInstallation? _sharedInstallation;
    private PostgreSqlDatabaseProvider? _provider;

    public string RepositoryRoot => _repositoryRoot ?? throw CreateNotInitializedException();

    public string SourceInstallDirectory => _sourceInstallDirectory ?? throw CreateNotInitializedException();

    public string RootDirectory => SharedInstallation.RootDirectory;

    public string InstallDirectory => SharedInstallation.InstallDirectory;

    public string DataDirectory => SharedInstallation.DataDirectory;

    public string SocketDirectory => SharedInstallation.SocketDirectory;

    public string LogFilePath => SharedInstallation.LogFilePath;

    public PostgreSqlDatabaseProvider Provider => _provider ?? throw CreateNotInitializedException();

    public string CurrentUser => Environment.UserName;

    private TestPostgreSqlInstallation SharedInstallation =>
        _sharedInstallation ?? throw CreateNotInitializedException();

    public async Task InitializeAsync()
    {
        var repositoryRoot = RepositoryRootLocator.FindContainingDirectory(GetSourcePath(), "wakaze.root");
        var sourceInstallDirectory = Path.Combine(repositoryRoot, "vendors", "install", "postgresql");
        if (!Directory.Exists(sourceInstallDirectory))
            throw new InvalidOperationException(
                $"PostgreSQL install directory was not found at '{sourceInstallDirectory}'.");

        var sharedInstallation = await TestPostgreSqlInstallation.CreateAsync(sourceInstallDirectory, "provider-scope");
        await sharedInstallation.InitializeDatabaseAsync();
        await sharedInstallation.StartServerAsync();

        _repositoryRoot = repositoryRoot;
        _sourceInstallDirectory = sourceInstallDirectory;
        _sharedInstallation = sharedInstallation;
        _provider = new PostgreSqlDatabaseProvider(new PostgreSqlDatabaseProviderOptions
        {
            ToolBinaryDirectory = sharedInstallation.ToolBinaryDirectory
        });
    }

    public DatabaseEndpointLocation CreateEndpoint(string databaseName)
    {
        return new DatabaseEndpointLocation(SocketDirectory, null, databaseName);
    }

    public DatabaseConnectionRequest CreateAdminConnection(string? databaseName = null)
    {
        return new DatabaseConnectionRequest(
            new DatabaseCredential(CurrentUser, null),
            databaseName,
            null,
            null);
    }

    public async ValueTask<IDatabase> CreateDatabaseAsync(string databaseName,
        CancellationToken cancellationToken = default)
    {
        return await Provider.CreateAsync(
            new DatabaseProvisioningRequest(
                Provider.ProviderId,
                CreateEndpoint(databaseName),
                databaseName,
                CreateAdminConnection()),
            cancellationToken);
    }

    public async Task ExecuteSqlAsync(string databaseName, string sql, CancellationToken cancellationToken = default)
    {
        var database = await CreateDatabaseAsync(databaseName, cancellationToken);
        await using (database)
        await using (var connection = await database.OpenConnectionAsync(cancellationToken: cancellationToken))
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<T?> ExecuteScalarAsync<T>(IDatabase database, string sql,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await database.OpenConnectionAsync(cancellationToken: cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? default : (T)result;
    }

    public async Task<string> ExecuteInstalledScalarAsync(
        TestPostgreSqlInstallation installation,
        string databaseName,
        string sql,
        string? userName = null,
        CancellationToken cancellationToken = default)
    {
        var result = await installation.RunInstalledCaptureAsync(
            "psql",
            CreatePsqlArguments(installation.SocketDirectory, databaseName, sql, userName, true),
            cancellationToken);
        return result.StandardOutput.Trim();
    }

    public Task ExecuteInstalledSqlAsync(
        TestPostgreSqlInstallation installation,
        string databaseName,
        string sql,
        string? userName = null,
        CancellationToken cancellationToken = default)
    {
        return installation.RunInstalledAsync(
            "psql",
            CreatePsqlArguments(installation.SocketDirectory, databaseName, sql, userName, false),
            cancellationToken);
    }

    public async Task<TestPostgreSqlInstallation> CreateInstalledEnvironmentAsync(
        string scenarioName,
        bool initializeDatabase = false,
        bool startServer = false,
        CancellationToken cancellationToken = default)
    {
        var installation = await TestPostgreSqlInstallation.CreateAsync(SourceInstallDirectory, scenarioName);
        if (initializeDatabase) await installation.InitializeDatabaseAsync(cancellationToken);

        if (startServer) await installation.StartServerAsync(cancellationToken);

        return installation;
    }

    public async ValueTask DisposeAsync()
    {
        if (_sharedInstallation is not null) await _sharedInstallation.DisposeAsync();
    }

    private static IReadOnlyList<string> CreatePsqlArguments(
        string socketDirectory,
        string databaseName,
        string sql,
        string? userName,
        bool captureScalar)
    {
        var arguments = new List<string>
        {
            "-h",
            socketDirectory
        };

        if (!string.IsNullOrWhiteSpace(userName))
        {
            arguments.Add("-U");
            arguments.Add(userName);
        }

        arguments.Add("-d");
        arguments.Add(databaseName);

        if (captureScalar)
        {
            arguments.Add("-Atqc");
        }
        else
        {
            arguments.Add("-v");
            arguments.Add("ON_ERROR_STOP=1");
            arguments.Add("-c");
        }

        arguments.Add(sql);
        return arguments;
    }

    private static string GetSourcePath([CallerFilePath] string path = "")
    {
        return path;
    }

    private static InvalidOperationException CreateNotInitializedException()
    {
        return new InvalidOperationException("The PostgreSQL test scope has not been initialized.");
    }
}

public sealed class TestPostgreSqlInstallation : IAsyncDisposable
{
    private TestPostgreSqlInstallation(
        string rootDirectory,
        string installDirectory,
        string dataDirectory,
        string socketDirectory,
        string logFilePath)
    {
        RootDirectory = rootDirectory;
        InstallDirectory = installDirectory;
        DataDirectory = dataDirectory;
        SocketDirectory = socketDirectory;
        LogFilePath = logFilePath;
    }

    public string RootDirectory { get; }

    public string InstallDirectory { get; }

    public string ToolBinaryDirectory => Path.Combine(InstallDirectory, "bin");

    public string DataDirectory { get; }

    public string SocketDirectory { get; }

    public string LogFilePath { get; }

    public bool IsInitialized { get; private set; }

    public bool IsRunning { get; private set; }

    public static Task<TestPostgreSqlInstallation> CreateAsync(string sourceInstallDirectory, string scenarioName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceInstallDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioName);

        if (!Directory.Exists(sourceInstallDirectory))
            throw new InvalidOperationException(
                $"PostgreSQL install directory was not found at '{sourceInstallDirectory}'.");

        var temporaryBaseDirectory = OperatingSystem.IsMacOS() ? "/tmp" : Path.GetTempPath();
        var rootDirectory = Path.Combine(
            temporaryBaseDirectory,
            $"wakaze-pg-{SanitizeScenarioName(scenarioName)}-{Guid.NewGuid():N}");
        var installDirectory = Path.Combine(rootDirectory, "postgresql");
        var dataDirectory = Path.Combine(rootDirectory, "data");
        var socketDirectory = Path.Combine(rootDirectory, "socket");
        var logFilePath = Path.Combine(rootDirectory, "postgres.log");

        Directory.CreateDirectory(rootDirectory);
        Directory.CreateDirectory(socketDirectory);
        DirectoryTree.Copy(sourceInstallDirectory, installDirectory);

        return Task.FromResult(new TestPostgreSqlInstallation(
            rootDirectory,
            installDirectory,
            dataDirectory,
            socketDirectory,
            logFilePath));
    }

    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized) return;

        await RunInstalledAsync(
            "initdb",
            ["-D", DataDirectory, "--auth-local=trust", "--auth-host=trust"],
            cancellationToken);
        IsInitialized = true;
    }

    public async Task StartServerAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning) return;

        if (!IsInitialized)
            throw new InvalidOperationException(
                "The PostgreSQL data directory must be initialized before the server starts.");

        try
        {
            await RunInstalledAsync(
                "pg_ctl",
                [
                    "-D", DataDirectory, "-l", LogFilePath, "-o",
                    $"-c listen_addresses='' -k {QuoteCommandValue(SocketDirectory)}", "-w", "start"
                ],
                cancellationToken);
        }
        catch (Exception ex)
        {
            var logOutput = File.Exists(LogFilePath)
                ? await File.ReadAllTextAsync(LogFilePath, cancellationToken)
                : "<postgres log file was not created>";
            throw new InvalidOperationException(
                $"Failed to start PostgreSQL. Log file '{LogFilePath}':{Environment.NewLine}{logOutput}",
                ex);
        }

        IsRunning = true;
    }

    public async Task StopServerAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning) return;

        await RunInstalledAsync("pg_ctl", ["-D", DataDirectory, "-w", "stop", "-m", "fast"], cancellationToken);
        IsRunning = false;
    }

    public Task RunInstalledAsync(
        string toolName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        return ProcessCommandRunner.RunAsync(
            new ProcessCommandRequest(
                GetInstalledToolPath(toolName),
                arguments,
                RootDirectory,
                false,
                null,
                true),
            cancellationToken);
    }

    public Task<ProcessCommandResult> RunInstalledCaptureAsync(
        string toolName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        return ProcessCommandRunner.RunAsync(
            new ProcessCommandRequest(
                GetInstalledToolPath(toolName),
                arguments,
                RootDirectory,
                true,
                null,
                true),
            cancellationToken);
    }

    public string GetInstalledToolPath(string toolName)
    {
        var fileName = OperatingSystem.IsWindows() ? $"{toolName}.exe" : toolName;
        var path = Path.Combine(ToolBinaryDirectory, fileName);
        if (!File.Exists(path))
            throw new InvalidOperationException($"Required PostgreSQL tool was not found at '{path}'.");

        return path;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopServerAsync();
        }
        catch
        {
        }

        try
        {
            if (Directory.Exists(RootDirectory)) DirectoryTree.DeleteIfExists(RootDirectory);
        }
        catch
        {
        }
    }

    private static string QuoteCommandValue(string value)
    {
        return
            $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }

    private static string SanitizeScenarioName(string scenarioName)
    {
        return string.Concat(scenarioName.Select(static ch => char.IsLetterOrDigit(ch) ? ch : '-')).Trim('-');
    }
}
