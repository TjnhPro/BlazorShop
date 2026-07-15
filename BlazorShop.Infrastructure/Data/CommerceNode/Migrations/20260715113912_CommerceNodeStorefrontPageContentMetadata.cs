using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStorefrontPageContentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "display_order",
                table: "storefront_page",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "include_in_navigation",
                table: "storefront_page",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "navigation_location",
                table: "storefront_page",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "page_key",
                table: "storefront_page",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_storefront_page_store_id_include_in_navigation_is_published~",
                table: "storefront_page",
                columns: new[] { "store_id", "include_in_navigation", "is_published", "archived_at", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_storefront_page_store_id_page_key",
                table: "storefront_page",
                columns: new[] { "store_id", "page_key" },
                unique: true,
                filter: "page_key IS NOT NULL AND archived_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_storefront_page_store_id_page_key_archived_at",
                table: "storefront_page",
                columns: new[] { "store_id", "page_key", "archived_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_storefront_page_store_id_include_in_navigation_is_published~",
                table: "storefront_page");

            migrationBuilder.DropIndex(
                name: "IX_storefront_page_store_id_page_key",
                table: "storefront_page");

            migrationBuilder.DropIndex(
                name: "IX_storefront_page_store_id_page_key_archived_at",
                table: "storefront_page");

            migrationBuilder.DropColumn(
                name: "display_order",
                table: "storefront_page");

            migrationBuilder.DropColumn(
                name: "include_in_navigation",
                table: "storefront_page");

            migrationBuilder.DropColumn(
                name: "navigation_location",
                table: "storefront_page");

            migrationBuilder.DropColumn(
                name: "page_key",
                table: "storefront_page");
        }
    }
}
