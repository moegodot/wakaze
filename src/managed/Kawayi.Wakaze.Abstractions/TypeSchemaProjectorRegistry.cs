namespace Kawayi.Wakaze.Abstractions;

/// <summary>
/// Stores explicitly registered schema projectors and resolves chained projections transitively.
/// </summary>
public sealed class TypeSchemaProjectorRegistry : ITypeSchemaProjector
{
    private readonly Dictionary<UriTypeSchema, Dictionary<UriTypeSchema, Func<ITypedObject, ITypedObject>>> _edges = [];

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

        return TryFindPath(source, target, out _);
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

        if (!TryFindPath(source.TypeSchema, target, out var path))
        {
            projected = null;
            return false;
        }

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

    private bool TryFindPath(
        UriTypeSchema source,
        UriTypeSchema target,
        out IReadOnlyList<(UriTypeSchema Target, Func<ITypedObject, ITypedObject> Projector)> path)
    {
        path = Array.Empty<(UriTypeSchema Target, Func<ITypedObject, ITypedObject> Projector)>();

        var visited = new HashSet<UriTypeSchema> { source };
        var queue = new Queue<UriTypeSchema>();
        var previous = new Dictionary<UriTypeSchema, UriTypeSchema>();
        queue.Enqueue(source);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!_edges.TryGetValue(current, out var neighbors))
            {
                continue;
            }

            foreach (var next in neighbors.Keys)
            {
                if (!visited.Add(next))
                {
                    continue;
                }

                previous[next] = current;
                if (next == target)
                {
                    path = BuildPath(source, target, previous);
                    return true;
                }

                queue.Enqueue(next);
            }
        }

        return false;
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
}
