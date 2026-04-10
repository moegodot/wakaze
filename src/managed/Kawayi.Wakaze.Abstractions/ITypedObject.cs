namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Represents an object bound to an exact versioned type schema.
/// </summary>
public interface ITypedObject
{
    /// <summary>
    /// Gets the exact schema identity of the object.
    /// </summary>
    UriTypeSchema TypeSchema { get; }
}
