using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStorefrontPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "storefront_page",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    intro = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    body_html = table.Column<string>(type: "text", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    include_in_sitemap = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    meta_title = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    meta_description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    canonical_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    og_title = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    og_description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    og_image = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    robots_index = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    robots_follow = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storefront_page", x => x.id);
                    table.ForeignKey(
                        name: "FK_storefront_page_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_storefront_page_public_id",
                table: "storefront_page",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_storefront_page_store_id_include_in_sitemap_is_published_ar~",
                table: "storefront_page",
                columns: new[] { "store_id", "include_in_sitemap", "is_published", "archived_at" });

            migrationBuilder.CreateIndex(
                name: "IX_storefront_page_store_id_is_published_archived_at",
                table: "storefront_page",
                columns: new[] { "store_id", "is_published", "archived_at" });

            migrationBuilder.CreateIndex(
                name: "IX_storefront_page_store_id_slug",
                table: "storefront_page",
                columns: new[] { "store_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_storefront_page_store_id_updated_at",
                table: "storefront_page",
                columns: new[] { "store_id", "updated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "storefront_page");
        }
    }
}
