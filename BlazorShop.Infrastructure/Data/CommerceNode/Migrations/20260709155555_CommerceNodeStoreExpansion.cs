using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStoreExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "apple_touch_icon_url",
                table: "commerce_store",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cdn_host",
                table: "commerce_store",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "display_order",
                table: "commerce_store",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "favicon_url",
                table: "commerce_store",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "force_https",
                table: "commerce_store",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "html_body_id",
                table: "commerce_store",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "logo_url",
                table: "commerce_store",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "maintenance_message",
                table: "commerce_store",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "maintenance_mode_enabled",
                table: "commerce_store",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "metadata_json",
                table: "commerce_store",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ms_tile_color",
                table: "commerce_store",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ms_tile_image_url",
                table: "commerce_store",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "png_icon_url",
                table: "commerce_store",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ssl_enabled",
                table: "commerce_store",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "ssl_port",
                table: "commerce_store",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "support_email",
                table: "commerce_store",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "support_phone",
                table: "commerce_store",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_commerce_store_display_order",
                table: "commerce_store",
                column: "display_order");

            migrationBuilder.Sql(
                """
                INSERT INTO commerce_store (
                    id,
                    public_id,
                    store_key,
                    name,
                    status,
                    base_url,
                    force_https,
                    ssl_enabled,
                    display_order,
                    default_currency_code,
                    default_culture,
                    support_email,
                    support_phone,
                    maintenance_mode_enabled,
                    maintenance_message,
                    created_at,
                    updated_at)
                SELECT
                    '11111111-1111-4111-8111-111111111111',
                    '22222222-2222-4222-8222-222222222222',
                    'default',
                    COALESCE(NULLIF(settings."StoreName", ''), 'BlazorShop'),
                    'active',
                    NULL,
                    TRUE,
                    TRUE,
                    0,
                    COALESCE(NULLIF(settings."DefaultCurrency", ''), 'USD'),
                    COALESCE(NULLIF(settings."DefaultCulture", ''), 'en-US'),
                    NULLIF(settings."StoreSupportEmail", ''),
                    NULLIF(settings."StoreSupportPhone", ''),
                    settings."MaintenanceModeEnabled",
                    NULLIF(settings."MaintenanceMessage", ''),
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM (
                    SELECT *
                    FROM "AdminSettings"
                    ORDER BY "UpdatedOn" DESC
                    LIMIT 1
                ) settings
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM commerce_store
                    WHERE archived_at IS NULL
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_commerce_store_display_order",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "apple_touch_icon_url",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "cdn_host",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "display_order",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "favicon_url",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "force_https",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "html_body_id",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "logo_url",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "maintenance_message",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "maintenance_mode_enabled",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "metadata_json",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "ms_tile_color",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "ms_tile_image_url",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "png_icon_url",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "ssl_enabled",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "ssl_port",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "support_email",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "support_phone",
                table: "commerce_store");
        }
    }
}
