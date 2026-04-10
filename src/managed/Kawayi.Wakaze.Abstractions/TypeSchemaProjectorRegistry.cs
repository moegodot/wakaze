namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Stores explicitly registered schema projectors and resolves chained projections transitively.
/// </summary>
public sealed class TypeSchemaProjectorRegistry : ITypeSchemaProjector
{
    private readonly Dictionary<UriTypeSchema, Dictionary<UriTypeSchema, Func<ITypedObject, ITypedObject>>> _edges = [];
    private readonly Dictionary<UriTypeSchema, HashSet<UriTypeSchema>> _declaredProjectableTargets = [];

    /// <summary>
    /// Registers a direct projector from <paramref name="source"/> to <paramref name="target"/>.
    /// </summary>
    /// <param name="source">The source schema.</param>
    /// <param name="target">The target schema.</param>
    /// <param name="projector">The direct projection function.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="projector"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the schemas do not belong to the same family.
    /// </exception>
    public void Register(
        UriTypeSchema source,
        UriTypeSchema target,
        Func<ITypedObject, ITypedObject> projector)
    {
        ArgumentNullException.ThrowIfNull(projector);
        EnsureSameFamily(source, target);
        EnsureDeclaredProjectableTarget(source, target);

        if (source == target)
        {
            return;
        }

        if (!_edges.TryGetValue(source, out var targets))
        {
            targets = [];
            _edges[source] = targets;
        }

        targets[target] = projector;
    }

    /// <summary>
    /// Registers the declared projectable targets for <typeparamref name="TSchema"/>.
    /// </summary>
    /// <typeparam name="TSchema">The source schema definition.</typeparam>
    /// <typeparam name="TFamily">The source family definition.</typeparam>
    /// <typeparam name="TScheme">The source scheme definition.</typeparam>
    public void RegisterSchema<TSchema, TFamily, TScheme>()
        where TSchema : ISchemaDefinition<TFamily, TScheme>
        where TFamily : ITypeFamilyDefinition<TScheme>
        where TScheme : IUriSchemeDefinition
    {
        var targets = new HashSet<UriTypeSchema>(TSchema.ProjectableTargets);

        foreach (var target in targets)
        {
            EnsureSameFamily(TSchema.Schema, target);
        }

        _declaredProjectableTargets[TSchema.Schema] = targets;
    }

    /// <summary>
    /// Registers a direct projector declared by schema definitions.
    /// </summary>
    /// <typeparam name="TSourceSchema">The source schema definition.</typeparam>
    /// <typeparam name="TSourceFamily">The source family definition.</typeparam>
    /// <typeparam name="TSourceScheme">The source scheme definition.</typeparam>
    /// <typeparam name="TTargetSchema">The target schema definition.</typeparam>
    /// <typeparam name="TTargetFamily">The target family definition.</typeparam>
    /// <typeparam name="TTargetScheme">The target scheme definition.</typeparam>
    /// <param name="projector">The direct projection function.</param>
    public void Register<TSourceSchema, TSourceFamily, TSourceScheme, TTargetSchema, TTargetFamily, TTargetScheme>(
        Func<ITypedObject, ITypedObject> projector)
        where TSourceSchema : ISchemaDefinition<TSourceFamily, TSourceScheme>
        where TSourceFamily : ITypeFamilyDefinition<TSourceScheme>
        where TSourceScheme : IUriSchemeDefinition
        where TTargetSchema : ISchemaDefinition<TTargetFamily, TTargetScheme>
        where TTargetFamily : ITypeFamilyDefinition<TTargetScheme>
        where TTargetScheme : IUriSchemeDefinition
    {
        RegisterSchema<TSourceSchema, TSourceFamily, TSourceScheme>();
        Register(TSourceSchema.Schema, TTargetSchema.Schema, projector);
    }

    /// <inheritdoc />
    public bool CanProject(UriTypeSchema source, UriTypeSchema target)
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
            current => _edges.TryGetValue(current, out var neighbors) ? neighbors.Keys : [],
            out _);
    }

    /// <inheritdoc />
    public bool TryProject(ITypedObject source, UriTypeSchema target, out ITypedObject? projected)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.TypeSchema == target)
        {
            projected = source;
            return true;
        }

        if (source.TypeSchema.TypeUri != target.TypeUri)
        {
            projected = null;
            return false;
        }

        if (!TypeSchemaGraphSearch.TryFindPath(
                source.TypeSchema,
                target,
                current => _edges.TryGetValue(current, out var neighbors) ? neighbors.Keys : [],
                out var previous))
        {
            projected = null;
            return false;
        }

        var path = BuildPath(source.TypeSchema, target, previous);

        ITypedObject current = source;
        foreach (var edge in path)
        {
            var next = edge.Projector(current) ??
                       throw new InvalidOperationException("A registered type schema projector returned null.");

            if (next.TypeSchema != edge.Target)
            {
                throw new InvalidOperationException(
                    $"A registered type schema projector declared target '{edge.Target}' but returned '{next.TypeSchema}'.");
            }

            current = next;
        }

        projected = current;
        return true;
    }

    private IReadOnlyList<(UriTypeSchema Target, Func<ITypedObject, ITypedObject> Projector)> BuildPath(
        UriTypeSchema source,
        UriTypeSchema target,
        IReadOnlyDictionary<UriTypeSchema, UriTypeSchema> previous)
    {
        var result = new List<(UriTypeSchema Target, Func<ITypedObject, ITypedObject> Projector)>();
        var current = target;

        while (current != source)
        {
            var parent = previous[current];
            var projector = _edges[parent][current];
            result.Add((current, projector));
            current = parent;
        }

        result.Reverse();
        return result;
    }

    private static void EnsureSameFamily(UriTypeSchema source, UriTypeSchema target)
    {
        if (source.TypeUri != target.TypeUri)
        {
            throw new ArgumentException("Projection edges must stay within the same type family.");
        }
    }

    private void EnsureDeclaredProjectableTarget(UriTypeSchema source, UriTypeSchema target)
    {
        if (!_declaredProjectableTargets.TryGetValue(source, out var declaredTargets))
        {
            return;
        }

        if (!declaredTargets.Contains(target))
        {
            throw new ArgumentException(
                $"Projection target '{target}' is not declared in the source schema metadata for '{source}'.",
                nameof(target));
        }
    }
}
