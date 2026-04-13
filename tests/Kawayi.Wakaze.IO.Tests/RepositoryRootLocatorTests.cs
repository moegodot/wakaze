using System.Runtime.CompilerServices;
using Kawayi.Wakaze.IO;

namespace Kawayi.Wakaze.IO.Tests;

public sealed class RepositoryRootLocatorTests
{
    [Test]
    public async Task FindContainingDirectory_ResolvesFromFilePath()
    {
        var result = RepositoryRootLocator.FindContainingDirectory(GetSourcePath(), "wakaze.root");

        await Assert.That(File.Exists(Path.Combine(result, "wakaze.root"))).IsTrue();
    }

    [Test]
    public async Task FindContainingDirectory_ResolvesFromDirectoryPath()
    {
        var startDirectory = Path.GetDirectoryName(GetSourcePath())
                             ?? throw new InvalidOperationException("Unable to resolve the test directory.");

        var result = RepositoryRootLocator.FindContainingDirectory(startDirectory, "wakaze.root");

        await Assert.That(File.Exists(Path.Combine(result, "wakaze.root"))).IsTrue();
    }

    private static string GetSourcePath([CallerFilePath] string path = "")
    {
        return path;
    }
}
