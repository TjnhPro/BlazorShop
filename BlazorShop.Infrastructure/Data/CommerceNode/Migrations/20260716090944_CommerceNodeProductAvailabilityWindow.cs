using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeProductAvailabilityWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableEndUtc",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableStartUtc",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_IsPublished_ArchivedAt_AvailableStartUtc_A~",
                table: "Products",
                columns: new[] { "StoreId", "IsPublished", "ArchivedAt", "AvailableStartUtc", "AvailableEndUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_IsPublished_ArchivedAt_AvailableStartUtc_A~",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AvailableEndUtc",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AvailableStartUtc",
                table: "Products");
        }
    }
}
