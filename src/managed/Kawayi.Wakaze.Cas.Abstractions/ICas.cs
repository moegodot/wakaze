namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Represents a content-addressed storage system that supports querying, reading, and writing blobs.
/// </summary>
public interface ICas : ICasReader, ICasWriter, ICasQuerier, IDisposable
{
}
