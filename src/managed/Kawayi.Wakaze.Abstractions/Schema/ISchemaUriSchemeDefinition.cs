namespace Kawayi.Wakaze.Abstractions.Schema;

/// <summary>
/// Defines a schema URI scheme category such as <c>semantic</c> or <c>database</c>.
/// </summary>
public interface ISchemaUriSchemeDefinition
{
    /// <summary>
    /// Gets the URI scheme identifier.
    /// </summary>
    static abstract string UriScheme { get; }
}
