namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Describes optional overrides used when acquiring a database connection.
/// </summary>
/// <param name="Credential">Optional credential material for the connection attempt.</param>
/// <param name="DatabaseName">An optional database name override for server-based engines.</param>
/// <param name="ReadOnly">
/// An optional hint that requests a read-only connection when the underlying provider supports it.
/// </param>
/// <param name="Properties">Optional provider-specific connection properties.</param>
public readonly record struct DatabaseConnectionRequest(
    DatabaseCredential? Credential = null,
    string? DatabaseName = null,
    bool? ReadOnly = null,
    IReadOnlyDictionary<string, string?>? Properties = null);
