using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.ControlPlane.Migrations
{
    /// <inheritdoc />
    public partial class ControlPlaneStoreProvisioningStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_store_registry_status",
                table: "store_registry");

            migrationBuilder.AddCheckConstraint(
                name: "ck_store_registry_status",
                table: "store_registry",
                sql: "status in ('active', 'provisioning', 'disabled', 'archived')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_store_registry_status",
                table: "store_registry");

            migrationBuilder.Sql(
                """
                UPDATE store_registry
                SET status = 'disabled'
                WHERE status = 'provisioning';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_store_registry_status",
                table: "store_registry",
                sql: "status in ('active', 'disabled', 'archived')");
        }
    }
}
