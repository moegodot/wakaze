namespace Kawayi.Wakaze.Db.Abstractions;

public interface IDatabaseConnectionStringProvider
{
    /// <summary>
    /// Builds a provider-specific connection string for the current database resource.
    /// </summary>
    /// <param name="request">Optional connection overrides such as credentials or provider-specific properties.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A provider-specific connection string.</returns>
    ValueTask<string> GetConnectionStringAsync(
        DatabaseConnectionRequest request = default,
        CancellationToken cancellationToken = default);
}
