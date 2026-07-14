using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeCheckoutSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "checkout_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cart_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cart_version = table.Column<int>(type: "integer", nullable: false),
                    customer_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    customer_phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    shipping_full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    shipping_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    shipping_phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    shipping_address1 = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    shipping_address2 = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    shipping_city = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    shipping_state = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    shipping_postal_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    shipping_country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    payment_method_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    shipping_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    grand_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    validation_issues_json = table.Column<string>(type: "jsonb", nullable: true),
                    next_action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkout_sessions", x => x.id);
                    table.CheckConstraint("ck_checkout_sessions_state", "state in ('draft', 'ready', 'order_pending', 'completed', 'expired', 'cancelled')");
                    table.ForeignKey(
                        name: "FK_checkout_sessions_cart_sessions_cart_session_id",
                        column: x => x.cart_session_id,
                        principalTable: "cart_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_checkout_sessions_commerce_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "commerce_customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_checkout_sessions_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_cart_session_id",
                table: "checkout_sessions",
                column: "cart_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_customer_id",
                table: "checkout_sessions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_expires_at_utc",
                table: "checkout_sessions",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_public_id",
                table: "checkout_sessions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_store_id_cart_session_id_state",
                table: "checkout_sessions",
                columns: new[] { "store_id", "cart_session_id", "state" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkout_sessions");
        }
    }
}
