namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Defines a typed URI scheme category such as <c>semantic</c> or <c>database</c>.
/// </summary>
public interface IUriSchemeDefinition
{
    /// <summary>
    /// Gets the URI scheme identifier.
    /// </summary>
    static abstract string UriScheme { get; }
}
