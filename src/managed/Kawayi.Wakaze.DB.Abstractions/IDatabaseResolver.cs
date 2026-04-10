namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Resolves a provider-specific database resource from a provider-neutral location request.
/// </summary>
public interface IDatabaseResolver
{
    /// <summary>
    /// Attempts to resolve a database resource from the supplied request.
    /// </summary>
    /// <param name="request">The location and optional provider constraints to inspect.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>
    /// The resolved database resource, or <see langword="null"/> when the request does not represent a supported database.
    /// </returns>
    ValueTask<IDatabase?> TryResolveAsync(
        DatabaseResolutionRequest request,
        CancellationToken cancellationToken = default);
}
