using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeVariationTemplateFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "Products",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Simple");

            migrationBuilder.AddColumn<Guid>(
                name: "VariationTemplateId",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "variation_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variation_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "variation_template_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variation_template_options", x => x.id);
                    table.ForeignKey(
                        name: "FK_variation_template_options_variation_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "variation_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variation_template_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variation_template_values", x => x.id);
                    table.ForeignKey(
                        name: "FK_variation_template_values_variation_template_options_option~",
                        column: x => x.option_id,
                        principalTable: "variation_template_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_ProductType",
                table: "Products",
                columns: new[] { "StoreId", "ProductType" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_VariationTemplateId",
                table: "Products",
                column: "VariationTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_variation_template_options_public_id",
                table: "variation_template_options",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variation_template_options_template_id_name",
                table: "variation_template_options",
                columns: new[] { "template_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variation_template_options_template_id_sort_order",
                table: "variation_template_options",
                columns: new[] { "template_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_variation_template_values_option_id_sort_order",
                table: "variation_template_values",
                columns: new[] { "option_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_variation_template_values_option_id_value",
                table: "variation_template_values",
                columns: new[] { "option_id", "value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variation_template_values_public_id",
                table: "variation_template_values",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variation_templates_public_id",
                table: "variation_templates",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variation_templates_store_id_is_active",
                table: "variation_templates",
                columns: new[] { "store_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_variation_templates_store_id_slug",
                table: "variation_templates",
                columns: new[] { "store_id", "slug" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_variation_templates_VariationTemplateId",
                table: "Products",
                column: "VariationTemplateId",
                principalTable: "variation_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_variation_templates_VariationTemplateId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "variation_template_values");

            migrationBuilder.DropTable(
                name: "variation_template_options");

            migrationBuilder.DropTable(
                name: "variation_templates");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_ProductType",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_VariationTemplateId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VariationTemplateId",
                table: "Products");
        }
    }
}
