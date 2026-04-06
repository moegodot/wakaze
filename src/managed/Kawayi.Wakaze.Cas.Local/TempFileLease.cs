namespace Kawayi.Wakaze.Cas.Local;

internal sealed class TempFileLease : IAsyncDisposable
{
    private const int BufferSize = 64 * 1024;

    private bool _committed;

    private TempFileLease(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public static TempFileLease Create(string tempRoot)
    {
        Directory.CreateDirectory(tempRoot);

        while (true)
        {
            var path = System.IO.Path.Combine(tempRoot, $"{Guid.NewGuid():N}.tmp");

            try
            {
                using var stream = new FileStream(
                    path,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    1,
                    FileOptions.None);

                return new TempFileLease(path);
            }
            catch (IOException)
            {
            }
        }
    }

    public FileStream OpenWriteStream()
    {
        return new FileStream(
            Path,
            FileMode.Open,
            FileAccess.Write,
            FileShare.None,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    public void MarkCommitted()
    {
        _committed = true;
    }

    public ValueTask DisposeAsync()
    {
        if (_committed)
        {
            return ValueTask.CompletedTask;
        }

        try
        {
            File.Delete(Path);
        }
        catch
        {
        }

        return ValueTask.CompletedTask;
    }
}
