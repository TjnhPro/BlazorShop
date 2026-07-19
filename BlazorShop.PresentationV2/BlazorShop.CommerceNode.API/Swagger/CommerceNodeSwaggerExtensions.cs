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
    using Microsoft.AspNetCore.Mvc.ApiExplorer;
    using Microsoft.OpenApi;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public static partial class CommerceNodeSwaggerExtensions
    {
        public const string CommerceAdminDocumentName = "commerce-admin";

        public const string StorefrontDocumentName = "storefront";

        public static IServiceCollection AddCommerceNodeSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(
                    CommerceAdminDocumentName,
                    new OpenApiInfo
                    {
                        Title = "Commerce Node Admin",
                        Version = "v1",
                        Description = "Commerce Node admin/control APIs. Store-scoped endpoints use the required storeKey query parameter.",
                    });

                options.SwaggerDoc(
                    StorefrontDocumentName,
                    new OpenApiInfo
                    {
                        Title = "Storefront API",
                        Version = "v1",
                        Description = "Storefront APIs scoped by api/storefront/stores/{storeKey}/*.",
                    });

                options.DocInclusionPredicate(ShouldIncludeApiDescription);
                options.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Description = "JWT bearer token returned by Storefront auth endpoints.",
                    });
                options.AddSecurityDefinition(
                    "RefreshCookie",
                    new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        Name = "__Host-blazorshop-refresh",
                        In = ParameterLocation.Cookie,
                        Description = "HttpOnly refresh token cookie set by Storefront login.",
                    });
                options.OperationFilter<CommerceNodeAdminCredentialHeaderOperationFilter>();
                options.OperationFilter<CommerceAdminStoreKeyOperationFilter>();
                options.OperationFilter<CommerceStoreAdminOperationMetadataFilter>();
                options.OperationFilter<CommerceCurrencyAdminOperationMetadataFilter>();
                options.OperationFilter<CommerceSecurityPrivacyAdminOperationMetadataFilter>();
                options.OperationFilter<CommerceShippingAdminOperationMetadataFilter>();
                options.OperationFilter<CommerceTransactionalMessageAdminOperationMetadataFilter>();
                options.OperationFilter<CommerceCategoryMediaAdminOperationMetadataFilter>();
                options.OperationFilter<CommerceNavigationAdminOperationMetadataFilter>();
                options.OperationFilter<CommerceSeoSlugAdminOperationMetadataFilter>();
                options.OperationFilter<StorefrontOperationMetadataFilter>();
                options.DocumentFilter<StorefrontSecurityDocumentFilter>();
                options.SchemaFilter<StorefrontContractSchemaFilter>();
            });

            return services;
        }

        public static IApplicationBuilder UseCommerceNodeSwaggerUi(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(
                    $"/swagger/{CommerceAdminDocumentName}/swagger.json",
                    "Commerce Node Admin");
                options.SwaggerEndpoint(
                    $"/swagger/{StorefrontDocumentName}/swagger.json",
                    "Storefront API");
            });

            return app;
        }

        private static bool ShouldIncludeApiDescription(string documentName, ApiDescription apiDescription)
        {
            var relativePath = NormalizePath(apiDescription.RelativePath);

            return documentName switch
            {
                CommerceAdminDocumentName => relativePath.StartsWith("api/commerce/", StringComparison.OrdinalIgnoreCase),
                StorefrontDocumentName => relativePath.StartsWith("api/storefront/stores/{storekey}/", StringComparison.OrdinalIgnoreCase),
                _ => false,
            };
        }

        internal static string NormalizePath(string? relativePath)
        {
            return (relativePath ?? string.Empty)
                .Split('?', 2)[0]
                .Trim('/')
                .ToLowerInvariant();
        }

        internal static bool IsCommerceAdminStoreScopedPath(string relativePath)
        {
            return CommerceAdminStoreScopeMiddleware.IsStoreScopedCommerceAdminPath(relativePath);
        }

        internal static void EnsureHeaderParameter(OpenApiOperation operation, string name, string description)
        {
            EnsureParameter(operation, name, ParameterLocation.Header, description, required: true);
        }

        internal static void EnsureQueryParameter(OpenApiOperation operation, string name, string description)
        {
            EnsureParameter(operation, name, ParameterLocation.Query, description, required: true);
        }

        private static void EnsureParameter(
            OpenApiOperation operation,
            string name,
            ParameterLocation location,
            string description,
            bool required)
        {
            operation.Parameters ??= [];

            if (operation.Parameters.Any(parameter =>
                    string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase)
                    && parameter.In == location))
            {
                return;
            }

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = name,
                In = location,
                Required = required,
                Description = description,
                Schema = new OpenApiSchema { Type = JsonSchemaType.String },
            });
        }

        private sealed record StorefrontOperationMetadata(
            string OperationId,
            string Summary,
            Type SuccessResponseType,
            IReadOnlyList<int> ErrorStatusCodes,
            StorefrontSecurityRequirement Security = StorefrontSecurityRequirement.None,
            string? Description = null,
            bool Deprecated = false);

        private enum StorefrontSecurityRequirement
        {
            None,
            Bearer,
            RefreshCookie,
        }
    }
}
