using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStoreCurrencyExchangeRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "store_currency_exchange_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    target_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(28,12)", precision: 28, scale: 12, nullable: false),
                    provider_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    effective_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_manual = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_currency_exchange_rates", x => x.id);
                    table.CheckConstraint("ck_store_currency_exchange_rates_base_currency_code", "char_length(base_currency_code) = 3");
                    table.CheckConstraint("ck_store_currency_exchange_rates_distinct_currency", "base_currency_code <> target_currency_code");
                    table.CheckConstraint("ck_store_currency_exchange_rates_expires_after_effective", "expires_at is null or expires_at > effective_at");
                    table.CheckConstraint("ck_store_currency_exchange_rates_rate", "rate > 0");
                    table.CheckConstraint("ck_store_currency_exchange_rates_target_currency_code", "char_length(target_currency_code) = 3");
                    table.ForeignKey(
                        name: "FK_store_currency_exchange_rates_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_store_currency_exchange_rates_store_id_base_currency_code_t~",
                table: "store_currency_exchange_rates",
                columns: new[] { "store_id", "base_currency_code", "target_currency_code", "provider_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_store_currency_exchange_rates_store_id_target_currency_code~",
                table: "store_currency_exchange_rates",
                columns: new[] { "store_id", "target_currency_code", "is_enabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_currency_exchange_rates");
        }
    }
}
