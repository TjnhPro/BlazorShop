using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStoreSecurityPrivacySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "store_security_privacy_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    consent_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    consent_banner_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    visitor_cookie_lifetime_days = table.Column<int>(type: "integer", nullable: false),
                    consent_event_retention_days = table.Column<int>(type: "integer", nullable: false),
                    optional_categories_default_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    policy_page_path = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    captcha_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    captcha_provider_system_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    captcha_public_site_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    captcha_secret_reference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    captcha_secret_last_rotated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    captcha_minimum_score = table.Column<double>(type: "double precision", nullable: false),
                    captcha_login_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    captcha_registration_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    captcha_newsletter_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    captcha_password_recovery_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    captcha_contact_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    captcha_review_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    refresh_token_ip_retention_days = table.Column<int>(type: "integer", nullable: false),
                    refresh_token_user_agent_retention_days = table.Column<int>(type: "integer", nullable: false),
                    captcha_verification_log_retention_days = table.Column<int>(type: "integer", nullable: false),
                    newsletter_consent_evidence_retention_days = table.Column<int>(type: "integer", nullable: false),
                    anonymize_ip_after_retention_window = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by_user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_security_privacy_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_store_security_privacy_settings_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_store_security_privacy_settings_public_id",
                table: "store_security_privacy_settings",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_store_security_privacy_settings_store_id",
                table: "store_security_privacy_settings",
                column: "store_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_security_privacy_settings");
        }
    }
}
