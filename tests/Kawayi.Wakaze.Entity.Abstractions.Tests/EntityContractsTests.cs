using System.Collections.Immutable;
using Kawayi.Wakaze.Abstractions;
using Kawayi.Wakaze.Abstractions.Schema;
using Kawayi.Wakaze.Entity.Abstractions;

namespace Kawayi.Wakaze.Entity.Abstractions.Tests;

public class EntityContractsTests
{
    [Test]
    public async Task Entity_And_EntityRef_Are_Directly_Constructible()
    {
        var entityId = EntityId.GenerateNew();
        var revision = EntityRevision.GenerateNew(entityId);
        var payload = new FakeTypedObject(new SchemaId("type://wakaze.dev/entity/v1"), "alpha");
        var entity = new Entity(entityId, revision, payload, new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc));
        var entityRef = new EntityRef(entityId, EntityId.GenerateNew(), RefKind.Strong);

        await Assert.That(entity.Id).IsEqualTo(entityId);
        await Assert.That(entity.Revision).IsEqualTo(revision);
        await Assert.That(entity.Payload).IsEqualTo<ITypedObject>(payload);
        await Assert.That(entity.Payload.SchemaId).IsEqualTo(payload.SchemaId);
        await Assert.That(entityRef.From).IsEqualTo(entityId);
        await Assert.That(entityRef.RefKind).IsEqualTo(RefKind.Strong);
    }

    [Test]
    public async Task EntityRevision_Equality_Uses_Entity_And_Opaque_Token()
    {
        var entityId = EntityId.GenerateNew();
        var token = Guid.CreateVersion7();
        var left = new EntityRevision(entityId, token);
        var same = new EntityRevision(entityId, token);
        var otherEntity = new EntityRevision(EntityId.GenerateNew(), token);
        var otherToken = new EntityRevision(entityId, Guid.CreateVersion7());

        await Assert.That(left == same).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(same.GetHashCode());
        await Assert.That(left != otherEntity).IsTrue();
        await Assert.That(left != otherToken).IsTrue();
    }

    [Test]
    public async Task GetByRevisionAsync_Uses_Revision_As_The_Historical_Address()
    {
        var store = new FakeEntityStore();
        var entity = CreateEntity("first");

        await store.ExecuteAsync((context, cancellationToken) => context.PutAsync(entity, cancellationToken));

        var historical = await store.GetByRevisionAsync(entity.Revision);

        await Assert.That(historical).IsNotNull();
        await Assert.That(historical!.Id).IsEqualTo(entity.Id);
        await Assert.That(historical.Revision).IsEqualTo(entity.Revision);
    }

    [Test]
    public async Task Reader_Uses_Current_Visible_State_Semantics_After_Delete()
    {
        var store = new FakeEntityStore();
        var entity = CreateEntity("visible");

        await store.ExecuteAsync((context, cancellationToken) => context.PutAsync(entity, cancellationToken));
        await store.ExecuteAsync((context, cancellationToken) => context.DeleteAsync(entity.Id, cancellationToken));

        await Assert.That(await store.GetAsync(entity.Id)).IsNull();
        await Assert.That(await store.ExistsAsync(entity.Id)).IsFalse();
        await Assert.That(await store.GetRevisionAsync(entity.Id)).IsNull();

        var historical = await store.GetByRevisionAsync(entity.Revision);
        await Assert.That(historical).IsNotNull();
        await Assert.That(historical!.DeletedAt).IsNull();
    }

    [Test]
    public async Task WriteContext_Observes_Its_Own_Writes_And_Deletes()
    {
        var store = new FakeEntityStore();
        var source = CreateEntity("source");
        var target = CreateEntity("target");
        var logicalRef = new EntityRef(source.Id, target.Id, RefKind.Strong);

        await store.ExecuteAsync(async (context, cancellationToken) =>
        {
            await context.PutAsync(source, cancellationToken);
            await context.PutAsync(target, cancellationToken);
            await context.AddRefAsync(logicalRef, cancellationToken);

            var written = await context.GetAsync(source.Id, cancellationToken);
            var outgoing = await context.GetOutgoingRefsAsync(source.Id, cancellationToken);

            await Assert.That(written).IsNotNull();
            await Assert.That(written!.Revision).IsEqualTo(source.Revision);
            await Assert.That(outgoing.Length).IsEqualTo(1);
            await Assert.That(outgoing[0]).IsEqualTo(logicalRef);

            await context.DeleteAsync(source.Id, cancellationToken);

            await Assert.That(await context.GetAsync(source.Id, cancellationToken)).IsNull();
            await Assert.That(await context.ExistsAsync(source.Id, cancellationToken)).IsFalse();
        });
    }

    [Test]
    public async Task ExecuteAsync_Rolls_Back_When_The_Delegate_Throws()
    {
        var store = new FakeEntityStore();
        var original = CreateEntity("original");
        var replacement = CreateEntity("replacement", original.Id);

        await store.ExecuteAsync((context, cancellationToken) => context.PutAsync(original, cancellationToken));

        await AssertThrowsAsync<InvalidOperationException>(async () =>
        {
            await store.ExecuteAsync(async (context, cancellationToken) =>
            {
                await context.PutAsync(replacement, cancellationToken);
                throw new InvalidOperationException("boom");
            });
        });

        var current = await store.GetAsync(original.Id);

        await Assert.That(current).IsNotNull();
        await Assert.That(current!.Revision).IsEqualTo(original.Revision);
        await Assert.That(current.Payload).IsEqualTo<ITypedObject>(original.Payload);
    }

    [Test]
    public async Task ExecuteAsync_Rejects_Nested_Atomic_Execution()
    {
        var store = new FakeEntityStore();

        await AssertThrowsAsync<InvalidOperationException>(async () =>
        {
            await store.ExecuteAsync((_, cancellationToken) =>
                store.ExecuteAsync((_, _) => ValueTask.CompletedTask, cancellationToken));
        });
    }

    [Test]
    public async Task Snapshot_Reads_Are_Stable_After_Later_Commits()
    {
        var store = new FakeEntityStore();
        var original = CreateEntity("original");
        var replacement = CreateEntity("replacement", original.Id);

        await store.ExecuteAsync((context, cancellationToken) => context.PutAsync(original, cancellationToken));

        await using var snapshot = await store.OpenSnapshotAsync();

        await store.ExecuteAsync((context, cancellationToken) => context.PutAsync(replacement, cancellationToken));

        var snapshotEntity = await snapshot.GetAsync(original.Id);
        var liveEntity = await store.GetAsync(original.Id);

        await Assert.That(snapshotEntity).IsNotNull();
        await Assert.That(liveEntity).IsNotNull();
        await Assert.That(snapshotEntity!.Revision).IsEqualTo(original.Revision);
        await Assert.That(liveEntity!.Revision).IsEqualTo(replacement.Revision);
    }

    [Test]
    public async Task LoadAsync_Returns_Entity_And_Requested_Logical_Edges()
    {
        var store = new FakeEntityStore();
        var source = CreateEntity("source");
        var target = CreateEntity("target");
        var logicalRef = new EntityRef(source.Id, target.Id, RefKind.Weak);

        await store.ExecuteAsync(async (context, cancellationToken) =>
        {
            await context.PutAsync(source, cancellationToken);
            await context.PutAsync(target, cancellationToken);
            await context.AddRefAsync(logicalRef, cancellationToken);
        });

        var sourceLoad = await store.LoadAsync(source.Id, new EntityLoadOptions(includeOutgoingRefs: true));
        var targetLoad = await store.LoadAsync(target.Id, new EntityLoadOptions(includeIncomingRefs: true));

        await Assert.That(sourceLoad).IsNotNull();
        await Assert.That(targetLoad).IsNotNull();
        await Assert.That(sourceLoad!.Entity.Id).IsEqualTo(source.Id);
        await Assert.That(sourceLoad.OutgoingRefs.Length).IsEqualTo(1);
        await Assert.That(sourceLoad.OutgoingRefs[0]).IsEqualTo(logicalRef);
        await Assert.That(sourceLoad.IncomingRefs.Length).IsEqualTo(0);
        await Assert.That(targetLoad!.IncomingRefs.Length).IsEqualTo(1);
        await Assert.That(targetLoad.IncomingRefs[0]).IsEqualTo(logicalRef);
        await Assert.That(targetLoad.OutgoingRefs.Length).IsEqualTo(0);
    }

    [Test]
    public async Task TryPutAsync_Rejects_Revision_Preconditions_For_Another_Entity()
    {
        var store = new FakeEntityStore();
        var entity = CreateEntity("entity");
        var other = CreateEntity("other");

        await AssertThrowsAsync<ArgumentException>(async () =>
        {
            await store.ExecuteAsync(async (context, cancellationToken) =>
            {
                await context.TryPutAsync(
                    entity,
                    new EntityOpPrecondition[] { new MustHaveRevision(other.Revision) },
                    cancellationToken);
            });
        });
    }

    private static Entity CreateEntity(string payloadValue, EntityId? entityId = null)
    {
        var id = entityId ?? EntityId.GenerateNew();
        return new Entity(
            id,
            EntityRevision.GenerateNew(id),
            new FakeTypedObject(new SchemaId($"type://wakaze.dev/{payloadValue}/v1"), payloadValue),
            new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc));
    }

    private static async Task AssertThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
            throw new Exception($"Expected {typeof(TException).Name}.");
        }
        catch (TException)
        {
        }
    }

    private sealed record FakeTypedObject(SchemaId SchemaId, string Value) : ITypedObject;

    private sealed class FakeEntityStore : IEntityStore
    {
        private Dictionary<EntityId, Entity> _visibleEntities = [];
        private Dictionary<EntityId, List<Entity>> _history = [];
        private List<EntityRef> _refs = [];
        private bool _inAtomic;

        public ValueTask<Entity?> GetAsync(EntityId id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(_visibleEntities.GetValueOrDefault(id));
        }

        public ValueTask<bool> ExistsAsync(EntityId id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(_visibleEntities.ContainsKey(id));
        }

        public ValueTask<ImmutableArray<EntityRef>> GetOutgoingRefsAsync(
            EntityId id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(_refs
                .Where(edge => edge.From == id && IsVisible(edge.From) && IsVisible(edge.To))
                .ToImmutableArray());
        }

        public ValueTask<ImmutableArray<EntityRef>> GetIncomingRefsAsync(
            EntityId id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(_refs
                .Where(edge => edge.To == id && IsVisible(edge.From) && IsVisible(edge.To))
                .ToImmutableArray());
        }

        public async ValueTask<EntityLoadResult?> LoadAsync(
            EntityId id,
            EntityLoadOptions options = default,
            CancellationToken cancellationToken = default)
        {
            var entity = await GetAsync(id, cancellationToken);
            if (entity is null)
            {
                return null;
            }

            var outgoing = options.IncludeOutgoingRefs
                ? await GetOutgoingRefsAsync(id, cancellationToken)
                : ImmutableArray<EntityRef>.Empty;
            var incoming = options.IncludeIncomingRefs
                ? await GetIncomingRefsAsync(id, cancellationToken)
                : ImmutableArray<EntityRef>.Empty;

            return new EntityLoadResult(entity, outgoing, incoming);
        }

        public ValueTask<EntityRevision?> GetRevisionAsync(EntityId id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(_visibleEntities.TryGetValue(id, out var entity) ? entity.Revision : (EntityRevision?)null);
        }

        public ValueTask<IEntityReadSnapshot> OpenSnapshotAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult<IEntityReadSnapshot>(new FakeEntityReadSnapshot(_visibleEntities, _refs));
        }

        public async ValueTask ExecuteAsync(
            Func<IEntityWriteContext, CancellationToken, ValueTask> action,
            CancellationToken cancellationToken = default)
        {
            await ExecuteAsync<object?>(
                async (context, token) =>
                {
                    await action(context, token);
                    return null;
                },
                cancellationToken);
        }

        public async ValueTask<TResult> ExecuteAsync<TResult>(
            Func<IEntityWriteContext, CancellationToken, ValueTask<TResult>> action,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_inAtomic)
            {
                throw new InvalidOperationException("Nested atomic execution is not supported.");
            }

            _inAtomic = true;
            var txVisible = CloneVisible(_visibleEntities);
            var txHistory = CloneHistory(_history);
            var txRefs = new List<EntityRef>(_refs);
            var context = new FakeEntityWriteContext(txVisible, txHistory, txRefs);

            try
            {
                var result = await action(context, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                _visibleEntities = txVisible;
                _history = txHistory;
                _refs = txRefs;
                return result;
            }
            finally
            {
                _inAtomic = false;
            }
        }

        public ValueTask<Entity?> GetByRevisionAsync(
            EntityRevision revision,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_history.TryGetValue(revision.EntityId, out var revisions))
            {
                return ValueTask.FromResult<Entity?>(null);
            }

            return ValueTask.FromResult(revisions.LastOrDefault(entity => entity.Revision == revision));
        }

        public async IAsyncEnumerable<EntityRevision> ListRevisionsAsync(
            EntityId id,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_history.TryGetValue(id, out var revisions))
            {
                yield break;
            }

            foreach (var revision in revisions.Select(entity => entity.Revision))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return revision;
                await Task.Yield();
            }
        }

        private bool IsVisible(EntityId id)
        {
            return _visibleEntities.ContainsKey(id);
        }

        private static Dictionary<EntityId, Entity> CloneVisible(IReadOnlyDictionary<EntityId, Entity> source)
        {
            return source.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private static Dictionary<EntityId, List<Entity>> CloneHistory(IReadOnlyDictionary<EntityId, List<Entity>> source)
        {
            return source.ToDictionary(pair => pair.Key, pair => new List<Entity>(pair.Value));
        }

        private sealed class FakeEntityReadSnapshot : IEntityReadSnapshot
        {
            private readonly Dictionary<EntityId, Entity> _visibleEntities;
            private readonly ImmutableArray<EntityRef> _refs;

            public FakeEntityReadSnapshot(
                IReadOnlyDictionary<EntityId, Entity> visibleEntities,
                IReadOnlyCollection<EntityRef> refs)
            {
                _visibleEntities = visibleEntities.ToDictionary(pair => pair.Key, pair => pair.Value);
                _refs = refs.ToImmutableArray();
            }

            public ValueTask<Entity?> GetAsync(EntityId id, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(_visibleEntities.GetValueOrDefault(id));
            }

            public ValueTask<bool> ExistsAsync(EntityId id, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(_visibleEntities.ContainsKey(id));
            }

            public ValueTask<ImmutableArray<EntityRef>> GetOutgoingRefsAsync(
                EntityId id,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                return ValueTask.FromResult(_refs
                    .Where(edge => edge.From == id && _visibleEntities.ContainsKey(edge.From) && _visibleEntities.ContainsKey(edge.To))
                    .ToImmutableArray());
            }

            public ValueTask<ImmutableArray<EntityRef>> GetIncomingRefsAsync(
                EntityId id,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                return ValueTask.FromResult(_refs
                    .Where(edge => edge.To == id && _visibleEntities.ContainsKey(edge.From) && _visibleEntities.ContainsKey(edge.To))
                    .ToImmutableArray());
            }

            public async ValueTask<EntityLoadResult?> LoadAsync(
                EntityId id,
                EntityLoadOptions options = default,
                CancellationToken cancellationToken = default)
            {
                var entity = await GetAsync(id, cancellationToken);
                if (entity is null)
                {
                    return null;
                }

                var outgoing = options.IncludeOutgoingRefs
                    ? await GetOutgoingRefsAsync(id, cancellationToken)
                    : ImmutableArray<EntityRef>.Empty;
                var incoming = options.IncludeIncomingRefs
                    ? await GetIncomingRefsAsync(id, cancellationToken)
                    : ImmutableArray<EntityRef>.Empty;

                return new EntityLoadResult(entity, outgoing, incoming);
            }

            public ValueTask<EntityRevision?> GetRevisionAsync(EntityId id, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(_visibleEntities.TryGetValue(id, out var entity) ? entity.Revision : (EntityRevision?)null);
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }

        private sealed class FakeEntityWriteContext(
            Dictionary<EntityId, Entity> visibleEntities,
            Dictionary<EntityId, List<Entity>> history,
            List<EntityRef> refs) : IEntityWriteContext
        {
            public ValueTask<Entity?> GetAsync(EntityId id, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(visibleEntities.GetValueOrDefault(id));
            }

            public ValueTask<bool> ExistsAsync(EntityId id, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(visibleEntities.ContainsKey(id));
            }

            public ValueTask<ImmutableArray<EntityRef>> GetOutgoingRefsAsync(
                EntityId id,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                return ValueTask.FromResult(refs
                    .Where(edge => edge.From == id && visibleEntities.ContainsKey(edge.From) && visibleEntities.ContainsKey(edge.To))
                    .ToImmutableArray());
            }

            public ValueTask<ImmutableArray<EntityRef>> GetIncomingRefsAsync(
                EntityId id,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                return ValueTask.FromResult(refs
                    .Where(edge => edge.To == id && visibleEntities.ContainsKey(edge.From) && visibleEntities.ContainsKey(edge.To))
                    .ToImmutableArray());
            }

            public async ValueTask<EntityLoadResult?> LoadAsync(
                EntityId id,
                EntityLoadOptions options = default,
                CancellationToken cancellationToken = default)
            {
                var entity = await GetAsync(id, cancellationToken);
                if (entity is null)
                {
                    return null;
                }

                var outgoing = options.IncludeOutgoingRefs
                    ? await GetOutgoingRefsAsync(id, cancellationToken)
                    : ImmutableArray<EntityRef>.Empty;
                var incoming = options.IncludeIncomingRefs
                    ? await GetIncomingRefsAsync(id, cancellationToken)
                    : ImmutableArray<EntityRef>.Empty;

                return new EntityLoadResult(entity, outgoing, incoming);
            }

            public ValueTask<EntityRevision?> GetRevisionAsync(EntityId id, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ValueTask.FromResult(visibleEntities.TryGetValue(id, out var entity) ? entity.Revision : (EntityRevision?)null);
            }

            public ValueTask PutAsync(Entity entity, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entity.DeletedAt is not null)
                {
                    throw new ArgumentException("Visible writes must not carry a deleted timestamp.", nameof(entity));
                }

                visibleEntities[entity.Id] = entity;

                if (!history.TryGetValue(entity.Id, out var revisions))
                {
                    revisions = [];
                    history[entity.Id] = revisions;
                }

                revisions.Add(entity);
                return ValueTask.CompletedTask;
            }

            public ValueTask DeleteAsync(EntityId id, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!visibleEntities.Remove(id, out var existing))
                {
                    return ValueTask.CompletedTask;
                }

                if (!history.TryGetValue(id, out var revisions))
                {
                    revisions = [];
                    history[id] = revisions;
                }

                var deletedAt = DateTime.UtcNow;
                var tombstone = new Entity(
                    existing.Id,
                    EntityRevision.GenerateNew(id),
                    existing.Payload,
                    existing.CreatedAt,
                    deletedAt,
                    deletedAt);
                revisions.Add(tombstone);
                refs.RemoveAll(edge => edge.From == id || edge.To == id);
                return ValueTask.CompletedTask;
            }

            public ValueTask AddRefAsync(EntityRef entityRef, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!visibleEntities.ContainsKey(entityRef.From) || !visibleEntities.ContainsKey(entityRef.To))
                {
                    throw new InvalidOperationException("Logical references require visible source and target entities.");
                }

                if (!refs.Contains(entityRef))
                {
                    refs.Add(entityRef);
                }

                return ValueTask.CompletedTask;
            }

            public ValueTask RemoveRefAsync(
                EntityId from,
                EntityId to,
                RefKind refKind,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                refs.RemoveAll(edge => edge.From == from && edge.To == to && edge.RefKind == refKind);
                return ValueTask.CompletedTask;
            }

            public async ValueTask<EntityOpResult> TryPutAsync(
                Entity entity,
                ReadOnlyMemory<EntityOpPrecondition> preconditions = default,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outcome = EvaluatePreconditions(entity.Id, preconditions.Span);
                if (!outcome.Succeeded)
                {
                    return outcome;
                }

                await PutAsync(entity, cancellationToken);
                return EntityOpResult.Success;
            }

            public async ValueTask<EntityOpResult> TryDeleteAsync(
                EntityId id,
                ReadOnlyMemory<EntityOpPrecondition> preconditions = default,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var outcome = EvaluatePreconditions(id, preconditions.Span);
                if (!outcome.Succeeded)
                {
                    return outcome;
                }

                await DeleteAsync(id, cancellationToken);
                return EntityOpResult.Success;
            }

            private EntityOpResult EvaluatePreconditions(EntityId targetId, ReadOnlySpan<EntityOpPrecondition> preconditions)
            {
                visibleEntities.TryGetValue(targetId, out var current);

                foreach (var precondition in preconditions)
                {
                    switch (precondition)
                    {
                        case MustExist:
                            if (current is null)
                            {
                                return EntityOpResult.Failed(new EntityNotFound());
                            }

                            break;

                        case MustNotExist:
                            if (current is not null)
                            {
                                return EntityOpResult.Failed(new EntityAlreadyExists());
                            }

                            break;

                        case MustHaveRevision mustHaveRevision:
                            if (mustHaveRevision.Revision.EntityId != targetId)
                            {
                                throw new ArgumentException(
                                    "The revision precondition targets a different entity.",
                                    nameof(preconditions));
                            }

                            if (current?.Revision != mustHaveRevision.Revision)
                            {
                                return EntityOpResult.Failed(new RevisionMismatch(current?.Revision));
                            }

                            break;
                    }
                }

                return EntityOpResult.Success;
            }
        }
    }
}
