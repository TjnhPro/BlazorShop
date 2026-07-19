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
        private sealed class CommerceNodeAdminCredentialHeaderOperationFilter : IOperationFilter
        {
            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var relativePath = NormalizePath(context.ApiDescription.RelativePath);
                if (!relativePath.StartsWith("api/commerce/", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                EnsureHeaderParameter(
                    operation,
                    CommerceNodeCredentialMiddleware.NodeKeyHeaderName,
                    "Commerce Node key configured for this node.");
                EnsureHeaderParameter(
                    operation,
                    CommerceNodeCredentialMiddleware.NodeSecretHeaderName,
                    "Commerce Node secret configured for this node.");
            }
        }

        private sealed class CommerceAdminStoreKeyOperationFilter : IOperationFilter
        {
            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var relativePath = NormalizePath(context.ApiDescription.RelativePath);
                if (!IsCommerceAdminStoreScopedPath(relativePath))
                {
                    return;
                }

                EnsureQueryParameter(
                    operation,
                    "storeKey",
                    "Store key for the Commerce Admin store scope.");
            }
        }
    }
}
