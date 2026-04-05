namespace Kawayi.Wakaze.Cas.Abstractions;

public interface ICasWriter
{
    ulong Write(Stream stream);
}
