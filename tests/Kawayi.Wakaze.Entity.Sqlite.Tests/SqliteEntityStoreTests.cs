using System.Collections.Immutable;
using Kawayi.Wakaze.Cas.Abstractions;
using Kawayi.Wakaze.Entity.Abstractions;
using EntityModel = Kawayi.Wakaze.Entity.Abstractions.Entity;

namespace Kawayi.Wakaze.Entity.Sqlite.Tests;

public class SqliteEntityStoreTests
{
    [Test]
    public async Task PutAsync_AndCurrentQueries_ReturnExpectedEntity()
    {
        await using var scope = await TestSqliteEntityStoreScope.CreateAsync();
        var entityId = EntityId.GenerateNew();
        var relatedId = EntityId.GenerateNew();
        var entity = CreateEntity(
            entityId,
            [new EntityRef(relatedId, RefKind.Strong)],
            [CreateBlobId(0x21), CreateBlobId(0x11)]);

        EntityRevision? writtenRevision = null;
        await scope.Store.ExecuteAsync(async (context, cancellationToken) =>
        {
            await context.PutAsync(entity, cancellationToken);
            writtenRevision = await context.GetRevisionAsync(entityId, cancellationToken);
        });

        var roundTrip = await scope.Store.GetAsync(entityId);
        var revision = await scope.Store.GetRevisionAsync(entityId);

        if (writtenRevision is null || revision is null)
            throw new Exception("Expected the entity revision to be available.");

        await Assert.That(roundTrip).IsEqualTo(entity);
        await Assert.That(await scope.Store.ExistsAsync(entityId)).IsTrue();
        await Assert.That(revision.Value).IsEqualTo(writtenRevision.Value);
    }

    [Test]
    public async Task MultiplePuts_CreateDistinctRevisions_AndHistoryRemainsReadable()
    {
        await using var scope = await TestSqliteEntityStoreScope.CreateAsync();
        var entityId = EntityId.GenerateNew();
        var first = CreateEntity(entityId, [new EntityRef(EntityId.GenerateNew(), RefKind.Weak)]);
        var second = CreateEntity(entityId, [new EntityRef(EntityId.GenerateNew(), RefKind.Strong)]);

        var firstRevision = await PutAndGetRevisionAsync(scope.Store, first);
        var secondRevision = await PutAndGetRevisionAsync(scope.Store, second);
        var revisions = await CollectAsync(scope.Store.ListRevisionsAsync(entityId));

        await Assert.That(firstRevision.Equals(secondRevision)).IsFalse();
        await Assert.That(revisions.Count).IsEqualTo(2);
        await Assert.That(revisions[0]).IsEqualTo(firstRevision);
        await Assert.That(revisions[1]).IsEqualTo(secondRevision);
        await Assert.That(await scope.Store.GetByRevisionAsync(entityId, firstRevision)).IsEqualTo(first);
        await Assert.That(await scope.Store.GetByRevisionAsync(entityId, secondRevision)).IsEqualTo(second);
    }

    [Test]
    public async Task TryPutAsync_AndTryDeleteAsync_RespectExpectedRevision()
    {
        await using var scope = await TestSqliteEntityStoreScope.CreateAsync();
        var entityId = EntityId.GenerateNew();
        var first = CreateEntity(entityId, [new EntityRef(EntityId.GenerateNew(), RefKind.Weak)]);
        var second = CreateEntity(entityId, [new EntityRef(EntityId.GenerateNew(), RefKind.Strong)]);
        var initialRevision = await PutAndGetRevisionAsync(scope.Store, first);

        var tryPutSucceeded = await scope.Store.ExecuteAsync((context, cancellationToken) =>
            context.TryPutAsync(second, initialRevision, cancellationToken));
        var currentRevision = await scope.Store.GetRevisionAsync(entityId);

        if (currentRevision is null)
            throw new Exception("Expected a visible revision after a successful conditional put.");

        var staleTryPutSucceeded = await scope.Store.ExecuteAsync((context, cancellationToken) =>
            context.TryPutAsync(first, initialRevision, cancellationToken));
        var staleTryDeleteSucceeded = await scope.Store.ExecuteAsync((context, cancellationToken) =>
            context.TryDeleteAsync(entityId, initialRevision, cancellationToken));
        var currentTryDeleteSucceeded = await scope.Store.ExecuteAsync((context, cancellationToken) =>
            context.TryDeleteAsync(entityId, currentRevision.Value, cancellationToken));
        var retryAfterDeleteSucceeded = await scope.Store.ExecuteAsync((context, cancellationToken) =>
            context.TryPutAsync(first, currentRevision.Value, cancellationToken));

        await Assert.That(tryPutSucceeded).IsTrue();
        await Assert.That(staleTryPutSucceeded).IsFalse();
        await Assert.That(staleTryDeleteSucceeded).IsFalse();
        await Assert.That(currentTryDeleteSucceeded).IsTrue();
        await Assert.That(retryAfterDeleteSucceeded).IsFalse();
    }

    [Test]
    public async Task DeleteSemantics_KeepHistory_AndHideCurrentVisibility()
    {
        await using var scope = await TestSqliteEntityStoreScope.CreateAsync();
        var entityId = EntityId.GenerateNew();
        var entity = CreateEntity(entityId, [new EntityRef(EntityId.GenerateNew(), RefKind.Strong)]);
        var revision = await PutAndGetRevisionAsync(scope.Store, entity);

        await scope.Store.ExecuteAsync((context, cancellationToken) =>
            context.DeleteAsync(entityId, cancellationToken));

        var defaultRead = await scope.Store.GetAsync(entityId);
        var includeDeletedRead = await scope.Store.GetAsync(entityId, new EntityReadOptions(true));
        var history = await CollectAsync(scope.Store.ListRevisionsAsync(entityId));

        await Assert.That(defaultRead).IsNull();
        await Assert.That(await scope.Store.ExistsAsync(entityId)).IsFalse();
        await Assert.That(await scope.Store.GetRevisionAsync(entityId)).IsNull();
        await Assert.That(includeDeletedRead).IsEqualTo(entity);
        await Assert.That(history.Count).IsEqualTo(1);
        await Assert.That(history[0]).IsEqualTo(revision);
        await Assert.That(await scope.Store.GetByRevisionAsync(entityId, revision)).IsEqualTo(entity);
    }

    [Test]
    public async Task GetReferrersAsync_OnlyReturnsCurrentVisibleReferrers()
    {
        await using var scope = await TestSqliteEntityStoreScope.CreateAsync();
        var targetId = EntityId.GenerateNew();
        var referrerA = CreateEntity(
            EntityId.GenerateNew(),
            [new EntityRef(targetId, RefKind.Strong), new EntityRef(targetId, RefKind.Weak)]);
        var referrerB = CreateEntity(EntityId.GenerateNew(), [new EntityRef(targetId, RefKind.Strong)]);
        var unrelated = CreateEntity(EntityId.GenerateNew(), [new EntityRef(EntityId.GenerateNew(), RefKind.Weak)]);

        await scope.Store.PutGraphAsync([CreateEntity(targetId), referrerA, referrerB, unrelated]);
        await scope.Store.ExecuteAsync((context, cancellationToken) =>
            context.DeleteAsync(referrerB.Id, cancellationToken));

        var referrers = await CollectAsync(scope.Store.GetReferrersAsync(targetId));

        await Assert.That(referrers.Count).IsEqualTo(1);
        await Assert.That(referrers[0]).IsEqualTo(referrerA);
    }

    [Test]
    public async Task BlobRefs_RoundTripWithSortedValueSemantics()
    {
        await using var scope = await TestSqliteEntityStoreScope.CreateAsync();
        var entityId = EntityId.GenerateNew();
        var blobA = CreateBlobId(0x10);
        var blobB = CreateBlobId(0x20);
        var entity = CreateEntity(entityId, blobRefs: [blobB, blobA, blobA]);

        await PutAndGetRevisionAsync(scope.Store, entity);
        var roundTrip = await scope.Store.GetAsync(entityId);

        if (roundTrip is null) throw new Exception("Expected the entity to be readable after it was written.");

        await Assert.That(roundTrip).IsEqualTo(entity);
        await Assert.That(roundTrip.BlobRefs.Length).IsEqualTo(3);
        await Assert.That(roundTrip.BlobRefs[0]).IsEqualTo(blobA);
        await Assert.That(roundTrip.BlobRefs[1]).IsEqualTo(blobA);
        await Assert.That(roundTrip.BlobRefs[2]).IsEqualTo(blobB);
    }

    [Test]
    public async Task ExecuteAsync_CommitsOnSuccess_AndRollsBackOnFailure()
    {
        await using var scope = await TestSqliteEntityStoreScope.CreateAsync();
        var committedA = CreateEntity(EntityId.GenerateNew());
        var committedB = CreateEntity(EntityId.GenerateNew());
        var rolledBack = CreateEntity(EntityId.GenerateNew());

        await scope.Store.ExecuteAsync(async (context, cancellationToken) =>
        {
            await context.PutAsync(committedA, cancellationToken);
            await context.PutAsync(committedB, cancellationToken);
        });

        try
        {
            await scope.Store.ExecuteAsync(async (context, cancellationToken) =>
            {
                await context.PutAsync(rolledBack, cancellationToken);
                throw new InvalidOperationException("Rollback probe.");
            });
        }
        catch (InvalidOperationException)
        {
        }

        await Assert.That(await scope.Store.GetAsync(committedA.Id)).IsEqualTo(committedA);
        await Assert.That(await scope.Store.GetAsync(committedB.Id)).IsEqualTo(committedB);
        await Assert.That(await scope.Store.GetAsync(rolledBack.Id)).IsNull();
    }

    [Test]
    public async Task PutGraphAsync_WritesMutuallyReferencingEntities()
    {
        await using var scope = await TestSqliteEntityStoreScope.CreateAsync();
        var leftId = EntityId.GenerateNew();
        var rightId = EntityId.GenerateNew();
        var left = CreateEntity(leftId, [new EntityRef(rightId, RefKind.Strong)]);
        var right = CreateEntity(rightId, [new EntityRef(leftId, RefKind.Weak)]);

        await scope.Store.PutGraphAsync([left, right]);

        var leftRoundTrip = await scope.Store.GetAsync(leftId);
        var rightRoundTrip = await scope.Store.GetAsync(rightId);
        var leftReferrers = await CollectAsync(scope.Store.GetReferrersAsync(leftId));
        var rightReferrers = await CollectAsync(scope.Store.GetReferrersAsync(rightId));

        await Assert.That(leftRoundTrip).IsEqualTo(left);
        await Assert.That(rightRoundTrip).IsEqualTo(right);
        await Assert.That(leftReferrers.Count).IsEqualTo(1);
        await Assert.That(leftReferrers[0]).IsEqualTo(right);
        await Assert.That(rightReferrers.Count).IsEqualTo(1);
        await Assert.That(rightReferrers[0]).IsEqualTo(left);
    }

    [Test]
    public async Task OpenSnapshotAsync_KeepsStableViewAfterLaterCommit()
    {
        await using var scope = await TestSqliteEntityStoreScope.CreateAsync();
        var entityId = EntityId.GenerateNew();
        var first = CreateEntity(entityId, [new EntityRef(EntityId.GenerateNew(), RefKind.Weak)]);
        var second = CreateEntity(entityId, [new EntityRef(EntityId.GenerateNew(), RefKind.Strong)]);

        await PutAndGetRevisionAsync(scope.Store, first);

        await using var snapshot = await scope.Store.OpenSnapshotAsync();

        await PutAndGetRevisionAsync(scope.Store, second);

        await Assert.That(await snapshot.GetAsync(entityId)).IsEqualTo(first);
        await Assert.That(await scope.Store.GetAsync(entityId)).IsEqualTo(second);
    }

    [Test]
    public async Task ConcurrentConditionalUpdate_AllowsOnlyOneWinner()
    {
        await using var scope = await TestSqliteEntityStoreScope.CreateAsync();
        var entityId = EntityId.GenerateNew();
        var initial = CreateEntity(entityId, [new EntityRef(EntityId.GenerateNew(), RefKind.Weak)]);
        var candidateA = CreateEntity(entityId, [new EntityRef(EntityId.GenerateNew(), RefKind.Strong)]);
        var candidateB = CreateEntity(entityId, [new EntityRef(EntityId.GenerateNew(), RefKind.Weak)]);
        var initialRevision = await PutAndGetRevisionAsync(scope.Store, initial);

        var firstTask = scope.Store.ExecuteAsync((context, cancellationToken) =>
                context.TryPutAsync(candidateA, initialRevision, cancellationToken))
            .AsTask();

        var secondTask = scope.Store.ExecuteAsync((context, cancellationToken) =>
                context.TryPutAsync(candidateB, initialRevision, cancellationToken))
            .AsTask();

        var results = await Task.WhenAll(firstTask, secondTask);
        var current = await scope.Store.GetAsync(entityId);

        await Assert.That(results.Count(static result => result)).IsEqualTo(1);

        if (current is null) throw new Exception("Expected one of the concurrent updates to win.");

        if (current != candidateA && current != candidateB)
            throw new Exception("The persisted entity did not match either concurrent candidate.");
    }

    [Test]
    public async Task MigrateAsync_IsIdempotent_OnEmptyDatabase()
    {
        await using var scope = await TestSqliteEntityStoreScope.CreateAsync(false);

        await scope.Migrator.MigrateAsync();
        await scope.Migrator.MigrateAsync();

        var entity = CreateEntity(EntityId.GenerateNew(), [new EntityRef(EntityId.GenerateNew(), RefKind.Strong)]);
        await PutAndGetRevisionAsync(scope.Store, entity);

        await Assert.That(await scope.Store.GetAsync(entity.Id)).IsEqualTo(entity);
    }

    private static async ValueTask<EntityRevision> PutAndGetRevisionAsync(
        SqliteEntityStore store,
        EntityModel entity)
    {
        return await store.ExecuteAsync(async (context, cancellationToken) =>
        {
            await context.PutAsync(entity, cancellationToken);

            var revision = await context.GetRevisionAsync(entity.Id, cancellationToken);
            if (revision is null) throw new Exception("Expected a visible revision after put.");

            return revision.Value;
        });
    }

    private static EntityModel CreateEntity(
        EntityId id,
        IEnumerable<EntityRef>? refs = null,
        IEnumerable<BlobId>? blobRefs = null)
    {
        return new EntityModel(
            id,
            refs?.ToImmutableArray() ?? [],
            blobRefs?.ToImmutableArray() ?? []);
    }

    private static BlobId CreateBlobId(byte seed, params (int Index, byte Value)[] overrides)
    {
        Kawayi.Wakaze.Digest.Blake3 digest = default;
        Span<byte> bytes = digest;

        for (var index = 0; index < bytes.Length; index++) bytes[index] = unchecked((byte)(seed + index));

        foreach (var (index, value) in overrides) bytes[index] = value;

        return new BlobId(digest);
    }

    private static async ValueTask<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
    {
        var result = new List<T>();
        await foreach (var item in source) result.Add(item);

        return result;
    }
}
