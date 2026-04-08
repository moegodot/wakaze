#nullable disable

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kawayi.Wakaze.Entity.Sqlite.Migrations;

/// <inheritdoc />
[DbContext(typeof(EntityStoreDbContext))]
[Migration("20260408000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EntityContentRevisions",
            columns: table => new
            {
                RevisionId = table.Column<long>(type: "INTEGER", nullable: false),
                EntityId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EntityContentRevisions", x => x.RevisionId);
            });

        migrationBuilder.CreateTable(
            name: "StoreMetadata",
            columns: table => new
            {
                StoreMetadataId = table.Column<int>(type: "INTEGER", nullable: false),
                EntityStoreId = table.Column<Guid>(type: "TEXT", nullable: false),
                EpochId = table.Column<long>(type: "INTEGER", nullable: false),
                NextRevisionId = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StoreMetadata", x => x.StoreMetadataId);
            });

        migrationBuilder.CreateTable(
            name: "EntityHeads",
            columns: table => new
            {
                EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                CurrentRevisionId = table.Column<long>(type: "INTEGER", nullable: false),
                LastContentRevisionId = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EntityHeads", x => x.EntityId);
                table.ForeignKey(
                    name: "FK_EntityHeads_EntityContentRevisions_LastContentRevisionId",
                    column: x => x.LastContentRevisionId,
                    principalTable: "EntityContentRevisions",
                    principalColumn: "RevisionId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "EntityRevisionBlobRefs",
            columns: table => new
            {
                RevisionId = table.Column<long>(type: "INTEGER", nullable: false),
                Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                BlobId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EntityRevisionBlobRefs", x => new { x.RevisionId, x.Ordinal });
                table.ForeignKey(
                    name: "FK_EntityRevisionBlobRefs_EntityContentRevisions_RevisionId",
                    column: x => x.RevisionId,
                    principalTable: "EntityContentRevisions",
                    principalColumn: "RevisionId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "EntityRevisionRefs",
            columns: table => new
            {
                RevisionId = table.Column<long>(type: "INTEGER", nullable: false),
                Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                TargetEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                Kind = table.Column<byte>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EntityRevisionRefs", x => new { x.RevisionId, x.Ordinal });
                table.ForeignKey(
                    name: "FK_EntityRevisionRefs_EntityContentRevisions_RevisionId",
                    column: x => x.RevisionId,
                    principalTable: "EntityContentRevisions",
                    principalColumn: "RevisionId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EntityContentRevisions_EntityId_RevisionId",
            table: "EntityContentRevisions",
            columns: ["EntityId", "RevisionId"]);

        migrationBuilder.CreateIndex(
            name: "IX_EntityHeads_LastContentRevisionId",
            table: "EntityHeads",
            column: "LastContentRevisionId");

        migrationBuilder.CreateIndex(
            name: "IX_EntityRevisionRefs_TargetEntityId",
            table: "EntityRevisionRefs",
            column: "TargetEntityId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EntityHeads");
        migrationBuilder.DropTable(name: "EntityRevisionBlobRefs");
        migrationBuilder.DropTable(name: "EntityRevisionRefs");
        migrationBuilder.DropTable(name: "StoreMetadata");
        migrationBuilder.DropTable(name: "EntityContentRevisions");
    }
}
