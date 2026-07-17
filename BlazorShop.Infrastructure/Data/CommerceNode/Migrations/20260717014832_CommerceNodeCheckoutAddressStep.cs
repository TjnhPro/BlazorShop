using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeCheckoutAddressStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "billing_address_snapshot_json",
                table: "checkout_sessions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_address_source",
                table: "checkout_sessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "direct");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "billing_address_snapshot_json",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "shipping_address_source",
                table: "checkout_sessions");
        }
    }
}
