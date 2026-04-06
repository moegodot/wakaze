using System.Text;
using Kawayi.Wakaze.Cas.Local;

namespace Kawayi.Wakaze.Cas.Local.Tests;

internal sealed class TestCasScope : IDisposable
{
    public TestCasScope()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(),
            "wakaze-cas-local-tests",
            Guid.NewGuid().ToString("N"));

        Cas = new FileSystemCas(RootPath);
    }

    public FileSystemCas Cas { get; }

    public string RootPath { get; }

    public string ContentRoot => Path.Combine(RootPath, "content");

    public string TempRoot => Path.Combine(RootPath, "temp");

    public void Dispose()
    {
        Cas.Dispose();

        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch
        {
        }
    }

    public static MemoryStream Utf8Stream(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text), writable: false);
    }
}
