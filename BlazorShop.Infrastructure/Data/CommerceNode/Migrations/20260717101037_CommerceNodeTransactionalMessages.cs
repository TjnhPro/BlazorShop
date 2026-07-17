using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeTransactionalMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "message_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    system_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: true),
                    language_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    subject_template = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    body_html_template = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_message_templates_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "queued_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_system_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    language_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    to_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    to_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    from_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    from_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    reply_to_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    body_html = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "pending"),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    next_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    error_message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    related_entity_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    related_entity_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    attachment_metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_queued_messages", x => x.id);
                    table.CheckConstraint("ck_queued_messages_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_queued_messages_max_attempts", "max_attempts >= 1");
                    table.CheckConstraint("ck_queued_messages_priority", "priority >= 0");
                    table.CheckConstraint("ck_queued_messages_status", "status in ('pending', 'sending', 'sent', 'waiting_retry', 'failed', 'cancelled')");
                    table.ForeignKey(
                        name: "FK_queued_messages_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_queued_messages_message_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "message_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "message_templates",
                columns: new[] { "id", "body_html_template", "created_at_utc", "description", "is_active", "language_code", "public_id", "store_id", "subject_template", "system_name", "updated_at_utc" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-000000000001"), "<p>Hello {{Customer.FullName}},</p><p>Confirm your account: <a href=\"{{Account.ActivationUrl}}\">Confirm email</a></p>", new DateTimeOffset(new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Default customer account activation template.", true, null, new Guid("11111111-1111-1111-1111-100000000001"), null, "Confirm your {{Store.Name}} account", "customer.account_activation", new DateTimeOffset(new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-000000000002"), "<p>Hello {{Customer.FullName}},</p><p>Reset your password: <a href=\"{{Account.PasswordResetUrl}}\">Reset password</a></p>", new DateTimeOffset(new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Default customer password recovery template.", true, null, new Guid("11111111-1111-1111-1111-100000000002"), null, "Reset your {{Store.Name}} password", "customer.password_recovery", new DateTimeOffset(new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-000000000003"), "<p>Thanks for your order {{Order.Reference}}.</p><p>Total: {{Order.Total}} {{Order.Currency}}</p>", new DateTimeOffset(new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Default order placed confirmation template.", true, null, new Guid("11111111-1111-1111-1111-100000000003"), null, "Order {{Order.Reference}} confirmed", "order.placed", new DateTimeOffset(new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-000000000004"), "<p>Your payment status is now {{Order.PaymentStatus}}.</p>", new DateTimeOffset(new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Default order payment status notification template.", true, null, new Guid("11111111-1111-1111-1111-100000000004"), null, "Payment update for {{Order.Reference}}", "order.payment_status_changed", new DateTimeOffset(new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-000000000005"), "<p>Your shipping status is now {{Order.ShippingStatus}}.</p><p>Tracking: {{Shipment.TrackingNumber}}</p>", new DateTimeOffset(new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Default order fulfillment status notification template.", true, null, new Guid("11111111-1111-1111-1111-100000000005"), null, "Shipping update for {{Order.Reference}}", "order.fulfillment_status_changed", new DateTimeOffset(new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("11111111-1111-1111-1111-000000000006"), "<p>From: {{Contact.Name}} ({{Contact.Email}})</p><p>{{Contact.Message}}</p>", new DateTimeOffset(new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Default storefront contact form delivery template.", true, null, new Guid("11111111-1111-1111-1111-100000000006"), null, "Contact form: {{Contact.Subject}}", "storefront.contact_form", new DateTimeOffset(new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_message_templates_public_id",
                table: "message_templates",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_message_templates_store_id_system_name",
                table: "message_templates",
                columns: new[] { "store_id", "system_name" });

            migrationBuilder.CreateIndex(
                name: "IX_message_templates_system_name_is_active",
                table: "message_templates",
                columns: new[] { "system_name", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_message_templates_system_name_store_id_language_code",
                table: "message_templates",
                columns: new[] { "system_name", "store_id", "language_code" },
                unique: true,
                filter: "store_id IS NOT NULL AND language_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_message_templates_unique_global_default_language",
                table: "message_templates",
                column: "system_name",
                unique: true,
                filter: "store_id IS NULL AND language_code IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_message_templates_unique_global_language",
                table: "message_templates",
                columns: new[] { "system_name", "language_code" },
                unique: true,
                filter: "store_id IS NULL AND language_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_message_templates_unique_store_default_language",
                table: "message_templates",
                columns: new[] { "system_name", "store_id" },
                unique: true,
                filter: "store_id IS NOT NULL AND language_code IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_queued_messages_correlation_id",
                table: "queued_messages",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_queued_messages_idempotency_key",
                table: "queued_messages",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_queued_messages_public_id",
                table: "queued_messages",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_queued_messages_store_id_related_entity_type_related_entity~",
                table: "queued_messages",
                columns: new[] { "store_id", "related_entity_type", "related_entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_queued_messages_store_id_status_next_attempt_at_utc",
                table: "queued_messages",
                columns: new[] { "store_id", "status", "next_attempt_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_queued_messages_store_id_template_system_name_created_at_utc",
                table: "queued_messages",
                columns: new[] { "store_id", "template_system_name", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_queued_messages_template_id",
                table: "queued_messages",
                column: "template_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "queued_messages");

            migrationBuilder.DropTable(
                name: "message_templates");
        }
    }
}
