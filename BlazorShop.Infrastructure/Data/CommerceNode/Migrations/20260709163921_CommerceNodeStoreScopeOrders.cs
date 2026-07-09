using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStoreScopeOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_UserId_CreatedOn",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_NewsletterSubscribers_Email",
                table: "NewsletterSubscribers");

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "NewsletterSubscribers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "CheckoutOrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Orders"
                SET "StoreId" = (
                    SELECT id
                    FROM commerce_store
                    WHERE store_key = 'default' AND archived_at IS NULL
                    ORDER BY created_at
                    LIMIT 1
                )
                WHERE "StoreId" IS NULL;

                UPDATE "NewsletterSubscribers"
                SET "StoreId" = (
                    SELECT id
                    FROM commerce_store
                    WHERE store_key = 'default' AND archived_at IS NULL
                    ORDER BY created_at
                    LIMIT 1
                )
                WHERE "StoreId" IS NULL;

                UPDATE "CheckoutOrderItems"
                SET "StoreId" = (
                    SELECT id
                    FROM commerce_store
                    WHERE store_key = 'default' AND archived_at IS NULL
                    ORDER BY created_at
                    LIMIT 1
                )
                WHERE "StoreId" IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StoreId",
                table: "Orders",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StoreId_UserId_CreatedOn",
                table: "Orders",
                columns: new[] { "StoreId", "UserId", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscribers_StoreId",
                table: "NewsletterSubscribers",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscribers_StoreId_Email",
                table: "NewsletterSubscribers",
                columns: new[] { "StoreId", "Email" },
                unique: true,
                filter: "\"StoreId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutOrderItems_StoreId_UserId_CreatedOn",
                table: "CheckoutOrderItems",
                columns: new[] { "StoreId", "UserId", "CreatedOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_StoreId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_StoreId_UserId_CreatedOn",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_NewsletterSubscribers_StoreId",
                table: "NewsletterSubscribers");

            migrationBuilder.DropIndex(
                name: "IX_NewsletterSubscribers_StoreId_Email",
                table: "NewsletterSubscribers");

            migrationBuilder.DropIndex(
                name: "IX_CheckoutOrderItems_StoreId_UserId_CreatedOn",
                table: "CheckoutOrderItems");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "NewsletterSubscribers");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "CheckoutOrderItems");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_CreatedOn",
                table: "Orders",
                columns: new[] { "UserId", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsletterSubscribers_Email",
                table: "NewsletterSubscribers",
                column: "Email",
                unique: true);
        }
    }
}
