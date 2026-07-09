using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStorefrontDeploymentImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "storefront_deployment_image",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    image = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storefront_deployment_image", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "storefront_deployment_image",
                columns: new[] { "id", "created_at", "image", "is_default", "is_enabled", "key", "updated_at", "version" },
                values: new object[] { new Guid("0aa383ff-dc89-4a30-bc13-6c4cae7b72b6"), new DateTimeOffset(new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "blazorshop-storefront-v2:latest", true, true, "storefront-v2", new DateTimeOffset(new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "latest" });

            migrationBuilder.CreateIndex(
                name: "IX_storefront_deployment_image_image",
                table: "storefront_deployment_image",
                column: "image",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_storefront_deployment_image_is_default",
                table: "storefront_deployment_image",
                column: "is_default",
                unique: true,
                filter: "is_enabled = true AND is_default = true");

            migrationBuilder.CreateIndex(
                name: "IX_storefront_deployment_image_is_enabled_is_default",
                table: "storefront_deployment_image",
                columns: new[] { "is_enabled", "is_default" });

            migrationBuilder.CreateIndex(
                name: "IX_storefront_deployment_image_key",
                table: "storefront_deployment_image",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "storefront_deployment_image");
        }
    }
}
