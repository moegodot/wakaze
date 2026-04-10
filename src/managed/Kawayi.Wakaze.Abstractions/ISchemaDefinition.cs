namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Defines an exact schema identifier within a family and scheme category.
/// </summary>
/// <typeparam name="TFamily">The family definition.</typeparam>
/// <typeparam name="TScheme">The URI scheme category.</typeparam>
public interface ISchemaDefinition<TFamily, TScheme> : ISchemaDefinitionMarker
    where TFamily : ISchemaFamilyDefinition<TScheme>
    where TScheme : ISchemaUriSchemeDefinition
{
    /// <summary>
    /// Gets exact target schemas that can consume values produced as this schema without projection.
    /// </summary>
    static abstract IReadOnlyList<SchemaId> CompatibleTargets { get; }

    /// <summary>
    /// Gets exact target schemas that may be produced from this schema through explicit projection.
    /// </summary>
    static abstract IReadOnlyList<SchemaId> ProjectableTargets { get; }
}
