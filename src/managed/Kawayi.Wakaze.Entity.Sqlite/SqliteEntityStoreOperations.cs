using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Kawayi.Wakaze.Entity.Abstractions;
using Microsoft.EntityFrameworkCore;
using EntityModel = Kawayi.Wakaze.Entity.Abstractions.Entity;

namespace Kawayi.Wakaze.Entity.Sqlite;

internal static class SqliteEntityStoreOperations
{
    public static async ValueTask<EntityModel?> GetAsync(
        EntityStoreDbContext context,
        SqliteStoreIdentity identity,
        EntityId id,
        EntityReadOptions options,
        CancellationToken cancellationToken)
    {
        var head = await context.EntityHeads
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.EntityId == id.Id, cancellationToken);

        if (head is null) return null;

        if (head.IsDeleted && !options.IncludeDeleted) return null;

        if (head.LastContentRevisionId is null) return null;

        return await GetContentByRevisionIdAsync(context, id, head.LastContentRevisionId.Value, cancellationToken);
    }

    public static async ValueTask<bool> ExistsAsync(
        EntityStoreDbContext context,
        EntityId id,
        CancellationToken cancellationToken)
    {
        return await context.EntityHeads
            .AsNoTracking()
            .AnyAsync(x => x.EntityId == id.Id && !x.IsDeleted && x.LastContentRevisionId != null, cancellationToken);
    }

    public static async ValueTask<EntityRevision?> GetRevisionAsync(
        EntityStoreDbContext context,
        SqliteStoreIdentity identity,
        EntityId id,
        CancellationToken cancellationToken)
    {
        var head = await context.EntityHeads
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.EntityId == id.Id, cancellationToken);

        if (head is null || head.IsDeleted || head.LastContentRevisionId is null) return null;

        return identity.CreateEntityRevision(id, head.CurrentRevisionId);
    }

    public static async ValueTask<EntityModel?> GetByRevisionAsync(
        EntityStoreDbContext context,
        SqliteStoreIdentity identity,
        EntityId id,
        EntityRevision revision,
        CancellationToken cancellationToken)
    {
        if (!identity.Matches(id, revision, checked((long)revision.Revision.RevisionId))) return null;

        return await GetContentByRevisionIdAsync(
            context,
            id,
            checked((long)revision.Revision.RevisionId),
            cancellationToken);
    }

    public static async IAsyncEnumerable<EntityRevision> ListRevisionsAsync(
        EntityStoreDbContext context,
        SqliteStoreIdentity identity,
        EntityId id,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var revisionIds = context.EntityContentRevisions
            .AsNoTracking()
            .Where(x => x.EntityId == id.Id)
            .OrderBy(x => x.RevisionId)
            .Select(x => x.RevisionId)
            .AsAsyncEnumerable();

        await foreach (var revisionId in revisionIds.WithCancellation(cancellationToken))
            yield return identity.CreateEntityRevision(id, revisionId);
    }

    public static async IAsyncEnumerable<EntityModel> GetReferrersAsync(
        EntityStoreDbContext context,
        EntityId target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var currentReferrers = await (
                from head in context.EntityHeads.AsNoTracking()
                join revisionRef in context.EntityRevisionRefs.AsNoTracking()
                    on head.LastContentRevisionId equals revisionRef.RevisionId
                where !head.IsDeleted && revisionRef.TargetEntityId == target.Id
                select new { head.EntityId, RevisionId = head.LastContentRevisionId!.Value })
            .Distinct()
            .OrderBy(x => x.EntityId)
            .ToListAsync(cancellationToken);

        foreach (var referrer in currentReferrers)
        {
            var entity = await GetContentByRevisionIdAsync(
                context,
                new EntityId(referrer.EntityId),
                referrer.RevisionId,
                cancellationToken);

            if (entity is not null) yield return entity;
        }
    }

    public static async ValueTask PutAsync(
        EntityStoreDbContext context,
        StoreMetadataRow metadata,
        EntityModel entity,
        CancellationToken cancellationToken)
    {
        await PutCoreAsync(context, metadata, entity, null, cancellationToken);
    }

    public static async ValueTask<bool> TryPutAsync(
        EntityStoreDbContext context,
        StoreMetadataRow metadata,
        EntityModel entity,
        EntityRevision expectedRevision,
        CancellationToken cancellationToken)
    {
        return await PutCoreAsync(context, metadata, entity, expectedRevision, cancellationToken);
    }

    public static async ValueTask DeleteAsync(
        EntityStoreDbContext context,
        StoreMetadataRow metadata,
        EntityId id,
        CancellationToken cancellationToken)
    {
        await DeleteCoreAsync(context, metadata, id, null, cancellationToken);
    }

    public static async ValueTask<bool> TryDeleteAsync(
        EntityStoreDbContext context,
        StoreMetadataRow metadata,
        EntityId id,
        EntityRevision expectedRevision,
        CancellationToken cancellationToken)
    {
        return await DeleteCoreAsync(context, metadata, id, expectedRevision, cancellationToken);
    }

    private static async ValueTask<EntityModel?> GetContentByRevisionIdAsync(
        EntityStoreDbContext context,
        EntityId id,
        long revisionId,
        CancellationToken cancellationToken)
    {
        var contentExists = await context.EntityContentRevisions
            .AsNoTracking()
            .AnyAsync(x => x.RevisionId == revisionId && x.EntityId == id.Id, cancellationToken);

        if (!contentExists) return null;

        var refs = await context.EntityRevisionRefs
            .AsNoTracking()
            .Where(x => x.RevisionId == revisionId)
            .OrderBy(x => x.Ordinal)
            .Select(x => new EntityRef(new EntityId(x.TargetEntityId), x.Kind))
            .ToListAsync(cancellationToken);

        var blobRefs = await context.EntityRevisionBlobRefs
            .AsNoTracking()
            .Where(x => x.RevisionId == revisionId)
            .OrderBy(x => x.Ordinal)
            .Select(x => x.BlobId)
            .ToListAsync(cancellationToken);

        return new EntityModel(id, refs.ToImmutableArray(), blobRefs.ToImmutableArray());
    }

    private static async ValueTask<bool> PutCoreAsync(
        EntityStoreDbContext context,
        StoreMetadataRow metadata,
        EntityModel entity,
        EntityRevision? expectedRevision,
        CancellationToken cancellationToken)
    {
        var head = await context.EntityHeads.SingleOrDefaultAsync(x => x.EntityId == entity.Id.Id, cancellationToken);
        var identity = new SqliteStoreIdentity(metadata.EntityStoreId, checked((ulong)metadata.EpochId));

        if (expectedRevision is not null)
            if (head is null || !identity.Matches(entity.Id, expectedRevision.Value, head.CurrentRevisionId))
                return false;

        var revisionId = AllocateRevisionId(metadata);
        var revision = new EntityContentRevisionRow
        {
            RevisionId = revisionId,
            EntityId = entity.Id.Id
        };

        for (var index = 0; index < entity.Refs.Length; index++)
            revision.Refs.Add(new EntityRevisionRefRow
            {
                RevisionId = revisionId,
                Ordinal = index,
                TargetEntityId = entity.Refs[index].Target.Id,
                Kind = entity.Refs[index].Kind
            });

        for (var index = 0; index < entity.BlobRefs.Length; index++)
            revision.BlobRefs.Add(new EntityRevisionBlobRefRow
            {
                RevisionId = revisionId,
                Ordinal = index,
                BlobId = entity.BlobRefs[index]
            });

        context.EntityContentRevisions.Add(revision);

        if (head is null)
        {
            context.EntityHeads.Add(new EntityHeadRow
            {
                EntityId = entity.Id.Id,
                IsDeleted = false,
                CurrentRevisionId = revisionId,
                LastContentRevisionId = revisionId
            });
        }
        else
        {
            head.IsDeleted = false;
            head.CurrentRevisionId = revisionId;
            head.LastContentRevisionId = revisionId;
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async ValueTask<bool> DeleteCoreAsync(
        EntityStoreDbContext context,
        StoreMetadataRow metadata,
        EntityId id,
        EntityRevision? expectedRevision,
        CancellationToken cancellationToken)
    {
        var head = await context.EntityHeads.SingleOrDefaultAsync(x => x.EntityId == id.Id, cancellationToken);
        var identity = new SqliteStoreIdentity(metadata.EntityStoreId, checked((ulong)metadata.EpochId));

        if (expectedRevision is not null)
            if (head is null || !identity.Matches(id, expectedRevision.Value, head.CurrentRevisionId))
                return false;

        var revisionId = AllocateRevisionId(metadata);

        if (head is null)
        {
            context.EntityHeads.Add(new EntityHeadRow
            {
                EntityId = id.Id,
                IsDeleted = true,
                CurrentRevisionId = revisionId,
                LastContentRevisionId = null
            });
        }
        else
        {
            head.IsDeleted = true;
            head.CurrentRevisionId = revisionId;
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static long AllocateRevisionId(StoreMetadataRow metadata)
    {
        var revisionId = metadata.NextRevisionId;
        metadata.NextRevisionId = checked(revisionId + 1);
        return revisionId;
    }
}
