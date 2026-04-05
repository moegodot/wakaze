namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Access cas system with readonly permission
/// </summary>
public interface ICasReader : ICasQuerier, IDisposable
{
    ValueTask<Stream> OpenReadAsync(
        ReadRequest request,
        CancellationToken cancellationToken = default);
}
