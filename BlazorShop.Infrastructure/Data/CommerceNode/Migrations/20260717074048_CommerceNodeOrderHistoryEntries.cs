using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeOrderHistoryEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_history_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    old_value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    new_value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    visible_to_customer = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "system")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_history_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_history_entries_Orders_order_id",
                        column: x => x.order_id,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_history_entries_order_id",
                table: "order_history_entries",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_history_entries_store_id_event_type_created_at_utc",
                table: "order_history_entries",
                columns: new[] { "store_id", "event_type", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_order_history_entries_store_id_order_id_created_at_utc",
                table: "order_history_entries",
                columns: new[] { "store_id", "order_id", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_history_entries");
        }
    }
}
