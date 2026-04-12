namespace Kawayi.Wakaze.IO;

/// <summary>
/// Provides directory-tree file system operations that preserve symbolic links and Unix file modes.
/// </summary>
public static class DirectoryTree
{
    /// <summary>
    /// Recursively copies a directory tree to a destination directory.
    /// </summary>
    /// <param name="sourceDirectory">The source directory path.</param>
    /// <param name="destinationDirectory">The destination directory path.</param>
    public static void Copy(string sourceDirectory, string destinationDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        var source = new DirectoryInfo(sourceDirectory);
        if (!source.Exists)
        {
            throw new DirectoryNotFoundException($"The source directory '{sourceDirectory}' was not found.");
        }

        CopyCore(source, destinationDirectory);
    }

    /// <summary>
    /// Deletes a directory tree when it exists.
    /// </summary>
    /// <param name="directoryPath">The directory path to delete.</param>
    public static void DeleteIfExists(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    private static void CopyCore(DirectoryInfo sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        CopyUnixModeIfSupported(sourceDirectory.FullName, destinationDirectory);

        foreach (var entry in sourceDirectory.EnumerateFileSystemInfos())
        {
            var destinationPath = Path.Combine(destinationDirectory, entry.Name);
            if (entry.LinkTarget is { } linkTarget)
            {
                CreateSymbolicLink(entry, destinationPath, linkTarget);
                continue;
            }

            if (entry is DirectoryInfo childDirectory)
            {
                CopyCore(childDirectory, destinationPath);
                continue;
            }

            if (entry is FileInfo file)
            {
                file.CopyTo(destinationPath);
                CopyUnixModeIfSupported(file.FullName, destinationPath);
            }
        }
    }

    private static void CreateSymbolicLink(FileSystemInfo source, string destinationPath, string linkTarget)
    {
        if (source.Attributes.HasFlag(FileAttributes.Directory))
        {
            Directory.CreateSymbolicLink(destinationPath, linkTarget);
            return;
        }

        File.CreateSymbolicLink(destinationPath, linkTarget);
    }

    private static void CopyUnixModeIfSupported(string sourcePath, string destinationPath)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        File.SetUnixFileMode(destinationPath, File.GetUnixFileMode(sourcePath));
    }
}
