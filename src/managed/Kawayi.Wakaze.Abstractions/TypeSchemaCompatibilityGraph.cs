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

        return TryFindPath(source, target, out _);
    }

    private bool TryFindPath(
        UriTypeSchema source,
        UriTypeSchema target,
        out Dictionary<UriTypeSchema, UriTypeSchema> previous)
    {
        previous = [];
        var visited = new HashSet<UriTypeSchema> { source };
        var queue = new Queue<UriTypeSchema>();
        queue.Enqueue(source);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!_edges.TryGetValue(current, out var neighbors))
            {
                continue;
            }

            foreach (var next in neighbors)
            {
                if (!visited.Add(next))
                {
                    continue;
                }

                previous[next] = current;
                if (next == target)
                {
                    return true;
                }

                queue.Enqueue(next);
            }
        }

        return false;
    }

    private static void EnsureSameFamily(UriTypeSchema source, UriTypeSchema target)
    {
        if (source.TypeUri != target.TypeUri)
        {
            throw new ArgumentException("Compatibility edges must stay within the same type family.");
        }
    }
}
