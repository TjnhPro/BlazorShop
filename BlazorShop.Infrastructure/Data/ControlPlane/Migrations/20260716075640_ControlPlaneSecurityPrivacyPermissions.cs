using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlazorShop.Infrastructure.Data.ControlPlane.Migrations
{
    /// <inheritdoc />
    public partial class ControlPlaneSecurityPrivacyPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "control_plane_permission",
                columns: new[] { "id", "created_at", "description", "key" },
                values: new object[,]
                {
                    { 23L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Read Commerce security and privacy settings through Control Plane.", "commerce.security_privacy.read" },
                    { 24L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Update Commerce security and privacy settings through Control Plane.", "commerce.security_privacy.write" },
                    { 25L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Edit Commerce captcha settings through Control Plane.", "commerce.captcha_settings.edit" },
                    { 26L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Edit Commerce consent settings through Control Plane.", "commerce.consent_settings.edit" }
                });

            migrationBuilder.InsertData(
                table: "control_plane_role_permission",
                columns: new[] { "permission_id", "role_id", "created_at" },
                values: new object[,]
                {
                    { 23L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 24L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 25L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 26L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 23L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 24L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 25L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 26L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 23L, 3L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 23L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 24L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 25L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 26L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 23L, 2L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 24L, 2L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 25L, 2L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 26L, 2L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 23L, 3L });

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 23L);

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 24L);

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 25L);

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 26L);
        }
    }
}
