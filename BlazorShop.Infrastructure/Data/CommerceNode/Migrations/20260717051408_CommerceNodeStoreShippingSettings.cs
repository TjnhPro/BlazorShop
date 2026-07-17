using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStoreShippingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "store_shipping_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    origin_full_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    origin_company = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    origin_address1 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    origin_address2 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    origin_city = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    origin_state_province_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    origin_postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    origin_country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    enabled_country_codes_json = table.Column<string>(type: "jsonb", nullable: true),
                    default_flat_rate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    free_shipping_threshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    surcharge_policy = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "sum"),
                    default_delivery_estimate_text = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by_user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_shipping_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_store_shipping_settings_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_store_shipping_settings_public_id",
                table: "store_shipping_settings",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_store_shipping_settings_store_id",
                table: "store_shipping_settings",
                column: "store_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_shipping_settings");
        }
    }
}
