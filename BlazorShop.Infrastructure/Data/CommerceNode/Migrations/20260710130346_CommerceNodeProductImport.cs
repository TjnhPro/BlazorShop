using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeProductImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_import_job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_public_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    stored_file_path = table.Column<string>(type: "text", nullable: false),
                    file_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    total_rows = table.Column<int>(type: "integer", nullable: false),
                    created_count = table.Column<int>(type: "integer", nullable: false),
                    updated_count = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false),
                    skipped_count = table.Column<int>(type: "integer", nullable: false),
                    media_queued_count = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_import_job", x => x.id);
                    table.CheckConstraint("ck_product_import_job_mode", "mode in ('create_only', 'upsert')");
                    table.CheckConstraint("ck_product_import_job_status", "status in ('Queued', 'Running', 'Completed', 'CompletedWithErrors', 'Failed')");
                });

            migrationBuilder.CreateTable(
                name: "product_import_row",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    media_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    media_task_public_id = table.Column<Guid>(type: "uuid", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    error_json = table.Column<string>(type: "jsonb", nullable: true),
                    raw_data_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_import_row", x => x.id);
                    table.CheckConstraint("ck_product_import_row_action", "action in ('Created', 'Updated', 'Skipped', 'Failed')");
                    table.CheckConstraint("ck_product_import_row_media_status", "media_status in ('None', 'Queued')");
                    table.CheckConstraint("ck_product_import_row_status", "status in ('Pending', 'Succeeded', 'Failed', 'Skipped')");
                    table.ForeignKey(
                        name: "FK_product_import_row_product_import_job_job_id",
                        column: x => x.job_id,
                        principalTable: "product_import_job",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_import_job_public_id",
                table: "product_import_job",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_import_job_store_id_mode_file_hash",
                table: "product_import_job",
                columns: new[] { "store_id", "mode", "file_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_import_job_store_id_status_created_at",
                table: "product_import_job",
                columns: new[] { "store_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_product_import_job_task_public_id",
                table: "product_import_job",
                column: "task_public_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_import_row_job_id_row_number",
                table: "product_import_row",
                columns: new[] { "job_id", "row_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_import_row_job_id_sku",
                table: "product_import_row",
                columns: new[] { "job_id", "sku" });

            migrationBuilder.CreateIndex(
                name: "IX_product_import_row_job_id_status",
                table: "product_import_row",
                columns: new[] { "job_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_product_import_row_media_task_public_id",
                table: "product_import_row",
                column: "media_task_public_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_import_row_product_id",
                table: "product_import_row",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_import_row");

            migrationBuilder.DropTable(
                name: "product_import_job");
        }
    }
}
