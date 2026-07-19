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
        private sealed class CommerceStoreAdminOperationMetadataFilter : IOperationFilter
        {
            private static readonly IReadOnlyDictionary<string, CommerceStoreOperationMetadata> Metadata =
                new Dictionary<string, CommerceStoreOperationMetadata>(StringComparer.Ordinal)
                {
                    ["List"] = new(
                        "CommerceStores_List",
                        "List Commerce Node stores.",
                        typeof(CommerceNodeApiResponse<CommerceStoreListResponse>),
                        [StatusCodes.Status500InternalServerError]),
                    ["Get"] = new(
                        "CommerceStores_Get",
                        "Get a Commerce Node store.",
                        typeof(CommerceNodeApiResponse<CommerceStoreDetail>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    ["Create"] = new(
                        "CommerceStores_Create",
                        "Create a Commerce Node store.",
                        typeof(CommerceNodeApiResponse<CommerceStoreDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["Update"] = new(
                        "CommerceStores_Update",
                        "Update a Commerce Node store.",
                        typeof(CommerceNodeApiResponse<CommerceStoreDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["Activate"] = new(
                        "CommerceStores_Activate",
                        "Activate a Commerce Node store.",
                        typeof(CommerceNodeApiResponse<CommerceStoreDetail>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["Deactivate"] = new(
                        "CommerceStores_Deactivate",
                        "Deactivate a Commerce Node store.",
                        typeof(CommerceNodeApiResponse<CommerceStoreDetail>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["Archive"] = new(
                        "CommerceStores_Archive",
                        "Archive a Commerce Node store.",
                        typeof(CommerceNodeApiResponse<CommerceStoreDetail>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["AddDomain"] = new(
                        "CommerceStores_AddDomain",
                        "Add a domain to a Commerce Node store.",
                        typeof(CommerceNodeApiResponse<CommerceStoreDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["VerifyDomain"] = new(
                        "CommerceStores_VerifyDomain",
                        "Verify a Commerce Node store domain.",
                        typeof(CommerceNodeApiResponse<CommerceStoreDetail>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["DisableDomain"] = new(
                        "CommerceStores_DisableDomain",
                        "Disable a Commerce Node store domain.",
                        typeof(CommerceNodeApiResponse<CommerceStoreDetail>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["SetPrimaryDomain"] = new(
                        "CommerceStores_SetPrimaryDomain",
                        "Set the primary domain for a Commerce Node store.",
                        typeof(CommerceNodeApiResponse<CommerceStoreDetail>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                };

            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var relativePath = NormalizePath(context.ApiDescription.RelativePath);
                if (!relativePath.StartsWith("api/commerce/admin/stores", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor actionDescriptor
                    || !string.Equals(actionDescriptor.ControllerName, "CommerceStores", StringComparison.Ordinal)
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

        private sealed record CommerceStoreOperationMetadata(
            string OperationId,
            string Summary,
            Type ResponseType,
            int[] ErrorStatusCodes);
        }
    }
}
