using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeOrderPlacementSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "base_discount_total_amount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_grand_total_amount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_shipping_total_amount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_subtotal_amount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_tax_total_amount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "billing_address_snapshot_json",
                table: "Orders",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "discount_total_amount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "grand_total_amount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "guest_access_token_expires_at_utc",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "guest_access_token_hash",
                table: "Orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_address_snapshot_json",
                table: "Orders",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_method_snapshot_json",
                table: "Orders",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "shipping_total_amount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "store_base_url_snapshot",
                table: "Orders",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "store_company_address_snapshot",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "store_company_email_snapshot",
                table: "Orders",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "store_company_name_snapshot",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "store_company_phone_snapshot",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "store_key_snapshot",
                table: "Orders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "store_name_snapshot",
                table: "Orders",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "store_public_id",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "subtotal_amount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_total_amount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "base_discount_total_amount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "base_grand_total_amount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "base_shipping_total_amount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "base_subtotal_amount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "base_tax_total_amount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "billing_address_snapshot_json",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "discount_total_amount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "grand_total_amount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "guest_access_token_expires_at_utc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "guest_access_token_hash",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_address_snapshot_json",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_method_snapshot_json",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_total_amount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "store_base_url_snapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "store_company_address_snapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "store_company_email_snapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "store_company_name_snapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "store_company_phone_snapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "store_key_snapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "store_name_snapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "store_public_id",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "subtotal_amount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "tax_total_amount",
                table: "Orders");
        }
    }
}
