using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStoreNavigationCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "store_navigation_menu",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_navigation_menu", x => x.id);
                    table.CheckConstraint("ck_store_navigation_menu_system_name", "system_name in ('main', 'footer_company', 'footer_support', 'footer_legal', 'utility', 'mobile')");
                    table.ForeignKey(
                        name: "FK_store_navigation_menu_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_navigation_menu_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    target_entity_public_id = table.Column<Guid>(type: "uuid", nullable: true),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    opens_in_new_tab = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_navigation_menu_item", x => x.id);
                    table.CheckConstraint("ck_store_navigation_menu_item_display_order", "display_order >= 0");
                    table.CheckConstraint("ck_store_navigation_menu_item_external_url", "target_type <> 'external_url' OR url LIKE 'https://%'");
                    table.CheckConstraint("ck_store_navigation_menu_item_group_shape", "target_type <> 'group' OR (target_key IS NULL AND target_entity_public_id IS NULL AND url IS NULL)");
                    table.CheckConstraint("ck_store_navigation_menu_item_target_type", "target_type in ('system', 'category', 'page', 'product', 'external_url', 'group', 'internal_route')");
                    table.ForeignKey(
                        name: "FK_store_navigation_menu_item_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_store_navigation_menu_item_store_navigation_menu_item_paren~",
                        column: x => x.parent_item_id,
                        principalTable: "store_navigation_menu_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_store_navigation_menu_item_store_navigation_menu_menu_id",
                        column: x => x.menu_id,
                        principalTable: "store_navigation_menu",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_store_navigation_menu_public_id",
                table: "store_navigation_menu",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_store_navigation_menu_store_id_is_enabled",
                table: "store_navigation_menu",
                columns: new[] { "store_id", "is_enabled" });

            migrationBuilder.CreateIndex(
                name: "IX_store_navigation_menu_store_id_system_name",
                table: "store_navigation_menu",
                columns: new[] { "store_id", "system_name" },
                unique: true,
                filter: "archived_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_store_navigation_menu_item_menu_id_is_enabled_archived_at",
                table: "store_navigation_menu_item",
                columns: new[] { "menu_id", "is_enabled", "archived_at" });

            migrationBuilder.CreateIndex(
                name: "IX_store_navigation_menu_item_parent_item_id",
                table: "store_navigation_menu_item",
                column: "parent_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_store_navigation_menu_item_public_id",
                table: "store_navigation_menu_item",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_store_navigation_menu_item_store_id_menu_id_parent_item_id_~",
                table: "store_navigation_menu_item",
                columns: new[] { "store_id", "menu_id", "parent_item_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_store_navigation_menu_item_store_id_target_type_target_enti~",
                table: "store_navigation_menu_item",
                columns: new[] { "store_id", "target_type", "target_entity_public_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_navigation_menu_item");

            migrationBuilder.DropTable(
                name: "store_navigation_menu");
        }
    }
}
