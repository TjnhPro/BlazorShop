using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeOrderShippingSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "shipping_currency_code",
                table: "Orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_delivery_estimate_text",
                table: "Orders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_method_code",
                table: "Orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_method_key",
                table: "Orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_method_name",
                table: "Orders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_provider_system_name",
                table: "Orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_total",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "shipping_currency_code",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_delivery_estimate_text",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_method_code",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_method_key",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_method_name",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_provider_system_name",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_total",
                table: "Orders");
        }
    }
}
