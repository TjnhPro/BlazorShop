using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeVariationOptionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color_hex",
                table: "variation_template_values",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "control_type",
                table: "variation_template_options",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "dropdown");

            migrationBuilder.AddColumn<bool>(
                name: "is_required",
                table: "variation_template_options",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_variation_template_option_control_type",
                table: "variation_template_options",
                sql: "control_type in ('dropdown', 'radio', 'color')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_variation_template_option_control_type",
                table: "variation_template_options");

            migrationBuilder.DropColumn(
                name: "color_hex",
                table: "variation_template_values");

            migrationBuilder.DropColumn(
                name: "control_type",
                table: "variation_template_options");

            migrationBuilder.DropColumn(
                name: "is_required",
                table: "variation_template_options");
        }
    }
}
