namespace Kawayi.Wakaze.Db.Abstractions;

/// <summary>
/// Dumps a database resource to a portable directory representation.
/// </summary>
public interface IDatabaseDumper
{
    /// <summary>
    /// Writes a dump of the specified database resource to <paramref name="dumpDirectory"/>.
    /// </summary>
    /// <param name="dumpDirectory">The target directory that receives the dump contents.</param>
    /// <param name="database">The database resource to dump.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    Task DumpToAsync(string dumpDirectory, IDatabase database, CancellationToken cancellationToken = default);
}
