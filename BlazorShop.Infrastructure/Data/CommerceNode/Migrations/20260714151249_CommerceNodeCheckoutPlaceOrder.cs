using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeCheckoutPlaceOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ArtworkAssetId",
                table: "OrderLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArtworkVersion",
                table: "OrderLines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FulfillmentProviderKey",
                table: "OrderLines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalizationHash",
                table: "OrderLines",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalizationJson",
                table: "OrderLines",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "checkout_sessions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "order_id",
                table: "checkout_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "placed_at_utc",
                table: "checkout_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_ArtworkAssetId",
                table: "OrderLines",
                column: "ArtworkAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_order_id",
                table: "checkout_sessions",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_checkout_sessions_store_id_idempotency_key",
                table: "checkout_sessions",
                columns: new[] { "store_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_checkout_sessions_Orders_order_id",
                table: "checkout_sessions",
                column: "order_id",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_checkout_sessions_Orders_order_id",
                table: "checkout_sessions");

            migrationBuilder.DropIndex(
                name: "IX_OrderLines_ArtworkAssetId",
                table: "OrderLines");

            migrationBuilder.DropIndex(
                name: "IX_checkout_sessions_order_id",
                table: "checkout_sessions");

            migrationBuilder.DropIndex(
                name: "IX_checkout_sessions_store_id_idempotency_key",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "ArtworkAssetId",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "ArtworkVersion",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "FulfillmentProviderKey",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "PersonalizationHash",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "PersonalizationJson",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "order_id",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "placed_at_utc",
                table: "checkout_sessions");
        }
    }
}
