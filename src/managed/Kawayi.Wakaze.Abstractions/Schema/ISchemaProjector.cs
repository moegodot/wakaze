namespace Kawayi.Wakaze.Abstractions.Schema;

/// <summary>
/// Projects typed objects into other compatible schemas.
/// </summary>
public interface ISchemaProjector
{
    /// <summary>
    /// Determines whether a value can be projected from <paramref name="source"/> into <paramref name="target"/>.
    /// </summary>
    /// <param name="source">The source schema.</param>
    /// <param name="target">The target schema.</param>
    /// <returns>
    /// <see langword="true"/> when a projection path exists; otherwise, <see langword="false"/>.
    /// </returns>
    bool CanProject(SchemaId source, SchemaId target);

    /// <summary>
    /// Attempts to project a typed object into <paramref name="target"/>.
    /// </summary>
    /// <param name="source">The source object.</param>
    /// <param name="target">The target schema.</param>
    /// <param name="projected">The projected object when projection succeeds; otherwise, <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> when projection succeeds; otherwise, <see langword="false"/>.
    /// </returns>
    bool TryProject(ITypedObject source, SchemaId target, out ITypedObject? projected);
}
