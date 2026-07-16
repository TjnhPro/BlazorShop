using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    [Migration("20260716170000_CommerceNodeCatalogSearchPublicFields")]
    public partial class CommerceNodeCatalogSearchPublicFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS ix_products_public_search_fts_simple
                ON "Products"
                USING GIN (to_tsvector('simple',
                    coalesce("Name", '') || ' ' ||
                    coalesce("ShortDescription", '') || ' ' ||
                    coalesce("Description", '') || ' ' ||
                    coalesce("Sku", '')))
                WHERE "ArchivedAt" IS NULL
                  AND "IsPublished" = TRUE
                  AND "PublishedOn" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_products_public_search_fts_simple;");
        }
    }
}
