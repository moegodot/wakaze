using Kawayi.Wakaze.Cas.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kawayi.Wakaze.Entity.Sqlite;

internal sealed class EntityStoreDbContext(DbContextOptions<EntityStoreDbContext> options) : DbContext(options)
{
    public DbSet<StoreMetadataRow> StoreMetadata => Set<StoreMetadataRow>();

    public DbSet<EntityHeadRow> EntityHeads => Set<EntityHeadRow>();

    public DbSet<EntityContentRevisionRow> EntityContentRevisions => Set<EntityContentRevisionRow>();

    public DbSet<EntityRevisionRefRow> EntityRevisionRefs => Set<EntityRevisionRefRow>();

    public DbSet<EntityRevisionBlobRefRow> EntityRevisionBlobRefs => Set<EntityRevisionBlobRefRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var blobIdConverter = new ValueConverter<BlobId, string>(
            static value => value.ToString("R", null),
            static value => new BlobId(Kawayi.Wakaze.Digest.Blake3.Parse(value, null)));

        modelBuilder.Entity<StoreMetadataRow>(entity =>
        {
            entity.ToTable("StoreMetadata");
            entity.HasKey(x => x.StoreMetadataId);
            entity.Property(x => x.StoreMetadataId).ValueGeneratedNever();
        });

        modelBuilder.Entity<EntityHeadRow>(entity =>
        {
            entity.ToTable("EntityHeads");
            entity.HasKey(x => x.EntityId);

            entity.HasOne(x => x.LastContentRevision)
                .WithMany()
                .HasForeignKey(x => x.LastContentRevisionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EntityContentRevisionRow>(entity =>
        {
            entity.ToTable("EntityContentRevisions");
            entity.HasKey(x => x.RevisionId);
            entity.Property(x => x.RevisionId).ValueGeneratedNever();
            entity.HasIndex(x => new { x.EntityId, x.RevisionId });
        });

        modelBuilder.Entity<EntityRevisionRefRow>(entity =>
        {
            entity.ToTable("EntityRevisionRefs");
            entity.HasKey(x => new { x.RevisionId, x.Ordinal });
            entity.Property(x => x.Kind).HasConversion<byte>();
            entity.HasIndex(x => x.TargetEntityId);

            entity.HasOne(x => x.Revision)
                .WithMany(x => x.Refs)
                .HasForeignKey(x => x.RevisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EntityRevisionBlobRefRow>(entity =>
        {
            entity.ToTable("EntityRevisionBlobRefs");
            entity.HasKey(x => new { x.RevisionId, x.Ordinal });
            entity.Property(x => x.BlobId)
                .HasConversion(blobIdConverter)
                .HasMaxLength(64);

            entity.HasOne(x => x.Revision)
                .WithMany(x => x.BlobRefs)
                .HasForeignKey(x => x.RevisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

internal sealed class StoreMetadataRow
{
    public int StoreMetadataId { get; set; }

    public Guid EntityStoreId { get; set; }

    public long EpochId { get; set; }

    public long NextRevisionId { get; set; }
}

internal sealed class EntityHeadRow
{
    public Guid EntityId { get; set; }

    public bool IsDeleted { get; set; }

    public long CurrentRevisionId { get; set; }

    public long? LastContentRevisionId { get; set; }

    public EntityContentRevisionRow? LastContentRevision { get; set; }
}

internal sealed class EntityContentRevisionRow
{
    public long RevisionId { get; set; }

    public Guid EntityId { get; set; }

    public List<EntityRevisionRefRow> Refs { get; } = [];

    public List<EntityRevisionBlobRefRow> BlobRefs { get; } = [];
}

internal sealed class EntityRevisionRefRow
{
    public long RevisionId { get; set; }

    public int Ordinal { get; set; }

    public Guid TargetEntityId { get; set; }

    public Abstractions.RefKind Kind { get; set; }

    public EntityContentRevisionRow Revision { get; set; } = null!;
}

internal sealed class EntityRevisionBlobRefRow
{
    public long RevisionId { get; set; }

    public int Ordinal { get; set; }

    public BlobId BlobId { get; set; }

    public EntityContentRevisionRow Revision { get; set; } = null!;
}
