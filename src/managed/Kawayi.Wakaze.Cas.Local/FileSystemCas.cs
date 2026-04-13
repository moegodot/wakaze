using System.Buffers;
using System.Runtime.CompilerServices;
using Blake3Net = Blake3;
using Kawayi.Wakaze.Cas.Abstractions;
using Kawayi.Wakaze.Cas.Abstractions.Admin;

namespace Kawayi.Wakaze.Cas.Local;

/// <summary>
/// Stores blobs in a local file system backed content-addressed storage.
/// </summary>
/// <remarks>
/// Blob content is addressed by its BLAKE3 digest and persisted beneath the configured root path.
/// </remarks>
public sealed class FileSystemCas : ICas, ICasAdmin
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
        if (!File.Exists(path)) return ValueTask.FromResult<BlobStat?>(null);

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
        if (!File.Exists(path)) throw new FileNotFoundException("The requested blob was not found.", path);

        var stream = OpenReadStream(path);
        if (request.Range.Kind == BlobRangeKind.Full) return ValueTask.FromResult<Stream>(stream);

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
        if (!content.CanRead) throw new ArgumentException("The content stream must be readable.", nameof(content));

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
                if (bytesRead == 0) break;

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
                File.Move(tempFile.Path, finalPath, false);
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

    /// <summary>
    /// Attempts to delete a blob from the local store.
    /// </summary>
    /// <param name="id">The identifier of the blob to delete.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A value that resolves to <see langword="true"/> when the blob existed and was deleted; otherwise, <see langword="false"/>.</returns>
    public ValueTask<bool> TryDeleteAsync(
        BlobId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = _paths.GetContentPath(id);
        if (!File.Exists(path)) return ValueTask.FromResult(false);

        try
        {
            File.Delete(path);
            return ValueTask.FromResult(true);
        }
        catch (DirectoryNotFoundException)
        {
            return ValueTask.FromResult(false);
        }
    }

    /// <summary>
    /// Enumerates blob identifiers currently stored in the local store.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the enumeration.</param>
    /// <returns>An asynchronous sequence of blob identifiers currently present in the local store.</returns>
    public async IAsyncEnumerable<BlobId> ScanBlobIdsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_paths.ContentRoot)) yield break;

        foreach (var path in Directory.EnumerateFiles(_paths.ContentRoot, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryParseCanonicalBlobId(path, out var id)) continue;

            yield return id;
        }
    }

    private static BlobId CreateBlobId(ReadOnlySpan<byte> hashBytes)
    {
        Kawayi.Wakaze.Digest.Blake3 digest = default;
        Span<byte> destination = digest;
        hashBytes.CopyTo(destination);
        return new BlobId(digest);
    }

    private bool TryParseCanonicalBlobId(string path, out BlobId id)
    {
        var relativePath = Path.GetRelativePath(_paths.ContentRoot, path);
        if (relativePath == ".")
        {
            id = default;
            return false;
        }

        var pathSegments = relativePath.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        if (pathSegments.Length != 3
            || pathSegments[0].Length != 2
            || pathSegments[1].Length != 2
            || pathSegments[2].Length != 64
            || !Digest.Blake3.TryParse(pathSegments[2], null, out var digest)
            || !pathSegments[2].StartsWith(pathSegments[0], StringComparison.OrdinalIgnoreCase)
            || !pathSegments[2].AsSpan(2, 2).Equals(pathSegments[1], StringComparison.OrdinalIgnoreCase))
        {
            id = default;
            return false;
        }

        id = new BlobId(digest);
        return true;
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

    /// <summary>
    /// Releases resources associated with the current instance.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
