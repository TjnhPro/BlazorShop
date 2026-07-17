using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeProductShippingSurcharge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ShippingSurcharge",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingSurcharge",
                table: "Products");
        }
    }
}
