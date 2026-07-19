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
        private sealed class StorefrontContractSchemaFilter : ISchemaFilter
        {
            private static readonly IReadOnlyDictionary<Type, string[]> RequiredArrayPropertiesByType =
                new Dictionary<Type, string[]>
                {
                    [typeof(StorefrontCategoryTreeNodeResponse)] = ["children"],
                    [typeof(StorefrontCategoryPageResponse)] = ["products"],
                    [typeof(StorefrontProductResponse)] = ["variants"],
                    [typeof(StorefrontProductVariantResponse)] = ["attributes"],
                    [typeof(StorefrontProductSelectionPreviewResponse)] = ["validationMessages", "selectedAttributes"],
                    [typeof(StorefrontCartResponse)] = ["lines"],
                    [typeof(StorefrontCartValidationResponse)] = ["issues"],
                    [typeof(StorefrontCheckoutPreviewResponse)] = ["completedSteps", "lines", "issues"],
                    [typeof(StorefrontCheckoutSessionResponse)] = ["completedSteps", "shippingOptions", "paymentMethods", "lines", "issues"],
                    [typeof(StorefrontCheckoutReviewResponse)] = ["completedSteps", "lines", "issues"],
                    [typeof(StorefrontOrderResponse)] = ["trackingEvents", "historyEntries", "lines"],
                    [typeof(StorefrontCustomerOrderDetailResponse)] = ["trackingEvents", "historyEntries", "lines"],
                    [typeof(StorefrontOrderLineResponse)] = ["variantAttributes"],
                    [typeof(StorefrontAddressFieldConfigurationResponse)] = ["stateProvinceRequiredCountryCodes"],
                    [typeof(GetPublicCatalogSitemap)] = ["categories", "products", "pages"],
                    [typeof(StorefrontVariationTemplateDto)] = ["options"],
                    [typeof(StorefrontVariationOptionDto)] = ["values"],
                };

            public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
            {
                if (schema is not OpenApiSchema openApiSchema)
                {
                    return;
                }

                if (context.Type == typeof(CommerceNodeApiErrorResponse))
                {
                    openApiSchema.Required = new HashSet<string>(StringComparer.Ordinal)
                    {
                        "success",
                        "code",
                        "message",
                        "traceId",
                    };

                    ForceRequiredString(openApiSchema, "code");
                    ForceRequiredString(openApiSchema, "message");
                    ForceRequiredString(openApiSchema, "traceId");
                    return;
                }

                if (context.Type.IsGenericType
                    && context.Type.GetGenericTypeDefinition() == typeof(StorefrontPagedResponse<>))
                {
                    ForceRequiredArray(openApiSchema, "items");
                    return;
                }

                if (!RequiredArrayPropertiesByType.TryGetValue(context.Type, out var requiredArrayProperties))
                {
                    return;
                }

                foreach (var propertyName in requiredArrayProperties)
                {
                    ForceRequiredArray(openApiSchema, propertyName);
                }
            }

            private static void ForceRequiredString(OpenApiSchema schema, string propertyName)
            {
                if (schema.Properties is null
                    || !schema.Properties.TryGetValue(propertyName, out var propertySchema)
                    || propertySchema is not OpenApiSchema openApiPropertySchema)
                {
                    return;
                }

                openApiPropertySchema.Type = JsonSchemaType.String;
            }

            private static void ForceRequiredArray(OpenApiSchema schema, string propertyName)
            {
                schema.Required ??= new HashSet<string>(StringComparer.Ordinal);
                schema.Required.Add(propertyName);

                if (schema.Properties is null
                    || !schema.Properties.TryGetValue(propertyName, out var propertySchema)
                    || propertySchema is not OpenApiSchema openApiPropertySchema)
                {
                    return;
                }

                openApiPropertySchema.Type = JsonSchemaType.Array;
            }
        }
    }
}
