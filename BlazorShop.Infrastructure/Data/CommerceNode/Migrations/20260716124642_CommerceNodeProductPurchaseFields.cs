using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeProductPurchaseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryEstimateText",
                table: "Products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FreeShipping",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HideWhenOutOfStock",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ManageStock",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxOrderQuantity",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinOrderQuantity",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "PurchasingDisabled",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PurchasingDisabledReason",
                table: "Products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityStep",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "ShippingRequired",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryEstimateText",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "FreeShipping",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HideWhenOutOfStock",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ManageStock",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaxOrderQuantity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MinOrderQuantity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PurchasingDisabled",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PurchasingDisabledReason",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "QuantityStep",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShippingRequired",
                table: "Products");
        }
    }
}
