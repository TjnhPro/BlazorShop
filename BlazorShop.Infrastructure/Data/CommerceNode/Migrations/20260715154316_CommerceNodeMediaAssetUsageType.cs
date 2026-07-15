using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeMediaAssetUsageType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "usage_type",
                table: "commerce_media_asset",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "content");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_media_asset_store_id_usage_type_updated_at",
                table: "commerce_media_asset",
                columns: new[] { "store_id", "usage_type", "updated_at" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_commerce_media_asset_usage_type",
                table: "commerce_media_asset",
                sql: "usage_type in ('content', 'branding', 'theme', 'category')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_commerce_media_asset_store_id_usage_type_updated_at",
                table: "commerce_media_asset");

            migrationBuilder.DropCheckConstraint(
                name: "ck_commerce_media_asset_usage_type",
                table: "commerce_media_asset");

            migrationBuilder.DropColumn(
                name: "usage_type",
                table: "commerce_media_asset");
        }
    }
}
