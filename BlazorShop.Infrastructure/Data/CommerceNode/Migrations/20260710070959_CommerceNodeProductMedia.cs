using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeProductMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_source_url = table.Column<string>(type: "text", nullable: true),
                    original_storage_path = table.Column<string>(type: "text", nullable: true),
                    content_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    file_name = table.Column<string>(type: "text", nullable: true),
                    mime_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    alt_text = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_media", x => x.id);
                    table.CheckConstraint("ck_product_media_file_size", "file_size_bytes IS NULL OR file_size_bytes > 0");
                    table.CheckConstraint("ck_product_media_height", "height IS NULL OR height > 0");
                    table.CheckConstraint("ck_product_media_sort_order", "sort_order >= 0");
                    table.CheckConstraint("ck_product_media_status", "status in ('pending', 'downloading', 'stored', 'failed', 'deleted')");
                    table.CheckConstraint("ck_product_media_version", "version >= 1");
                    table.CheckConstraint("ck_product_media_width", "width IS NULL OR width > 0");
                    table.ForeignKey(
                        name: "FK_product_media_Products_product_id",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_media_deleted_at",
                table: "product_media",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "IX_product_media_product_id",
                table: "product_media",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_media_public_id",
                table: "product_media",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_media_status",
                table: "product_media",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_product_media_store_id_content_hash",
                table: "product_media",
                columns: new[] { "store_id", "content_hash" },
                filter: "content_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_product_media_store_id_product_id_is_primary",
                table: "product_media",
                columns: new[] { "store_id", "product_id", "is_primary" },
                unique: true,
                filter: "deleted_at IS NULL AND is_primary = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_product_media_store_id_product_id_sort_order",
                table: "product_media",
                columns: new[] { "store_id", "product_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_product_media_store_id_product_id_status",
                table: "product_media",
                columns: new[] { "store_id", "product_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_media");
        }
    }
}
