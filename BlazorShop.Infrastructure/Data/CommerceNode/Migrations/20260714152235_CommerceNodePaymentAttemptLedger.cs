using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodePaymentAttemptLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkout_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_method_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provider_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    provider_session_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    next_action_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    next_action_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    failure_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    failure_message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_attempts", x => x.id);
                    table.CheckConstraint("ck_payment_attempts_state", "state in ('created', 'requires_action', 'authorized', 'captured', 'failed', 'cancelled', 'expired')");
                    table.ForeignKey(
                        name: "FK_payment_attempts_Orders_order_id",
                        column: x => x.order_id,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_payment_attempts_checkout_sessions_checkout_session_id",
                        column: x => x.checkout_session_id,
                        principalTable: "checkout_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payment_attempts_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_provider_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_attempt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    event_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_provider_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_provider_events_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payment_provider_events_payment_attempts_payment_attempt_id",
                        column: x => x.payment_attempt_id,
                        principalTable: "payment_attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_attempts_checkout_session_id",
                table: "payment_attempts",
                column: "checkout_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_attempts_order_id",
                table: "payment_attempts",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_attempts_provider_key_provider_session_id",
                table: "payment_attempts",
                columns: new[] { "provider_key", "provider_session_id" },
                filter: "provider_session_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payment_attempts_public_id",
                table: "payment_attempts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_attempts_store_id_idempotency_key",
                table: "payment_attempts",
                columns: new[] { "store_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_attempts_store_id_state_created_at_utc",
                table: "payment_attempts",
                columns: new[] { "store_id", "state", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_provider_events_payment_attempt_id",
                table: "payment_provider_events",
                column: "payment_attempt_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_provider_events_provider_key_event_id",
                table: "payment_provider_events",
                columns: new[] { "provider_key", "event_id" },
                unique: true,
                filter: "event_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payment_provider_events_store_id_provider_key_created_at_utc",
                table: "payment_provider_events",
                columns: new[] { "store_id", "provider_key", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_provider_events");

            migrationBuilder.DropTable(
                name: "payment_attempts");
        }
    }
}
