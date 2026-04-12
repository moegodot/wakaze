using System.Data.Common;
using Kawayi.Wakaze.Db.Abstractions;
using Npgsql;

namespace Kawayi.Wakaze.Db.PostgreSql;

/// <summary>
/// Represents a PostgreSQL database resource.
/// </summary>
public sealed class PostgreSqlDatabase : IDatabase
{
    private readonly DatabaseEndpointLocation _location;
    private readonly DatabaseConnectionRequest _baselineConnection;

    internal PostgreSqlDatabase(
        DatabaseDescriptor descriptor,
        DatabaseEndpointLocation location,
        DatabaseConnectionRequest baselineConnection)
    {
        Descriptor = descriptor;
        _location = location;
        _baselineConnection = NormalizeDatabaseName(baselineConnection, location.DatabaseName!);
    }

    /// <summary>
    /// Gets the stable descriptor for the database resource.
    /// </summary>
    public DatabaseDescriptor Descriptor { get; }

    internal DatabaseEndpointLocation Location => _location;

    internal DatabaseConnectionRequest BaselineConnection => _baselineConnection;

    /// <summary>
    /// Builds an Npgsql connection string for the current database resource.
    /// </summary>
    /// <param name="request">Optional connection overrides.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>An Npgsql connection string.</returns>
    public ValueTask<string> GetConnectionStringAsync(
        DatabaseConnectionRequest request = default,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var merged = MergeRequests(_baselineConnection, request);
        var builder = new NpgsqlConnectionStringBuilder();

        ApplyProperties(builder, merged.Properties);

        builder.Host = _location.Host;
        if (_location.Port is int port)
        {
            builder.Port = port;
        }

        builder.Database = GetRequiredDatabaseName(merged, _location);

        if (merged.Credential is DatabaseCredential credential)
        {
            if (!string.IsNullOrWhiteSpace(credential.UserName))
            {
                builder.Username = credential.UserName;
            }

            if (credential.Password is not null)
            {
                builder.Password = credential.Password;
            }
        }

        if (merged.ReadOnly is true)
        {
            builder.Options = AppendReadOnlyOption(builder.Options);
        }

        return ValueTask.FromResult(builder.ConnectionString);
    }

    /// <summary>
    /// Opens an <see cref="NpgsqlConnection"/> for the current database resource.
    /// </summary>
    /// <param name="request">Optional connection overrides.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>An open <see cref="NpgsqlConnection"/> instance.</returns>
    public async ValueTask<DbConnection> OpenConnectionAsync(
        DatabaseConnectionRequest request = default,
        CancellationToken cancellationToken = default)
    {
        var connectionString = await GetConnectionStringAsync(request, cancellationToken);
        var connection = new NpgsqlConnection(connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Releases the database wrapper.
    /// </summary>
    /// <returns>A completed disposal operation.</returns>
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    internal static DatabaseConnectionRequest NormalizeDatabaseName(DatabaseConnectionRequest request, string databaseName)
    {
        return request with { DatabaseName = databaseName };
    }

    internal static DatabaseConnectionRequest MergeRequests(
        DatabaseConnectionRequest baseline,
        DatabaseConnectionRequest overrideRequest)
    {
        return new DatabaseConnectionRequest(
            MergeCredential(baseline.Credential, overrideRequest.Credential),
            overrideRequest.DatabaseName ?? baseline.DatabaseName,
            overrideRequest.ReadOnly ?? baseline.ReadOnly,
            MergeProperties(baseline.Properties, overrideRequest.Properties));
    }

    internal static string GetRequiredDatabaseName(DatabaseConnectionRequest request, DatabaseEndpointLocation location)
    {
        var databaseName = request.DatabaseName ?? location.DatabaseName;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new ArgumentException("A database name is required for PostgreSQL endpoint locations.");
        }

        return databaseName;
    }

    private static DatabaseCredential? MergeCredential(DatabaseCredential? baseline, DatabaseCredential? overrideCredential)
    {
        if (baseline is null)
        {
            return overrideCredential;
        }

        if (overrideCredential is null)
        {
            return baseline;
        }

        return new DatabaseCredential(
            overrideCredential.Value.UserName ?? baseline.Value.UserName,
            overrideCredential.Value.Password ?? baseline.Value.Password);
    }

    private static IReadOnlyDictionary<string, string?>? MergeProperties(
        IReadOnlyDictionary<string, string?>? baseline,
        IReadOnlyDictionary<string, string?>? overrideProperties)
    {
        if (baseline is null && overrideProperties is null)
        {
            return null;
        }

        var merged = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (baseline is not null)
        {
            foreach (var pair in baseline)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        if (overrideProperties is not null)
        {
            foreach (var pair in overrideProperties)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        return merged;
    }

    private static void ApplyProperties(NpgsqlConnectionStringBuilder builder, IReadOnlyDictionary<string, string?>? properties)
    {
        if (properties is null)
        {
            return;
        }

        foreach (var pair in properties)
        {
            builder[pair.Key] = pair.Value;
        }
    }

    private static string AppendReadOnlyOption(string? options)
    {
        const string readOnlyOption = "-c default_transaction_read_only=on";
        if (string.IsNullOrWhiteSpace(options))
        {
            return readOnlyOption;
        }

        if (options.Contains("default_transaction_read_only=on", StringComparison.Ordinal))
        {
            return options;
        }

        return $"{options} {readOnlyOption}";
    }
}
