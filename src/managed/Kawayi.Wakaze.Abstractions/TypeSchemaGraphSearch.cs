namespace Kawayi.Wakaze.Abstractions;

internal static class TypeSchemaGraphSearch
{
    public static bool TryFindPath(
        UriTypeSchema source,
        UriTypeSchema target,
        Func<UriTypeSchema, IEnumerable<UriTypeSchema>> getNeighbors,
        out Dictionary<UriTypeSchema, UriTypeSchema> previous)
    {
        ArgumentNullException.ThrowIfNull(getNeighbors);

        previous = [];
        var visited = new HashSet<UriTypeSchema> { source };
        var queue = new Queue<UriTypeSchema>();
        queue.Enqueue(source);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var next in getNeighbors(current))
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
}
