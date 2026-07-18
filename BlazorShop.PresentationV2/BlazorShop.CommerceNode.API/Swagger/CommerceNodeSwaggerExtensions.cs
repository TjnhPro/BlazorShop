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

    public static class CommerceNodeSwaggerExtensions
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
                operation.Responses["200"] = CreateJsonResponse(context, metadata.ResponseType, "Success.");
                foreach (var statusCode in metadata.ErrorStatusCodes)
                {
                    operation.Responses[statusCode.ToString()] = CreateJsonResponse(context, metadata.ResponseType, "Error.");
                }
            }

            private static OpenApiResponse CreateJsonResponse(OperationFilterContext context, Type responseType, string description)
            {
                return new OpenApiResponse
                {
                    Description = description,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new()
                        {
                            Schema = context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository),
                        },
                    },
                };
            }

        private sealed record CommerceStoreOperationMetadata(
            string OperationId,
            string Summary,
            Type ResponseType,
            int[] ErrorStatusCodes);
        }

        private sealed class CommerceCategoryMediaAdminOperationMetadataFilter : IOperationFilter
        {
            private static readonly IReadOnlyDictionary<string, CategoryMediaOperationMetadata> Metadata =
                new Dictionary<string, CategoryMediaOperationMetadata>
                {
                    ["GetPrimary"] = new(
                        "CommerceCategoryMedia_GetPrimary",
                        "Get a category primary media assignment.",
                        typeof(CommerceNodeApiResponse<CategoryMediaAssignmentDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    ["SetPrimary"] = new(
                        "CommerceCategoryMedia_SetPrimary",
                        "Set a category primary media asset.",
                        typeof(CommerceNodeApiResponse<CategoryMediaAssignmentDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["ClearPrimary"] = new(
                        "CommerceCategoryMedia_ClearPrimary",
                        "Clear a category primary media assignment.",
                        typeof(CommerceNodeApiResponse<CategoryMediaAssignmentDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                };

            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var relativePath = NormalizePath(context.ApiDescription.RelativePath);
                if (!relativePath.StartsWith("api/commerce/admin/categories", StringComparison.OrdinalIgnoreCase)
                    || !relativePath.Contains("/media", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor actionDescriptor
                    || !string.Equals(actionDescriptor.ControllerName, "CommerceCategoryMedia", StringComparison.Ordinal)
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
                operation.Responses["200"] = CreateJsonResponse(context, metadata.ResponseType, "Success.");
                foreach (var statusCode in metadata.ErrorStatusCodes)
                {
                    operation.Responses[statusCode.ToString()] = CreateJsonResponse(context, metadata.ResponseType, "Error.");
                }
            }

            private static OpenApiResponse CreateJsonResponse(OperationFilterContext context, Type responseType, string description)
            {
                return new OpenApiResponse
                {
                    Description = description,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new()
                        {
                            Schema = context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository),
                        },
                    },
                };
            }

            private sealed record CategoryMediaOperationMetadata(
                string OperationId,
                string Summary,
                Type ResponseType,
                int[] ErrorStatusCodes);
        }

        private sealed class CommerceCurrencyAdminOperationMetadataFilter : IOperationFilter
        {
            private static readonly IReadOnlyDictionary<string, CommerceCurrencyOperationMetadata> Metadata =
                new Dictionary<string, CommerceCurrencyOperationMetadata>
                {
                    ["Get"] = new(
                        "CommerceCurrencies_List",
                        "List store-supported currencies.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StoreCurrencyDto>>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    ["Update"] = new(
                        "CommerceCurrencies_Update",
                        "Update a store-supported currency.",
                        typeof(CommerceNodeApiResponse<StoreCurrencyDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["GetExchangeRates"] = new(
                        "CommerceCurrencies_ListExchangeRates",
                        "List store currency exchange rates.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StoreCurrencyExchangeRateDto>>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    ["GetExchangeRateProviders"] = new(
                        "CommerceCurrencies_ListExchangeRateProviders",
                        "List configured exchange-rate providers.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    ["FetchExchangeRates"] = new(
                        "CommerceCurrencies_FetchExchangeRates",
                        "Fetch provider exchange rates into the store rate table.",
                        typeof(CommerceNodeApiResponse<StoreCurrencyExchangeRateProviderFetchResult>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["QueueExchangeRateUpdate"] = new(
                        "CommerceCurrencies_QueueExchangeRateUpdateTask",
                        "Queue an exchange-rate provider update task.",
                        typeof(CommerceNodeApiResponse<CommerceTaskSummary>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["UpsertExchangeRate"] = new(
                        "CommerceCurrencies_UpsertExchangeRate",
                        "Create or update a manual currency exchange rate.",
                        typeof(CommerceNodeApiResponse<StoreCurrencyExchangeRateDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["DisableExchangeRate"] = new(
                        "CommerceCurrencies_DisableExchangeRate",
                        "Disable a manual currency exchange rate.",
                        typeof(CommerceNodeApiResponse<StoreCurrencyExchangeRateDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                };

            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var relativePath = NormalizePath(context.ApiDescription.RelativePath);
                if (!relativePath.StartsWith("api/commerce/admin/currencies", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor actionDescriptor
                    || !string.Equals(actionDescriptor.ControllerName, "CommerceCurrencies", StringComparison.Ordinal)
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
                operation.Responses["200"] = CreateJsonResponse(context, metadata.ResponseType, "Success.");
                foreach (var statusCode in metadata.ErrorStatusCodes)
                {
                    operation.Responses[statusCode.ToString()] = CreateJsonResponse(context, metadata.ResponseType, "Error.");
                }
            }

            private static OpenApiResponse CreateJsonResponse(OperationFilterContext context, Type responseType, string description)
            {
                return new OpenApiResponse
                {
                    Description = description,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new()
                        {
                            Schema = context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository),
                        },
                    },
                };
            }

        private sealed record CommerceCurrencyOperationMetadata(
                string OperationId,
                string Summary,
                Type ResponseType,
                int[] ErrorStatusCodes);
        }

        private sealed class CommerceSecurityPrivacyAdminOperationMetadataFilter : IOperationFilter
        {
            private static readonly IReadOnlyDictionary<string, CommerceSecurityPrivacyOperationMetadata> Metadata =
                new Dictionary<string, CommerceSecurityPrivacyOperationMetadata>(StringComparer.Ordinal)
                {
                    ["Get"] = new(
                        "CommerceSecurityPrivacy_Get",
                        "Get store security and privacy settings.",
                        typeof(CommerceNodeApiResponse<StoreSecurityPrivacySettingsDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    ["Update"] = new(
                        "CommerceSecurityPrivacy_Update",
                        "Update store security and privacy settings.",
                        typeof(CommerceNodeApiResponse<StoreSecurityPrivacySettingsDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                };

            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var relativePath = NormalizePath(context.ApiDescription.RelativePath);
                if (!relativePath.StartsWith("api/commerce/admin/security-privacy", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor actionDescriptor
                    || !string.Equals(actionDescriptor.ControllerName, "CommerceSecurityPrivacy", StringComparison.Ordinal)
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
                operation.Responses["200"] = CreateJsonResponse(context, metadata.ResponseType, "Success.");
                foreach (var statusCode in metadata.ErrorStatusCodes)
                {
                    operation.Responses[statusCode.ToString()] = CreateJsonResponse(context, metadata.ResponseType, "Error.");
                }
            }

            private static OpenApiResponse CreateJsonResponse(OperationFilterContext context, Type responseType, string description)
            {
                return new OpenApiResponse
                {
                    Description = description,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new()
                        {
                            Schema = context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository),
                        },
                    },
                };
            }

            private sealed record CommerceSecurityPrivacyOperationMetadata(
                string OperationId,
                string Summary,
                Type ResponseType,
                int[] ErrorStatusCodes);
        }

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
                operation.Responses["200"] = CreateJsonResponse(context, metadata.ResponseType, "Success.");
                foreach (var statusCode in metadata.ErrorStatusCodes)
                {
                    operation.Responses[statusCode.ToString()] = CreateJsonResponse(context, metadata.ResponseType, "Error.");
                }
            }

            private static OpenApiResponse CreateJsonResponse(OperationFilterContext context, Type responseType, string description)
            {
                return new OpenApiResponse
                {
                    Description = description,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new()
                        {
                            Schema = context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository),
                        },
                    },
                };
            }

            private sealed record CommerceShippingOperationMetadata(
                string OperationId,
                string Summary,
                Type ResponseType,
                int[] ErrorStatusCodes);
        }

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
                operation.Responses["200"] = CreateJsonResponse(context, metadata.ResponseType, "Success.");
                foreach (var statusCode in metadata.ErrorStatusCodes)
                {
                    operation.Responses[statusCode.ToString()] = CreateJsonResponse(context, metadata.ResponseType, "Error.");
                }
            }

            private static OpenApiResponse CreateJsonResponse(OperationFilterContext context, Type responseType, string description)
            {
                return new OpenApiResponse
                {
                    Description = description,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new()
                        {
                            Schema = context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository),
                        },
                    },
                };
            }

            private sealed record CommerceNavigationOperationMetadata(
                string OperationId,
                string Summary,
                Type ResponseType,
                int[] ErrorStatusCodes);
        }

        private sealed class CommerceSeoSlugAdminOperationMetadataFilter : IOperationFilter
        {
            private static readonly IReadOnlyDictionary<string, CommerceSeoSlugOperationMetadata> Metadata =
                new Dictionary<string, CommerceSeoSlugOperationMetadata>
                {
                    ["Generate"] = new(
                        "CommerceSeoSlugs_Generate",
                        "Generate a store-scoped SEO slug.",
                        typeof(CommerceNodeApiResponse<StoreSeoSlugPolicyResult>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["Validate"] = new(
                        "CommerceSeoSlugs_Validate",
                        "Validate a store-scoped SEO slug.",
                        typeof(CommerceNodeApiResponse<StoreSeoSlugPolicyResult>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    ["History"] = new(
                        "CommerceSeoSlugs_ListHistory",
                        "List store-scoped SEO slug history.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StoreSeoSlugHistoryDto>>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                };

            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var relativePath = NormalizePath(context.ApiDescription.RelativePath);
                if (!relativePath.StartsWith("api/commerce/admin/seo/slugs", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor actionDescriptor
                    || !string.Equals(actionDescriptor.ControllerName, "CommerceSeoSlugLifecycle", StringComparison.Ordinal)
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
                operation.Responses["200"] = CreateJsonResponse(context, metadata.ResponseType, "Success.");
                foreach (var statusCode in metadata.ErrorStatusCodes)
                {
                    operation.Responses[statusCode.ToString()] = CreateJsonResponse(context, metadata.ResponseType, "Error.");
                }
            }

            private static OpenApiResponse CreateJsonResponse(OperationFilterContext context, Type responseType, string description)
            {
                return new OpenApiResponse
                {
                    Description = description,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new()
                        {
                            Schema = context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository),
                        },
                    },
                };
            }

            private sealed record CommerceSeoSlugOperationMetadata(
                string OperationId,
                string Summary,
                Type ResponseType,
                int[] ErrorStatusCodes);
        }

        private sealed class CommerceTransactionalMessageAdminOperationMetadataFilter : IOperationFilter
        {
            private static readonly IReadOnlyDictionary<(string Controller, string Action), CommerceTransactionalMessageOperationMetadata> Metadata =
                new Dictionary<(string Controller, string Action), CommerceTransactionalMessageOperationMetadata>
                {
                    [("CommerceStoreEmailSettings", "Get")] = new(
                        "CommerceStoreEmailSettings_Get",
                        "Get store email SMTP settings.",
                        typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceStoreEmailSettings", "Update")] = new(
                        "CommerceStoreEmailSettings_Update",
                        "Update store email SMTP settings.",
                        typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("CommerceStoreEmailSettings", "RotatePassword")] = new(
                        "CommerceStoreEmailSettings_RotatePassword",
                        "Rotate the store SMTP password.",
                        typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceStoreEmailSettings", "ClearPassword")] = new(
                        "CommerceStoreEmailSettings_ClearPassword",
                        "Clear the store SMTP password.",
                        typeof(CommerceNodeApiResponse<StoreEmailSettingsResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceStoreEmailSettings", "SendTest")] = new(
                        "CommerceStoreEmailSettings_SendTest",
                        "Send a store SMTP test email.",
                        typeof(CommerceNodeApiResponse<SendStoreEmailTestResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceMessageTemplates", "List")] = new(
                        "CommerceMessageTemplates_List",
                        "List transactional message templates.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<MessageTemplateAdminSummary>>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError]),
                    [("CommerceMessageTemplates", "Get")] = new(
                        "CommerceMessageTemplates_Get",
                        "Get a transactional message template.",
                        typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceMessageTemplates", "Update")] = new(
                        "CommerceMessageTemplates_Update",
                        "Update a store transactional message template override.",
                        typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("CommerceMessageTemplates", "Reset")] = new(
                        "CommerceMessageTemplates_Reset",
                        "Reset a store transactional message template override.",
                        typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceMessageTemplates", "Preview")] = new(
                        "CommerceMessageTemplates_Preview",
                        "Preview a transactional message template.",
                        typeof(CommerceNodeApiResponse<MessageTemplatePreviewResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceQueuedMessages", "List")] = new(
                        "CommerceQueuedMessages_List",
                        "List queued transactional messages.",
                        typeof(CommerceNodeApiResponse<QueuedMessageAdminListResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError]),
                    [("CommerceQueuedMessages", "Get")] = new(
                        "CommerceQueuedMessages_Get",
                        "Get a queued transactional message.",
                        typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("CommerceQueuedMessages", "Retry")] = new(
                        "CommerceQueuedMessages_Retry",
                        "Retry a queued transactional message.",
                        typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("CommerceQueuedMessages", "Cancel")] = new(
                        "CommerceQueuedMessages_Cancel",
                        "Cancel a queued transactional message.",
                        typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                };

            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor actionDescriptor
                    || !Metadata.TryGetValue((actionDescriptor.ControllerName, actionDescriptor.ActionName), out var metadata))
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
                operation.Responses["200"] = CreateJsonResponse(context, metadata.ResponseType, "Success.");
                foreach (var statusCode in metadata.ErrorStatusCodes)
                {
                    operation.Responses[statusCode.ToString()] = CreateJsonResponse(context, metadata.ResponseType, "Error.");
                }
            }

            private static OpenApiResponse CreateJsonResponse(OperationFilterContext context, Type responseType, string description)
            {
                return new OpenApiResponse
                {
                    Description = description,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new()
                        {
                            Schema = context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository),
                        },
                    },
                };
            }

            private sealed record CommerceTransactionalMessageOperationMetadata(
                string OperationId,
                string Summary,
                Type ResponseType,
                int[] ErrorStatusCodes);
        }

        private sealed class StorefrontOperationMetadataFilter : IOperationFilter
        {
            private static readonly IReadOnlyDictionary<(string Controller, string Action), StorefrontOperationMetadata> Metadata =
                new Dictionary<(string Controller, string Action), StorefrontOperationMetadata>
                {
                    [("StorefrontScopedAddress", "GetCountries")] = new(
                        "StorefrontAddress_ListCountries",
                        "List Storefront address countries.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontAddressCountryResponse>>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAddress", "GetStates")] = new(
                        "StorefrontAddress_ListStates",
                        "List Storefront address states for a country.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontAddressStateProvinceResponse>>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAddress", "GetConfiguration")] = new(
                        "StorefrontAddress_GetConfiguration",
                        "Get Storefront address field configuration.",
                        typeof(CommerceNodeApiResponse<StorefrontAddressFieldConfigurationResponse>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCustomerAddresses", "List")] = new(
                        "StorefrontCustomerAddresses_List",
                        "List current customer addresses.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontCustomerAddressResponse>>),
                        [StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCustomerAddresses", "Create")] = new(
                        "StorefrontCustomerAddresses_Create",
                        "Create a current customer address.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerAddressResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCustomerAddresses", "Update")] = new(
                        "StorefrontCustomerAddresses_Update",
                        "Update a current customer address.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerAddressResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCustomerAddresses", "Delete")] = new(
                        "StorefrontCustomerAddresses_Delete",
                        "Delete a current customer address.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCustomerAddresses", "SetDefaultShipping")] = new(
                        "StorefrontCustomerAddresses_SetDefaultShipping",
                        "Set current customer default shipping address.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerAddressResponse>),
                        [StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCustomerAddresses", "SetDefaultBilling")] = new(
                        "StorefrontCustomerAddresses_SetDefaultBilling",
                        "Set current customer default billing address.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerAddressResponse>),
                        [StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCustomerProfile", "GetProfile")] = new(
                        "StorefrontCustomerProfile_Get",
                        "Get current customer profile.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerProfileResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCustomerProfile", "UpdateProfile")] = new(
                        "StorefrontCustomerProfile_Update",
                        "Update current customer profile.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerProfileResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedAuth", "Register")] = new(
                        "StorefrontAuth_Register",
                        "Register a Storefront customer.",
                        typeof(CommerceNodeApiResponse<StorefrontRegistrationResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAuth", "GetRegistrationPolicy")] = new(
                        "StorefrontAuth_GetRegistrationPolicy",
                        "Get Storefront registration policy.",
                        typeof(CommerceNodeApiResponse<StorefrontRegistrationPolicyResponse>),
                        [StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAuth", "Login")] = new(
                        "StorefrontAuth_Login",
                        "Sign in a Storefront customer.",
                        typeof(CommerceNodeApiResponse<StorefrontTokenResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAuth", "RefreshToken")] = new(
                        "StorefrontAuth_RefreshToken",
                        "Refresh a Storefront access token.",
                        typeof(CommerceNodeApiResponse<StorefrontTokenResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.RefreshCookie),
                    [("StorefrontScopedAuth", "Logout")] = new(
                        "StorefrontAuth_Logout",
                        "Sign out a Storefront customer.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.RefreshCookie),
                    [("StorefrontScopedAuth", "ChangePassword")] = new(
                        "StorefrontAuth_ChangePassword",
                        "Change the current customer's password.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedAuth", "ForgotPassword")] = new(
                        "StorefrontAuth_ForgotPassword",
                        "Request a Storefront password reset.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAuth", "ResetPassword")] = new(
                        "StorefrontAuth_ResetPassword",
                        "Reset a Storefront customer password.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAuth", "ConfirmEmail")] = new(
                        "StorefrontAuth_ConfirmEmail",
                        "Confirm a Storefront customer email.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedAuth", "UpdateProfile")] = new(
                        "StorefrontAuth_UpdateProfile",
                        "Update the current customer profile.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),

                    [("StorefrontScopedCatalog", "GetCategories")] = new(
                        "StorefrontCatalog_ListCategories",
                        "List published Storefront categories.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontCategoryResponse>>),
                        [StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetCategoryTree")] = new(
                        "StorefrontCatalog_GetCategoryTree",
                        "Get the published category tree.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontCategoryTreeNodeResponse>>),
                        [StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetCategoryById")] = new(
                        "StorefrontCatalog_GetCategoryById",
                        "Get a published category by ID.",
                        typeof(CommerceNodeApiResponse<StorefrontCategoryResponse>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetCategoryBySlug")] = new(
                        "StorefrontCatalog_GetCategoryBySlug",
                        "Get a published category page by slug.",
                        typeof(CommerceNodeApiResponse<StorefrontCategoryPageResponse>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetProductsByCategory")] = new(
                        "StorefrontCatalog_ListProductsByCategory",
                        "List published products in a category.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontCatalogProductResponse>>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetProductFilterMetadata")] = new(
                        "StorefrontCatalog_GetProductFilterMetadata",
                        "Get Storefront product filter metadata.",
                        typeof(CommerceNodeApiResponse<StorefrontProductFilterMetadataResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetSearchSuggestions")] = new(
                        "StorefrontCatalog_GetSearchSuggestions",
                        "Get Storefront catalog search suggestions.",
                        typeof(CommerceNodeApiResponse<StorefrontSearchSuggestionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetProducts")] = new(
                        "StorefrontCatalog_QueryProducts",
                        "Query published Storefront products.",
                        typeof(CommerceNodeApiResponse<StorefrontPagedResponse<StorefrontCatalogProductResponse>>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetProductById")] = new(
                        "StorefrontCatalog_GetProductById",
                        "Get a published product by ID.",
                        typeof(CommerceNodeApiResponse<StorefrontProductResponse>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetProductBySlug")] = new(
                        "StorefrontCatalog_GetProductBySlug",
                        "Get a published product by slug.",
                        typeof(CommerceNodeApiResponse<StorefrontProductResponse>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "PreviewProductSelection")] = new(
                        "StorefrontCatalog_PreviewProductSelection",
                        "Preview a Storefront product selection.",
                        typeof(CommerceNodeApiResponse<StorefrontProductSelectionPreviewResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCatalog", "GetSitemap")] = new(
                        "StorefrontCatalog_GetSitemap",
                        "Get the published catalog sitemap.",
                        typeof(CommerceNodeApiResponse<GetPublicCatalogSitemap>),
                        [StatusCodes.Status500InternalServerError]),

                    [("StorefrontScopedCart", "CreateSession")] = new(
                        "StorefrontCart_CreateSession",
                        "Create or resume a server cart session.",
                        typeof(CommerceNodeApiResponse<StorefrontCartSessionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "Get")] = new(
                        "StorefrontCart_Get",
                        "Get the current server cart.",
                        typeof(CommerceNodeApiResponse<StorefrontCartResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "AddLine")] = new(
                        "StorefrontCart_AddLine",
                        "Add a line to the server cart.",
                        typeof(CommerceNodeApiResponse<StorefrontCartResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "UpdateLine")] = new(
                        "StorefrontCart_UpdateLine",
                        "Update a server cart line.",
                        typeof(CommerceNodeApiResponse<StorefrontCartResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "RemoveLine")] = new(
                        "StorefrontCart_RemoveLine",
                        "Remove a server cart line.",
                        typeof(CommerceNodeApiResponse<StorefrontCartResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "Clear")] = new(
                        "StorefrontCart_Clear",
                        "Clear the current server cart.",
                        typeof(CommerceNodeApiResponse<StorefrontCartResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "Validate")] = new(
                        "StorefrontCart_Validate",
                        "Validate the server cart without changing it.",
                        typeof(CommerceNodeApiResponse<StorefrontCartValidationResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "Recalculate")] = new(
                        "StorefrontCart_Recalculate",
                        "Recalculate server cart snapshots.",
                        typeof(CommerceNodeApiResponse<StorefrontCartResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "MergeCurrentCustomer")] = new(
                        "StorefrontCart_MergeCurrentCustomer",
                        "Merge the current guest cart into the authenticated customer cart.",
                        typeof(CommerceNodeApiResponse<StorefrontCartResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedCurrency", "SetPreference")] = new(
                        "StorefrontCurrency_SetPreference",
                        "Set a Storefront currency preference.",
                        typeof(CommerceNodeApiResponse<StorefrontCurrencyPreferenceResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "Start")] = new(
                        "StorefrontCheckout_Start",
                        "Start or resume a Storefront checkout session.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutSessionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "Load")] = new(
                        "StorefrontCheckout_Load",
                        "Load a Storefront checkout session.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutSessionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "Cancel")] = new(
                        "StorefrontCheckout_Cancel",
                        "Cancel a Storefront checkout session.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutSessionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "UpdateAddresses")] = new(
                        "StorefrontCheckout_UpdateAddresses",
                        "Update checkout billing and shipping addresses.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutSessionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "SelectShippingMethod")] = new(
                        "StorefrontCheckout_SelectShippingMethod",
                        "Select a checkout shipping method.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutSessionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "SelectPaymentMethod")] = new(
                        "StorefrontCheckout_SelectPaymentMethod",
                        "Select a checkout payment method.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutSessionResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "Review")] = new(
                        "StorefrontCheckout_Review",
                        "Review a checkout before placing an order.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutReviewResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "Preview")] = new(
                        "StorefrontCheckout_Preview",
                        "Preview and validate a Storefront checkout session.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutPreviewResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCheckout", "PlaceOrder")] = new(
                        "StorefrontCheckout_PlaceOrder",
                        "Place a COD order from a checkout session.",
                        typeof(CommerceNodeApiResponse<StorefrontPlaceOrderResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedNewsletter", "Subscribe")] = new(
                        "StorefrontNewsletter_Subscribe",
                        "Subscribe an email to the Storefront newsletter.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedContact", "Submit")] = new(
                        "StorefrontContact_Submit",
                        "Submit a Storefront contact request.",
                        typeof(CommerceNodeApiResponse<StorefrontContactResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedOrders", "GetCurrentUserOrders")] = new(
                        "StorefrontOrders_ListCurrentUserOrders",
                        "List orders for the current customer.",
                        typeof(CommerceNodeApiResponse<StorefrontPagedResponse<StorefrontCustomerOrderListItemResponse>>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedOrders", "GetCurrentUserOrder")] = new(
                        "StorefrontOrders_GetCurrentUserOrder",
                        "Get an order for the current customer.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerOrderDetailResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedOrders", "GetCurrentUserOrderReceipt")] = new(
                        "StorefrontOrders_GetCurrentUserOrderReceipt",
                        "Get a receipt projection for the current customer order.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerOrderDetailResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedOrders", "GetGuestOrder")] = new(
                        "StorefrontOrders_GetGuestOrder",
                        "Get a guest order by reference and access token.",
                        typeof(CommerceNodeApiResponse<StorefrontCustomerOrderDetailResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedPages", "GetBySlug")] = new(
                        "StorefrontPages_GetBySlug",
                        "Get a published Storefront page by slug.",
                        typeof(CommerceNodeApiResponse<StorefrontPagePublicDto>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedPages", "ListNavigation")] = new(
                        "StorefrontPages_ListNavigation",
                        "List published Storefront content navigation links.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontPageNavigationLinkDto>>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedNavigation", "GetMenu")] = new(
                        "StorefrontNavigation_GetMenu",
                        "Get a Storefront navigation menu.",
                        typeof(CommerceNodeApiResponse<StoreNavigationPublicMenuDto>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedConfiguration", "Get")] = new(
                        "StorefrontConfiguration_Get",
                        "Get public Storefront configuration.",
                        typeof(CommerceNodeApiResponse<StorefrontPublicConfigurationResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedConsent", "Current")] = new(
                        "StorefrontConsent_Current",
                        "Get the current Storefront consent state.",
                        typeof(CommerceNodeApiResponse<StorefrontConsentResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedConsent", "Save")] = new(
                        "StorefrontConsent_Save",
                        "Save Storefront consent category selections.",
                        typeof(CommerceNodeApiResponse<StorefrontConsentResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status429TooManyRequests, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedConsent", "Revoke")] = new(
                        "StorefrontConsent_Revoke",
                        "Revoke Storefront optional consent categories.",
                        typeof(CommerceNodeApiResponse<StorefrontConsentResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status429TooManyRequests, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedPayments", "GetPaymentMethods")] = new(
                        "StorefrontPayments_ListMethods",
                        "List enabled Storefront payment methods.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontPaymentMethodResponse>>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedPayments", "GetAttempt")] = new(
                        "StorefrontPayments_GetAttempt",
                        "Get a Storefront payment attempt.",
                        typeof(CommerceNodeApiResponse<StorefrontPaymentAttemptResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedPayments", "HandleProviderCallback")] = new(
                        "StorefrontPayments_HandleProviderCallback",
                        "Accept a Storefront payment provider callback.",
                        typeof(CommerceNodeApiResponse<StorefrontPaymentWebhookAcceptedResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedPayments", "HandleWebhook")] = new(
                        "StorefrontPayments_HandleWebhook",
                        "Accept a Storefront payment provider webhook.",
                        typeof(CommerceNodeApiResponse<StorefrontPaymentWebhookAcceptedResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedRecommendations", "GetRecommendations")] = new(
                        "StorefrontRecommendations_ListProductRecommendations",
                        "List Storefront product recommendations.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontProductRecommendationResponse>>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedSeo", "GetSettings")] = new(
                        "StorefrontSeo_GetSettings",
                        "Get Storefront SEO settings.",
                        typeof(CommerceNodeApiResponse<SeoSettingsDto>),
                        [StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedSeo", "ResolveRedirect")] = new(
                        "StorefrontSeo_ResolveRedirect",
                        "Resolve a Storefront SEO redirect.",
                        typeof(CommerceNodeApiResponse<SeoRedirectResolutionDto>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedStore", "Current")] = new(
                        "StorefrontStore_GetCurrent",
                        "Get the current Storefront store.",
                        typeof(CommerceNodeApiResponse<StorefrontCurrentStoreResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedStore", "Maintenance")] = new(
                        "StorefrontStore_GetMaintenance",
                        "Get the current Storefront maintenance state.",
                        typeof(CommerceNodeApiResponse<StorefrontMaintenanceResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                };

            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var relativePath = NormalizePath(context.ApiDescription.RelativePath);
                if (!relativePath.StartsWith("api/storefront/stores/{storekey}/", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
                {
                    return;
                }

                var controllerName = actionDescriptor.ControllerName;
                var actionName = actionDescriptor.ActionName;
                if (!Metadata.TryGetValue((controllerName, actionName), out var metadata))
                {
                    return;
                }

                operation.OperationId = metadata.OperationId;
                operation.Summary = metadata.Summary;
                operation.Description = metadata.Description;
                operation.Deprecated = metadata.Deprecated;

                if (operation.RequestBody is OpenApiRequestBody requestBody)
                {
                    requestBody.Required = true;
                }

                ApplySuccessResponse(operation, context, metadata.SuccessResponseType);
                ApplyErrorResponses(operation, context, metadata.ErrorStatusCodes);
                ApplySecurity(operation, context, metadata.Security);
                ApplyParameterMetadata(operation, metadata.OperationId);
            }

            private static void ApplySuccessResponse(
                OpenApiOperation operation,
                OperationFilterContext context,
                Type responseType)
            {
                operation.Responses ??= new OpenApiResponses();
                operation.Responses["200"] = new OpenApiResponse
                {
                    Description = "OK",
                    Content = CreateJsonContent(context, responseType),
                };
            }

            private static void ApplyErrorResponses(
                OpenApiOperation operation,
                OperationFilterContext context,
                IReadOnlyList<int> statusCodes)
            {
                foreach (var statusCode in statusCodes.Distinct())
                {
                    operation.Responses ??= new OpenApiResponses();
                    operation.Responses[statusCode.ToString()] = new OpenApiResponse
                    {
                        Description = GetErrorDescription(statusCode),
                        Content = CreateJsonContent(context, typeof(CommerceNodeApiErrorResponse)),
                    };
                }
            }

            private static Dictionary<string, OpenApiMediaType> CreateJsonContent(
                OperationFilterContext context,
                Type responseType)
            {
                return new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new()
                    {
                        Schema = context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository),
                    },
                };
            }

            private static void ApplySecurity(
                OpenApiOperation operation,
                OperationFilterContext context,
                StorefrontSecurityRequirement explicitSecurity)
            {
                var security = explicitSecurity;
                if (security == StorefrontSecurityRequirement.None
                    && RequiresAuthorization(context.MethodInfo))
                {
                    security = StorefrontSecurityRequirement.Bearer;
                }

                if (security == StorefrontSecurityRequirement.None)
                {
                    return;
                }

                operation.Security ??= [];
                operation.Security.Clear();
                operation.Security.Add(CreateSecurityRequirement(security));
            }

            private static void ApplyParameterMetadata(OpenApiOperation operation, string operationId)
            {
                if (!string.Equals(operationId, "StorefrontCatalog_QueryProducts", StringComparison.Ordinal))
                {
                    return;
                }

                var sortByParameter = operation.Parameters?
                    .FirstOrDefault(parameter => string.Equals(parameter.Name, "sortBy", StringComparison.OrdinalIgnoreCase));
                if (sortByParameter?.Schema is not OpenApiSchema sortBySchema)
                {
                    return;
                }

                sortBySchema.Type = JsonSchemaType.String;
                sortBySchema.Enum = StorefrontProductCatalogSortValues.All
                    .Select(value => JsonValue.Create(value)!)
                    .Cast<JsonNode>()
                    .ToList();
            }

            private static bool RequiresAuthorization(MethodInfo methodInfo)
            {
                var controllerType = methodInfo.DeclaringType;
                var hasAllowAnonymous = methodInfo.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any()
                    || controllerType?.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any() == true;
                if (hasAllowAnonymous)
                {
                    return false;
                }

                return methodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any()
                    || controllerType?.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any() == true;
            }

            private static OpenApiSecurityRequirement CreateSecurityRequirement(StorefrontSecurityRequirement security)
            {
                var schemeName = security == StorefrontSecurityRequirement.RefreshCookie ? "RefreshCookie" : "Bearer";
                return new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(schemeName, null!, null)
                    {
                        Reference = new OpenApiReferenceWithDescription
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = schemeName,
                        },
                    }] = [],
                };
            }

            private static string GetErrorDescription(int statusCode)
            {
                return statusCode switch
                {
                    StatusCodes.Status400BadRequest => "Bad Request",
                    StatusCodes.Status401Unauthorized => "Unauthorized",
                    StatusCodes.Status403Forbidden => "Forbidden",
                    StatusCodes.Status404NotFound => "Not Found",
                    StatusCodes.Status409Conflict => "Conflict",
                    StatusCodes.Status500InternalServerError => "Internal Server Error",
                    _ => "Error",
                };
            }
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
