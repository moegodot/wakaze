namespace Kawayi.Wakaze.IO;

/// <summary>
/// Resolves a containing directory by walking upward until a marker file is found.
/// </summary>
public static class RepositoryRootLocator
{
    /// <summary>
    /// Finds the nearest containing directory that contains the specified marker file.
    /// </summary>
    /// <param name="startPath">The starting file or directory path.</param>
    /// <param name="markerFileName">The marker file name to locate.</param>
    /// <returns>The containing directory that owns the marker file.</returns>
    public static string FindContainingDirectory(string startPath, string markerFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(startPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(markerFileName);

        var currentDirectory = ResolveStartingDirectory(startPath);

        while (true)
        {
            if (File.Exists(Path.Combine(currentDirectory, markerFileName))) return currentDirectory;

            var parentDirectory = Directory.GetParent(currentDirectory);
            if (parentDirectory is null)
                throw new InvalidOperationException(
                    $"Unable to locate '{markerFileName}' starting from '{startPath}'.");

            currentDirectory = parentDirectory.FullName;
        }
    }

    private static string ResolveStartingDirectory(string startPath)
    {
        var fullPath = Path.GetFullPath(startPath);

        if (Directory.Exists(fullPath)) return fullPath;

        var containingDirectory = Path.GetDirectoryName(fullPath);
        if (containingDirectory is null)
            throw new InvalidOperationException("Unable to resolve the starting directory.");

        return containingDirectory;
    }
}
