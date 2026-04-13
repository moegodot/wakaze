namespace Kawayi.Wakaze.Abstractions.Schema;

/// <summary>
/// Resolves compatibility relationships between exact schema identifiers.
/// </summary>
public interface ISchemaCompatibility
{
    /// <summary>
    /// Determines whether a value produced as <paramref name="source"/> can be consumed as <paramref name="target"/>.
    /// This is a schema compatibility predicate and must not be interpreted as CLR type assignability
    /// , inheritance, or pattern-matching equivalence.
    /// </summary>
    /// <param name="source">The source schema.</param>
    /// <param name="target">The target schema.</param>
    /// <returns>
    /// <see langword="true"/> when the source schema can be read as the target schema; otherwise, <see langword="false"/>.
    /// </returns>
    bool CanReadAs(SchemaId source, SchemaId target);
}
