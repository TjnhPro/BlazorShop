using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeCustomerProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "company",
                table: "commerce_customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                table: "commerce_customers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "commerce_customers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_activity_at_utc",
                table: "commerce_customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                table: "commerce_customers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preferred_currency_code",
                table: "commerce_customers",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preferred_language",
                table: "commerce_customers",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "company",
                table: "commerce_customers");

            migrationBuilder.DropColumn(
                name: "first_name",
                table: "commerce_customers");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "commerce_customers");

            migrationBuilder.DropColumn(
                name: "last_activity_at_utc",
                table: "commerce_customers");

            migrationBuilder.DropColumn(
                name: "last_name",
                table: "commerce_customers");

            migrationBuilder.DropColumn(
                name: "preferred_currency_code",
                table: "commerce_customers");

            migrationBuilder.DropColumn(
                name: "preferred_language",
                table: "commerce_customers");
        }
    }
}
