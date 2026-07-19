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
        private sealed class StorefrontSecurityDocumentFilter : IDocumentFilter
        {
            private static readonly IReadOnlyDictionary<string, StorefrontSecurityRequirement> OperationSecurity =
                new Dictionary<string, StorefrontSecurityRequirement>(StringComparer.Ordinal)
                {
                    ["StorefrontAuth_RefreshToken"] = StorefrontSecurityRequirement.RefreshCookie,
                    ["StorefrontAuth_Logout"] = StorefrontSecurityRequirement.RefreshCookie,
                    ["StorefrontAuth_ChangePassword"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontAuth_UpdateProfile"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontCustomerAddresses_List"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontCustomerAddresses_Create"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontCustomerAddresses_Update"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontCustomerAddresses_Delete"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontCustomerAddresses_SetDefaultShipping"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontCustomerAddresses_SetDefaultBilling"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontCustomerProfile_Get"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontCustomerProfile_Update"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontCart_MergeCurrentCustomer"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontOrders_ListCurrentUserOrders"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontOrders_GetCurrentUserOrder"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontOrders_GetCurrentUserOrderReceipt"] = StorefrontSecurityRequirement.Bearer,
                };

            public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
            {
                foreach (var pathItem in swaggerDoc.Paths.Values)
                {
                    foreach (var operation in GetOperations(pathItem))
                    {
                        if (operation.OperationId is null
                            || !OperationSecurity.TryGetValue(operation.OperationId, out var security))
                        {
                            continue;
                        }

                        operation.Security ??= [];
                        operation.Security.Clear();
                        operation.Security.Add(CreateSecurityRequirement(swaggerDoc, security));
                    }
                }
            }

            private static IEnumerable<OpenApiOperation> GetOperations(IOpenApiPathItem pathItem)
            {
                var operations = pathItem.Operations;
                return operations is null ? [] : operations.Values;
            }

            private static OpenApiSecurityRequirement CreateSecurityRequirement(
                OpenApiDocument swaggerDoc,
                StorefrontSecurityRequirement security)
            {
                var schemeName = security == StorefrontSecurityRequirement.RefreshCookie ? "RefreshCookie" : "Bearer";
                return new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(schemeName, swaggerDoc, null)] = [],
                };
            }
        }
    }
}
