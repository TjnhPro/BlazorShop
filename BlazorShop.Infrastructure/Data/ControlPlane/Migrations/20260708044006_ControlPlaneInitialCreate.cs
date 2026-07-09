using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BlazorShop.Infrastructure.Data.ControlPlane.Migrations
{
    /// <inheritdoc />
    public partial class ControlPlaneInitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "commerce_node",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    node_key = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    disabled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_node", x => x.id);
                    table.CheckConstraint("ck_commerce_node_status", "status in ('unknown', 'healthy', 'warning', 'down', 'disabled')");
                });

            migrationBuilder.CreateTable(
                name: "control_plane_admin_user",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    identity_user_id = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_plane_admin_user", x => x.id);
                    table.CheckConstraint("ck_control_plane_admin_user_status", "status in ('active', 'disabled', 'invited')");
                });

            migrationBuilder.CreateTable(
                name: "control_plane_permission",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    key = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_plane_permission", x => x.id);
                    table.CheckConstraint("ck_control_plane_permission_key_lower", "key = lower(key)");
                });

            migrationBuilder.CreateTable(
                name: "control_plane_role",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    key = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_plane_role", x => x.id);
                    table.CheckConstraint("ck_control_plane_role_key_lower", "key = lower(key)");
                });

            migrationBuilder.CreateTable(
                name: "commerce_node_endpoint",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    node_id = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    disabled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_node_endpoint", x => x.id);
                    table.CheckConstraint("ck_commerce_node_endpoint_kind", "kind in ('control_api', 'storefront', 'internal_api')");
                    table.ForeignKey(
                        name: "FK_commerce_node_endpoint_commerce_node_node_id",
                        column: x => x.node_id,
                        principalTable: "commerce_node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "node_capability_snapshot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    node_id = table.Column<long>(type: "bigint", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    schema_version = table.Column<string>(type: "text", nullable: false),
                    checksum = table.Column<string>(type: "text", nullable: false),
                    capabilities_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_node_capability_snapshot", x => x.id);
                    table.ForeignKey(
                        name: "FK_node_capability_snapshot_commerce_node_node_id",
                        column: x => x.node_id,
                        principalTable: "commerce_node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "node_health_snapshot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    node_id = table.Column<long>(type: "bigint", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    status = table.Column<string>(type: "text", nullable: false),
                    http_status_code = table.Column<int>(type: "integer", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    dependency_status_json = table.Column<string>(type: "jsonb", nullable: true),
                    error_code = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    checked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_node_health_snapshot", x => x.id);
                    table.CheckConstraint("ck_node_health_snapshot_status", "status in ('healthy', 'warning', 'down', 'timeout', 'malformed', 'unknown')");
                    table.ForeignKey(
                        name: "FK_node_health_snapshot_commerce_node_node_id",
                        column: x => x.node_id,
                        principalTable: "commerce_node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_registry",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    node_id = table.Column<long>(type: "bigint", nullable: false),
                    store_key = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_registry", x => x.id);
                    table.CheckConstraint("ck_store_registry_status", "status in ('active', 'disabled', 'archived')");
                    table.ForeignKey(
                        name: "FK_store_registry_commerce_node_node_id",
                        column: x => x.node_id,
                        principalTable: "commerce_node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commerce_node_credential",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    node_id = table.Column<long>(type: "bigint", nullable: false),
                    key_id = table.Column<string>(type: "text", nullable: false),
                    secret_hash = table.Column<string>(type: "text", nullable: false),
                    hash_algorithm = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    revealed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_admin_user_id = table.Column<long>(type: "bigint", nullable: true),
                    revoked_by_admin_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commerce_node_credential", x => x.id);
                    table.CheckConstraint("ck_commerce_node_credential_status", "status in ('active', 'revoked', 'rotated')");
                    table.ForeignKey(
                        name: "FK_commerce_node_credential_commerce_node_node_id",
                        column: x => x.node_id,
                        principalTable: "commerce_node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_commerce_node_credential_control_plane_admin_user_created_b~",
                        column: x => x.created_by_admin_user_id,
                        principalTable: "control_plane_admin_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_commerce_node_credential_control_plane_admin_user_revoked_b~",
                        column: x => x.revoked_by_admin_user_id,
                        principalTable: "control_plane_admin_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "control_plane_admin_user_role",
                columns: table => new
                {
                    admin_user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_plane_admin_user_role", x => new { x.admin_user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_control_plane_admin_user_role_control_plane_admin_user_admi~",
                        column: x => x.admin_user_id,
                        principalTable: "control_plane_admin_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_control_plane_admin_user_role_control_plane_role_role_id",
                        column: x => x.role_id,
                        principalTable: "control_plane_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "control_plane_role_permission",
                columns: table => new
                {
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    permission_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_plane_role_permission", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_control_plane_role_permission_control_plane_permission_perm~",
                        column: x => x.permission_id,
                        principalTable: "control_plane_permission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_control_plane_role_permission_control_plane_role_role_id",
                        column: x => x.role_id,
                        principalTable: "control_plane_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "control_action",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    node_id = table.Column<long>(type: "bigint", nullable: false),
                    store_id = table.Column<long>(type: "bigint", nullable: true),
                    action_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    idempotency_key = table.Column<string>(type: "text", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: true),
                    result_json = table.Column<string>(type: "jsonb", nullable: true),
                    error_code = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_action", x => x.id);
                    table.CheckConstraint("ck_control_action_status", "status in ('queued', 'running', 'failed', 'succeeded', 'cancelled')");
                    table.ForeignKey(
                        name: "FK_control_action_commerce_node_node_id",
                        column: x => x.node_id,
                        principalTable: "commerce_node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_control_action_store_registry_store_id",
                        column: x => x.store_id,
                        principalTable: "store_registry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "store_domain_registry",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    store_id = table.Column<long>(type: "bigint", nullable: false),
                    domain = table.Column<string>(type: "text", nullable: false),
                    normalized_domain = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    disabled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_domain_registry", x => x.id);
                    table.CheckConstraint("ck_store_domain_registry_status", "status in ('pending', 'verified', 'disabled')");
                    table.ForeignKey(
                        name: "FK_store_domain_registry_store_registry_store_id",
                        column: x => x.store_id,
                        principalTable: "store_registry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "control_action_attempt",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    action_id = table.Column<long>(type: "bigint", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    http_status_code = table.Column<int>(type: "integer", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    response_json = table.Column<string>(type: "jsonb", nullable: true),
                    error_code = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_action_attempt", x => x.id);
                    table.CheckConstraint("ck_control_action_attempt_status", "status in ('running', 'failed', 'succeeded', 'cancelled')");
                    table.ForeignKey(
                        name: "FK_control_action_attempt_control_action_action_id",
                        column: x => x.action_id,
                        principalTable: "control_action",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "control_audit_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    actor_admin_user_id = table.Column<long>(type: "bigint", nullable: true),
                    actor_identity_user_id = table.Column<string>(type: "text", nullable: true),
                    actor_email = table.Column<string>(type: "text", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_public_id = table.Column<string>(type: "text", nullable: true),
                    node_id = table.Column<long>(type: "bigint", nullable: true),
                    store_id = table.Column<long>(type: "bigint", nullable: true),
                    control_action_id = table.Column<long>(type: "bigint", nullable: true),
                    result = table.Column<string>(type: "text", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_audit_log", x => x.id);
                    table.CheckConstraint("ck_control_audit_log_result", "result in ('success', 'failure', 'denied')");
                    table.ForeignKey(
                        name: "FK_control_audit_log_commerce_node_node_id",
                        column: x => x.node_id,
                        principalTable: "commerce_node",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_control_audit_log_control_action_control_action_id",
                        column: x => x.control_action_id,
                        principalTable: "control_action",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_control_audit_log_control_plane_admin_user_actor_admin_user~",
                        column: x => x.actor_admin_user_id,
                        principalTable: "control_plane_admin_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_control_audit_log_store_registry_store_id",
                        column: x => x.store_id,
                        principalTable: "store_registry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "control_plane_permission",
                columns: new[] { "id", "created_at", "description", "key" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Read node registry.", "nodes.read" },
                    { 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Create, update, and disable nodes.", "nodes.write" },
                    { 3L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Create, revoke, and rotate node credentials.", "credentials.rotate" },
                    { 4L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Read store registry metadata.", "stores.read" },
                    { 5L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Create, update, archive, and assign stores.", "stores.write" },
                    { 6L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Read node health and capability snapshots.", "health.read" },
                    { 7L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Read control action state and attempts.", "actions.read" },
                    { 8L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Read audit logs.", "audit.read" }
                });

            migrationBuilder.InsertData(
                table: "control_plane_role",
                columns: new[] { "id", "created_at", "description", "is_system", "key", "name", "updated_at" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Full Control Plane access.", true, "platform_owner", "Platform Owner", new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Manage nodes, stores, credentials, and health operations.", true, "node_operator", "Node Operator", new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Read-only audit and operations access.", true, "auditor", "Auditor", new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "control_plane_role_permission",
                columns: new[] { "permission_id", "role_id", "created_at" },
                values: new object[,]
                {
                    { 1L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 7L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 8L, 1L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 1L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 2L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 3L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 5L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 7L, 2L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 1L, 3L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 4L, 3L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 6L, 3L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 7L, 3L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { 8L, 3L, new DateTimeOffset(new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.CreateIndex(
                name: "commerce_node_active_node_key_uq",
                table: "commerce_node",
                column: "node_key",
                unique: true,
                filter: "disabled_at is null");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_node_public_id",
                table: "commerce_node",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commerce_node_status",
                table: "commerce_node",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "commerce_node_credential_active_node_idx",
                table: "commerce_node_credential",
                columns: new[] { "node_id", "status" },
                filter: "revoked_at is null");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_node_credential_created_by_admin_user_id",
                table: "commerce_node_credential",
                column: "created_by_admin_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_node_credential_key_id",
                table: "commerce_node_credential",
                column: "key_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commerce_node_credential_node_id",
                table: "commerce_node_credential",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_node_credential_revoked_by_admin_user_id",
                table: "commerce_node_credential",
                column: "revoked_by_admin_user_id");

            migrationBuilder.CreateIndex(
                name: "commerce_node_endpoint_active_kind_idx",
                table: "commerce_node_endpoint",
                columns: new[] { "node_id", "kind" },
                filter: "disabled_at is null");

            migrationBuilder.CreateIndex(
                name: "IX_commerce_node_endpoint_node_id",
                table: "commerce_node_endpoint",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "IX_control_action_node_id",
                table: "control_action",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "IX_control_action_node_id_idempotency_key",
                table: "control_action",
                columns: new[] { "node_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_control_action_public_id",
                table: "control_action",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_control_action_status_created_at",
                table: "control_action",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_control_action_store_id",
                table: "control_action",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_control_action_attempt_action_id",
                table: "control_action_attempt",
                column: "action_id");

            migrationBuilder.CreateIndex(
                name: "IX_control_action_attempt_action_id_attempt_number",
                table: "control_action_attempt",
                columns: new[] { "action_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_control_audit_log_action_created_at",
                table: "control_audit_log",
                columns: new[] { "action", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_control_audit_log_actor_admin_user_id",
                table: "control_audit_log",
                column: "actor_admin_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_control_audit_log_actor_email_created_at",
                table: "control_audit_log",
                columns: new[] { "actor_email", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_control_audit_log_control_action_id",
                table: "control_audit_log",
                column: "control_action_id");

            migrationBuilder.CreateIndex(
                name: "IX_control_audit_log_created_at",
                table: "control_audit_log",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_control_audit_log_node_id",
                table: "control_audit_log",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "IX_control_audit_log_public_id",
                table: "control_audit_log",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_control_audit_log_store_id",
                table: "control_audit_log",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "control_plane_admin_user_active_email_uq",
                table: "control_plane_admin_user",
                column: "email",
                unique: true,
                filter: "deleted_at is null");

            migrationBuilder.CreateIndex(
                name: "IX_control_plane_admin_user_identity_user_id",
                table: "control_plane_admin_user",
                column: "identity_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_control_plane_admin_user_role_role_id",
                table: "control_plane_admin_user_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_control_plane_permission_key",
                table: "control_plane_permission",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_control_plane_role_key",
                table: "control_plane_role",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_control_plane_role_permission_permission_id",
                table: "control_plane_role_permission",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_node_capability_snapshot_node_id",
                table: "node_capability_snapshot",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "IX_node_capability_snapshot_node_id_checksum",
                table: "node_capability_snapshot",
                columns: new[] { "node_id", "checksum" });

            migrationBuilder.CreateIndex(
                name: "IX_node_capability_snapshot_public_id",
                table: "node_capability_snapshot",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "node_capability_snapshot_current_idx",
                table: "node_capability_snapshot",
                columns: new[] { "node_id", "is_current" },
                filter: "is_current");

            migrationBuilder.CreateIndex(
                name: "IX_node_health_snapshot_node_id",
                table: "node_health_snapshot",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "IX_node_health_snapshot_node_id_checked_at",
                table: "node_health_snapshot",
                columns: new[] { "node_id", "checked_at" });

            migrationBuilder.CreateIndex(
                name: "IX_node_health_snapshot_public_id",
                table: "node_health_snapshot",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_node_health_snapshot_status_checked_at",
                table: "node_health_snapshot",
                columns: new[] { "status", "checked_at" });

            migrationBuilder.CreateIndex(
                name: "IX_store_domain_registry_store_id",
                table: "store_domain_registry",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "store_domain_registry_active_domain_uq",
                table: "store_domain_registry",
                column: "normalized_domain",
                unique: true,
                filter: "disabled_at is null");

            migrationBuilder.CreateIndex(
                name: "IX_store_registry_node_id",
                table: "store_registry",
                column: "node_id");

            migrationBuilder.CreateIndex(
                name: "IX_store_registry_public_id",
                table: "store_registry",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "store_registry_active_node_store_key_uq",
                table: "store_registry",
                columns: new[] { "node_id", "store_key" },
                unique: true,
                filter: "archived_at is null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commerce_node_credential");

            migrationBuilder.DropTable(
                name: "commerce_node_endpoint");

            migrationBuilder.DropTable(
                name: "control_action_attempt");

            migrationBuilder.DropTable(
                name: "control_audit_log");

            migrationBuilder.DropTable(
                name: "control_plane_admin_user_role");

            migrationBuilder.DropTable(
                name: "control_plane_role_permission");

            migrationBuilder.DropTable(
                name: "node_capability_snapshot");

            migrationBuilder.DropTable(
                name: "node_health_snapshot");

            migrationBuilder.DropTable(
                name: "store_domain_registry");

            migrationBuilder.DropTable(
                name: "control_action");

            migrationBuilder.DropTable(
                name: "control_plane_admin_user");

            migrationBuilder.DropTable(
                name: "control_plane_permission");

            migrationBuilder.DropTable(
                name: "control_plane_role");

            migrationBuilder.DropTable(
                name: "store_registry");

            migrationBuilder.DropTable(
                name: "commerce_node");
        }
    }
}
