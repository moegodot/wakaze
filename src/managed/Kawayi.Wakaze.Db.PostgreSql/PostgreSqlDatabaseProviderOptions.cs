namespace Kawayi.Wakaze.Db.PostgreSql;

/// <summary>
/// Configures the PostgreSQL database provider.
/// </summary>
public sealed class PostgreSqlDatabaseProviderOptions
{
    /// <summary>
    /// Gets or sets the directory that contains PostgreSQL command-line tools such as <c>pg_dump</c> and <c>pg_restore</c>.
    /// </summary>
    public string? ToolBinaryDirectory { get; set; }

    /// <summary>
    /// Gets or sets the database name used for administrative connections when no explicit administrative database is supplied.
    /// </summary>
    public string DefaultAdministrativeDatabaseName { get; set; } = "postgres";

    internal PostgreSqlDatabaseProviderOptions CloneValidated()
    {
        var clone = new PostgreSqlDatabaseProviderOptions
        {
            ToolBinaryDirectory = ToolBinaryDirectory,
            DefaultAdministrativeDatabaseName = DefaultAdministrativeDatabaseName
        };

        if (string.IsNullOrWhiteSpace(clone.ToolBinaryDirectory))
            throw new InvalidOperationException("Configure ToolBinaryDirectory.");

        clone.ToolBinaryDirectory = Path.GetFullPath(clone.ToolBinaryDirectory);
        if (!Directory.Exists(clone.ToolBinaryDirectory))
            throw new InvalidOperationException(
                $"The PostgreSQL tool directory '{clone.ToolBinaryDirectory}' does not exist.");

        if (string.IsNullOrWhiteSpace(clone.DefaultAdministrativeDatabaseName))
            throw new InvalidOperationException("Configure DefaultAdministrativeDatabaseName.");

        return clone;
    }
}
