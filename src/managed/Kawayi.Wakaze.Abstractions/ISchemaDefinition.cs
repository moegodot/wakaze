namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Defines an exact versioned typed URI schema within a family and scheme category.
/// </summary>
/// <typeparam name="TFamily">The family definition.</typeparam>
/// <typeparam name="TScheme">The URI scheme category.</typeparam>
public interface ISchemaDefinition<TFamily, TScheme> : ISchemaDefinitionMarker
    where TFamily : ITypeFamilyDefinition<TScheme>
    where TScheme : IUriSchemeDefinition
{
    /// <summary>
    /// Gets exact target schemas that can consume values produced as this schema without projection.
    /// </summary>
    static abstract IReadOnlyList<UriTypeSchema> CompatibleTargets { get; }

    /// <summary>
    /// Gets exact target schemas that may be produced from this schema through explicit projection.
    /// </summary>
    static abstract IReadOnlyList<UriTypeSchema> ProjectableTargets { get; }
}
