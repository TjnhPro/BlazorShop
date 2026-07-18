namespace BlazorShop.ControlPlane.API.Controllers
{
    using System.Globalization;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.Security;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.ControlPlane.API.Responses;
    using BlazorShop.Domain.Contracts;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    public abstract class ControlPlaneCommerceGatewayControllerBase : ControllerBase
    {
        protected const string ProductImportTemplateHeader = "sku,name,slug,category_slug,product_type,variation_template_slug,price,compare_price,quantity,is_published,available_start_utc,available_end_utc,gtin,barcode,manufacturer_part_number,condition,weight,length,width,height,short_description,description,image_urls";

        protected static IActionResult ToActionResult<TPayload>(ControlPlaneCommerceCatalogResult<TPayload> result)
        {
            if (result.Success)
            {
                return ControlPlaneApiResponseWriter.Success(
                    StatusCodes.Status200OK,
                    result.Payload,
                    string.IsNullOrWhiteSpace(result.Message) ? "Catalog request completed." : result.Message);
            }

            return result.Failure switch
            {
                ControlPlaneCommerceCatalogFailure.NotFound => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status404NotFound, result.Message, result.Payload),
                ControlPlaneCommerceCatalogFailure.RemoteFailure => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status502BadGateway, result.Message, result.Payload),
                ControlPlaneCommerceCatalogFailure.Validation => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status400BadRequest, result.Message, result.Payload),
                _ => ControlPlaneApiResponseWriter.Failure<TPayload>(StatusCodes.Status400BadRequest, result.Message, result.Payload),
            };
        }

        protected static string BuildProductImportErrorCsv(ProductImportJobDto job, IReadOnlyList<ProductImportRowDto> rows)
        {
            var builder = new StringBuilder();
            builder.AppendLine("row_number,sku,status,error_column,error_message,error_json");
            if (rows.Count == 0 && !string.IsNullOrWhiteSpace(job.ErrorMessage))
            {
                builder.AppendLine(string.Join(
                    ",",
                    Csv(string.Empty),
                    Csv(string.Empty),
                    Csv(job.Status),
                    Csv("file"),
                    Csv(job.ErrorMessage),
                    Csv(job.ErrorJson)));
                return builder.ToString();
            }

            foreach (var row in rows)
            {
                var errors = ExtractErrors(row);
                if (errors.Count == 0)
                {
                    builder.AppendLine(string.Join(
                        ",",
                        Csv(row.RowNumber.ToString(CultureInfo.InvariantCulture)),
                        Csv(row.Sku),
                        Csv(row.Status),
                        Csv(string.Empty),
                        Csv(row.ErrorMessage),
                        Csv(row.ErrorJson)));
                    continue;
                }

                foreach (var error in errors)
                {
                    builder.AppendLine(string.Join(
                        ",",
                        Csv(row.RowNumber.ToString(CultureInfo.InvariantCulture)),
                        Csv(row.Sku),
                        Csv(row.Status),
                        Csv(error.Column),
                        Csv(error.Message),
                        Csv(row.ErrorJson)));
                }
            }

            return builder.ToString();
        }

        protected static IReadOnlyList<(string Column, string Message)> ExtractErrors(ProductImportRowDto row)
        {
            if (string.IsNullOrWhiteSpace(row.ErrorJson))
            {
                return string.IsNullOrWhiteSpace(row.ErrorMessage)
                    ? []
                    : [(string.Empty, row.ErrorMessage)];
            }

            try
            {
                using var document = JsonDocument.Parse(row.ErrorJson);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return document.RootElement
                        .EnumerateArray()
                        .Select(ReadError)
                        .Where(error => !string.IsNullOrWhiteSpace(error.Column) || !string.IsNullOrWhiteSpace(error.Message))
                        .ToArray();
                }

                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    return [ReadError(document.RootElement)];
                }
            }
            catch (JsonException)
            {
                return [(string.Empty, row.ErrorMessage ?? row.ErrorJson)];
            }

            return [(string.Empty, row.ErrorMessage ?? row.ErrorJson)];
        }

        protected static (string Column, string Message) ReadError(JsonElement element)
        {
            return (
                ReadString(element, "column") ?? ReadString(element, "Column") ?? string.Empty,
                ReadString(element, "message") ?? ReadString(element, "Message") ?? element.ToString());
        }

        protected static string? ReadString(JsonElement element, string propertyName)
        {
            return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var property)
                ? property.GetString()
                : null;
        }

        protected static string Csv(string? value)
        {
            var normalized = value ?? string.Empty;
            return "\"" + normalized.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

    }
}
