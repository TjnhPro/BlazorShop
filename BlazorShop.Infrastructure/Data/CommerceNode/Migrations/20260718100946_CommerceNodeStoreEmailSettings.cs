using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStoreEmailSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "store_email_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    smtp_host = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: true),
                    smtp_port = table.Column<int>(type: "integer", nullable: false, defaultValue: 587),
                    use_ssl = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    username = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    protected_password = table.Column<string>(type: "text", nullable: true),
                    password_updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    from_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    from_display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    reply_to_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    delivery_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "smtp"),
                    capture_redirect_to_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by_user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_email_settings", x => x.id);
                    table.CheckConstraint("ck_store_email_settings_delivery_mode", "delivery_mode in ('smtp', 'capture')");
                    table.CheckConstraint("ck_store_email_settings_smtp_port", "smtp_port >= 1 AND smtp_port <= 65535");
                    table.ForeignKey(
                        name: "FK_store_email_settings_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_store_email_settings_public_id",
                table: "store_email_settings",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_store_email_settings_store_id",
                table: "store_email_settings",
                column: "store_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_store_email_settings_store_id_enabled_delivery_mode",
                table: "store_email_settings",
                columns: new[] { "store_id", "enabled", "delivery_mode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_email_settings");
        }
    }
}
