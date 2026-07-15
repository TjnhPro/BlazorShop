using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStoreSeoSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "store_seo_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    default_title_suffix = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    default_meta_description = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    default_og_image = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    base_canonical_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    company_logo_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    company_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    company_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    company_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    facebook_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    instagram_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    x_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_seo_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_store_seo_settings_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_store_seo_settings_store_id",
                table: "store_seo_settings",
                column: "store_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_seo_settings");
        }
    }
}
