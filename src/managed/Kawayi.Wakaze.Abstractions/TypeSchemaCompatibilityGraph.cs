namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Stores explicitly registered schema compatibility edges and resolves them transitively.
/// </summary>
public sealed class TypeSchemaCompatibilityGraph : ITypeSchemaCompatibility
{
    private readonly Dictionary<UriTypeSchema, HashSet<UriTypeSchema>> _edges = [];

    /// <summary>
    /// Registers a directed compatibility edge from <paramref name="source"/> to <paramref name="target"/>.
    /// </summary>
    /// <param name="source">The source schema that can be read as the target schema.</param>
    /// <param name="target">The target schema that the source can be read as.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the schemas do not belong to the same family.
    /// </exception>
    public void Register(UriTypeSchema source, UriTypeSchema target)
    {
        EnsureSameFamily(source, target);

        if (source == target)
        {
            return;
        }

        if (!_edges.TryGetValue(source, out var targets))
        {
            targets = [];
            _edges[source] = targets;
        }

        targets.Add(target);
    }

    /// <summary>
    /// Registers all compatibility edges declared by <typeparamref name="TSchema"/>.
    /// </summary>
    /// <typeparam name="TSchema">The source schema definition.</typeparam>
    /// <typeparam name="TFamily">The source family definition.</typeparam>
    /// <typeparam name="TScheme">The source scheme definition.</typeparam>
    public void Register<TSchema, TFamily, TScheme>()
        where TSchema : ISchemaDefinition<TFamily, TScheme>
        where TFamily : ITypeFamilyDefinition<TScheme>
        where TScheme : IUriSchemeDefinition
    {
        foreach (var target in TSchema.CompatibleTargets)
        {
            Register(TSchema.Schema, target);
        }
    }

    /// <inheritdoc />
    public bool CanReadAs(UriTypeSchema source, UriTypeSchema target)
    {
        if (source == target)
        {
            return true;
        }

        if (source.TypeUri != target.TypeUri)
        {
            return false;
        }

        return TypeSchemaGraphSearch.TryFindPath(
            source,
            target,
            current => _edges.TryGetValue(current, out var neighbors) ? neighbors : [],
            out _);
    }

    private static void EnsureSameFamily(UriTypeSchema source, UriTypeSchema target)
    {
        if (source.TypeUri != target.TypeUri)
        {
            throw new ArgumentException("Compatibility edges must stay within the same type family.");
        }
    }
}
