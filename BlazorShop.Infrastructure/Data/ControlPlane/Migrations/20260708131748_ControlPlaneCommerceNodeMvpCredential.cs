using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.ControlPlane.Migrations
{
    /// <inheritdoc />
    public partial class ControlPlaneCommerceNodeMvpCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "node_secret",
                table: "commerce_node",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "node_secret_updated_at",
                table: "commerce_node",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "node_secret",
                table: "commerce_node");

            migrationBuilder.DropColumn(
                name: "node_secret_updated_at",
                table: "commerce_node");
        }
    }
}
