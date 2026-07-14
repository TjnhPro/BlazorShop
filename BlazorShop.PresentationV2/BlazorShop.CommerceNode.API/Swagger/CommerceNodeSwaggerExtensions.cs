namespace BlazorShop.CommerceNode.API.Swagger
{
    using BlazorShop.CommerceNode.API.Middleware;

    using Microsoft.AspNetCore.Mvc.ApiExplorer;
    using Microsoft.OpenApi;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public static class CommerceNodeSwaggerExtensions
    {
        public const string CommerceAdminDocumentName = "commerce-admin";

        public const string StorefrontDocumentName = "storefront";

        public const string LegacyInternalDocumentName = "legacy-internal";

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

                options.SwaggerDoc(
                    LegacyInternalDocumentName,
                    new OpenApiInfo
                    {
                        Title = "Legacy Internal",
                        Version = "v1",
                        Description = "Legacy api/internal/* compatibility APIs. Do not use for new Storefront work.",
                    });

                options.DocInclusionPredicate(ShouldIncludeApiDescription);
                options.OperationFilter<CommerceNodeAdminCredentialHeaderOperationFilter>();
                options.OperationFilter<CommerceAdminStoreKeyOperationFilter>();
                options.OperationFilter<LegacyInternalStoreKeyHeaderOperationFilter>();
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
                options.SwaggerEndpoint(
                    $"/swagger/{LegacyInternalDocumentName}/swagger.json",
                    "Legacy Internal");
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
                LegacyInternalDocumentName => relativePath.StartsWith("api/internal/", StringComparison.OrdinalIgnoreCase),
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

        private sealed class LegacyInternalStoreKeyHeaderOperationFilter : IOperationFilter
        {
            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var relativePath = NormalizePath(context.ApiDescription.RelativePath);
                if (!relativePath.StartsWith("api/internal/", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                EnsureHeaderParameter(
                    operation,
                    "X-Store-Key",
                    "Legacy internal store key header. Compatibility only.");
            }
        }
    }
}
