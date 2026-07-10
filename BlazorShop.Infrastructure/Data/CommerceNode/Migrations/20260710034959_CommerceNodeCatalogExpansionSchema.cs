using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeCatalogExpansionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_SizeScale_SizeValue",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_Slug",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_StoreId_Slug",
                table: "Categories");

            migrationBuilder.AddColumn<string>(
                name: "AttributeSignature",
                table: "ProductVariants",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttributesJson",
                table: "ProductVariants",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "ProductVariants",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ComparePrice",
                table: "Products",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FullDescription",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "Products",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "OrderLines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "OrderLines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductVariantId",
                table: "OrderLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "OrderLines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantAttributesJson",
                table: "OrderLines",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentCategoryId",
                table: "Categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Categories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.Sql(
                """
                UPDATE "Products"
                SET "FullDescription" = COALESCE("FullDescription", "Description"),
                    "UpdatedAt" = COALESCE("CreatedOn", "UpdatedAt");
                """);

            migrationBuilder.Sql(
                """
                UPDATE "ProductVariants"
                SET "AttributesJson" = CASE
                        WHEN NULLIF(BTRIM("Color"), '') IS NOT NULL AND NULLIF(BTRIM("SizeValue"), '') IS NOT NULL THEN
                            jsonb_build_array(
                                jsonb_build_object('name', 'Color', 'value', BTRIM("Color")),
                                jsonb_build_object('name', 'Size', 'value', BTRIM("SizeValue")))
                        WHEN NULLIF(BTRIM("Color"), '') IS NOT NULL THEN
                            jsonb_build_array(jsonb_build_object('name', 'Color', 'value', BTRIM("Color")))
                        WHEN NULLIF(BTRIM("SizeValue"), '') IS NOT NULL THEN
                            jsonb_build_array(jsonb_build_object('name', 'Size', 'value', BTRIM("SizeValue")))
                        ELSE NULL
                    END,
                    "AttributeSignature" = CASE
                        WHEN NULLIF(BTRIM("Color"), '') IS NOT NULL AND NULLIF(BTRIM("SizeValue"), '') IS NOT NULL THEN
                            'color=' || LOWER(BTRIM("Color")) || '|size=' || LOWER(BTRIM("SizeValue"))
                        WHEN NULLIF(BTRIM("Color"), '') IS NOT NULL THEN
                            'color=' || LOWER(BTRIM("Color"))
                        WHEN NULLIF(BTRIM("SizeValue"), '') IS NOT NULL THEN
                            'size=' || LOWER(BTRIM("SizeValue"))
                        ELSE NULL
                    END,
                    "DisplayName" = CASE
                        WHEN NULLIF(BTRIM("Color"), '') IS NOT NULL AND NULLIF(BTRIM("SizeValue"), '') IS NOT NULL THEN
                            BTRIM("Color") || ' / ' || BTRIM("SizeValue")
                        WHEN NULLIF(BTRIM("Color"), '') IS NOT NULL THEN
                            BTRIM("Color")
                        WHEN NULLIF(BTRIM("SizeValue"), '') IS NOT NULL THEN
                            BTRIM("SizeValue")
                        ELSE NULL
                    END;
                """);

            migrationBuilder.Sql(
                """
                WITH ranked_defaults AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (PARTITION BY "ProductId" ORDER BY "Id") AS rn
                    FROM "ProductVariants"
                    WHERE "IsDefault" = TRUE
                )
                UPDATE "ProductVariants" variant
                SET "IsDefault" = FALSE
                FROM ranked_defaults ranked
                WHERE variant."Id" = ranked."Id"
                  AND ranked.rn > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId",
                unique: true,
                filter: "\"IsDefault\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_AttributeSignature",
                table: "ProductVariants",
                columns: new[] { "ProductId", "AttributeSignature" },
                unique: true,
                filter: "\"AttributeSignature\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_CategoryId_DisplayOrder_CreatedOn",
                table: "Products",
                columns: new[] { "StoreId", "CategoryId", "DisplayOrder", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_IsPublished_ArchivedAt",
                table: "Products",
                columns: new[] { "StoreId", "IsPublished", "ArchivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_Sku",
                table: "Products",
                columns: new[] { "StoreId", "Sku" },
                unique: true,
                filter: "\"StoreId\" IS NOT NULL AND \"Sku\" IS NOT NULL AND \"ArchivedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_Slug",
                table: "Products",
                columns: new[] { "StoreId", "Slug" },
                unique: true,
                filter: "\"StoreId\" IS NOT NULL AND \"Slug\" IS NOT NULL AND \"ArchivedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_ProductVariantId",
                table: "OrderLines",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_StoreId_IsPublished_ArchivedAt",
                table: "Categories",
                columns: new[] { "StoreId", "IsPublished", "ArchivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_StoreId_ParentCategoryId_DisplayOrder",
                table: "Categories",
                columns: new[] { "StoreId", "ParentCategoryId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_StoreId_Slug",
                table: "Categories",
                columns: new[] { "StoreId", "Slug" },
                unique: true,
                filter: "\"StoreId\" IS NOT NULL AND \"Slug\" IS NOT NULL AND \"ArchivedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId",
                table: "Categories",
                column: "ParentCategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderLines_ProductVariants_ProductVariantId",
                table: "OrderLines",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Categories_ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderLines_ProductVariants_ProductVariantId",
                table: "OrderLines");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_AttributeSignature",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_CategoryId_DisplayOrder_CreatedOn",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_IsPublished_ArchivedAt",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_Sku",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId_Slug",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_OrderLines_ProductVariantId",
                table: "OrderLines");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_StoreId_IsPublished_ArchivedAt",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_StoreId_ParentCategoryId_DisplayOrder",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_StoreId_Slug",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "AttributeSignature",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "AttributesJson",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ComparePrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FullDescription",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "VariantAttributesJson",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_SizeScale_SizeValue",
                table: "ProductVariants",
                columns: new[] { "ProductId", "SizeScale", "SizeValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId_Slug",
                table: "Products",
                columns: new[] { "StoreId", "Slug" },
                unique: true,
                filter: "\"StoreId\" IS NOT NULL AND \"Slug\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_StoreId_Slug",
                table: "Categories",
                columns: new[] { "StoreId", "Slug" },
                unique: true,
                filter: "\"StoreId\" IS NOT NULL AND \"Slug\" IS NOT NULL");
        }
    }
}
