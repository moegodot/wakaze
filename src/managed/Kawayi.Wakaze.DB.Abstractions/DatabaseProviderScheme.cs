using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Abstractions.Schema;

namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Database provider scheme URI scheme definition.
/// It's database's driver.
/// The two different provider and provide the database with same <see cref="DatabaseScheme"/>,
/// and they usually differs at the driver or strategy.
/// </summary>
public sealed class DatabaseProviderScheme : ISchemaUriSchemeDefinition
{
    /// <summary>
    /// Gets the database schema URI scheme identifier.
    /// </summary>
    public static string UriScheme => "database-provider";
}
