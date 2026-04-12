using Kawayi.Wakaze.Abstractions;

namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Defines the schema URI scheme used by database abstractions.
/// </summary>
public sealed class DatabaseScheme : ISchemaUriSchemeDefinition
{
    /// <summary>
    /// Gets the database schema URI scheme identifier.
    /// </summary>
    public static string UriScheme => "database";
}
