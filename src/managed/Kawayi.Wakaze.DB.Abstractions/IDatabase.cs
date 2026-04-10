using System.Data.Common;

namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Describes a database resource and provides provider-neutral connection acquisition.
/// </summary>
public interface IDatabase : IAsyncDisposable
{
    /// <summary>
    /// Gets the stable descriptor for the database resource.
    /// </summary>
    DatabaseDescriptor Descriptor { get; }

    /// <summary>
    /// Builds a provider-specific connection string for the current database resource.
    /// </summary>
    /// <param name="request">Optional connection overrides such as credentials or provider-specific properties.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A provider-specific connection string.</returns>
    ValueTask<string> GetConnectionStringAsync(
        DatabaseConnectionRequest request = default,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens and returns an ADO.NET connection for the current database resource.
    /// </summary>
    /// <param name="request">Optional connection overrides such as credentials or provider-specific properties.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>An open provider-specific <see cref="DbConnection"/> instance.</returns>
    ValueTask<DbConnection> OpenConnectionAsync(
        DatabaseConnectionRequest request = default,
        CancellationToken cancellationToken = default);
}
