using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeCustomerAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commerce_customer_addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    last_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    company = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    address1 = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    address2 = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    state_province_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    state_province_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_default_shipping = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_default_billing = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_customer_addresses", x => x.id);
                    table.ForeignKey(
                        name: "FK_commerce_customer_addresses_commerce_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "commerce_customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_commerce_customer_addresses_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commerce_customer_addresses_customer_id",
                table: "commerce_customer_addresses",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_customer_addresses_public_id",
                table: "commerce_customer_addresses",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commerce_customer_addresses_store_id_customer_id_country_co~",
                table: "commerce_customer_addresses",
                columns: new[] { "store_id", "customer_id", "country_code" });

            migrationBuilder.CreateIndex(
                name: "IX_commerce_customer_addresses_store_id_customer_id_deleted_at~",
                table: "commerce_customer_addresses",
                columns: new[] { "store_id", "customer_id", "deleted_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_commerce_customer_addresses_store_id_customer_id_is_defaul~1",
                table: "commerce_customer_addresses",
                columns: new[] { "store_id", "customer_id", "is_default_shipping" },
                unique: true,
                filter: "is_default_shipping = true AND deleted_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_customer_addresses_store_id_customer_id_is_default~",
                table: "commerce_customer_addresses",
                columns: new[] { "store_id", "customer_id", "is_default_billing" },
                unique: true,
                filter: "is_default_billing = true AND deleted_at_utc IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commerce_customer_addresses");
        }
    }
}
