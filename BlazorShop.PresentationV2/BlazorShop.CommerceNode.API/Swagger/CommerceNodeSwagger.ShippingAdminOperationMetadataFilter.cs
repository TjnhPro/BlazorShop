namespace BlazorShop.CommerceNode.API.Swagger
{
    using System.Reflection;
    using System.Text.Json.Nodes;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Discovery;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.CommerceNode.API.Contracts.Storefront;
    using BlazorShop.CommerceNode.API.Middleware;
    using BlazorShop.CommerceNode.API.Responses;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.OpenApi;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public static partial class CommerceNodeSwaggerExtensions
    {
        private sealed class CommerceShippingAdminOperationMetadataFilter : IOperationFilter
        {
            private static readonly IReadOnlyDictionary<string, CommerceShippingOperationMetadata> Metadata =
                new Dictionary<string, CommerceShippingOperationMetadata>(StringComparer.Ordinal)
                {
                    ["Get"] = new(
                        "CommerceShippingSettings_Get",
                        "Get store shipping settings.",
                        typeof(CommerceNodeApiResponse<StoreShippingSettingsDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    ["Update"] = new(
                        "CommerceShippingSettings_Update",
                        "Update store shipping settings.",
                        typeof(CommerceNodeApiResponse<StoreShippingSettingsDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                };

            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var relativePath = NormalizePath(context.ApiDescription.RelativePath);
                if (!relativePath.StartsWith("api/commerce/admin/shipping/settings", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor actionDescriptor
                    || !string.Equals(actionDescriptor.ControllerName, "CommerceShippingSettings", StringComparison.Ordinal)
                    || !Metadata.TryGetValue(actionDescriptor.ActionName, out var metadata))
                {
                    return;
                }

                operation.OperationId = metadata.OperationId;
                operation.Summary = metadata.Summary;

                if (operation.RequestBody is OpenApiRequestBody requestBody)
                {
                    requestBody.Required = true;
                }

                operation.Responses ??= new OpenApiResponses();
                operation.Responses["200"] = CommerceNodeSwaggerResponseHelpers.CreateJsonResponse(context, metadata.ResponseType, "Success.");
                foreach (var statusCode in metadata.ErrorStatusCodes)
                {
                    operation.Responses[statusCode.ToString()] = CommerceNodeSwaggerResponseHelpers.CreateJsonResponse(context, metadata.ResponseType, "Error.");
                }
            }

        private sealed record CommerceShippingOperationMetadata(
                string OperationId,
                string Summary,
                Type ResponseType,
                int[] ErrorStatusCodes);
        }
    }
}
