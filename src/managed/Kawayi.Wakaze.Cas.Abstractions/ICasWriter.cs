namespace Kawayi.Wakaze.Cas.Abstractions;

public interface ICasWriter : IDisposable
{
    ValueTask<PutResult> PutAsync(
        Stream content,
        CancellationToken cancellationToken = default);
}
