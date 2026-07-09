using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStoreProvisioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commerce_store",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    control_plane_store_public_id = table.Column<Guid>(type: "uuid", nullable: true),
                    store_key = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    base_url = table.Column<string>(type: "text", nullable: true),
                    default_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    default_culture = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_store", x => x.id);
                    table.CheckConstraint("ck_commerce_store_default_currency_code", "char_length(default_currency_code) = 3");
                    table.CheckConstraint("ck_commerce_store_status", "status in ('active', 'disabled', 'archived')");
                });

            migrationBuilder.CreateTable(
                name: "commerce_store_domain",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain = table.Column<string>(type: "text", nullable: false),
                    normalized_domain = table.Column<string>(type: "text", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    disabled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_store_domain", x => x.id);
                    table.CheckConstraint("ck_commerce_store_domain_status", "status in ('pending', 'verified', 'disabled')");
                    table.ForeignKey(
                        name: "FK_commerce_store_domain_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commerce_store_control_plane_store_public_id",
                table: "commerce_store",
                column: "control_plane_store_public_id");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_store_public_id",
                table: "commerce_store",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commerce_store_status",
                table: "commerce_store",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_store_store_key",
                table: "commerce_store",
                column: "store_key",
                unique: true,
                filter: "archived_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_store_domain_normalized_domain",
                table: "commerce_store_domain",
                column: "normalized_domain",
                unique: true,
                filter: "disabled_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_store_domain_store_id",
                table: "commerce_store_domain",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_store_domain_store_id_is_primary",
                table: "commerce_store_domain",
                columns: new[] { "store_id", "is_primary" },
                unique: true,
                filter: "is_primary = true AND disabled_at IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_store_deployment_commerce_store_store_id",
                table: "store_deployment",
                column: "store_id",
                principalTable: "commerce_store",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_store_deployment_commerce_store_store_id",
                table: "store_deployment");

            migrationBuilder.DropTable(
                name: "commerce_store_domain");

            migrationBuilder.DropTable(
                name: "commerce_store");
        }
    }
}
