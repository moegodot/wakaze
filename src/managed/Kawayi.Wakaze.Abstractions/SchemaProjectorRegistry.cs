namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Stores explicitly registered schema projectors and resolves chained projections transitively.
/// </summary>
public sealed class SchemaProjectorRegistry : ISchemaProjector
{
    private readonly Dictionary<SchemaId, Dictionary<SchemaId, Func<ITypedObject, ITypedObject>>> _edges = [];
    private readonly Dictionary<SchemaId, HashSet<SchemaId>> _declaredProjectableTargets = [];

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
        SchemaId source,
        SchemaId target,
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
        where TFamily : ISchemaFamilyDefinition<TScheme>
        where TScheme : ISchemaUriSchemeDefinition
    {
        var targets = new HashSet<SchemaId>(TSchema.ProjectableTargets);

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
        where TSourceFamily : ISchemaFamilyDefinition<TSourceScheme>
        where TSourceScheme : ISchemaUriSchemeDefinition
        where TTargetSchema : ISchemaDefinition<TTargetFamily, TTargetScheme>
        where TTargetFamily : ISchemaFamilyDefinition<TTargetScheme>
        where TTargetScheme : ISchemaUriSchemeDefinition
    {
        RegisterSchema<TSourceSchema, TSourceFamily, TSourceScheme>();
        Register(TSourceSchema.Schema, TTargetSchema.Schema, projector);
    }

    /// <inheritdoc />
    public bool CanProject(SchemaId source, SchemaId target)
    {
        if (source == target)
        {
            return true;
        }

        if (source.Family != target.Family)
        {
            return false;
        }

        return SchemaGraphSearch.TryFindPath(
            source,
            target,
            current => _edges.TryGetValue(current, out var neighbors) ? neighbors.Keys : [],
            out _);
    }

    /// <inheritdoc />
    public bool TryProject(ITypedObject source, SchemaId target, out ITypedObject? projected)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.SchemaId == target)
        {
            projected = source;
            return true;
        }

        if (source.SchemaId.Family != target.Family)
        {
            projected = null;
            return false;
        }

        if (!SchemaGraphSearch.TryFindPath(
                source.SchemaId,
                target,
                current => _edges.TryGetValue(current, out var neighbors) ? neighbors.Keys : [],
                out var previous))
        {
            projected = null;
            return false;
        }

        var path = BuildPath(source.SchemaId, target, previous);

        ITypedObject current = source;
        foreach (var edge in path)
        {
            var next = edge.Projector(current) ??
                       throw new InvalidOperationException("A registered schema projector returned null.");

            if (next.SchemaId != edge.Target)
            {
                throw new InvalidOperationException(
                    $"A registered schema projector declared target '{edge.Target}' but returned '{next.SchemaId}'.");
            }

            current = next;
        }

        projected = current;
        return true;
    }

    private IReadOnlyList<(SchemaId Target, Func<ITypedObject, ITypedObject> Projector)> BuildPath(
        SchemaId source,
        SchemaId target,
        IReadOnlyDictionary<SchemaId, SchemaId> previous)
    {
        var result = new List<(SchemaId Target, Func<ITypedObject, ITypedObject> Projector)>();
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

    private static void EnsureSameFamily(SchemaId source, SchemaId target)
    {
        if (source.Family != target.Family)
        {
            throw new ArgumentException("Projection edges must stay within the same schema family.");
        }
    }

    private void EnsureDeclaredProjectableTarget(SchemaId source, SchemaId target)
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
