extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.OpenApi;
    using Microsoft.OpenApi.Reader;

    using Xunit;

    using CommerceNodeProgram = CommerceNodeApi::Program;

    public sealed class CommerceNodeStorefrontOpenApiContractTests : IClassFixture<WebApplicationFactory<CommerceNodeProgram>>
    {
        private const string StorefrontSwaggerPath = "/swagger/storefront/swagger.json";
        private const string PathSnapshotPath = "PresentationV2/CommerceNode/Snapshots/storefront-openapi.paths.snapshot.txt";
        private const string SwaggerSnapshotPath = "PresentationV2/CommerceNode/Snapshots/storefront-openapi.snapshot.json";

        private static readonly string[] ProtectedOperationIds =
        [
            "StorefrontAuth_ChangePassword",
            "StorefrontAuth_UpdateProfile",
            "StorefrontCustomerAddresses_List",
            "StorefrontCustomerAddresses_Create",
            "StorefrontCustomerAddresses_Update",
            "StorefrontCustomerAddresses_Delete",
            "StorefrontCustomerAddresses_SetDefaultShipping",
            "StorefrontCustomerAddresses_SetDefaultBilling",
            "StorefrontCart_MergeCurrentCustomer",
            "StorefrontCart_SaveCheckout",
            "StorefrontOrders_Confirm",
            "StorefrontOrders_ListCurrentUserOrders",
            "StorefrontOrders_ListCurrentUserOrderItems",
        ];

        private static readonly IReadOnlyDictionary<string, string> ProtectedOperationSecuritySchemes =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["StorefrontAuth_RefreshToken"] = "RefreshCookie",
                ["StorefrontAuth_Logout"] = "RefreshCookie",
                ["StorefrontAuth_ChangePassword"] = "Bearer",
                ["StorefrontAuth_UpdateProfile"] = "Bearer",
                ["StorefrontCustomerAddresses_List"] = "Bearer",
                ["StorefrontCustomerAddresses_Create"] = "Bearer",
                ["StorefrontCustomerAddresses_Update"] = "Bearer",
                ["StorefrontCustomerAddresses_Delete"] = "Bearer",
                ["StorefrontCustomerAddresses_SetDefaultShipping"] = "Bearer",
                ["StorefrontCustomerAddresses_SetDefaultBilling"] = "Bearer",
                ["StorefrontCart_MergeCurrentCustomer"] = "Bearer",
                ["StorefrontCart_SaveCheckout"] = "Bearer",
                ["StorefrontOrders_Confirm"] = "Bearer",
                ["StorefrontOrders_ListCurrentUserOrders"] = "Bearer",
                ["StorefrontOrders_ListCurrentUserOrderItems"] = "Bearer",
            };

        private static readonly (string SchemaName, string PropertyName)[] NonNullableResponseCollections =
        [
            ("StorefrontPagedResponse", "items"),
            ("StorefrontCategoryTreeNodeResponse", "children"),
            ("StorefrontCategoryPageResponse", "breadcrumbs"),
            ("StorefrontCategoryPageResponse", "products"),
            ("StorefrontProductResponse", "variants"),
            ("StorefrontProductVariantResponse", "attributes"),
            ("StorefrontProductSelectionPreviewResponse", "validationMessages"),
            ("StorefrontProductSelectionPreviewResponse", "selectedAttributes"),
            ("StorefrontCartResponse", "lines"),
            ("StorefrontCartValidationResponse", "issues"),
            ("StorefrontCheckoutPreviewResponse", "completedSteps"),
            ("StorefrontCheckoutPreviewResponse", "lines"),
            ("StorefrontCheckoutPreviewResponse", "issues"),
            ("StorefrontCheckoutSessionResponse", "completedSteps"),
            ("StorefrontCheckoutSessionResponse", "shippingOptions"),
            ("StorefrontCheckoutSessionResponse", "paymentMethods"),
            ("StorefrontCheckoutSessionResponse", "lines"),
            ("StorefrontCheckoutSessionResponse", "issues"),
            ("StorefrontCheckoutReviewResponse", "completedSteps"),
            ("StorefrontCheckoutReviewResponse", "lines"),
            ("StorefrontCheckoutReviewResponse", "issues"),
            ("StorefrontOrderResponse", "trackingEvents"),
            ("StorefrontOrderResponse", "historyEntries"),
            ("StorefrontOrderResponse", "lines"),
            ("StorefrontOrderLineResponse", "variantAttributes"),
            ("StorefrontProductFilterMetadataResponse", "pageSizes"),
            ("StorefrontProductFilterMetadataResponse", "sortOptions"),
            ("StorefrontProductFilterMetadataResponse", "facets"),
            ("StorefrontFilterFacetResponse", "choices"),
            ("StorefrontSearchSuggestionResponse", "items"),
            ("GetPublicCatalogSitemap", "categories"),
            ("GetPublicCatalogSitemap", "products"),
            ("GetPublicCatalogSitemap", "pages"),
            ("StorefrontVariationTemplateDto", "options"),
            ("StorefrontVariationOptionDto", "values"),
        ];

        private static readonly string[] MonetaryPropertyNames =
        [
            "price",
            "comparePrice",
            "effectivePrice",
            "unitPrice",
            "lineTotal",
            "totalAmount",
            "amountPaid",
        ];

        private static readonly string[] PublicConfigurationSchemaNames =
        [
            "StorefrontPublicConfigurationResponse",
            "StorefrontStoreIdentityResponse",
            "StorefrontBrandingResponse",
            "StorefrontLocaleOptionsResponse",
            "StorefrontCurrencyOptionsResponse",
            "StorefrontMaintenanceStateResponse",
            "StorefrontFeatureFlagsResponse",
            "StorefrontSeoDefaultsResponse",
            "StorefrontCurrentStoreResponse",
            "StorefrontPaymentMethodResponse",
            "SeoSettingsDto",
        ];

        private static readonly string[] ForbiddenPublicConfigurationPropertyNames =
        [
            "settingsJson",
            "metadataJson",
            "smtpPassword",
            "password",
            "secret",
            "nodeSecret",
            "nodeKey",
            "privateKey",
            "apiKey",
            "accessToken",
            "refreshToken",
            "createdBy",
            "createdByAdminUserId",
            "updatedBy",
            "deletedAt",
            "archivedAt",
            "controlPlaneStorePublicId",
        ];

        private readonly WebApplicationFactory<CommerceNodeProgram> factory;

        public CommerceNodeStorefrontOpenApiContractTests(WebApplicationFactory<CommerceNodeProgram> factory)
        {
            this.factory = factory;
        }

        [Fact]
        public async Task StorefrontSwagger_CanBeFetchedAndParsed()
        {
            using var client = this.CreateSwaggerClient();

            using var response = await client.GetAsync(StorefrontSwaggerPath);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            Assert.Equal("3.0.4", root.GetProperty("openapi").GetString());
            Assert.Equal("Storefront API", root.GetProperty("info").GetProperty("title").GetString());
            Assert.True(root.GetProperty("paths").EnumerateObject().Any());
            Assert.True(root.GetProperty("components").GetProperty("schemas").EnumerateObject().Any());
        }

        [Fact]
        public async Task StorefrontSwagger_PassesOpenApiReaderValidation()
        {
            var swagger = await this.GetStorefrontSwaggerTextAsync();
            var result = OpenApiDocument.Parse(swagger, "json", new OpenApiReaderSettings());
            var errors = result.Diagnostic?.Errors.Select(error => error.ToString()).ToArray()
                ?? [];

            Assert.NotNull(result.Document);
            Assert.Empty(errors);
        }

        [Fact]
        public async Task StorefrontSwagger_PathSnapshotMatchesBaseline()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var actual = GetPathSnapshot(swagger);
            var expected = await File.ReadAllTextAsync(GetSnapshotAbsolutePath(PathSnapshotPath));

            Assert.Equal(NormalizeLineEndings(expected), actual);
        }

        [Fact]
        public async Task StorefrontSwagger_FullSnapshotMatchesBaseline()
        {
            var swagger = await this.GetStorefrontSwaggerTextAsync();
            var expected = await File.ReadAllTextAsync(GetSnapshotAbsolutePath(SwaggerSnapshotPath));

            Assert.Equal(NormalizeJson(expected), NormalizeJson(swagger));
        }

        [Fact]
        public async Task StorefrontSwagger_AllOperationsHaveStableMetadataAndResponseSchemas()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var operations = GetOperations(swagger).ToArray();

            Assert.NotEmpty(operations);

            foreach (var operation in operations)
            {
                var operationId = operation.Value["operationId"]?.GetValue<string>();
                var summary = operation.Value["summary"]?.GetValue<string>();
                var responses = operation.Value["responses"]?.AsObject();

                Assert.False(string.IsNullOrWhiteSpace(operationId));
                Assert.False(string.IsNullOrWhiteSpace(summary));
                Assert.NotNull(responses);
                Assert.True(responses!.Count > 1, $"{operationId} must not declare only 200 OK.");

                foreach (var response in responses)
                {
                    var content = response.Value?["content"]?.AsObject();
                    Assert.NotNull(content);

                    var schema = content!["application/json"]?["schema"];
                    Assert.NotNull(schema);
                    Assert.True(HasSchemaShape(schema!), $"{operationId} {response.Key} must have a response schema.");
                }
            }
        }

        [Fact]
        public async Task StorefrontSwagger_ProtectedOperationsHaveSecurityMetadata()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var operations = GetOperations(swagger)
                .ToDictionary(
                    operation => operation.Value["operationId"]?.GetValue<string>() ?? string.Empty,
                    operation => operation.Value,
                    StringComparer.Ordinal);

            foreach (var operationId in ProtectedOperationIds)
            {
                Assert.True(operations.TryGetValue(operationId, out var operation), $"{operationId} was not found.");
                Assert.NotNull(operation!["security"]);
            }
        }

        [Fact]
        public async Task StorefrontSwagger_PublicSchemasDoNotExposeUnsafeContracts()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = swagger["components"]?["schemas"]?.AsObject()
                ?? throw new InvalidOperationException("Swagger document does not contain component schemas.");

            var unsafeSchemaNames = new[]
            {
                "CreateOrderItem",
                "ProcessCart",
                "ProductCatalogQuery",
                "Product",
                "Category",
                "OrderItem",
                "CommerceStoreDetail",
            };

            foreach (var unsafeSchemaName in unsafeSchemaNames)
            {
                Assert.DoesNotContain(schemas.Select(schema => schema.Key), name => string.Equals(name, unsafeSchemaName, StringComparison.Ordinal));
            }

            var serializedSchemas = schemas.ToJsonString();
            Assert.DoesNotContain("\"userId\"", serializedSchemas, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"isPublished\"", serializedSchemas, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task StorefrontSwagger_PublicConfigurationSchemasDoNotExposeSecretsOrInternalFields()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);

            foreach (var schemaName in PublicConfigurationSchemaNames)
            {
                var schema = schemas[schemaName]?.AsObject()
                    ?? throw new InvalidOperationException($"{schemaName} schema was not found.");
                var propertyNames = GetPropertyNames(schema).ToArray();

                foreach (var forbidden in ForbiddenPublicConfigurationPropertyNames)
                {
                    Assert.DoesNotContain(forbidden, propertyNames, StringComparer.OrdinalIgnoreCase);
                }
            }

            var currentStore = schemas["StorefrontCurrentStoreResponse"]!.AsObject();
            var currentStoreProperties = GetPropertyNames(currentStore).ToArray();
            Assert.Contains("defaultCurrencyCode", currentStoreProperties);
            Assert.Contains("defaultCulture", currentStoreProperties);
            Assert.Contains("maintenanceModeEnabled", currentStoreProperties);

            var paymentMethod = schemas["StorefrontPaymentMethodResponse"]!.AsObject();
            var paymentMethodProperties = GetPropertyNames(paymentMethod).ToArray();
            Assert.Contains("key", paymentMethodProperties);
            Assert.Contains("name", paymentMethodProperties);
            Assert.Contains("description", paymentMethodProperties);

            var category = schemas["StorefrontCategoryResponse"]!.AsObject();
            var categoryProperties = GetPropertyNames(category).ToArray();
            Assert.Contains("description", categoryProperties);

            var publicConfiguration = schemas["StorefrontPublicConfigurationResponse"]!.AsObject();
            var publicConfigurationProperties = GetPropertyNames(publicConfiguration).ToArray();
            Assert.Contains("storeIdentity", publicConfigurationProperties);
            Assert.Contains("branding", publicConfigurationProperties);
            Assert.Contains("localeOptions", publicConfigurationProperties);
            Assert.Contains("currencyOptions", publicConfigurationProperties);
            Assert.Contains("maintenanceState", publicConfigurationProperties);
            Assert.Contains("featureFlags", publicConfigurationProperties);
            Assert.Contains("paymentMethods", publicConfigurationProperties);
            Assert.Contains("seoDefaults", publicConfigurationProperties);
        }

        [Fact]
        public async Task StorefrontSwagger_PublicConfigurationEndpointHasGeneratorSafeContract()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var operation = GetOperation(swagger, "StorefrontConfiguration_Get");

            Assert.False(string.IsNullOrWhiteSpace(operation["summary"]?.GetValue<string>()));
            Assert.True(operation["responses"]?.AsObject().Count > 1);
            Assert.Null(operation["requestBody"]);
            Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(operation));
            Assert.True(schemas.ContainsKey("StorefrontPublicConfigurationResponse"));
            Assert.True(schemas.ContainsKey("StorefrontSeoDefaultsResponse"));
        }

        [Fact]
        public async Task StorefrontSwagger_AuthRecoveryEndpointsHaveGeneratorSafeContracts()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var operations = GetOperations(swagger)
                .ToDictionary(
                    operation => operation.Value["operationId"]?.GetValue<string>() ?? string.Empty,
                    operation => operation.Value,
                    StringComparer.Ordinal);

            var registrationPolicy = operations["StorefrontAuth_GetRegistrationPolicy"];
            Assert.False(string.IsNullOrWhiteSpace(registrationPolicy["summary"]?.GetValue<string>()));
            Assert.True(registrationPolicy["responses"]?.AsObject().Count > 1);
            Assert.Null(registrationPolicy["requestBody"]);
            Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(registrationPolicy));

            var forgotPassword = operations["StorefrontAuth_ForgotPassword"];
            Assert.False(string.IsNullOrWhiteSpace(forgotPassword["summary"]?.GetValue<string>()));
            Assert.True(forgotPassword["responses"]?.AsObject().Count > 1);
            AssertRequiredRequestBody(forgotPassword);
            Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(forgotPassword));

            var resetPassword = operations["StorefrontAuth_ResetPassword"];
            Assert.False(string.IsNullOrWhiteSpace(resetPassword["summary"]?.GetValue<string>()));
            Assert.True(resetPassword["responses"]?.AsObject().Count > 1);
            AssertRequiredRequestBody(resetPassword);
            Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(resetPassword));

            var policySchema = schemas["StorefrontRegistrationPolicyResponse"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontRegistrationPolicyResponse schema was not found.");
            Assert.Contains("mode", GetPropertyNames(policySchema));
            Assert.Contains("registrationAllowed", GetPropertyNames(policySchema));

            var forgotSchema = schemas["StorefrontForgotPasswordRequest"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontForgotPasswordRequest schema was not found.");
            Assert.Contains("email", GetRequiredProperties(forgotSchema));
            Assert.Equal("email", forgotSchema["properties"]?["email"]?["format"]?.GetValue<string>());
            Assert.Equal(254, forgotSchema["properties"]?["email"]?["maxLength"]?.GetValue<int>());
            Assert.DoesNotContain("userId", GetPropertyNames(forgotSchema), StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("customerId", GetPropertyNames(forgotSchema), StringComparer.OrdinalIgnoreCase);

            var resetSchema = schemas["StorefrontResetPasswordRequest"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontResetPasswordRequest schema was not found.");
            var required = GetRequiredProperties(resetSchema).ToArray();
            Assert.Contains("email", required);
            Assert.Contains("token", required);
            Assert.Contains("password", required);
            Assert.Contains("confirmPassword", required);
            Assert.Equal("email", resetSchema["properties"]?["email"]?["format"]?.GetValue<string>());
            Assert.Equal(8, resetSchema["properties"]?["password"]?["minLength"]?.GetValue<int>());
            Assert.DoesNotContain("userId", GetPropertyNames(resetSchema), StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("customerId", GetPropertyNames(resetSchema), StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task StorefrontSwagger_CustomerAddressBookHasGeneratorSafeContract()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var operations = GetOperations(swagger)
                .ToDictionary(
                    operation => operation.Value["operationId"]?.GetValue<string>() ?? string.Empty,
                    operation => operation.Value,
                    StringComparer.Ordinal);

            var expectedOperationIds = new[]
            {
                "StorefrontCustomerAddresses_List",
                "StorefrontCustomerAddresses_Create",
                "StorefrontCustomerAddresses_Update",
                "StorefrontCustomerAddresses_Delete",
                "StorefrontCustomerAddresses_SetDefaultShipping",
                "StorefrontCustomerAddresses_SetDefaultBilling",
            };

            foreach (var operationId in expectedOperationIds)
            {
                Assert.True(operations.TryGetValue(operationId, out var operation), $"{operationId} was not found.");
                Assert.False(string.IsNullOrWhiteSpace(operation!["summary"]?.GetValue<string>()));
                Assert.True(operation["responses"]?.AsObject().Count > 1, $"{operationId} must declare success and error responses.");
                Assert.Contains("Bearer", GetSecuritySchemeNames(operation));
            }

            AssertRequiredRequestBody(operations["StorefrontCustomerAddresses_Create"]);
            AssertRequiredRequestBody(operations["StorefrontCustomerAddresses_Update"]);
            Assert.Null(operations["StorefrontCustomerAddresses_Delete"]["requestBody"]);
            Assert.Null(operations["StorefrontCustomerAddresses_SetDefaultShipping"]["requestBody"]);
            Assert.Null(operations["StorefrontCustomerAddresses_SetDefaultBilling"]["requestBody"]);

            var requestSchema = schemas["StorefrontCustomerAddressRequest"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCustomerAddressRequest schema was not found.");
            var requestProperties = GetPropertyNames(requestSchema).ToArray();

            Assert.Contains("firstName", requestProperties);
            Assert.Contains("lastName", requestProperties);
            Assert.Contains("address1", requestProperties);
            Assert.Contains("city", requestProperties);
            Assert.Contains("postalCode", requestProperties);
            Assert.Contains("countryCode", requestProperties);
            Assert.Contains("email", requestProperties);
            Assert.DoesNotContain("customerId", requestProperties, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("storeId", requestProperties, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("createdAtUtc", requestProperties, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("updatedAtUtc", requestProperties, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("deletedAtUtc", requestProperties, StringComparer.OrdinalIgnoreCase);

            Assert.Contains("firstName", GetRequiredProperties(requestSchema));
            Assert.Contains("lastName", GetRequiredProperties(requestSchema));
            Assert.Contains("address1", GetRequiredProperties(requestSchema));
            Assert.Contains("city", GetRequiredProperties(requestSchema));
            Assert.Contains("postalCode", GetRequiredProperties(requestSchema));
            Assert.Contains("countryCode", GetRequiredProperties(requestSchema));
            Assert.Equal("email", requestSchema["properties"]?["email"]?["format"]?.GetValue<string>());
            Assert.Equal(120, requestSchema["properties"]?["firstName"]?["maxLength"]?.GetValue<int>());
            Assert.Equal(240, requestSchema["properties"]?["address1"]?["maxLength"]?.GetValue<int>());
            Assert.True(schemas.ContainsKey("StorefrontCustomerAddressResponse"));
        }

        [Fact]
        public async Task StorefrontSwagger_ProductSelectionPreviewHasGeneratorSafeContract()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var operation = GetOperation(swagger, "StorefrontCatalog_PreviewProductSelection");

            Assert.False(string.IsNullOrWhiteSpace(operation["summary"]?.GetValue<string>()));
            Assert.True(operation["responses"]?.AsObject().Count > 1);
            AssertRequiredRequestBody(operation);
            Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(operation));

            var requestSchema = ResolveRequestBodySchema(operation, schemas);
            var requestProperties = GetPropertyNames(requestSchema);
            Assert.Contains("productVariantId", requestProperties);
            Assert.Contains("selectedAttributes", requestProperties);
            Assert.Contains("quantity", requestProperties);
            Assert.Contains("currencyCode", requestProperties);
            Assert.Equal(1, requestSchema["properties"]?["quantity"]?["minimum"]?.GetValue<int>());

            Assert.True(schemas.ContainsKey("StorefrontProductSelectionPreviewRequest"));
            Assert.True(schemas.ContainsKey("StorefrontProductSelectionPreviewResponse"));

            var responseSchema = schemas["StorefrontProductSelectionPreviewResponse"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontProductSelectionPreviewResponse schema was not found.");
            var responseProperties = GetPropertyNames(responseSchema);
            Assert.Contains("canAddToCart", responseProperties);
            Assert.Contains("validationMessages", responseProperties);
            Assert.Contains("selectedAttributes", responseProperties);
            Assert.Contains("primaryImageUrl", responseProperties);
            Assert.DoesNotContain("product", responseProperties, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("variant", responseProperties, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task StorefrontSwagger_ProductFilterMetadataHasGeneratorSafeContract()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var operation = GetOperation(swagger, "StorefrontCatalog_GetProductFilterMetadata");

            Assert.False(string.IsNullOrWhiteSpace(operation["summary"]?.GetValue<string>()));
            Assert.True(operation["responses"]?.AsObject().Count > 1);
            Assert.Null(operation["requestBody"]);
            Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(operation));

            var parameters = operation["parameters"]?.AsArray()
                ?? throw new InvalidOperationException("Filter metadata operation does not contain parameters.");
            Assert.Contains(parameters, parameter =>
                string.Equals(parameter?["name"]?.GetValue<string>(), "categorySlug", StringComparison.Ordinal));
            Assert.Contains(parameters, parameter =>
                string.Equals(parameter?["name"]?.GetValue<string>(), "searchTerm", StringComparison.Ordinal));
            Assert.Contains(parameters, parameter =>
                string.Equals(parameter?["name"]?.GetValue<string>(), "currencyCode", StringComparison.Ordinal));

            Assert.True(schemas.ContainsKey("StorefrontProductFilterMetadataResponse"));
            Assert.True(schemas.ContainsKey("StorefrontFilterFacetResponse"));
            Assert.True(schemas.ContainsKey("StorefrontFilterChoiceResponse"));
            Assert.True(schemas.ContainsKey("StorefrontPriceFacetResponse"));
            Assert.True(schemas.ContainsKey("StorefrontProductSortOptionResponse"));

            var metadataSchema = schemas["StorefrontProductFilterMetadataResponse"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontProductFilterMetadataResponse schema was not found.");
            var properties = GetPropertyNames(metadataSchema).ToArray();
            Assert.Contains("pageSizes", properties);
            Assert.Contains("sortOptions", properties);
            Assert.Contains("facets", properties);
            Assert.Contains("priceRange", properties);
            Assert.Contains("minimumSearchTermLength", properties);
        }

        [Fact]
        public async Task StorefrontSwagger_SearchSuggestionsHaveGeneratorSafeContract()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var operation = GetOperation(swagger, "StorefrontCatalog_GetSearchSuggestions");

            Assert.False(string.IsNullOrWhiteSpace(operation["summary"]?.GetValue<string>()));
            Assert.True(operation["responses"]?.AsObject().Count > 1);
            Assert.Null(operation["requestBody"]);
            Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(operation));

            var parameters = operation["parameters"]?.AsArray()
                ?? throw new InvalidOperationException("Search suggestions operation does not contain parameters.");
            Assert.Contains(parameters, parameter =>
                string.Equals(parameter?["name"]?.GetValue<string>(), "searchTerm", StringComparison.Ordinal));
            Assert.Contains(parameters, parameter =>
                string.Equals(parameter?["name"]?.GetValue<string>(), "categorySlug", StringComparison.Ordinal));
            Assert.Contains(parameters, parameter =>
                string.Equals(parameter?["name"]?.GetValue<string>(), "limit", StringComparison.Ordinal));
            Assert.Contains(parameters, parameter =>
                string.Equals(parameter?["name"]?.GetValue<string>(), "currencyCode", StringComparison.Ordinal));

            Assert.True(schemas.ContainsKey("StorefrontSearchSuggestionResponse"));
            Assert.True(schemas.ContainsKey("StorefrontSearchSuggestionItemResponse"));

            var responseSchema = schemas["StorefrontSearchSuggestionResponse"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontSearchSuggestionResponse schema was not found.");
            var responseProperties = GetPropertyNames(responseSchema).ToArray();
            Assert.Contains("minimumSearchTermLength", responseProperties);
            Assert.Contains("limit", responseProperties);
            Assert.Contains("items", responseProperties);

            var itemSchema = schemas["StorefrontSearchSuggestionItemResponse"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontSearchSuggestionItemResponse schema was not found.");
            var itemProperties = GetPropertyNames(itemSchema).ToArray();
            Assert.Contains("url", itemProperties);
            Assert.Contains("displayPrice", itemProperties);
            Assert.Contains("displayCurrencyCode", itemProperties);
            Assert.DoesNotContain("isPublished", itemProperties, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task StorefrontSwagger_ProductSellabilityProjectionHasGeneratorSafeContract()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);

            var catalogProduct = schemas["StorefrontCatalogProductResponse"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCatalogProductResponse schema was not found.");
            var product = schemas["StorefrontProductResponse"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontProductResponse schema was not found.");
            var variant = schemas["StorefrontProductVariantResponse"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontProductVariantResponse schema was not found.");

            foreach (var schema in new[] { catalogProduct, product })
            {
                var properties = GetPropertyNames(schema);
                Assert.Contains("purchasable", properties);
                Assert.Contains("purchaseBlockReasons", properties);
                Assert.Contains("stockStatus", properties);
                Assert.Contains("availableQuantity", properties);
                Assert.Contains("minOrderQuantity", properties);
                Assert.Contains("maxOrderQuantity", properties);
                Assert.Contains("quantityStep", properties);
                Assert.Contains("manageStock", properties);
                Assert.Contains("shippingRequired", properties);
                Assert.Contains("freeShipping", properties);
                Assert.Contains("deliveryEstimateText", properties);
                Assert.Contains("inStock", properties);
                Assert.Contains("quantity", properties);
            }

            var productProperties = GetPropertyNames(product);
            Assert.Contains("weight", productProperties);
            Assert.Contains("length", productProperties);
            Assert.Contains("width", productProperties);
            Assert.Contains("height", productProperties);

            var variantProperties = GetPropertyNames(variant);
            Assert.Contains("isActive", variantProperties);
            Assert.Contains("purchasable", variantProperties);
            Assert.Contains("purchaseBlockReasons", variantProperties);
            Assert.Contains("stockStatus", variantProperties);
            Assert.Contains("availableQuantity", variantProperties);
            Assert.Contains("stock", variantProperties);
        }

        [Fact]
        public async Task StorefrontSwagger_RiskyContractFixesAreReflectedInOpenApi()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = swagger["components"]?["schemas"]?.AsObject()
                ?? throw new InvalidOperationException("Swagger document does not contain component schemas.");

            var saveCheckout = GetOperation(swagger, "StorefrontCart_SaveCheckout");
            AssertRequiredRequestBody(saveCheckout);
            var saveCheckoutSchema = ResolveRequestBodySchema(saveCheckout, schemas);
            Assert.DoesNotContain("userId", GetPropertyNames(saveCheckoutSchema), StringComparer.OrdinalIgnoreCase);

            var confirm = GetOperation(swagger, "StorefrontOrders_Confirm");
            AssertRequiredRequestBody(confirm);
            Assert.DoesNotContain(
                confirm["parameters"]?.AsArray() ?? [],
                parameter => string.Equals(parameter?["name"]?.GetValue<string>(), "status", StringComparison.OrdinalIgnoreCase));

            var cartItem = schemas["StorefrontCartItemRequest"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCartItemRequest schema was not found.");
            Assert.Equal(1, cartItem["properties"]?["quantity"]?["minimum"]?.GetValue<int>());

            var orderItem = schemas["StorefrontOrderItemRequest"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontOrderItemRequest schema was not found.");
            Assert.Equal(1, orderItem["properties"]?["quantity"]?["minimum"]?.GetValue<int>());

            var catalog = GetOperation(swagger, "StorefrontCatalog_QueryProducts");
            var catalogParameters = catalog["parameters"]?.AsArray()
                ?? throw new InvalidOperationException("Catalog query operation does not contain parameters.");
            Assert.DoesNotContain(
                catalogParameters,
                parameter => string.Equals(parameter?["name"]?.GetValue<string>(), "isPublished", StringComparison.OrdinalIgnoreCase));

            var pageSize = GetParameter(catalogParameters, "pageSize");
            Assert.Equal(1, pageSize["schema"]?["minimum"]?.GetValue<int>());
            Assert.Equal(100, pageSize["schema"]?["maximum"]?.GetValue<int>());

            var sortBy = GetParameter(catalogParameters, "sortBy");
            Assert.Contains("newest", sortBy["schema"]?["pattern"]?.GetValue<string>(), StringComparison.Ordinal);

            var payPalCapture = GetOperation(swagger, "StorefrontPayments_CapturePayPal");
            Assert.NotNull(payPalCapture["requestBody"]);
            Assert.True(payPalCapture["deprecated"]?.GetValue<bool>() == true);
            Assert.Contains("Compatibility route retained", payPalCapture["description"]?.GetValue<string>(), StringComparison.Ordinal);
            Assert.Contains("provider operation contract", payPalCapture["description"]?.GetValue<string>(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task StorefrontSwagger_FinalHardening_DoesNotExposeObjectOrAuthDoubleEnvelope()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var schemaNames = schemas.Select(schema => schema.Key).ToArray();
            var serializedSchemas = schemas.ToJsonString();

            Assert.DoesNotContain("ObjectCommerceNodeApiResponse", schemaNames, StringComparer.Ordinal);
            Assert.DoesNotContain("StorefrontAuthResponseCommerceNodeApiResponse", schemaNames, StringComparer.Ordinal);
            Assert.DoesNotContain("ObjectCommerceNodeApiResponse", serializedSchemas, StringComparison.Ordinal);

            if (schemas.TryGetPropertyValue("StorefrontAuthResponse", out var legacyAuthSchema))
            {
                var legacyAuthProperties = GetPropertyNames(legacyAuthSchema!.AsObject()).ToArray();
                Assert.DoesNotContain("success", legacyAuthProperties, StringComparer.OrdinalIgnoreCase);
                Assert.DoesNotContain("message", legacyAuthProperties, StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task StorefrontSwagger_FinalHardening_ErrorResponseHasRequiredMachineReadableShape()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var errorSchema = schemas["CommerceNodeApiErrorResponse"]?.AsObject()
                ?? throw new InvalidOperationException("CommerceNodeApiErrorResponse schema was not found.");

            var required = GetRequiredProperties(errorSchema).ToArray();

            Assert.Contains("success", required, StringComparer.Ordinal);
            Assert.Contains("code", required, StringComparer.Ordinal);
            Assert.Contains("message", required, StringComparer.Ordinal);
            Assert.Contains("traceId", required, StringComparer.Ordinal);
            Assert.False(IsNullableProperty(errorSchema, "traceId"), "traceId must be required and non-nullable.");
        }

        [Fact]
        public async Task StorefrontSwagger_FinalHardening_PublicResponseCollectionsAreRequiredAndNonNullable()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var failures = new List<string>();

            foreach (var (schemaNameFragment, propertyName) in NonNullableResponseCollections)
            {
                var matchingSchemas = schemas
                    .Where(schema => schema.Key.Contains(schemaNameFragment, StringComparison.Ordinal))
                    .Select(schema => schema.Value?.AsObject())
                    .Where(schema => schema is not null)
                    .Cast<JsonObject>()
                    .Where(schema => schema["properties"]?.AsObject().ContainsKey(propertyName) == true)
                    .ToArray();

                if (matchingSchemas.Length == 0)
                {
                    failures.Add($"{schemaNameFragment}.{propertyName}: schema/property not found");
                    continue;
                }

                foreach (var schema in matchingSchemas)
                {
                    var required = GetRequiredProperties(schema);
                    if (!required.Contains(propertyName, StringComparer.Ordinal))
                    {
                        failures.Add($"{schemaNameFragment}.{propertyName}: not required");
                    }

                    if (IsNullableProperty(schema, propertyName))
                    {
                        failures.Add($"{schemaNameFragment}.{propertyName}: nullable");
                    }
                }
            }

            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        }

        [Fact]
        public async Task StorefrontSwagger_FinalHardening_PublicContractUsesAmountPaidAndDecimalMoneyFields()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var serializedSchemas = schemas.ToJsonString();

            Assert.DoesNotContain("amountPayed", serializedSchemas, StringComparison.Ordinal);
            Assert.Contains("amountPaid", serializedSchemas, StringComparison.Ordinal);

            foreach (var propertyName in MonetaryPropertyNames)
            {
                var moneySchemas = FindPropertySchemas(schemas, propertyName).ToArray();
                Assert.NotEmpty(moneySchemas);

                foreach (var schema in moneySchemas)
                {
                    Assert.Equal("number", schema["type"]?.GetValue<string>());
                }
            }
        }

        [Fact]
        public async Task StorefrontSwagger_FinalHardening_SortByIsNamedStringEnum()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var catalog = GetOperation(swagger, "StorefrontCatalog_QueryProducts");
            var catalogParameters = catalog["parameters"]?.AsArray()
                ?? throw new InvalidOperationException("Catalog query operation does not contain parameters.");
            var sortBy = GetParameter(catalogParameters, "sortBy");
            var schema = sortBy["schema"]?.AsObject()
                ?? throw new InvalidOperationException("sortBy parameter does not contain a schema.");

            Assert.Equal("string", schema["type"]?.GetValue<string>());
            var values = schema["enum"]?.AsArray().Select(value => value?.GetValue<string>()).ToArray()
                ?? throw new InvalidOperationException("sortBy parameter does not contain a named enum.");

            Assert.Contains("newest", values);
            Assert.Contains("priceLowToHigh", values);
            Assert.DoesNotContain(values, value => int.TryParse(value, out _));
        }

        [Fact]
        public async Task StorefrontSwagger_FinalHardening_SecurityRequirementsMatchRuntimeContract()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var operations = GetOperations(swagger)
                .ToDictionary(
                    operation => operation.Value["operationId"]?.GetValue<string>() ?? string.Empty,
                    operation => operation.Value,
                    StringComparer.Ordinal);

            foreach (var expected in ProtectedOperationSecuritySchemes)
            {
                Assert.True(operations.TryGetValue(expected.Key, out var operation), $"{expected.Key} was not found.");
                Assert.Contains(expected.Value, GetSecuritySchemeNames(operation!));
            }

            var anonymousOperationIds = operations.Keys.Except(ProtectedOperationSecuritySchemes.Keys, StringComparer.Ordinal);
            foreach (var operationId in anonymousOperationIds)
            {
                Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(operations[operationId]));
            }
        }

        [Fact]
        public async Task StorefrontSwagger_CanGenerateTypeScriptClientSmoke()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var client = GenerateTypeScriptClient(swagger);

            Assert.Contains("export class StorefrontApiClient", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCatalog_QueryProducts", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCatalog_GetProductFilterMetadata", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCatalog_GetSearchSuggestions", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCart_CreateSession", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCart_AddLine", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCart_Recalculate", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCart_MergeCurrentCustomer", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCheckout_Start", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCheckout_Load", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCheckout_Cancel", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCheckout_UpdateAddresses", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCheckout_SelectShippingMethod", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCheckout_SelectPaymentMethod", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCheckout_Review", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontPayments_CapturePayPal", client, StringComparison.Ordinal);
            Assert.DoesNotContain("any /* missing operationId */", client, StringComparison.Ordinal);
            Assert.DoesNotContain("Promise<any>", client, StringComparison.Ordinal);
        }

        [Fact]
        public async Task StorefrontSwagger_ServerCartEndpointsHaveGeneratorSafeContracts()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var storefrontOperations = GetOperations(swagger).ToArray();
            var operations = storefrontOperations
                .ToDictionary(
                    operation => operation.Value["operationId"]?.GetValue<string>() ?? string.Empty,
                    operation => operation.Value,
                    StringComparer.Ordinal);

            var expectedOperationIds = new[]
            {
                "StorefrontCart_CreateSession",
                "StorefrontCart_Get",
                "StorefrontCart_AddLine",
                "StorefrontCart_UpdateLine",
                "StorefrontCart_RemoveLine",
                "StorefrontCart_Clear",
                "StorefrontCart_Validate",
                "StorefrontCart_Recalculate",
                "StorefrontCart_MergeCurrentCustomer",
            };

            foreach (var operationId in expectedOperationIds)
            {
                Assert.True(operations.TryGetValue(operationId, out var operation), $"{operationId} was not found.");
                Assert.False(string.IsNullOrWhiteSpace(operation!["summary"]?.GetValue<string>()));
                Assert.True(operation["responses"]?.AsObject().Count > 1, $"{operationId} must declare success and error responses.");
            }

            var recalculateOperation = storefrontOperations.Single(operation =>
                string.Equals(operation.Value["operationId"]?.GetValue<string>(), "StorefrontCart_Recalculate", StringComparison.Ordinal));
            Assert.Equal("post", recalculateOperation.Method);
            Assert.EndsWith("/cart/recalculate", recalculateOperation.Path, StringComparison.Ordinal);

            AssertRequiredRequestBody(operations["StorefrontCart_CreateSession"]);
            AssertRequiredRequestBody(operations["StorefrontCart_AddLine"]);
            AssertRequiredRequestBody(operations["StorefrontCart_UpdateLine"]);
            AssertRequiredRequestBody(operations["StorefrontCart_Validate"]);
            AssertRequiredRequestBody(operations["StorefrontCart_Recalculate"]);

            var addLineSchema = schemas["StorefrontCartLineCreateRequest"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCartLineCreateRequest schema was not found.");
            Assert.Equal(1, addLineSchema["properties"]?["quantity"]?["minimum"]?.GetValue<int>());
            Assert.Equal(128, addLineSchema["properties"]?["personalizationHash"]?["maxLength"]?.GetValue<int>());
            Assert.Equal(8192, addLineSchema["properties"]?["personalizationJson"]?["maxLength"]?.GetValue<int>());
            Assert.Equal(64, addLineSchema["properties"]?["fulfillmentProviderKey"]?["maxLength"]?.GetValue<int>());

            var updateLineSchema = schemas["StorefrontCartLineUpdateRequest"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCartLineUpdateRequest schema was not found.");
            Assert.Equal(1, updateLineSchema["properties"]?["quantity"]?["minimum"]?.GetValue<int>());

            var recalculateSchema = schemas["StorefrontCartRecalculateRequest"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCartRecalculateRequest schema was not found.");
            Assert.Equal(1, recalculateSchema["properties"]?["expectedVersion"]?["minimum"]?.GetValue<int>());

            var cartResponseSchema = schemas["StorefrontCartResponse"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCartResponse schema was not found.");
            var cartResponseProperties = cartResponseSchema["properties"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCartResponse properties were not found.");
            Assert.Contains("summaryCount", cartResponseProperties);
            Assert.Contains("subtotal", cartResponseProperties);
            Assert.Contains("grandTotal", cartResponseProperties);
            Assert.Contains("checkoutAllowed", cartResponseProperties);
            Assert.Contains("warnings", cartResponseProperties);
            Assert.Contains("adjustments", cartResponseProperties);

            var cartLineResponseSchema = schemas["StorefrontCartLineResponse"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCartLineResponse schema was not found.");
            var cartLineResponseProperties = cartLineResponseSchema["properties"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCartLineResponse properties were not found.");
            Assert.Contains("displayName", cartLineResponseProperties);
            Assert.Contains("productUrl", cartLineResponseProperties);
            Assert.Contains("imageUrl", cartLineResponseProperties);
            Assert.Contains("selectedAttributes", cartLineResponseProperties);
            Assert.Contains("lineTotal", cartLineResponseProperties);
            Assert.Contains("quantityMinimum", cartLineResponseProperties);
            Assert.Contains("quantityMaximum", cartLineResponseProperties);
            Assert.Contains("quantityStep", cartLineResponseProperties);
            Assert.Contains("purchasable", cartLineResponseProperties);
            Assert.Contains("warnings", cartLineResponseProperties);

            Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(operations["StorefrontCart_Get"]));
            Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(operations["StorefrontCart_AddLine"]));
            Assert.Contains("Bearer", GetSecuritySchemeNames(operations["StorefrontCart_MergeCurrentCustomer"]));
        }

        [Fact]
        public async Task StorefrontSwagger_CheckoutSessionEndpointsHaveGeneratorSafeContracts()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var operations = GetOperations(swagger)
                .ToDictionary(
                    operation => operation.Value["operationId"]?.GetValue<string>() ?? string.Empty,
                    operation => operation.Value,
                    StringComparer.Ordinal);

            var expectedOperationIds = new[]
            {
                "StorefrontCheckout_Start",
                "StorefrontCheckout_Load",
                "StorefrontCheckout_Cancel",
                "StorefrontCheckout_UpdateAddresses",
                "StorefrontCheckout_SelectShippingMethod",
                "StorefrontCheckout_SelectPaymentMethod",
                "StorefrontCheckout_Review",
            };

            foreach (var operationId in expectedOperationIds)
            {
                Assert.True(operations.TryGetValue(operationId, out var operation), $"{operationId} was not found.");
                Assert.False(string.IsNullOrWhiteSpace(operation!["summary"]?.GetValue<string>()));
                Assert.True(operation["responses"]?.AsObject().Count > 1, $"{operationId} must declare success and error responses.");
                Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(operation));

                var parameters = operation["parameters"]?.AsArray()
                    ?? throw new InvalidOperationException($"{operationId} does not contain parameters.");
                Assert.Contains(parameters, parameter =>
                    string.Equals(parameter?["name"]?.GetValue<string>(), "X-Cart-Token", StringComparison.Ordinal));
            }

            AssertRequiredRequestBody(operations["StorefrontCheckout_Start"]);
            AssertRequiredRequestBody(operations["StorefrontCheckout_UpdateAddresses"]);
            AssertRequiredRequestBody(operations["StorefrontCheckout_SelectShippingMethod"]);
            AssertRequiredRequestBody(operations["StorefrontCheckout_SelectPaymentMethod"]);
            AssertRequiredRequestBody(operations["StorefrontCheckout_Review"]);
            Assert.Null(operations["StorefrontCheckout_Load"]["requestBody"]);
            Assert.Null(operations["StorefrontCheckout_Cancel"]["requestBody"]);
            Assert.True(schemas.ContainsKey("StorefrontCheckoutStartRequest"));
            Assert.True(schemas.ContainsKey("StorefrontCheckoutAddressStepRequest"));
            Assert.True(schemas.ContainsKey("StorefrontCheckoutShippingMethodRequest"));
            Assert.True(schemas.ContainsKey("StorefrontCheckoutShippingOptionResponse"));
            Assert.True(schemas.ContainsKey("StorefrontCheckoutPaymentMethodRequest"));
            Assert.True(schemas.ContainsKey("StorefrontCheckoutPaymentMethodOptionResponse"));
            Assert.True(schemas.ContainsKey("StorefrontCheckoutSessionResponse"));
            Assert.True(schemas.ContainsKey("StorefrontCheckoutReviewRequest"));
            Assert.True(schemas.ContainsKey("StorefrontCheckoutReviewResponse"));

            var addressRequestSchema = schemas["StorefrontCheckoutAddressStepRequest"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCheckoutAddressStepRequest schema was not found.");
            var addressRequestProperties = GetPropertyNames(addressRequestSchema).ToArray();
            Assert.Contains("billingAddress", addressRequestProperties);
            Assert.Contains("shippingAddress", addressRequestProperties);
            Assert.Contains("billingAddressId", addressRequestProperties);
            Assert.Contains("shippingAddressId", addressRequestProperties);
            Assert.Contains("useBillingAddressAsShippingAddress", addressRequestProperties);
            Assert.DoesNotContain("storeId", addressRequestProperties, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("customerId", addressRequestProperties, StringComparer.OrdinalIgnoreCase);

            var shippingMethodRequestSchema = schemas["StorefrontCheckoutShippingMethodRequest"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCheckoutShippingMethodRequest schema was not found.");
            var shippingMethodProperties = GetPropertyNames(shippingMethodRequestSchema).ToArray();
            Assert.Contains("shippingOptionKey", shippingMethodProperties);
            Assert.DoesNotContain("price", shippingMethodProperties, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("total", shippingMethodProperties, StringComparer.OrdinalIgnoreCase);

            var paymentMethodRequestSchema = schemas["StorefrontCheckoutPaymentMethodRequest"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCheckoutPaymentMethodRequest schema was not found.");
            var paymentMethodProperties = GetPropertyNames(paymentMethodRequestSchema).ToArray();
            Assert.Contains("paymentMethodKey", paymentMethodProperties);
            Assert.DoesNotContain("paymentStatus", paymentMethodProperties, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("orderStatus", paymentMethodProperties, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("total", paymentMethodProperties, StringComparer.OrdinalIgnoreCase);

            var reviewRequestSchema = schemas["StorefrontCheckoutReviewRequest"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCheckoutReviewRequest schema was not found.");
            var reviewRequestProperties = GetPropertyNames(reviewRequestSchema).ToArray();
            Assert.Contains("termsAccepted", reviewRequestProperties);
            Assert.Contains("termsVersion", reviewRequestProperties);
            Assert.DoesNotContain("cartVersion", reviewRequestProperties, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("grandTotal", reviewRequestProperties, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("paymentStatus", reviewRequestProperties, StringComparer.OrdinalIgnoreCase);

            var responseSchema = schemas["StorefrontCheckoutSessionResponse"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCheckoutSessionResponse schema was not found.");
            var responseProperties = GetPropertyNames(responseSchema).ToArray();
            Assert.Contains("checkoutSessionId", responseProperties);
            Assert.Contains("checkoutVersion", responseProperties);
            Assert.Contains("cartVersion", responseProperties);
            Assert.Contains("lastValidatedCartVersion", responseProperties);
            Assert.Contains("currentStep", responseProperties);
            Assert.Contains("completedSteps", responseProperties);
            Assert.Contains("shippingRequired", responseProperties);
            Assert.Contains("selectedShippingOption", responseProperties);
            Assert.Contains("shippingOptions", responseProperties);
            Assert.Contains("selectedPaymentMethod", responseProperties);
            Assert.Contains("paymentMethods", responseProperties);
            Assert.Contains("isActive", responseProperties);
            Assert.Contains("issues", responseProperties);
            Assert.DoesNotContain("storeId", responseProperties, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("customerId", responseProperties, StringComparer.OrdinalIgnoreCase);

            var reviewResponseSchema = schemas["StorefrontCheckoutReviewResponse"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCheckoutReviewResponse schema was not found.");
            var reviewResponseProperties = GetPropertyNames(reviewResponseSchema).ToArray();
            Assert.Contains("checkoutSessionId", reviewResponseProperties);
            Assert.Contains("checkoutVersion", reviewResponseProperties);
            Assert.Contains("cartVersion", reviewResponseProperties);
            Assert.Contains("customerEmail", reviewResponseProperties);
            Assert.Contains("billingAddress", reviewResponseProperties);
            Assert.Contains("shippingAddress", reviewResponseProperties);
            Assert.Contains("selectedShippingOption", reviewResponseProperties);
            Assert.Contains("selectedPaymentMethod", reviewResponseProperties);
            Assert.Contains("lines", reviewResponseProperties);
            Assert.Contains("subtotal", reviewResponseProperties);
            Assert.Contains("shippingTotal", reviewResponseProperties);
            Assert.Contains("taxTotal", reviewResponseProperties);
            Assert.Contains("discountTotal", reviewResponseProperties);
            Assert.Contains("grandTotal", reviewResponseProperties);
            Assert.Contains("currencyCode", reviewResponseProperties);
            Assert.Contains("termsRequired", reviewResponseProperties);
            Assert.Contains("termsAccepted", reviewResponseProperties);
            Assert.Contains("termsVersion", reviewResponseProperties);
            Assert.Contains("termsAcceptedAtUtc", reviewResponseProperties);
            Assert.Contains("placeOrderAllowed", reviewResponseProperties);
            Assert.Contains("nextRequiredStep", reviewResponseProperties);
            Assert.Contains("issues", reviewResponseProperties);
            Assert.DoesNotContain("storeId", reviewResponseProperties, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("customerId", reviewResponseProperties, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task StorefrontSwagger_CheckoutPreviewHasGeneratorSafeContract()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var operation = GetOperation(swagger, "StorefrontCheckout_Preview");

            Assert.False(string.IsNullOrWhiteSpace(operation["summary"]?.GetValue<string>()));
            Assert.True(operation["responses"]?.AsObject().Count > 1);
            AssertRequiredRequestBody(operation);
            Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(operation));

            var requestSchema = ResolveRequestBodySchema(operation, schemas);
            var requestProperties = GetPropertyNames(requestSchema);
            Assert.Contains("expectedCartVersion", requestProperties);
            Assert.Contains("shippingAddress", requestProperties);
            Assert.Contains("shippingAddressId", requestProperties);
            Assert.Contains("billingAddressId", requestProperties);
            Assert.Contains("useShippingAddressAsBillingAddress", requestProperties);
            Assert.DoesNotContain("carts", requestProperties, StringComparer.OrdinalIgnoreCase);

            Assert.True(schemas.ContainsKey("StorefrontCheckoutPreviewResponse"));
            Assert.True(schemas.ContainsKey("StorefrontCheckoutValidationIssueResponse"));

            var responseSchema = schemas["StorefrontCheckoutPreviewResponse"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCheckoutPreviewResponse schema was not found.");
            var responseProperties = GetPropertyNames(responseSchema).ToArray();
            Assert.Contains("checkoutVersion", responseProperties);
            Assert.Contains("lastValidatedCartVersion", responseProperties);
            Assert.Contains("currentStep", responseProperties);
            Assert.Contains("completedSteps", responseProperties);

            var parameters = operation["parameters"]?.AsArray()
                ?? throw new InvalidOperationException("Checkout preview operation does not contain parameters.");
            Assert.Contains(parameters, parameter =>
                string.Equals(parameter?["name"]?.GetValue<string>(), "X-Cart-Token", StringComparison.Ordinal));
        }

        [Fact]
        public async Task StorefrontSwagger_PlaceOrderHasGeneratorSafeContract()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var operation = GetOperation(swagger, "StorefrontCheckout_PlaceOrder");

            Assert.False(string.IsNullOrWhiteSpace(operation["summary"]?.GetValue<string>()));
            Assert.True(operation["responses"]?.AsObject().Count > 1);
            AssertRequiredRequestBody(operation);
            Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(operation));

            var requestSchema = ResolveRequestBodySchema(operation, schemas);
            var requestProperties = GetPropertyNames(requestSchema);
            Assert.Contains("checkoutSessionId", requestProperties);
            Assert.Contains("expectedCheckoutVersion", requestProperties);
            Assert.Contains("expectedCartVersion", requestProperties);
            Assert.Contains("idempotencyKey", requestProperties);
            Assert.Equal(1, requestSchema["properties"]?["expectedCheckoutVersion"]?["minimum"]?.GetValue<int>());
            Assert.Equal(1, requestSchema["properties"]?["expectedCartVersion"]?["minimum"]?.GetValue<int>());
            Assert.DoesNotContain("carts", requestProperties, StringComparer.OrdinalIgnoreCase);

            Assert.True(schemas.ContainsKey("StorefrontPlaceOrderResponse"));
        }

        [Fact]
        public async Task StorefrontSwagger_PaymentAttemptEndpointsHaveGeneratorSafeContracts()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var getAttempt = GetOperation(swagger, "StorefrontPayments_GetAttempt");
            var callback = GetOperation(swagger, "StorefrontPayments_HandleProviderCallback");
            var webhook = GetOperation(swagger, "StorefrontPayments_HandleWebhook");

            Assert.False(string.IsNullOrWhiteSpace(getAttempt["summary"]?.GetValue<string>()));
            Assert.True(getAttempt["responses"]?.AsObject().Count > 1);
            Assert.Null(getAttempt["requestBody"]);
            Assert.True(schemas.ContainsKey("StorefrontPaymentAttemptResponse"));

            AssertRequiredRequestBody(callback);
            AssertRequiredRequestBody(webhook);
            Assert.True(callback["responses"]?.AsObject().Count > 1);
            Assert.True(webhook["responses"]?.AsObject().Count > 1);
            Assert.True(schemas.ContainsKey("StorefrontPaymentCallbackRequest"));
            Assert.True(schemas.ContainsKey("StorefrontPaymentWebhookRequest"));
            Assert.True(schemas.ContainsKey("StorefrontPaymentWebhookAcceptedResponse"));
            var webhookRequestProperties = GetPropertyNames(schemas["StorefrontPaymentWebhookRequest"]!.AsObject());
            Assert.Contains("providerReference", webhookRequestProperties);
            Assert.Contains("providerSessionId", webhookRequestProperties);

            var parameters = webhook["parameters"]?.AsArray()
                ?? throw new InvalidOperationException("Webhook operation does not contain parameters.");
            Assert.Contains(parameters, parameter =>
                string.Equals(parameter?["name"]?.GetValue<string>(), "X-Provider-Signature", StringComparison.Ordinal));
        }

        [Fact]
        public async Task StorefrontSwagger_FinalHardening_HasNoBrokenSchemaReferences()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var schemas = GetSchemas(swagger);
            var failures = new List<string>();

            VisitReferences(swagger, reference =>
            {
                const string schemaPrefix = "#/components/schemas/";
                if (!reference.StartsWith(schemaPrefix, StringComparison.Ordinal))
                {
                    failures.Add($"Unsupported reference target: {reference}");
                    return;
                }

                var schemaName = reference[schemaPrefix.Length..];
                if (!schemas.ContainsKey(schemaName))
                {
                    failures.Add($"Missing schema reference: {reference}");
                }
            });

            Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
        }

        [Fact]
        public async Task StorefrontSwagger_CartCheckoutPaymentProviderEndpointsHaveGeneratorSafeContracts()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var operations = GetOperations(swagger)
                .ToDictionary(
                    operation => operation.Value["operationId"]?.GetValue<string>() ?? string.Empty,
                    operation => operation.Value,
                    StringComparer.Ordinal);

            var expectedOperationIds = new[]
            {
                "StorefrontCart_CreateSession",
                "StorefrontCart_Get",
                "StorefrontCart_AddLine",
                "StorefrontCart_UpdateLine",
                "StorefrontCart_RemoveLine",
                "StorefrontCart_Clear",
                "StorefrontCart_Validate",
                "StorefrontCart_Recalculate",
                "StorefrontCart_MergeCurrentCustomer",
                "StorefrontCheckout_Preview",
                "StorefrontCheckout_PlaceOrder",
                "StorefrontPayments_GetAttempt",
                "StorefrontPayments_HandleProviderCallback",
                "StorefrontPayments_HandleWebhook",
            };

            foreach (var operationId in expectedOperationIds)
            {
                Assert.True(operations.TryGetValue(operationId, out var operation), $"{operationId} was not found.");
                Assert.False(string.IsNullOrWhiteSpace(operation!["summary"]?.GetValue<string>()));
                Assert.True(operation["responses"]?.AsObject().Count > 1, $"{operationId} must declare success and error responses.");
            }
        }

        private HttpClient CreateSwaggerClient()
        {
            var configuredFactory = this.factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("CommerceNode:Database:MigrateOnStartup", "false");
                builder.UseSetting("CommerceTaskWorker:Enabled", "false");
            });

            return configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        private async Task<JsonObject> GetStorefrontSwaggerAsync()
        {
            var content = await this.GetStorefrontSwaggerTextAsync();

            return JsonNode.Parse(content)?.AsObject()
                ?? throw new InvalidOperationException("Storefront Swagger response was not a JSON object.");
        }

        private async Task<string> GetStorefrontSwaggerTextAsync()
        {
            using var client = this.CreateSwaggerClient();
            return await client.GetStringAsync(StorefrontSwaggerPath);
        }

        private static IEnumerable<(string Path, string Method, JsonObject Value)> GetOperations(JsonObject swagger)
        {
            var paths = swagger["paths"]?.AsObject()
                ?? throw new InvalidOperationException("Storefront Swagger document does not contain paths.");

            foreach (var path in paths.OrderBy(path => path.Key, StringComparer.Ordinal))
            {
                var pathItem = path.Value?.AsObject();
                if (pathItem is null)
                {
                    continue;
                }

                foreach (var operation in pathItem.Where(operation => IsHttpMethod(operation.Key)).OrderBy(operation => operation.Key, StringComparer.Ordinal))
                {
                    yield return (path.Key, operation.Key, operation.Value!.AsObject());
                }
            }
        }

        private static JsonObject GetOperation(JsonObject swagger, string operationId)
        {
            return GetOperations(swagger)
                .Select(operation => operation.Value)
                .Single(operation => string.Equals(operation["operationId"]?.GetValue<string>(), operationId, StringComparison.Ordinal));
        }

        private static JsonObject GetSchemas(JsonObject swagger)
        {
            return swagger["components"]?["schemas"]?.AsObject()
                ?? throw new InvalidOperationException("Swagger document does not contain component schemas.");
        }

        private static string GetPathSnapshot(JsonObject swagger)
        {
            var lines = GetOperations(swagger)
                .Select(operation => $"{operation.Method.ToUpperInvariant()} {operation.Path}");

            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        private static bool HasSchemaShape(JsonNode schema)
        {
            var schemaObject = schema.AsObject();
            return schemaObject.ContainsKey("$ref")
                || schemaObject.ContainsKey("type")
                || schemaObject.ContainsKey("oneOf")
                || schemaObject.ContainsKey("allOf")
                || schemaObject.ContainsKey("anyOf");
        }

        private static void AssertRequiredRequestBody(JsonObject operation)
        {
            Assert.True(operation["requestBody"]?["required"]?.GetValue<bool>() == true);
        }

        private static JsonObject ResolveRequestBodySchema(JsonObject operation, JsonObject schemas)
        {
            var schema = operation["requestBody"]?["content"]?["application/json"]?["schema"]
                ?? throw new InvalidOperationException("Operation request body does not contain an application/json schema.");

            return ResolveSchema(schema, schemas);
        }

        private static JsonObject ResolveSchema(JsonNode schema, JsonObject schemas)
        {
            var schemaObject = schema.AsObject();
            if (schemaObject.TryGetPropertyValue("$ref", out var referenceNode))
            {
                var schemaName = referenceNode?.GetValue<string>().Split('/').Last()
                    ?? throw new InvalidOperationException("Schema reference is empty.");
                return schemas[schemaName]?.AsObject()
                    ?? throw new InvalidOperationException($"Referenced schema '{schemaName}' was not found.");
            }

            if (schemaObject.TryGetPropertyValue("items", out var itemsNode) && itemsNode is not null)
            {
                return ResolveSchema(itemsNode, schemas);
            }

            return schemaObject;
        }

        private static IEnumerable<string> GetPropertyNames(JsonObject schema)
        {
            return schema["properties"]?.AsObject().Select(property => property.Key) ?? [];
        }

        private static IEnumerable<string> GetRequiredProperties(JsonObject schema)
        {
            return schema["required"]?.AsArray().Select(property => property?.GetValue<string>() ?? string.Empty) ?? [];
        }

        private static bool IsNullableProperty(JsonObject schema, string propertyName)
        {
            return schema["properties"]?[propertyName]?["nullable"]?.GetValue<bool>() == true;
        }

        private static IEnumerable<JsonObject> FindPropertySchemas(JsonObject schemas, string propertyName)
        {
            foreach (var schema in schemas)
            {
                var properties = schema.Value?["properties"]?.AsObject();
                if (properties is null || !properties.TryGetPropertyValue(propertyName, out var propertySchema) || propertySchema is null)
                {
                    continue;
                }

                yield return propertySchema.AsObject();
            }
        }

        private static IEnumerable<string> GetSecuritySchemeNames(JsonObject operation)
        {
            if (operation["security"] is not JsonArray security)
            {
                return [];
            }

            return security
                .SelectMany(requirement => requirement?.AsObject().Select(scheme => scheme.Key) ?? [])
                .ToArray();
        }

        private static JsonObject GetParameter(JsonArray parameters, string name)
        {
            return parameters
                .Select(parameter => parameter?.AsObject())
                .Single(parameter => string.Equals(parameter?["name"]?.GetValue<string>(), name, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Parameter '{name}' was not found.");
        }

        private static string GenerateTypeScriptClient(JsonObject swagger)
        {
            var builder = new StringBuilder();
            builder.AppendLine("export class StorefrontApiClient {");
            builder.AppendLine("  constructor(private readonly baseUrl: string, private readonly fetcher: typeof fetch = fetch) {}");

            foreach (var operation in GetOperations(swagger))
            {
                var operationId = operation.Value["operationId"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(operationId))
                {
                    builder.AppendLine("  any /* missing operationId */");
                    continue;
                }

                builder.Append("  async ");
                builder.Append(operationId);
                builder.AppendLine("(request?: unknown): Promise<unknown> {");
                builder.Append("    return this.fetcher(`${this.baseUrl}");
                builder.Append(operation.Path);
                builder.Append("`, { method: '");
                builder.Append(operation.Method.ToUpperInvariant());
                builder.AppendLine("', body: request ? JSON.stringify(request) : undefined });");
                builder.AppendLine("  }");
            }

            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void VisitReferences(JsonNode? node, Action<string> visit)
        {
            switch (node)
            {
                case JsonObject jsonObject:
                    foreach (var property in jsonObject)
                    {
                        if (string.Equals(property.Key, "$ref", StringComparison.Ordinal)
                            && property.Value is not null)
                        {
                            visit(property.Value.GetValue<string>());
                        }

                        VisitReferences(property.Value, visit);
                    }

                    break;

                case JsonArray jsonArray:
                    foreach (var item in jsonArray)
                    {
                        VisitReferences(item, visit);
                    }

                    break;
            }
        }

        private static bool IsHttpMethod(string value)
        {
            return value.Equals("get", StringComparison.OrdinalIgnoreCase)
                || value.Equals("post", StringComparison.OrdinalIgnoreCase)
                || value.Equals("put", StringComparison.OrdinalIgnoreCase)
                || value.Equals("patch", StringComparison.OrdinalIgnoreCase)
                || value.Equals("delete", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSnapshotAbsolutePath(string snapshotPath)
        {
            return Path.Combine(AppContext.BaseDirectory, snapshotPath);
        }

        private static string NormalizeJson(string json)
        {
            var node = JsonNode.Parse(json)
                ?? throw new InvalidOperationException("Snapshot JSON was empty.");
            return node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;
        }

        private static string NormalizeLineEndings(string value)
        {
            return value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", Environment.NewLine, StringComparison.Ordinal);
        }
    }
}
