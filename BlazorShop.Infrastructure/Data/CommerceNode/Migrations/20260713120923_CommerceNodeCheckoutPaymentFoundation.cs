using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeCheckoutPaymentFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Orders",
                newName: "order_status");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PaymentMethods",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PaymentMethods",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabledByDefault",
                table: "PaymentMethods",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "PaymentMethods",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "PaymentMethods",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "completed_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_email",
                table: "Orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_name",
                table: "Orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "order_status",
                table: "Orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "pending",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "payment_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_metadata_json",
                table: "Orders",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_method_key",
                table: "Orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "cod");

            migrationBuilder.AddColumn<string>(
                name: "payment_status",
                table: "Orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "pending");

            migrationBuilder.AddColumn<string>(
                name: "shipping_address1",
                table: "Orders",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_address2",
                table: "Orders",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_city",
                table: "Orders",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_country_code",
                table: "Orders",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_email",
                table: "Orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_full_name",
                table: "Orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_phone",
                table: "Orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_postal_code",
                table: "Orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "shipping_state",
                table: "Orders",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateTable(
                name: "store_payment_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    settings_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_payment_methods", x => x.id);
                    table.CheckConstraint("ck_store_payment_methods_key", "payment_method_key in ('cod', 'stripe', 'paypal')");
                    table.ForeignKey(
                        name: "FK_store_payment_methods_commerce_store_store_id",
                        column: x => x.store_id,
                        principalTable: "commerce_store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: new Guid("3604fc1d-cd6a-46ad-ace4-9b5f8e03f43b"),
                columns: new[] { "Description", "IsEnabledByDefault", "Key", "Name", "SortOrder" },
                values: new object[] { "Card payments through Stripe.", false, "stripe", "Stripe", 20 });

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: new Guid("6f2c2a7e-9f9b-4a0d-9f7f-2a1b3c4d5e6f"),
                columns: new[] { "Description", "IsEnabledByDefault", "Key", "SortOrder" },
                values: new object[] { "Test checkout payment method for MVP.", true, "cod", 10 });

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: new Guid("b2e5c1d4-7a9f-4d2c-8f1e-3a4b5c6d7e8f"),
                columns: new[] { "Description", "IsEnabledByDefault", "Key", "Name", "SortOrder" },
                values: new object[] { "PayPal payment skeleton.", false, "paypal", "PayPal", 30 });

            migrationBuilder.Sql(
                """
                UPDATE "Orders"
                SET payment_status = CASE
                    WHEN lower(order_status) IN ('paid', 'completed', 'complete', 'processing') THEN 'paid'
                    ELSE 'pending'
                END;

                UPDATE "Orders"
                SET payment_at = "CreatedOn"
                WHERE payment_status = 'paid' AND payment_at IS NULL;

                UPDATE "Orders"
                SET order_status = CASE
                    WHEN lower(order_status) = 'paid' THEN 'processing'
                    WHEN lower(order_status) IN ('completed', 'complete') THEN 'complete'
                    WHEN lower(order_status) IN ('cancelled', 'canceled') THEN 'cancelled'
                    WHEN lower(order_status) = 'processing' THEN 'processing'
                    ELSE 'pending'
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_Key",
                table: "PaymentMethods",
                column: "Key",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_payment_methods_key",
                table: "PaymentMethods",
                sql: "\"Key\" in ('cod', 'stripe', 'paypal')");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_payment_method_key",
                table: "Orders",
                column: "payment_method_key");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StoreId_customer_email_CreatedOn",
                table: "Orders",
                columns: new[] { "StoreId", "customer_email", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StoreId_order_status_CreatedOn",
                table: "Orders",
                columns: new[] { "StoreId", "order_status", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StoreId_payment_status_CreatedOn",
                table: "Orders",
                columns: new[] { "StoreId", "payment_status", "CreatedOn" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_order_status",
                table: "Orders",
                sql: "order_status in ('pending', 'processing', 'complete', 'cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_payment_method_key",
                table: "Orders",
                sql: "payment_method_key in ('cod', 'stripe', 'paypal')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_payment_status",
                table: "Orders",
                sql: "payment_status in ('pending', 'authorized', 'paid', 'partially_refunded', 'refunded', 'voided')");

            migrationBuilder.CreateIndex(
                name: "IX_store_payment_methods_store_id_enabled_display_order",
                table: "store_payment_methods",
                columns: new[] { "store_id", "enabled", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_store_payment_methods_store_id_payment_method_key",
                table: "store_payment_methods",
                columns: new[] { "store_id", "payment_method_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_payment_methods");

            migrationBuilder.DropIndex(
                name: "IX_PaymentMethods_Key",
                table: "PaymentMethods");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payment_methods_key",
                table: "PaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_Orders_payment_method_key",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_StoreId_customer_email_CreatedOn",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_StoreId_order_status_CreatedOn",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_StoreId_payment_status_CreatedOn",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_order_status",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_payment_method_key",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_payment_status",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "IsEnabledByDefault",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "customer_email",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "customer_name",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "order_status",
                table: "Orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldDefaultValue: "pending");

            migrationBuilder.DropColumn(
                name: "payment_at",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "payment_metadata_json",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "payment_method_key",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "payment_status",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_address1",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_address2",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_city",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_country_code",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_email",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_full_name",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_phone",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_postal_code",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "shipping_state",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "PaymentMethods",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160);

            migrationBuilder.RenameColumn(
                name: "order_status",
                table: "Orders",
                newName: "Status");

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: new Guid("3604fc1d-cd6a-46ad-ace4-9b5f8e03f43b"),
                column: "Name",
                value: "Credit Card");

            migrationBuilder.UpdateData(
                table: "PaymentMethods",
                keyColumn: "Id",
                keyValue: new Guid("b2e5c1d4-7a9f-4d2c-8f1e-3a4b5c6d7e8f"),
                column: "Name",
                value: "Bank Transfer");
        }
    }
}
