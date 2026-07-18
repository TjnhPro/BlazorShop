using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeOrderPlacedEmailTemplateHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "message_templates",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-000000000003"),
                columns: new[] { "body_html_template", "subject_template" },
                values: new object[] { "<p>Thanks for your order from {{Store.Name}}.</p><p>Order: {{Order.Reference}}</p><p>Total: {{Order.Total}} {{Order.Currency}}</p><p>View your order: <a href=\"{{Order.DetailUrl}}\">{{Order.DetailUrl}}</a></p>", "{{Store.Name}} order {{Order.Reference}} confirmed" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "message_templates",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-000000000003"),
                columns: new[] { "body_html_template", "subject_template" },
                values: new object[] { "<p>Thanks for your order {{Order.Reference}}.</p><p>Total: {{Order.Total}} {{Order.Currency}}</p>", "Order {{Order.Reference}} confirmed" });
        }
    }
}
