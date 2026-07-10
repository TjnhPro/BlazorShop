using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    [Migration("20260710120000_CommerceNodeCatalogSearchMvp")]
    public partial class CommerceNodeCatalogSearchMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS ix_products_name_fts_simple
                ON "Products"
                USING GIN (to_tsvector('simple', coalesce("Name", '')))
                WHERE "ArchivedAt" IS NULL
                  AND "IsPublished" = TRUE
                  AND "PublishedOn" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_products_name_fts_simple;");
        }
    }
}
