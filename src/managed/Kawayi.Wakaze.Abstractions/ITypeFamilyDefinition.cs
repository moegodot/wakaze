namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Defines a typed URI family within a scheme category.
/// </summary>
/// <typeparam name="TScheme">The URI scheme category.</typeparam>
public interface ITypeFamilyDefinition<TScheme>
    where TScheme : IUriSchemeDefinition
{
    /// <summary>
    /// Gets the versionless family URI.
    /// </summary>
    static abstract TypeUri TypeUri { get; }
}
