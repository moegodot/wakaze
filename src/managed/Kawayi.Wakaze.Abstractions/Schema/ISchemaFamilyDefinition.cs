namespace Kawayi.Wakaze.Abstractions.Schema;

/// <summary>
/// Defines a schema family within a scheme category.
/// </summary>
/// <typeparam name="TScheme">The URI scheme category.</typeparam>
public interface ISchemaFamilyDefinition<TScheme>
    where TScheme : ISchemaUriSchemeDefinition
{
    /// <summary>
    /// Gets the versionless schema family identifier.
    /// </summary>
    static abstract SchemaFamily Family { get; }
}
