namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Resolves compatibility relationships between versioned type schemas.
/// </summary>
public interface ITypeSchemaCompatibility
{
    /// <summary>
    /// Determines whether a value produced as <paramref name="source"/> can be consumed as <paramref name="target"/>.
    /// </summary>
    /// <param name="source">The source schema.</param>
    /// <param name="target">The target schema.</param>
    /// <returns>
    /// <see langword="true"/> when the source schema can be read as the target schema; otherwise, <see langword="false"/>.
    /// </returns>
    bool CanReadAs(UriTypeSchema source, UriTypeSchema target);
}
