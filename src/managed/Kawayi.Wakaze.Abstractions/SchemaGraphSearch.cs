namespace Kawayi.Wakaze.Abstractions;

internal static class SchemaGraphSearch
{
    public static bool TryFindPath(
        SchemaId source,
        SchemaId target,
        Func<SchemaId, IEnumerable<SchemaId>> getNeighbors,
        out Dictionary<SchemaId, SchemaId> previous)
    {
        ArgumentNullException.ThrowIfNull(getNeighbors);

        previous = [];
        var visited = new HashSet<SchemaId> { source };
        var queue = new Queue<SchemaId>();
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
