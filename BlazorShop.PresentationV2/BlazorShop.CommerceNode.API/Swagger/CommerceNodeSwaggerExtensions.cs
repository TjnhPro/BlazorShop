namespace BlazorShop.CommerceNode.API.Swagger
{
    using System.Reflection;
    using System.Text.Json.Nodes;

    using BlazorShop.Application.CommerceNode.StorefrontPages;
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

        private sealed class StorefrontOperationMetadataFilter : IOperationFilter
        {
            private static readonly IReadOnlyDictionary<(string Controller, string Action), StorefrontOperationMetadata> Metadata =
                new Dictionary<(string Controller, string Action), StorefrontOperationMetadata>
                {
                    [("StorefrontScopedAuth", "Register")] = new(
                        "StorefrontAuth_Register",
                        "Register a Storefront customer.",
                        typeof(CommerceNodeApiResponse<StorefrontRegistrationResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
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
                    [("StorefrontScopedCatalog", "GetSitemap")] = new(
                        "StorefrontCatalog_GetSitemap",
                        "Get the published catalog sitemap.",
                        typeof(CommerceNodeApiResponse<GetPublicCatalogSitemap>),
                        [StatusCodes.Status500InternalServerError]),

                    [("StorefrontScopedCart", "Checkout")] = new(
                        "StorefrontCart_Checkout",
                        "Create a Storefront checkout order.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutResultResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
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
                        "Validate and reprice the server cart.",
                        typeof(CommerceNodeApiResponse<StorefrontCartValidationResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedCart", "SaveCheckout")] = new(
                        "StorefrontCart_SaveCheckout",
                        "Save the current customer's checkout history.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedNewsletter", "Subscribe")] = new(
                        "StorefrontNewsletter_Subscribe",
                        "Subscribe an email to the Storefront newsletter.",
                        typeof(CommerceNodeApiResponse),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedOrders", "ConfirmOrder")] = new(
                        "StorefrontOrders_Confirm",
                        "Confirm an order for the current customer.",
                        typeof(CommerceNodeApiResponse<StorefrontCheckoutResultResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedOrders", "GetCurrentUserOrders")] = new(
                        "StorefrontOrders_ListCurrentUserOrders",
                        "List orders for the current customer.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontOrderResponse>>),
                        [StatusCodes.Status401Unauthorized, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedOrders", "GetCurrentUserOrderItems")] = new(
                        "StorefrontOrders_ListCurrentUserOrderItems",
                        "List order items for the current customer.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontOrderItemHistoryResponse>>),
                        [StatusCodes.Status401Unauthorized, StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError],
                        Security: StorefrontSecurityRequirement.Bearer),
                    [("StorefrontScopedPages", "GetBySlug")] = new(
                        "StorefrontPages_GetBySlug",
                        "Get a published Storefront page by slug.",
                        typeof(CommerceNodeApiResponse<StorefrontPagePublicDto>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedPayments", "GetPaymentMethods")] = new(
                        "StorefrontPayments_ListMethods",
                        "List enabled Storefront payment methods.",
                        typeof(CommerceNodeApiResponse<IReadOnlyList<StorefrontPaymentMethodResponse>>),
                        [StatusCodes.Status404NotFound, StatusCodes.Status500InternalServerError]),
                    [("StorefrontScopedPayments", "CapturePayPal")] = new(
                        "StorefrontPayments_CapturePayPal",
                        "Capture a PayPal Storefront payment.",
                        typeof(CommerceNodeApiResponse<StorefrontPayPalCaptureResponse>),
                        [StatusCodes.Status400BadRequest, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError]),
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
            StorefrontSecurityRequirement Security = StorefrontSecurityRequirement.None);

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
                    ["StorefrontCart_SaveCheckout"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontOrders_Confirm"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontOrders_ListCurrentUserOrders"] = StorefrontSecurityRequirement.Bearer,
                    ["StorefrontOrders_ListCurrentUserOrderItems"] = StorefrontSecurityRequirement.Bearer,
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
                    [typeof(StorefrontCartResponse)] = ["lines"],
                    [typeof(StorefrontCartValidationResponse)] = ["issues"],
                    [typeof(StorefrontOrderResponse)] = ["lines"],
                    [typeof(StorefrontOrderLineResponse)] = ["variantAttributes"],
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
