using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeStoreLifecycleProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_commerce_store_status",
                table: "commerce_store");

            migrationBuilder.AddColumn<string>(
                name: "company_address",
                table: "commerce_store",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_email",
                table: "commerce_store",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_name",
                table: "commerce_store",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_phone",
                table: "commerce_store",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE commerce_store
                SET
                    company_name = COALESCE(company_name, NULLIF(seo."CompanyName", '')),
                    company_email = COALESCE(company_email, NULLIF(seo."CompanyEmail", '')),
                    company_phone = COALESCE(company_phone, NULLIF(seo."CompanyPhone", '')),
                    company_address = COALESCE(company_address, NULLIF(seo."CompanyAddress", ''))
                FROM "SeoSettings" seo
                WHERE seo."Id" = (SELECT settings."Id" FROM "SeoSettings" settings ORDER BY settings."Id" LIMIT 1);
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_commerce_store_status",
                table: "commerce_store",
                sql: "status in ('active', 'provisioning', 'disabled', 'archived')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_commerce_store_status",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "company_address",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "company_email",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "company_name",
                table: "commerce_store");

            migrationBuilder.DropColumn(
                name: "company_phone",
                table: "commerce_store");

            migrationBuilder.Sql(
                """
                UPDATE commerce_store
                SET status = 'disabled'
                WHERE status = 'provisioning';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_commerce_store_status",
                table: "commerce_store",
                sql: "status in ('active', 'disabled', 'archived')");
        }
    }
}
