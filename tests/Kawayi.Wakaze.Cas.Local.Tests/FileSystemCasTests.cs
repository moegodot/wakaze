using System.Text;
using Kawayi.Wakaze.Cas.Abstractions;
using Kawayi.Wakaze.Cas.Abstractions.Admin;

namespace Kawayi.Wakaze.Cas.Local.Tests;

public class FileSystemCasTests
{
    [Test]
    public async Task PutAsync_WritesBlob_AndQueriesSucceed()
    {
        await using var scope = new TestCasScope();
        const string content = "hello local cas";

        PutResult putResult;
        using (var input = TestCasScope.Utf8Stream(content))
        {
            putResult = await scope.Cas.PutAsync(input);
        }

        await Assert.That(putResult.Length).IsEqualTo((ulong)Encoding.UTF8.GetByteCount(content));
        await Assert.That(await scope.Cas.ExistsAsync(putResult.Id)).IsTrue();

        var stat = await scope.Cas.StatAsync(putResult.Id);
        if (stat is null) throw new Exception("Expected blob stat to exist.");

        await Assert.That(stat.Value.Id).IsEqualTo(putResult.Id);
        await Assert.That(stat.Value.Size).IsEqualTo(putResult.Length);

        await using var stream = await scope.Cas.OpenReadAsync(new ReadRequest(putResult.Id, BlobRange.Full));
        var roundTrip = await ReadAllTextAsync(stream);
        await Assert.That(roundTrip).IsEqualTo(content);
    }

    [Test]
    public async Task OpenReadAsync_Supports_Slice_And_From_Ranges()
    {
        await using var scope = new TestCasScope();
        const string content = "0123456789abcdef";

        PutResult putResult;
        using (var input = TestCasScope.Utf8Stream(content))
        {
            putResult = await scope.Cas.PutAsync(input);
        }

        await using var slice = await scope.Cas.OpenReadAsync(new ReadRequest(putResult.Id, BlobRange.Slice(4, 6)));
        await using var from = await scope.Cas.OpenReadAsync(new ReadRequest(putResult.Id, BlobRange.From(10)));

        await Assert.That(await ReadAllTextAsync(slice)).IsEqualTo("456789");
        await Assert.That(await ReadAllTextAsync(from)).IsEqualTo("abcdef");
    }

    [Test]
    public async Task MissingBlob_QueriesAndRead_FollowExpectedSemantics()
    {
        await using var scope = new TestCasScope();
        var missingId = CreateBlobId(0xAB);

        await Assert.That(await scope.Cas.ExistsAsync(missingId)).IsFalse();
        await Assert.That(await scope.Cas.StatAsync(missingId)).IsNull();

        try
        {
            await using var _ = await scope.Cas.OpenReadAsync(new ReadRequest(missingId, BlobRange.Full));
            throw new Exception("Expected FileNotFoundException.");
        }
        catch (FileNotFoundException)
        {
        }
    }

    [Test]
    public async Task PutAsync_Deduplicates_WhenCalledTwiceWithSameContent()
    {
        await using var scope = new TestCasScope();
        const string content = "same payload for dedupe";

        PutResult first;
        PutResult second;

        using (var input = TestCasScope.Utf8Stream(content))
        {
            first = await scope.Cas.PutAsync(input);
        }

        using (var input = TestCasScope.Utf8Stream(content))
        {
            second = await scope.Cas.PutAsync(input);
        }

        await Assert.That(second.Id).IsEqualTo(first.Id);
        await Assert.That(second.Length).IsEqualTo(first.Length);
        await Assert.That(CountFiles(scope.ContentRoot)).IsEqualTo(1);
        await Assert.That(CountEntries(scope.TempRoot)).IsEqualTo(0);
    }

    [Test]
    public async Task PutAsync_Deduplicates_UnderConcurrentWriters()
    {
        await using var scope = new TestCasScope();
        const string content = "concurrent content payload";

        var tasks = Enumerable.Range(0, 12)
            .Select(async _ =>
            {
                using var input = TestCasScope.Utf8Stream(content);
                return await scope.Cas.PutAsync(input);
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var first = results[0];

        foreach (var result in results)
        {
            if (result.Id != first.Id) throw new Exception("Concurrent puts returned different blob ids.");

            if (result.Length != first.Length) throw new Exception("Concurrent puts returned different blob lengths.");
        }

        await Assert.That(CountFiles(scope.ContentRoot)).IsEqualTo(1);
        await Assert.That(CountEntries(scope.TempRoot)).IsEqualTo(0);
    }

    [Test]
    public async Task PutAsync_DifferentContent_ProducesDifferentBlobIds()
    {
        await using var scope = new TestCasScope();

        PutResult first;
        PutResult second;

        using (var input = TestCasScope.Utf8Stream("alpha"))
        {
            first = await scope.Cas.PutAsync(input);
        }

        using (var input = TestCasScope.Utf8Stream("beta"))
        {
            second = await scope.Cas.PutAsync(input);
        }

        if (first.Id == second.Id) throw new Exception("Different content produced the same blob id.");

        await Assert.That(CountFiles(scope.ContentRoot)).IsEqualTo(2);
    }

    [Test]
    public async Task PutAsync_StoresBlobAtDigestDerivedPath()
    {
        await using var scope = new TestCasScope();

        PutResult result;
        using (var input = TestCasScope.Utf8Stream("path-check"))
        {
            result = await scope.Cas.PutAsync(input);
        }

        var hex = result.Id.ToString("R", null);
        var expectedPath = Path.Combine(scope.ContentRoot, hex[..2], hex[2..4], hex);

        await Assert.That(File.Exists(expectedPath)).IsTrue();
    }

    [Test]
    public async Task TryDeleteAsync_ReturnsFalse_ForMissingBlob()
    {
        await using var scope = new TestCasScope();
        var admin = (ICasAdmin)scope.Cas;
        var missingId = CreateBlobId(0xCD);

        await Assert.That(await admin.TryDeleteAsync(missingId)).IsFalse();
    }

    [Test]
    public async Task TryDeleteAsync_RemovesExistingBlob()
    {
        await using var scope = new TestCasScope();
        var admin = (ICasAdmin)scope.Cas;

        PutResult putResult;
        using (var input = TestCasScope.Utf8Stream("delete me"))
        {
            putResult = await scope.Cas.PutAsync(input);
        }

        await Assert.That(await admin.TryDeleteAsync(putResult.Id)).IsTrue();
        await Assert.That(await scope.Cas.ExistsAsync(putResult.Id)).IsFalse();
        await Assert.That(await scope.Cas.StatAsync(putResult.Id)).IsNull();
    }

    [Test]
    public async Task ScanBlobIdsAsync_ReturnsStoredBlobIds()
    {
        await using var scope = new TestCasScope();
        var admin = (ICasAdmin)scope.Cas;

        var expected = new HashSet<BlobId>();
        foreach (var content in new[] { "scan-alpha", "scan-beta", "scan-gamma" })
        {
            using var input = TestCasScope.Utf8Stream(content);
            var result = await scope.Cas.PutAsync(input);
            expected.Add(result.Id);
        }

        var scanned = await CollectAsync(admin.ScanBlobIdsAsync());
        if (!scanned.SetEquals(expected)) throw new Exception("Scanned blob ids did not match the stored blob ids.");
    }

    [Test]
    public async Task ScanBlobIdsAsync_IgnoresTempFilesAndMalformedEntries()
    {
        await using var scope = new TestCasScope();
        var admin = (ICasAdmin)scope.Cas;

        PutResult putResult;
        using (var input = TestCasScope.Utf8Stream("scan-only-real-blob"))
        {
            putResult = await scope.Cas.PutAsync(input);
        }

        Directory.CreateDirectory(scope.TempRoot);
        await File.WriteAllTextAsync(Path.Combine(scope.TempRoot, "orphan.tmp"), "temp");

        Directory.CreateDirectory(Path.Combine(scope.ContentRoot, "zz", "yy"));
        await File.WriteAllTextAsync(Path.Combine(scope.ContentRoot, "zz", "yy", "not-a-digest"), "bad");

        var wrongShardName = new string('a', 64);
        Directory.CreateDirectory(Path.Combine(scope.ContentRoot, "ff", "ee"));
        await File.WriteAllTextAsync(Path.Combine(scope.ContentRoot, "ff", "ee", wrongShardName), "bad");

        var scanned = await CollectAsync(admin.ScanBlobIdsAsync());
        if (!scanned.SetEquals([putResult.Id]))
            throw new Exception("Scan returned blob ids for temp files or malformed entries.");
    }

    private static BlobId CreateBlobId(byte seed)
    {
        Kawayi.Wakaze.Digest.Blake3 digest = default;
        Span<byte> bytes = digest;

        for (var i = 0; i < bytes.Length; i++) bytes[i] = seed;

        return new BlobId(digest);
    }

    private static int CountEntries(string path)
    {
        return Directory.Exists(path)
            ? Directory.EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly).Count()
            : 0;
    }

    private static int CountFiles(string path)
    {
        return Directory.Exists(path)
            ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Count()
            : 0;
    }

    private static async Task<HashSet<BlobId>> CollectAsync(IAsyncEnumerable<BlobId> source)
    {
        var result = new HashSet<BlobId>();

        await foreach (var item in source) result.Add(item);

        return result;
    }

    private static async Task<string> ReadAllTextAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
