using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStoreCurrencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "store_currencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_default_display_currency = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    culture_name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    symbol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    decimal_digits = table.Column<int>(type: "integer", nullable: false),
                    unit_price_rounding_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    unit_price_rounding_increment = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    line_total_rounding_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    line_total_rounding_increment = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    order_total_rounding_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    order_total_rounding_increment = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_currencies", x => x.id);
                    table.CheckConstraint("ck_store_currencies_currency_code", "char_length(currency_code) = 3");
                    table.CheckConstraint("ck_store_currencies_decimal_digits", "decimal_digits >= 0 and decimal_digits <= 4");
                    table.CheckConstraint("ck_store_currencies_line_total_rounding_increment", "line_total_rounding_increment > 0");
                    table.CheckConstraint("ck_store_currencies_order_total_rounding_increment", "order_total_rounding_increment > 0");
                    table.CheckConstraint("ck_store_currencies_unit_price_rounding_increment", "unit_price_rounding_increment > 0");
                    table.ForeignKey(
                        name: "FK_store_currencies_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_store_currencies_store_id_currency_code",
                table: "store_currencies",
                columns: new[] { "store_id", "currency_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_store_currencies_store_id_is_enabled_display_order",
                table: "store_currencies",
                columns: new[] { "store_id", "is_enabled", "display_order" });

            migrationBuilder.Sql(
                """
                INSERT INTO store_currencies (
                    id,
                    store_id,
                    currency_code,
                    is_enabled,
                    is_default_display_currency,
                    display_order,
                    culture_name,
                    symbol,
                    decimal_digits,
                    unit_price_rounding_mode,
                    unit_price_rounding_increment,
                    line_total_rounding_mode,
                    line_total_rounding_increment,
                    order_total_rounding_mode,
                    order_total_rounding_increment,
                    created_at,
                    updated_at
                )
                SELECT
                    ('00000000-0000-4000-8000-' || lpad(row_number() over (order by id)::text, 12, '0'))::uuid,
                    id,
                    upper(default_currency_code),
                    true,
                    true,
                    0,
                    default_culture,
                    null,
                    2,
                    'halfAwayFromZero',
                    0.01,
                    'halfAwayFromZero',
                    0.01,
                    'halfAwayFromZero',
                    0.01,
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP
                FROM commerce_store
                WHERE default_currency_code IS NOT NULL
                    AND char_length(default_currency_code) = 3;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_currencies");
        }
    }
}
