namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Represents optional credential material used to connect to a database.
/// </summary>
/// <param name="UserName">The user name to authenticate as, when applicable.</param>
/// <param name="Password">The password to authenticate with, when applicable.</param>
public readonly record struct DatabaseCredential(
    string? UserName = null,
    string? Password = null);
