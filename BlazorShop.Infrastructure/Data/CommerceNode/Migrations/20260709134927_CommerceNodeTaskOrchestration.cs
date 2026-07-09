using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeTaskOrchestration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commerce_task",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    idempotency_key = table.Column<string>(type: "text", nullable: true),
                    lock_key = table.Column<string>(type: "text", nullable: true),
                    payload_schema_version = table.Column<string>(type: "text", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    result_json = table.Column<string>(type: "jsonb", nullable: true),
                    error_code = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    max_attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<string>(type: "text", nullable: true),
                    cancel_requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancel_reason = table.Column<string>(type: "text", nullable: true),
                    worker_id = table.Column<string>(type: "text", nullable: true),
                    last_heartbeat_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_task", x => x.id);
                    table.CheckConstraint("ck_commerce_task_attempt_count", "attempt_count >= 0");
                    table.CheckConstraint("ck_commerce_task_max_attempts", "max_attempts >= 1");
                    table.CheckConstraint("ck_commerce_task_status", "status in ('pending', 'running', 'waiting_retry', 'succeeded', 'failed', 'cancelled', 'dead')");
                });

            migrationBuilder.CreateTable(
                name: "commerce_task_step",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    step_key = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    result_json = table.Column<string>(type: "jsonb", nullable: true),
                    error_code = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_task_step", x => x.id);
                    table.CheckConstraint("ck_commerce_task_step_status", "status in ('pending', 'running', 'succeeded', 'failed', 'skipped', 'rolled_back')");
                    table.ForeignKey(
                        name: "FK_commerce_task_step_commerce_task_task_id",
                        column: x => x.task_id,
                        principalTable: "commerce_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_deployment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    storefront_image = table.Column<string>(type: "text", nullable: false),
                    container_name = table.Column<string>(type: "text", nullable: false),
                    network_name = table.Column<string>(type: "text", nullable: true),
                    public_url = table.Column<string>(type: "text", nullable: true),
                    internal_url = table.Column<string>(type: "text", nullable: true),
                    nginx_server_name = table.Column<string>(type: "text", nullable: true),
                    nginx_config_path = table.Column<string>(type: "text", nullable: true),
                    env_file_path = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    last_health_status = table.Column<string>(type: "text", nullable: true),
                    last_health_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deployed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_deployment", x => x.id);
                    table.CheckConstraint("ck_store_deployment_status", "status in ('provisioning', 'active', 'failed', 'disabled', 'removed')");
                    table.ForeignKey(
                        name: "FK_store_deployment_commerce_task_task_id",
                        column: x => x.task_id,
                        principalTable: "commerce_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commerce_task_correlation_id",
                table: "commerce_task",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_task_idempotency_key",
                table: "commerce_task",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_task_lock_key_status",
                table: "commerce_task",
                columns: new[] { "lock_key", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_commerce_task_public_id",
                table: "commerce_task",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commerce_task_status_next_attempt_at",
                table: "commerce_task",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "IX_commerce_task_task_type",
                table: "commerce_task",
                column: "task_type");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_task_step_task_id",
                table: "commerce_task_step",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_task_step_task_id_step_key_attempt_number",
                table: "commerce_task_step",
                columns: new[] { "task_id", "step_key", "attempt_number" });

            migrationBuilder.CreateIndex(
                name: "IX_store_deployment_container_name",
                table: "store_deployment",
                column: "container_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_store_deployment_status",
                table: "store_deployment",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_store_deployment_store_id",
                table: "store_deployment",
                column: "store_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_store_deployment_task_id",
                table: "store_deployment",
                column: "task_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commerce_task_step");

            migrationBuilder.DropTable(
                name: "store_deployment");

            migrationBuilder.DropTable(
                name: "commerce_task");
        }
    }
}
