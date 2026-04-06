using System.Buffers;
using Blake3Net = Blake3;
using Kawayi.Wakaze.Cas.Abstractions;

namespace Kawayi.Wakaze.Cas.Local;

/// <summary>
/// Stores blobs in a local file system backed content-addressed storage.
/// </summary>
/// <remarks>
/// Blob content is addressed by its BLAKE3 digest and persisted beneath the configured root path.
/// </remarks>
public sealed class FileSystemCas : ICas
{
    private const int CopyBufferSize = 64 * 1024;

    private readonly BlobPathStrategy _paths;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemCas"/> class.
    /// </summary>
    /// <param name="rootPath">The root directory that will hold the local CAS data.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="rootPath"/> is <see langword="null"/>, empty, or whitespace.</exception>
    public FileSystemCas(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        _paths = new BlobPathStrategy(Path.GetFullPath(rootPath));
    }

    /// <summary>
    /// Releases resources associated with the current instance.
    /// </summary>
    public void Dispose()
    {
    }

    /// <summary>
    /// Determines whether a blob with the specified identifier exists in the local store.
    /// </summary>
    /// <param name="id">The identifier of the blob to check.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A value that resolves to <see langword="true"/> when the blob exists; otherwise, <see langword="false"/>.</returns>
    public ValueTask<bool> ExistsAsync(
        BlobId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(File.Exists(_paths.GetContentPath(id)));
    }

    /// <summary>
    /// Retrieves metadata for a blob from the local store.
    /// </summary>
    /// <param name="id">The identifier of the blob to inspect.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A value that resolves to the blob metadata, or <see langword="null"/> when the blob does not exist.</returns>
    public ValueTask<BlobStat?> StatAsync(
        BlobId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = _paths.GetContentPath(id);
        if (!File.Exists(path))
        {
            return ValueTask.FromResult<BlobStat?>(null);
        }

        var fileInfo = new FileInfo(path);
        return ValueTask.FromResult<BlobStat?>(new BlobStat(id, checked((ulong)fileInfo.Length)));
    }

    /// <summary>
    /// Opens a readable stream for the requested blob range.
    /// </summary>
    /// <param name="request">The blob and range to read.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A value that resolves to a readable stream for the requested range.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the requested blob does not exist in the local store.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the requested range exceeds the blob length.</exception>
    public ValueTask<Stream> OpenReadAsync(
        ReadRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = _paths.GetContentPath(request.Id);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The requested blob was not found.", path);
        }

        var stream = OpenReadStream(path);
        if (request.Range.Kind == BlobRangeKind.Full)
        {
            return ValueTask.FromResult<Stream>(stream);
        }

        try
        {
            var blobLength = checked((ulong)stream.Length);
            var resolvedRange = request.Range.Resolve(blobLength);
            var offset = checked((long)resolvedRange.Offset);
            var length = checked((long)resolvedRange.Length);

            stream.Seek(offset, SeekOrigin.Begin);
            return ValueTask.FromResult<Stream>(new BoundedReadStream(stream, length));
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Stores content in the local store and returns its content-derived identifier.
    /// </summary>
    /// <param name="content">The readable stream that provides the blob content.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A value that resolves to the resulting blob identifier and stored length.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is not readable.</exception>
    public async ValueTask<PutResult> PutAsync(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanRead)
        {
            throw new ArgumentException("The content stream must be readable.", nameof(content));
        }

        await using var tempFile = TempFileLease.Create(_paths.TempRoot);
        await using var tempStream = tempFile.OpenWriteStream();

        var buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
        var hasher = Blake3Net.Hasher.New();
        ulong length = 0;

        try
        {
            while (true)
            {
                var bytesRead = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                await tempStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                hasher.Update(buffer.AsSpan(0, bytesRead));
                length = checked(length + (uint)bytesRead);
            }

            await tempStream.FlushAsync(cancellationToken);

            Span<byte> hashBytes = stackalloc byte[32];
            hasher.Finalize(hashBytes);

            var blobId = CreateBlobId(hashBytes);
            var finalPath = _paths.GetContentPath(blobId);

            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

            try
            {
                File.Move(tempFile.Path, finalPath, overwrite: false);
                tempFile.MarkCommitted();
            }
            catch (IOException) when (File.Exists(finalPath))
            {
                tempFile.MarkCommitted();
                TryDelete(tempFile.Path);
            }

            return new PutResult(blobId, length);
        }
        finally
        {
            hasher.Dispose();
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static BlobId CreateBlobId(ReadOnlySpan<byte> hashBytes)
    {
        Kawayi.Wakaze.Digest.Blake3 digest = default;
        Span<byte> destination = digest;
        hashBytes.CopyTo(destination);
        return new BlobId(digest);
    }

    private static FileStream OpenReadStream(string path)
    {
        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            CopyBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
        }
    }
}
