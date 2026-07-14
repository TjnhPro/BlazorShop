extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;

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
        public async Task StorefrontSwagger_CanGenerateTypeScriptClientSmoke()
        {
            var swagger = await this.GetStorefrontSwaggerAsync();
            var client = GenerateTypeScriptClient(swagger);

            Assert.Contains("export class StorefrontApiClient", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontCatalog_QueryProducts", client, StringComparison.Ordinal);
            Assert.Contains("StorefrontPayments_CapturePayPal", client, StringComparison.Ordinal);
            Assert.DoesNotContain("any /* missing operationId */", client, StringComparison.Ordinal);
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
