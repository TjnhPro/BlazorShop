using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorShop.Infrastructure.Data.CommerceNode.Migrations
{
    /// <inheritdoc />
    public partial class CommerceNodeCurrencyRateSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "base_amount",
                table: "payment_attempts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "base_currency_code",
                table: "payment_attempts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "exchange_rate",
                table: "payment_attempts",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "exchange_rate_effective_at_utc",
                table: "payment_attempts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "exchange_rate_expires_at_utc",
                table: "payment_attempts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "exchange_rate_provider_key",
                table: "payment_attempts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "exchange_rate_source",
                table: "payment_attempts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BaseCurrencyCode",
                table: "Orders",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseTotalAmount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "Orders",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExchangeRateEffectiveAtUtc",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExchangeRateExpiresAtUtc",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExchangeRateProviderKey",
                table: "Orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExchangeRateSource",
                table: "Orders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseLineTotal",
                table: "OrderLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseUnitPrice",
                table: "OrderLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConvertedUnitPrice",
                table: "OrderLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "OrderLines",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LineTotal",
                table: "OrderLines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "base_currency_code",
                table: "checkout_sessions",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_grand_total",
                table: "checkout_sessions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_subtotal",
                table: "checkout_sessions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "exchange_rate",
                table: "checkout_sessions",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "exchange_rate_effective_at_utc",
                table: "checkout_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "exchange_rate_expires_at_utc",
                table: "checkout_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "exchange_rate_provider_key",
                table: "checkout_sessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "exchange_rate_source",
                table: "checkout_sessions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "base_currency_code_snapshot",
                table: "cart_lines",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_unit_price_snapshot",
                table: "cart_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "exchange_rate_effective_at_utc",
                table: "cart_lines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "exchange_rate_expires_at_utc",
                table: "cart_lines",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "exchange_rate_provider_key",
                table: "cart_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "exchange_rate_snapshot",
                table: "cart_lines",
                type: "numeric(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "exchange_rate_source",
                table: "cart_lines",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "base_amount",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "base_currency_code",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "exchange_rate",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "exchange_rate_effective_at_utc",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "exchange_rate_expires_at_utc",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "exchange_rate_provider_key",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "exchange_rate_source",
                table: "payment_attempts");

            migrationBuilder.DropColumn(
                name: "BaseCurrencyCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BaseTotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExchangeRateEffectiveAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExchangeRateExpiresAtUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExchangeRateProviderKey",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExchangeRateSource",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BaseLineTotal",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "BaseUnitPrice",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "ConvertedUnitPrice",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "LineTotal",
                table: "OrderLines");

            migrationBuilder.DropColumn(
                name: "base_currency_code",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "base_grand_total",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "base_subtotal",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "exchange_rate",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "exchange_rate_effective_at_utc",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "exchange_rate_expires_at_utc",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "exchange_rate_provider_key",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "exchange_rate_source",
                table: "checkout_sessions");

            migrationBuilder.DropColumn(
                name: "base_currency_code_snapshot",
                table: "cart_lines");

            migrationBuilder.DropColumn(
                name: "base_unit_price_snapshot",
                table: "cart_lines");

            migrationBuilder.DropColumn(
                name: "exchange_rate_effective_at_utc",
                table: "cart_lines");

            migrationBuilder.DropColumn(
                name: "exchange_rate_expires_at_utc",
                table: "cart_lines");

            migrationBuilder.DropColumn(
                name: "exchange_rate_provider_key",
                table: "cart_lines");

            migrationBuilder.DropColumn(
                name: "exchange_rate_snapshot",
                table: "cart_lines");

            migrationBuilder.DropColumn(
                name: "exchange_rate_source",
                table: "cart_lines");
        }
    }
}
