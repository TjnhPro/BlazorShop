using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlazorShop.Infrastructure.Data.ControlPlane.Migrations
{
    /// <inheritdoc />
    public partial class ControlPlaneCommercePagePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "control_plane_permission",
                columns: new[] { "id", "created_at", "description", "key" },
                values: new object[,]
                {
                    { 13L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Read Commerce storefront pages through Control Plane.", "commerce.pages.read" },
                    { 14L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Create, update, publish, and archive Commerce storefront pages through Control Plane.", "commerce.pages.write" }
                });

            migrationBuilder.InsertData(
                table: "control_plane_role_permission",
                columns: new[] { "permission_id", "role_id", "created_at" },
                values: new object[,]
                {
                    { 13L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 14L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 13L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 14L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 13L, 3L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 13L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 14L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 13L, 2L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 14L, 2L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 13L, 3L });

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 13L);

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 14L);
        }
    }
}
