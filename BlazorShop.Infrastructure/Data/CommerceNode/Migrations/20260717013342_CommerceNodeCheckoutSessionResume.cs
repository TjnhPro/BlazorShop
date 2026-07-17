using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeCheckoutSessionResume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "checkout_version",
                table: "checkout_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "completed_steps_json",
                table: "checkout_sessions",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "current_step",
                table: "checkout_sessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "entry");

            migrationBuilder.AddColumn<int>(
                name: "last_validated_cart_version",
                table: "checkout_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE checkout_sessions
                SET
                    last_validated_cart_version = cart_version,
                    current_step = CASE
                        WHEN state = 'completed' THEN 'complete'
                        WHEN state = 'order_pending' THEN 'place_order'
                        WHEN state = 'ready' THEN 'review'
                        WHEN state = 'draft' THEN 'shipping_address'
                        ELSE 'entry'
                    END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "checkout_version",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "completed_steps_json",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "current_step",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "last_validated_cart_version",
                table: "checkout_sessions");
        }
    }
}
