using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStorefrontConsentCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "storefront_consent_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    event_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    consent_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    categories_json = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storefront_consent_event", x => x.id);
                    table.ForeignKey(
                        name: "FK_storefront_consent_event_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "storefront_consent_state",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    visitor_key_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    consent_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    essential_accepted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    preferences_accepted = table.Column<bool>(type: "boolean", nullable: false),
                    analytics_accepted = table.Column<bool>(type: "boolean", nullable: false),
                    marketing_accepted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storefront_consent_state", x => x.id);
                    table.ForeignKey(
                        name: "FK_storefront_consent_state_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_storefront_consent_event_occurred_at_utc",
                table: "storefront_consent_event",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_storefront_consent_event_store_id_consent_key",
                table: "storefront_consent_event",
                columns: new[] { "store_id", "consent_key" });

            migrationBuilder.CreateIndex(
                name: "IX_storefront_consent_state_consent_key",
                table: "storefront_consent_state",
                column: "consent_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_storefront_consent_state_expires_at_utc",
                table: "storefront_consent_state",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_storefront_consent_state_store_id_visitor_key_hash_consent_~",
                table: "storefront_consent_state",
                columns: new[] { "store_id", "visitor_key_hash", "consent_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "storefront_consent_event");

            migrationBuilder.DropTable(
                name: "storefront_consent_state");
        }
    }
}
