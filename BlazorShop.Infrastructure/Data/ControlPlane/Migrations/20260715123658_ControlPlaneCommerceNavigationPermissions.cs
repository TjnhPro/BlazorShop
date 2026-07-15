using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlazorShop.Infrastructure.Data.ControlPlane.Migrations
{
    /// <inheritdoc />
    public partial class ControlPlaneCommerceNavigationPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "control_plane_permission",
                columns: new[] { "id", "created_at", "description", "key" },
                values: new object[,]
                {
                    { 21L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Read Commerce storefront navigation through Control Plane.", "commerce.navigation.read" },
                    { 22L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Update Commerce storefront navigation through Control Plane.", "commerce.navigation.write" }
                });

            migrationBuilder.InsertData(
                table: "control_plane_role_permission",
                columns: new[] { "permission_id", "role_id", "created_at" },
                values: new object[,]
                {
                    { 21L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 22L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 21L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 22L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 21L, 3L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 21L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 22L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 21L, 2L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 22L, 2L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 21L, 3L });

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 21L);

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 22L);
        }
    }
}
