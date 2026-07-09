using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStoreScopeCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Slug",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                table: "Categories");

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "Products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "Categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Categories"
                SET "StoreId" = (
                    SELECT id
                    FROM commerce_store
                    WHERE store_key = 'default' AND archived_at IS NULL
                    ORDER BY created_at
                    LIMIT 1
                )
                WHERE "StoreId" IS NULL;

                UPDATE "Products" AS product
                SET "StoreId" = category."StoreId"
                FROM "Categories" AS category
                WHERE product."CategoryId" = category."Id"
                    AND product."StoreId" IS NULL
                    AND category."StoreId" IS NOT NULL;

                UPDATE "Products"
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
                name: "IX_Products_StoreId",
                table: "Products",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_Slug",
                table: "Products",
                columns: new[] { "StoreId", "Slug" },
                unique: true,
                filter: "\"StoreId\" IS NOT NULL AND \"Slug\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_StoreId",
                table: "Categories",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_StoreId_Slug",
                table: "Categories",
                columns: new[] { "StoreId", "Slug" },
                unique: true,
                filter: "\"StoreId\" IS NOT NULL AND \"Slug\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_Slug",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_StoreId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_StoreId_Slug",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);
        }
    }
}
