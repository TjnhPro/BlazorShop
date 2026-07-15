using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeRequiredCatalogStoreOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    active_store_count integer;
                    fallback_store_id uuid;
                BEGIN
                    SELECT COUNT(*)
                    INTO active_store_count
                    FROM commerce_store
                    WHERE archived_at IS NULL;

                    UPDATE "Products" AS product
                    SET "StoreId" = category."StoreId"
                    FROM "Categories" AS category
                    WHERE product."CategoryId" = category."Id"
                        AND product."StoreId" IS NULL
                        AND category."StoreId" IS NOT NULL;

                    IF EXISTS (SELECT 1 FROM "Categories" WHERE "StoreId" IS NULL)
                        OR EXISTS (SELECT 1 FROM "Products" WHERE "StoreId" IS NULL) THEN
                        IF active_store_count = 1 THEN
                            SELECT id
                            INTO fallback_store_id
                            FROM commerce_store
                            WHERE archived_at IS NULL
                            ORDER BY created_at, id
                            LIMIT 1;

                            UPDATE "Categories"
                            SET "StoreId" = fallback_store_id
                            WHERE "StoreId" IS NULL;

                            UPDATE "Products"
                            SET "StoreId" = fallback_store_id
                            WHERE "StoreId" IS NULL;
                        ELSE
                            RAISE EXCEPTION 'Cannot require Product/Category StoreId: null catalog rows exist and active CommerceStore count is %, so manual store mapping is required before applying this migration.', active_store_count;
                        END IF;
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "StoreId",
                table: "Products",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StoreId",
                table: "Categories",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_commerce_store_StoreId",
                table: "Categories",
                column: "StoreId",
                principalTable: "commerce_store",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_commerce_store_StoreId",
                table: "Products",
                column: "StoreId",
                principalTable: "commerce_store",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_commerce_store_StoreId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_commerce_store_StoreId",
                table: "Products");

            migrationBuilder.AlterColumn<Guid>(
                name: "StoreId",
                table: "Products",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "StoreId",
                table: "Categories",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
