using System.Text.Json;
using Kawayi.Wakaze.Db.Abstractions;
using Kawayi.Wakaze.Process;
using Npgsql;

namespace Kawayi.Wakaze.Db.PostgreSql;

/// <summary>
/// Provides PostgreSQL database resolution, provisioning, maintenance, dump, and restore operations.
/// </summary>
public sealed class PostgreSqlDatabaseProvider : IDatabaseProvider, IDatabaseMaintenanceService, IDatabaseDumper, IDatabaseRestorer
{
    private const string DumpArchiveFileName = "database.dump";
    private const string DumpManifestFileName = "manifest.json";
    private const string DumpFormat = "custom";

    private static readonly JsonSerializerOptions ManifestSerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly PostgreSqlDatabaseProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDatabaseProvider"/> class.
    /// </summary>
    /// <param name="options">The provider options.</param>
    public PostgreSqlDatabaseProvider(PostgreSqlDatabaseProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.CloneValidated();
    }

    /// <summary>
    /// Gets the stable provider identifier.
    /// </summary>
    public Kawayi.Wakaze.Abstractions.SchemaId<DatabaseScheme> ProviderId => PostgreSqlSchemaIds.Provider;

    /// <summary>
    /// Gets the stable engine identifier.
    /// </summary>
    public Kawayi.Wakaze.Abstractions.SchemaId<DatabaseScheme> Engine => PostgreSqlSchemaIds.Engine;

    /// <summary>
    /// Gets the provider display name.
    /// </summary>
    public string DisplayName => "PostgreSQL";

    /// <summary>
    /// Gets the ADO.NET invariant name.
    /// </summary>
    public string? AdoNetProviderInvariantName => "Npgsql";

    /// <summary>
    /// Gets the provider capability flags.
    /// </summary>
    public DatabaseCapabilities Capabilities =>
        DatabaseCapabilities.Transactions
        | DatabaseCapabilities.SnapshotReads
        | DatabaseCapabilities.SchemaManagement
        | DatabaseCapabilities.LogicalDump
        | DatabaseCapabilities.LogicalRestore
        | DatabaseCapabilities.HealthChecks
        | DatabaseCapabilities.Maintenance;

    /// <summary>
    /// Attempts to resolve a PostgreSQL database from an endpoint location.
    /// </summary>
    /// <param name="request">The database resolution request.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A resolved database, or <see langword="null"/> when the request does not target this provider.</returns>
    public ValueTask<IDatabase?> TryResolveAsync(
        DatabaseResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.ProviderId is { } providerId && providerId != ProviderId)
        {
            return ValueTask.FromResult<IDatabase?>(null);
        }

        if (request.Engine is { } engine && engine != Engine)
        {
            return ValueTask.FromResult<IDatabase?>(null);
        }

        if (request.Location is not DatabaseEndpointLocation endpoint)
        {
            return ValueTask.FromResult<IDatabase?>(null);
        }

        ValidateEndpoint(endpoint);

        var databaseName = request.Connection.DatabaseName ?? endpoint.DatabaseName;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new ArgumentException("A database name is required for PostgreSQL endpoint locations.", nameof(request));
        }

        var normalizedEndpoint = endpoint with { DatabaseName = databaseName };
        var baselineConnection = PostgreSqlDatabase.NormalizeDatabaseName(request.Connection, databaseName);
        return ValueTask.FromResult<IDatabase?>(CreateDatabase(normalizedEndpoint, baselineConnection));
    }

    /// <summary>
    /// Creates a new PostgreSQL database on an existing server.
    /// </summary>
    /// <param name="request">The provisioning request.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The created database resource.</returns>
    public async Task<IDatabase> CreateAsync(
        DatabaseProvisioningRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProviderId != ProviderId)
        {
            throw new ArgumentException($"Unsupported provider id '{request.ProviderId}'.", nameof(request));
        }

        if (request.Location is not DatabaseEndpointLocation endpoint)
        {
            throw new ArgumentException("PostgreSQL provisioning requires an endpoint location.", nameof(request));
        }

        ValidateEndpoint(endpoint);

        if (string.IsNullOrWhiteSpace(endpoint.DatabaseName))
        {
            throw new ArgumentException("A database name is required for PostgreSQL provisioning.", nameof(request));
        }

        var adminRequest = request.AdministrativeConnection with
        {
            DatabaseName = request.AdministrativeConnection.DatabaseName ?? _options.DefaultAdministrativeDatabaseName,
            ReadOnly = false
        };

        var createDatabaseCommand = BuildCreateDatabaseCommand(endpoint.DatabaseName, request.Properties);

        await using (var connection = await OpenNpgsqlConnectionAsync(endpoint, adminRequest, cancellationToken))
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = createDatabaseCommand;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var resolvedConnection = PostgreSqlDatabase.NormalizeDatabaseName(request.AdministrativeConnection, endpoint.DatabaseName);
        return CreateDatabase(endpoint, resolvedConnection);
    }

    /// <summary>
    /// Runs a PostgreSQL health check.
    /// </summary>
    /// <param name="database">The database to inspect.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A summarized health result.</returns>
    public async Task<DatabaseHealthCheckResult> CheckHealthAsync(
        IDatabase database,
        CancellationToken cancellationToken = default)
    {
        var postgreSqlDatabase = EnsureSupportedDatabase(database);

        try
        {
            await using var connection = await postgreSqlDatabase.OpenConnectionAsync(cancellationToken: cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            await command.ExecuteScalarAsync(cancellationToken);

            return new DatabaseHealthCheckResult(DatabaseHealthState.Healthy, "The database responded successfully.");
        }
        catch (Exception ex)
        {
            return new DatabaseHealthCheckResult(DatabaseHealthState.Unhealthy, ex.Message);
        }
    }

    /// <summary>
    /// Executes a PostgreSQL maintenance command.
    /// </summary>
    /// <param name="database">The database to maintain.</param>
    /// <param name="operation">The maintenance operation.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    public async Task ExecuteAsync(
        IDatabase database,
        DatabaseMaintenanceOperation operation,
        CancellationToken cancellationToken = default)
    {
        var postgreSqlDatabase = EnsureSupportedDatabase(database);
        var endpoint = postgreSqlDatabase.Location;
        var commandText = operation switch
        {
            DatabaseMaintenanceOperation.UpdateStatistics => "ANALYZE;",
            DatabaseMaintenanceOperation.CompactStorage => "VACUUM (FULL);",
            DatabaseMaintenanceOperation.RebuildIndexes => $"REINDEX DATABASE {QuoteIdentifier(endpoint.DatabaseName!)};",
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        await using var connection = await postgreSqlDatabase.OpenConnectionAsync(cancellationToken: cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Writes a PostgreSQL dump directory.
    /// </summary>
    /// <param name="dumpDirectory">The output directory.</param>
    /// <param name="database">The database to dump.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    public async Task DumpToAsync(string dumpDirectory, IDatabase database, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dumpDirectory);

        var postgreSqlDatabase = EnsureSupportedDatabase(database);
        var endpoint = postgreSqlDatabase.Location;
        var archivePath = Path.Combine(dumpDirectory, DumpArchiveFileName);
        var manifestPath = Path.Combine(dumpDirectory, DumpManifestFileName);

        Directory.CreateDirectory(dumpDirectory);

        await ProcessCommandRunner.RunAsync(
            new ProcessCommandRequest(
                GetRequiredToolPath("pg_dump"),
                BuildDumpArguments(endpoint, archivePath, postgreSqlDatabase.BaselineConnection),
                dumpDirectory,
                CaptureOutput: false,
                BuildToolEnvironment(postgreSqlDatabase.BaselineConnection),
                ThrowOnNonZeroExit: true),
            cancellationToken);

        var manifest = new PostgreSqlDumpManifest(
            ProviderId.ToString(),
            Engine.ToString(),
            nameof(DatabaseLocationKind.Endpoint),
            endpoint.Host,
            endpoint.Port,
            endpoint.DatabaseName!,
            DumpArchiveFileName,
            DumpFormat,
            DateTimeOffset.UtcNow);

        await using var manifestStream = File.Create(manifestPath);
        await JsonSerializer.SerializeAsync(manifestStream, manifest, ManifestSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Restores a PostgreSQL dump directory into a new database.
    /// </summary>
    /// <param name="dumpDirectory">The source dump directory.</param>
    /// <param name="target">The target provisioning request.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The restored database resource.</returns>
    public async Task<IDatabase> RestoreDumpAsync(
        string dumpDirectory,
        DatabaseProvisioningRequest target,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dumpDirectory);

        var manifestPath = Path.Combine(dumpDirectory, DumpManifestFileName);
        await using var manifestStream = File.OpenRead(manifestPath);
        var manifest = await JsonSerializer.DeserializeAsync<PostgreSqlDumpManifest>(
            manifestStream,
            ManifestSerializerOptions,
            cancellationToken)
            ?? throw new InvalidOperationException("The dump manifest could not be read.");

        ValidateManifest(manifest);

        var restoredDatabase = await CreateAsync(target, cancellationToken);
        var postgreSqlDatabase = EnsureSupportedDatabase(restoredDatabase);
        var archivePath = Path.Combine(dumpDirectory, manifest.ArchiveFileName);

        await ProcessCommandRunner.RunAsync(
            new ProcessCommandRequest(
                GetRequiredToolPath("pg_restore"),
                BuildRestoreArguments(postgreSqlDatabase.Location, archivePath, postgreSqlDatabase.BaselineConnection),
                dumpDirectory,
                CaptureOutput: false,
                BuildToolEnvironment(postgreSqlDatabase.BaselineConnection),
                ThrowOnNonZeroExit: true),
            cancellationToken);

        return restoredDatabase;
    }

    private PostgreSqlDatabase CreateDatabase(
        DatabaseEndpointLocation endpoint,
        DatabaseConnectionRequest baselineConnection)
    {
        var descriptor = new DatabaseDescriptor(
            ProviderId,
            Engine,
            endpoint,
            endpoint.DatabaseName ?? endpoint.Host,
            Capabilities);

        return new PostgreSqlDatabase(descriptor, endpoint, baselineConnection);
    }

    private async Task<NpgsqlConnection> OpenNpgsqlConnectionAsync(
        DatabaseEndpointLocation endpoint,
        DatabaseConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var database = CreateDatabase(
            endpoint with { DatabaseName = PostgreSqlDatabase.GetRequiredDatabaseName(request, endpoint) },
            request);

        return (NpgsqlConnection)await database.OpenConnectionAsync(cancellationToken: cancellationToken);
    }

    private static string BuildCreateDatabaseCommand(
        string databaseName,
        IReadOnlyDictionary<string, string?>? properties)
    {
        var clauses = new List<string> { $"CREATE DATABASE {QuoteIdentifier(databaseName)}" };

        if (properties is not null)
        {
            foreach (var pair in properties)
            {
                if (pair.Key.Equals("owner", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(pair.Value))
                    {
                        throw new ArgumentException("Provisioning property 'owner' cannot be empty.");
                    }

                    clauses.Add($"OWNER = {QuoteIdentifier(pair.Value)}");
                    continue;
                }

                if (pair.Key.Equals("template", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(pair.Value))
                    {
                        throw new ArgumentException("Provisioning property 'template' cannot be empty.");
                    }

                    clauses.Add($"TEMPLATE = {QuoteIdentifier(pair.Value)}");
                    continue;
                }

                throw new ArgumentException($"Unsupported provisioning property '{pair.Key}'.");
            }
        }

        return string.Join(" ", clauses) + ";";
    }

    private static void ValidateEndpoint(DatabaseEndpointLocation endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint.Host))
        {
            throw new ArgumentException("A host is required for PostgreSQL endpoint locations.");
        }
    }

    private PostgreSqlDatabase EnsureSupportedDatabase(IDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);

        if (database is not PostgreSqlDatabase postgreSqlDatabase)
        {
            throw new ArgumentException("The database resource was not created by this PostgreSQL provider.", nameof(database));
        }

        if (postgreSqlDatabase.Descriptor.ProviderId != ProviderId || postgreSqlDatabase.Descriptor.Engine != Engine)
        {
            throw new ArgumentException("The database resource does not match this PostgreSQL provider.", nameof(database));
        }

        return postgreSqlDatabase;
    }

    private string GetRequiredToolPath(string toolName)
    {
        var fileName = OperatingSystem.IsWindows() ? $"{toolName}.exe" : toolName;
        return Path.Combine(_options.ToolBinaryDirectory!, fileName);
    }

    private static IReadOnlyList<string> BuildDumpArguments(
        DatabaseEndpointLocation endpoint,
        string archivePath,
        DatabaseConnectionRequest connectionRequest)
    {
        return new[]
        {
            "--format=custom",
            $"--file={archivePath}",
            $"--host={endpoint.Host}",
            endpoint.Port is int port ? $"--port={port}" : string.Empty,
            GetUserArgument(connectionRequest),
            $"--dbname={endpoint.DatabaseName!}"
        }.Where(static x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }

    private static IReadOnlyList<string> BuildRestoreArguments(
        DatabaseEndpointLocation endpoint,
        string archivePath,
        DatabaseConnectionRequest connectionRequest)
    {
        return new[]
        {
            $"--host={endpoint.Host}",
            endpoint.Port is int port ? $"--port={port}" : string.Empty,
            GetUserArgument(connectionRequest),
            $"--dbname={endpoint.DatabaseName!}",
            archivePath
        }.Where(static x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }

    private static IReadOnlyDictionary<string, string?>? BuildToolEnvironment(DatabaseConnectionRequest connectionRequest)
    {
        var password = connectionRequest.Credential?.Password;
        if (password is null)
        {
            return null;
        }

        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["PGPASSWORD"] = password
        };
    }

    private static string GetUserArgument(DatabaseConnectionRequest connectionRequest)
    {
        var userName = connectionRequest.Credential?.UserName;
        return string.IsNullOrWhiteSpace(userName) ? string.Empty : $"--username={userName}";
    }

    private static void ValidateManifest(PostgreSqlDumpManifest manifest)
    {
        if (!string.Equals(manifest.ProviderId, PostgreSqlSchemaIds.Provider.ToString(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported dump provider id '{manifest.ProviderId}'.");
        }

        if (!string.Equals(manifest.Engine, PostgreSqlSchemaIds.Engine.ToString(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported dump engine '{manifest.Engine}'.");
        }

        if (!string.Equals(manifest.LocationKind, nameof(DatabaseLocationKind.Endpoint), StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported dump location kind '{manifest.LocationKind}'.");
        }

        if (!string.Equals(manifest.Format, DumpFormat, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported dump format '{manifest.Format}'.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Host) || string.IsNullOrWhiteSpace(manifest.DatabaseName))
        {
            throw new InvalidOperationException("The dump manifest is missing endpoint information.");
        }
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
