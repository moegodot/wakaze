using System.Runtime.CompilerServices;
using Kawayi.Wakaze.IO;

namespace Kawayi.Wakaze.IO.Tests;

public sealed class DirectoryTreeTests
{
    [Test]
    public async Task Copy_CopiesFilesAndNestedDirectories()
    {
        using var scope = new TemporaryDirectoryScope();
        var source = Path.Combine(scope.RootPath, "source");
        var destination = Path.Combine(scope.RootPath, "destination");
        Directory.CreateDirectory(Path.Combine(source, "nested"));
        await File.WriteAllTextAsync(Path.Combine(source, "root.txt"), "alpha");
        await File.WriteAllTextAsync(Path.Combine(source, "nested", "child.txt"), "beta");

        DirectoryTree.Copy(source, destination);

        await Assert.That(File.ReadAllText(Path.Combine(destination, "root.txt"))).IsEqualTo("alpha");
        await Assert.That(File.ReadAllText(Path.Combine(destination, "nested", "child.txt"))).IsEqualTo("beta");
    }

    [Test]
    public async Task Copy_PreservesSymbolicLinks()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var scope = new TemporaryDirectoryScope();
        var source = Path.Combine(scope.RootPath, "source");
        var destination = Path.Combine(scope.RootPath, "destination");
        Directory.CreateDirectory(source);
        await File.WriteAllTextAsync(Path.Combine(source, "target.txt"), "wakaze");
        File.CreateSymbolicLink(Path.Combine(source, "link.txt"), "target.txt");

        DirectoryTree.Copy(source, destination);

        var copiedLink = new FileInfo(Path.Combine(destination, "link.txt"));
        await Assert.That(copiedLink.LinkTarget).IsEqualTo("target.txt");
    }

    [Test]
    public async Task Copy_PreservesUnixFileMode()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var scope = new TemporaryDirectoryScope();
        var source = Path.Combine(scope.RootPath, "source");
        var destination = Path.Combine(scope.RootPath, "destination");
        Directory.CreateDirectory(source);

        var sourceFile = Path.Combine(source, "tool.sh");
        await File.WriteAllTextAsync(sourceFile, "#!/bin/sh\n");
        File.SetUnixFileMode(sourceFile, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        DirectoryTree.Copy(source, destination);

        var destinationMode = File.GetUnixFileMode(Path.Combine(destination, "tool.sh"));
        await Assert.That(destinationMode).IsEqualTo(
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    [Test]
    public async Task DeleteIfExists_DeletesExistingDirectory()
    {
        using var scope = new TemporaryDirectoryScope();
        var directory = Path.Combine(scope.RootPath, "delete-me");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "payload.txt"), "wakaze");

        DirectoryTree.DeleteIfExists(directory);

        await Assert.That(Directory.Exists(directory)).IsFalse();
    }

    [Test]
    public async Task DeleteIfExists_IgnoresMissingDirectory()
    {
        using var scope = new TemporaryDirectoryScope();
        var missingDirectory = Path.Combine(scope.RootPath, "missing");

        DirectoryTree.DeleteIfExists(missingDirectory);

        await Assert.That(Directory.Exists(missingDirectory)).IsFalse();
    }

    private sealed class TemporaryDirectoryScope : IDisposable
    {
        public TemporaryDirectoryScope()
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"wakaze-io-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(RootPath);
        }

        public string RootPath { get; }

        public void Dispose()
        {
            DirectoryTree.DeleteIfExists(RootPath);
        }
    }
}
