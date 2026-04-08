using Microsoft.Data.Sqlite;

namespace Kawayi.Wakaze.Entity.Sqlite;

/// <summary>
/// Configures the SQLite-backed entity store.
/// </summary>
public sealed class SqliteEntityStoreOptions
{
    /// <summary>
    /// Gets or sets the SQLite connection string to use for the store.
    /// </summary>
    /// <remarks>
    /// Set either <see cref="ConnectionString"/> or <see cref="DatabasePath"/>, but not both.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the SQLite database file path to use for the store.
    /// </summary>
    /// <remarks>
    /// Set either <see cref="DatabasePath"/> or <see cref="ConnectionString"/>, but not both.
    /// </remarks>
    public string? DatabasePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the directory that contains <see cref="DatabasePath"/> should be created automatically.
    /// </summary>
    public bool EnsureDatabaseDirectoryExists { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether migration should switch the database into WAL mode.
    /// </summary>
    public bool EnableWriteAheadLogging { get; set; } = true;

    internal SqliteEntityStoreOptions CloneValidated()
    {
        var clone = new SqliteEntityStoreOptions
        {
            ConnectionString = ConnectionString,
            DatabasePath = DatabasePath,
            EnsureDatabaseDirectoryExists = EnsureDatabaseDirectoryExists,
            EnableWriteAheadLogging = EnableWriteAheadLogging
        };

        var hasConnectionString = !string.IsNullOrWhiteSpace(clone.ConnectionString);
        var hasDatabasePath = !string.IsNullOrWhiteSpace(clone.DatabasePath);

        if (hasConnectionString == hasDatabasePath)
        {
            throw new InvalidOperationException("Configure exactly one of ConnectionString or DatabasePath.");
        }

        if (hasDatabasePath)
        {
            clone.DatabasePath = Path.GetFullPath(clone.DatabasePath!);
        }

        return clone;
    }

    internal string GetConnectionString()
    {
        var validated = CloneValidated();

        if (!string.IsNullOrWhiteSpace(validated.ConnectionString))
        {
            var builder = new SqliteConnectionStringBuilder(validated.ConnectionString)
            {
                ForeignKeys = true
            };

            return builder.ToString();
        }

        var path = validated.DatabasePath!;
        var pathBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            ForeignKeys = true,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        return pathBuilder.ToString();
    }

    internal void EnsureDatabaseDirectory()
    {
        var validated = CloneValidated();
        if (string.IsNullOrWhiteSpace(validated.DatabasePath) || !validated.EnsureDatabaseDirectoryExists)
        {
            return;
        }

        var directory = Path.GetDirectoryName(validated.DatabasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
