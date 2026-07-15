using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodePaymentProviderAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "icon_url",
                table: "store_payment_methods",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "max_order_total",
                table: "store_payment_methods",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "min_order_total",
                table: "store_payment_methods",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "short_display_text",
                table: "store_payment_methods",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supported_country_codes_json",
                table: "store_payment_methods",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supported_currency_codes_json",
                table: "store_payment_methods",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "icon_url",
                table: "store_payment_methods");

            migrationBuilder.DropColumn(
                name: "max_order_total",
                table: "store_payment_methods");

            migrationBuilder.DropColumn(
                name: "min_order_total",
                table: "store_payment_methods");

            migrationBuilder.DropColumn(
                name: "short_display_text",
                table: "store_payment_methods");

            migrationBuilder.DropColumn(
                name: "supported_country_codes_json",
                table: "store_payment_methods");

            migrationBuilder.DropColumn(
                name: "supported_currency_codes_json",
                table: "store_payment_methods");
        }
    }
}
