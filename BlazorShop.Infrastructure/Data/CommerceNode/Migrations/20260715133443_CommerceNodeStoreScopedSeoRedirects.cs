using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStoreScopedSeoRedirects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SeoRedirects_OldPath",
                table: "SeoRedirects");

            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "SeoRedirects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "SeoRedirects",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "SeoRedirects",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "SeoRedirects",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "SeoRedirects"
                SET "StoreId" = (
                    SELECT "id"
                    FROM "commerce_store"
                    WHERE "archived_at" IS NULL
                    ORDER BY "created_at", "id"
                    LIMIT 1
                )
                WHERE "StoreId" IS NULL
                  AND (
                    SELECT COUNT(*)
                    FROM "commerce_store"
                    WHERE "archived_at" IS NULL
                  ) = 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SeoRedirects_StoreId_EntityType_EntityId",
                table: "SeoRedirects",
                columns: new[] { "StoreId", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SeoRedirects_StoreId_IsActive_OldPath",
                table: "SeoRedirects",
                columns: new[] { "StoreId", "IsActive", "OldPath" });

            migrationBuilder.CreateIndex(
                name: "IX_SeoRedirects_StoreId_OldPath",
                table: "SeoRedirects",
                columns: new[] { "StoreId", "OldPath" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"StoreId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_SeoRedirects_commerce_store_StoreId",
                table: "SeoRedirects",
                column: "StoreId",
                principalTable: "commerce_store",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeoRedirects_commerce_store_StoreId",
                table: "SeoRedirects");

            migrationBuilder.DropIndex(
                name: "IX_SeoRedirects_StoreId_EntityType_EntityId",
                table: "SeoRedirects");

            migrationBuilder.DropIndex(
                name: "IX_SeoRedirects_StoreId_IsActive_OldPath",
                table: "SeoRedirects");

            migrationBuilder.DropIndex(
                name: "IX_SeoRedirects_StoreId_OldPath",
                table: "SeoRedirects");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "SeoRedirects");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "SeoRedirects");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "SeoRedirects");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "SeoRedirects");

            migrationBuilder.CreateIndex(
                name: "IX_SeoRedirects_OldPath",
                table: "SeoRedirects",
                column: "OldPath",
                unique: true);
        }
    }
}
