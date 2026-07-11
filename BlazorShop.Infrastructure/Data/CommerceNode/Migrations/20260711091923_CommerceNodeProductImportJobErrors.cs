using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeProductImportJobErrors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "error_json",
                table: "product_import_job",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_message",
                table: "product_import_job",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "error_json",
                table: "product_import_job");

            migrationBuilder.DropColumn(
                name: "error_message",
                table: "product_import_job");
        }
    }
}
