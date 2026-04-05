namespace Kawayi.Wakaze.Cas.Abstractions;

/// <summary>
/// Fully capable content addressed storage(CAS) system
/// </summary>
public interface ICas : ICasReader, ICasWriter, ICasQuerier
{
}
