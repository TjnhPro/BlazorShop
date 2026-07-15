using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlazorShop.Infrastructure.Data.ControlPlane.Migrations
{
    /// <inheritdoc />
    public partial class ControlPlaneConfigurationFeaturePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "control_plane_permission",
                columns: new[] { "id", "created_at", "description", "key" },
                values: new object[,]
                {
                    { 15L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Read Commerce store configuration through Control Plane.", "commerce.settings.read" },
                    { 16L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Update Commerce store configuration through Control Plane.", "commerce.settings.write" },
                    { 17L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Read Commerce feature state through Control Plane.", "commerce.features.read" },
                    { 18L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Update Commerce feature state through Control Plane.", "commerce.features.write" },
                    { 19L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Read Commerce provider configuration through Control Plane.", "commerce.providers.read" },
                    { 20L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Update Commerce provider configuration through Control Plane.", "commerce.providers.write" }
                });

            migrationBuilder.InsertData(
                table: "control_plane_role_permission",
                columns: new[] { "permission_id", "role_id", "created_at" },
                values: new object[,]
                {
                    { 15L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 16L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 17L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 18L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 19L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 20L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 15L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 16L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 17L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 18L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 19L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 20L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 15L, 3L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 17L, 3L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 19L, 3L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 15L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 16L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 17L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 18L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 19L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 20L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 15L, 2L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 16L, 2L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 17L, 2L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 18L, 2L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 19L, 2L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 20L, 2L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 15L, 3L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 17L, 3L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 19L, 3L });

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 15L);

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 16L);

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 17L);

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 18L);

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 19L);

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 20L);
        }
    }
}
