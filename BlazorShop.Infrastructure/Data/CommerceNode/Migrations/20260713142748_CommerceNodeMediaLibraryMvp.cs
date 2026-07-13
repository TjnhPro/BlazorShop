using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeMediaLibraryMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commerce_media_asset",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    canonical_file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    display_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    alt_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    title_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    original_storage_path = table.Column<string>(type: "text", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    extension = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_media_asset", x => x.id);
                    table.CheckConstraint("ck_commerce_media_asset_file_size", "file_size_bytes > 0");
                    table.CheckConstraint("ck_commerce_media_asset_height", "height IS NULL OR height > 0");
                    table.CheckConstraint("ck_commerce_media_asset_width", "width IS NULL OR width > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_commerce_media_asset_public_id",
                table: "commerce_media_asset",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commerce_media_asset_store_id_canonical_file_name",
                table: "commerce_media_asset",
                columns: new[] { "store_id", "canonical_file_name" });

            migrationBuilder.CreateIndex(
                name: "IX_commerce_media_asset_store_id_content_hash",
                table: "commerce_media_asset",
                columns: new[] { "store_id", "content_hash" });

            migrationBuilder.CreateIndex(
                name: "IX_commerce_media_asset_store_id_updated_at",
                table: "commerce_media_asset",
                columns: new[] { "store_id", "updated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commerce_media_asset");
        }
    }
}
