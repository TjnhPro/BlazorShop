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
        private sealed class CommerceNavigationAdminOperationMetadataFilter : IOperationFilter
        {
            private static readonly IReadOnlyDictionary<string, CommerceNavigationOperationMetadata> Metadata =
                new Dictionary<string, CommerceNavigationOperationMetadata>
                {
                    ["ListMenus"] = new(
                        "CommerceNavigation_ListMenus",
                        "List store navigation menus.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StoreNavigationMenuSummaryDto>>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    ["CreateMenu"] = new(
                        "CommerceNavigation_CreateMenu",
                        "Create a store navigation menu.",
                        typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["GetMenu"] = new(
                        "CommerceNavigation_GetMenu",
                        "Get a store navigation menu.",
                        typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    ["UpdateMenu"] = new(
                        "CommerceNavigation_UpdateMenu",
                        "Update a store navigation menu.",
                        typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    ["CreateItem"] = new(
                        "CommerceNavigation_CreateItem",
                        "Create a store navigation menu item.",
                        typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    ["UpdateItem"] = new(
                        "CommerceNavigation_UpdateItem",
                        "Update a store navigation menu item.",
                        typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    ["ArchiveItem"] = new(
                        "CommerceNavigation_ArchiveItem",
                        "Archive a store navigation menu item.",
                        typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    ["UpdateItemOrder"] = new(
                        "CommerceNavigation_UpdateItemOrder",
                        "Reorder store navigation menu items.",
                        typeof(CommerceNodeApiResponse<StoreNavigationMenuDetailDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    ["ListSystemTargets"] = new(
                        "CommerceNavigation_ListSystemTargets",
                        "List supported navigation system targets.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StoreNavigationTargetOptionDto>>),
                        [StatusCodes.Status500InternalServerError]),
                };

            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var relativePath = NormalizePath(context.ApiDescription.RelativePath);
                if (!relativePath.StartsWith("api/commerce/admin/navigation", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor actionDescriptor
                    || !string.Equals(actionDescriptor.ControllerName, "CommerceNavigation", StringComparison.Ordinal)
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

        private sealed record CommerceNavigationOperationMetadata(
                string OperationId,
                string Summary,
                Type ResponseType,
                int[] ErrorStatusCodes);
        }
    }
}
