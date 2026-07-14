using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeServerCartSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cart_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    app_user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    last_activity_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    converted_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    merged_into_cart_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_cart_sessions_AspNetUsers_app_user_id",
                        column: x => x.app_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_cart_sessions_Orders_converted_order_id",
                        column: x => x.converted_order_id,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_cart_sessions_cart_sessions_merged_into_cart_id",
                        column: x => x.merged_into_cart_id,
                        principalTable: "cart_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_cart_sessions_commerce_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "commerce_customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_cart_sessions_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cart_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cart_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    selected_attributes_json = table.Column<string>(type: "jsonb", nullable: true),
                    personalization_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    personalization_json = table.Column<string>(type: "jsonb", nullable: true),
                    artwork_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    artwork_version = table.Column<int>(type: "integer", nullable: true),
                    fulfillment_provider_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price_snapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    currency_code_snapshot = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_cart_lines_ProductVariants_product_variant_id",
                        column: x => x.product_variant_id,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_cart_lines_Products_product_id",
                        column: x => x.product_id,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cart_lines_cart_sessions_cart_session_id",
                        column: x => x.cart_session_id,
                        principalTable: "cart_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cart_lines_artwork_asset_id",
                table: "cart_lines",
                column: "artwork_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_lines_cart_session_id_line_key",
                table: "cart_lines",
                columns: new[] { "cart_session_id", "line_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cart_lines_product_id",
                table: "cart_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_lines_product_variant_id",
                table: "cart_lines",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_sessions_app_user_id",
                table: "cart_sessions",
                column: "app_user_id",
                filter: "app_user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_cart_sessions_converted_order_id",
                table: "cart_sessions",
                column: "converted_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_sessions_customer_id",
                table: "cart_sessions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_sessions_expires_at_utc",
                table: "cart_sessions",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_cart_sessions_merged_into_cart_id",
                table: "cart_sessions",
                column: "merged_into_cart_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_sessions_public_id",
                table: "cart_sessions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cart_sessions_store_id_state",
                table: "cart_sessions",
                columns: new[] { "store_id", "state" });

            migrationBuilder.CreateIndex(
                name: "IX_cart_sessions_token_hash",
                table: "cart_sessions",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cart_lines");

            migrationBuilder.DropTable(
                name: "cart_sessions");
        }
    }
}
