using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlazorShop.Infrastructure.Data.ControlPlane.Migrations
{
    /// <inheritdoc />
    public partial class ControlPlaneUserManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "public_id",
                table: "control_plane_admin_user",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "status_changed_at",
                table: "control_plane_admin_user",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "status_changed_by_admin_user_id",
                table: "control_plane_admin_user",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status_reason",
                table: "control_plane_admin_user",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "control_plane_admin_user_permission",
                columns: table => new
                {
                    admin_user_id = table.Column<long>(type: "bigint", nullable: false),
                    permission_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by_admin_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_plane_admin_user_permission", x => new { x.admin_user_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_control_plane_admin_user_permission_control_plane_admin_use~",
                        column: x => x.admin_user_id,
                        principalTable: "control_plane_admin_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_control_plane_admin_user_permission_control_plane_admin_us~1",
                        column: x => x.created_by_admin_user_id,
                        principalTable: "control_plane_admin_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_control_plane_admin_user_permission_control_plane_permissio~",
                        column: x => x.permission_id,
                        principalTable: "control_plane_permission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "control_plane_permission",
                columns: new[] { "id", "created_at", "description", "key" },
                values: new object[,]
                {
                    { 9L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "List and view Control Plane users.", "users.read" },
                    { 10L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Create, update, enable, and disable Control Plane users.", "users.write" },
                    { 11L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Assign and remove Control Plane roles.", "roles.assign" },
                    { 12L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Assign and remove direct Control Plane user permissions.", "permissions.manage" }
                });

            migrationBuilder.InsertData(
                table: "control_plane_role_permission",
                columns: new[] { "permission_id", "role_id", "created_at" },
                values: new object[,]
                {
                    { 9L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 10L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 11L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 12L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "control_plane_admin_user_public_id_uq",
                table: "control_plane_admin_user",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_control_plane_admin_user_status",
                table: "control_plane_admin_user",
                column: "status",
                filter: "deleted_at is null");

            migrationBuilder.CreateIndex(
                name: "ix_control_plane_admin_user_status_changed_by",
                table: "control_plane_admin_user",
                column: "status_changed_by_admin_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_control_plane_admin_user_permission_created_by",
                table: "control_plane_admin_user_permission",
                column: "created_by_admin_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_control_plane_admin_user_permission_permission_id",
                table: "control_plane_admin_user_permission",
                column: "permission_id");

            migrationBuilder.AddForeignKey(
                name: "FK_control_plane_admin_user_control_plane_admin_user_status_ch~",
                table: "control_plane_admin_user",
                column: "status_changed_by_admin_user_id",
                principalTable: "control_plane_admin_user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_control_plane_admin_user_control_plane_admin_user_status_ch~",
                table: "control_plane_admin_user");

            migrationBuilder.DropTable(
                name: "control_plane_admin_user_permission");

            migrationBuilder.DropIndex(
                name: "control_plane_admin_user_public_id_uq",
                table: "control_plane_admin_user");

            migrationBuilder.DropIndex(
                name: "ix_control_plane_admin_user_status",
                table: "control_plane_admin_user");

            migrationBuilder.DropIndex(
                name: "ix_control_plane_admin_user_status_changed_by",
                table: "control_plane_admin_user");

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 9L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 10L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 11L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_role_permission",
                keyColumns: new[] { "permission_id", "role_id" },
                keyValues: new object[] { 12L, 1L });

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 9L);

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 10L);

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 11L);

            migrationBuilder.DeleteData(
                table: "control_plane_permission",
                keyColumn: "id",
                keyValue: 12L);

            migrationBuilder.DropColumn(
                name: "public_id",
                table: "control_plane_admin_user");

            migrationBuilder.DropColumn(
                name: "status_changed_at",
                table: "control_plane_admin_user");

            migrationBuilder.DropColumn(
                name: "status_changed_by_admin_user_id",
                table: "control_plane_admin_user");

            migrationBuilder.DropColumn(
                name: "status_reason",
                table: "control_plane_admin_user");
        }
    }
}
