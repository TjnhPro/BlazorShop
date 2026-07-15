using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeCategoryMediaAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category_media_assignment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alt_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_media_assignment", x => x.id);
                    table.CheckConstraint("ck_category_media_assignment_sort_order", "sort_order >= 0");
                    table.ForeignKey(
                        name: "FK_category_media_assignment_Categories_category_id",
                        column: x => x.category_id,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_category_media_assignment_commerce_media_asset_media_asset_~",
                        column: x => x.media_asset_id,
                        principalTable: "commerce_media_asset",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_category_media_assignment_category_id",
                table: "category_media_assignment",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_media_assignment_media_asset_id",
                table: "category_media_assignment",
                column: "media_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_media_assignment_store_id_category_id_is_primary",
                table: "category_media_assignment",
                columns: new[] { "store_id", "category_id", "is_primary" },
                unique: true,
                filter: "is_primary = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_category_media_assignment_store_id_category_id_sort_order",
                table: "category_media_assignment",
                columns: new[] { "store_id", "category_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_category_media_assignment_store_id_media_asset_id",
                table: "category_media_assignment",
                columns: new[] { "store_id", "media_asset_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_media_assignment");
        }
    }
}
