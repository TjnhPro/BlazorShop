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
                ["StorefrontCart_SaveCheckout"] = "Bearer",
                ["StorefrontOrders_Confirm"] = "Bearer",
                ["StorefrontOrders_ListCurrentUserOrders"] = "Bearer",
                ["StorefrontOrders_ListCurrentUserOrderItems"] = "Bearer",
            };

        private static readonly (string SchemaName, string PropertyName)[] NonNullableResponseCollections =
        [
            ("StorefrontPagedResponse", "items"),
            ("StorefrontCategoryTreeNodeResponse", "children"),
            ("StorefrontCategoryPageResponse", "products"),
            ("StorefrontProductResponse", "variants"),
            ("StorefrontProductVariantResponse", "attributes"),
            ("StorefrontCartResponse", "lines"),
            ("StorefrontCartValidationResponse", "issues"),
            ("StorefrontOrderResponse", "lines"),
            ("StorefrontOrderLineResponse", "variantAttributes"),
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

            Assert.NotNull(GetOperation(swagger, "StorefrontPayments_CapturePayPal")["requestBody"]);
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
            Assert.Contains("StorefrontCart_CreateSession", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCart_AddLine", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontPayments_CapturePayPal", client, StringComparison.Ordinal);
            Assert.DoesNotContain("any /* missing operationId */", client, StringComparison.Ordinal);
            Assert.DoesNotContain("Promise<any>", client, StringComparison.Ordinal);
        }

        [Fact]
        public async Task StorefrontSwagger_ServerCartEndpointsHaveGeneratorSafeContracts()
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
                "StorefrontCart_CreateSession",
                "StorefrontCart_Get",
                "StorefrontCart_AddLine",
                "StorefrontCart_UpdateLine",
                "StorefrontCart_RemoveLine",
                "StorefrontCart_Clear",
                "StorefrontCart_Validate",
            };

            foreach (var operationId in expectedOperationIds)
            {
                Assert.True(operations.TryGetValue(operationId, out var operation), $"{operationId} was not found.");
                Assert.False(string.IsNullOrWhiteSpace(operation!["summary"]?.GetValue<string>()));
                Assert.True(operation["responses"]?.AsObject().Count > 1, $"{operationId} must declare success and error responses.");
            }

            AssertRequiredRequestBody(operations["StorefrontCart_CreateSession"]);
            AssertRequiredRequestBody(operations["StorefrontCart_AddLine"]);
            AssertRequiredRequestBody(operations["StorefrontCart_UpdateLine"]);
            AssertRequiredRequestBody(operations["StorefrontCart_Validate"]);

            var addLineSchema = schemas["StorefrontCartLineCreateRequest"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCartLineCreateRequest schema was not found.");
            Assert.Equal(1, addLineSchema["properties"]?["quantity"]?["minimum"]?.GetValue<int>());

            var updateLineSchema = schemas["StorefrontCartLineUpdateRequest"]?.AsObject()
                ?? throw new InvalidOperationException("StorefrontCartLineUpdateRequest schema was not found.");
            Assert.Equal(1, updateLineSchema["properties"]?["quantity"]?["minimum"]?.GetValue<int>());

            Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(operations["StorefrontCart_Get"]));
            Assert.DoesNotContain("Bearer", GetSecuritySchemeNames(operations["StorefrontCart_AddLine"]));
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
            Assert.DoesNotContain("carts", requestProperties, StringComparer.OrdinalIgnoreCase);

            Assert.True(schemas.ContainsKey("StorefrontCheckoutPreviewResponse"));
            Assert.True(schemas.ContainsKey("StorefrontCheckoutValidationIssueResponse"));

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
            Assert.Contains("expectedCartVersion", requestProperties);
            Assert.Contains("idempotencyKey", requestProperties);
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
