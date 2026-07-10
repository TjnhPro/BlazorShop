namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Globalization;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities.ControlPlane;

    using Microsoft.EntityFrameworkCore;

    public sealed class ControlPlaneCommerceCatalogService : IControlPlaneCommerceCatalogService
    {
        private const string ControlApiEndpointKind = "control_api";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly ControlPlaneDbContext dbContext;
        private readonly HttpClient httpClient;

        public ControlPlaneCommerceCatalogService(ControlPlaneDbContext dbContext, HttpClient httpClient)
        {
            this.dbContext = dbContext;
            this.httpClient = httpClient;
        }

        public Task<ControlPlaneCommerceCatalogResult<PagedResult<GetCatalogProduct>>> QueryProductsAsync(
            Guid storePublicId,
            ProductCatalogQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<PagedResult<GetCatalogProduct>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/products/query" + BuildProductQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<GetProduct>> GetProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<GetProduct>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/{productId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> CreateProductAsync(
            Guid storePublicId,
            CreateProduct request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/products",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> UpdateProductAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProduct request,
            CancellationToken cancellationToken = default)
        {
            request.Id = productId;
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/products/{productId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> ArchiveProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/products/{productId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductImportUploadResponse>> UploadProductImportAsync(
            Guid storePublicId,
            ProductImportUploadRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendMultipartAsync<ProductImportUploadResponse>(
                storePublicId,
                "api/commerce/admin/products/import",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductImportJobListResponse>> ListProductImportsAsync(
            Guid storePublicId,
            ProductImportJobListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductImportJobListResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/products/imports" + BuildProductImportQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductImportJobDetailDto>> GetProductImportAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductImportJobDetailDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/imports/{jobPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductImportRowsResponse>> ListProductImportRowsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            ProductImportRowsQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductImportRowsResponse>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/imports/{jobPublicId:D}/rows" + BuildProductImportRowsQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> ListProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductMediaListResponse>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/{productId:D}/media",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ImportProductMediaResponse>> ImportProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ImportProductMediaRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ImportProductMediaResponse>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/products/{productId:D}/media/import",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> UpdateProductMediaOrderAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductMediaListResponse>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/products/{productId:D}/media/order",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductMediaDto>> SetPrimaryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductMediaDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/products/{productId:D}/media/{mediaPublicId:D}/primary",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductMediaListResponse>> DeleteProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductMediaListResponse>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/products/{productId:D}/media/{mediaPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ImportProductMediaResponse>> RetryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ImportProductMediaResponse>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/products/{productId:D}/media/{mediaPublicId:D}/retry",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<GetCategory>>> ListCategoriesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<GetCategory>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/categories",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<GetCategoryTreeNode>>> GetCategoryTreeAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<GetCategoryTreeNode>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/categories/tree",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> CreateCategoryAsync(
            Guid storePublicId,
            CreateCategory request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/categories",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> UpdateCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategory request,
            CancellationToken cancellationToken = default)
        {
            request.Id = categoryId;
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/categories/{categoryId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> ArchiveCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/categories/{categoryId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<GetProductVariant>>> ListVariantsAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<GetProductVariant>>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/{productId:D}/variants",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> CreateVariantAsync(
            Guid storePublicId,
            Guid productId,
            CreateProductVariant request,
            CancellationToken cancellationToken = default)
        {
            request.ProductId = productId;
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/products/{productId:D}/variants",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> UpdateVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            UpdateProductVariant request,
            CancellationToken cancellationToken = default)
        {
            request.ProductId = productId;
            request.Id = variantId;
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/products/{productId:D}/variants/{variantId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<object>> DeleteVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<object>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/products/{productId:D}/variants/{variantId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<PagedResult<AdminInventoryItemDto>>> QueryInventoryAsync(
            Guid storePublicId,
            AdminInventoryQueryDto query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<PagedResult<AdminInventoryItemDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/inventory" + BuildInventoryQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<AdminInventoryItemDto>> UpdateProductStockAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductStockDto request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<AdminInventoryItemDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/inventory/products/{productId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<AdminInventoryVariantDto>> UpdateVariantStockAsync(
            Guid storePublicId,
            Guid variantId,
            UpdateVariantStockDto request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<AdminInventoryVariantDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/inventory/variants/{variantId:D}",
                request,
                cancellationToken);
        }

        private async Task<ControlPlaneCommerceCatalogResult<TPayload>> SendAsync<TPayload>(
            Guid storePublicId,
            HttpMethod method,
            string path,
            object? body,
            CancellationToken cancellationToken)
        {
            var store = await this.LoadStoreAsync(storePublicId, cancellationToken);
            var validation = ValidateStoreForRemoteCall(store);
            if (validation is not null)
            {
                return validation.ToResult<TPayload>();
            }

            try
            {
                using var request = new HttpRequestMessage(method, AppendPath(GetControlApiUrl(store!.Node!), path));
                request.Headers.TryAddWithoutValidation("X-Node-Key", store.Node!.NodeKey);
                request.Headers.TryAddWithoutValidation("X-Node-Secret", store.Node.NodeSecret);
                request.Headers.TryAddWithoutValidation("X-Store-Key", store.StoreKey);

                if (body is not null)
                {
                    request.Content = JsonContent.Create(body, options: SerializerOptions);
                }

                using var response = await this.httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    return Failure<TPayload>(
                        "Commerce Node returned an empty response.",
                        ControlPlaneCommerceCatalogFailure.RemoteFailure,
                        (int)response.StatusCode);
                }

                var envelope = JsonSerializer.Deserialize<CommerceNodeEnvelope<TPayload>>(responseBody, SerializerOptions);
                if (envelope is null)
                {
                    return Failure<TPayload>(
                        "Commerce Node returned a malformed response envelope.",
                        ControlPlaneCommerceCatalogFailure.RemoteFailure,
                        (int)response.StatusCode);
                }

                if (!response.IsSuccessStatusCode || !envelope.Success)
                {
                    return new ControlPlaneCommerceCatalogResult<TPayload>(
                        false,
                        string.IsNullOrWhiteSpace(envelope.Message) ? "Commerce Node catalog request failed." : envelope.Message,
                        envelope.Data,
                        ToFailure(response.StatusCode),
                        (int)response.StatusCode);
                }

                return new ControlPlaneCommerceCatalogResult<TPayload>(
                    true,
                    envelope.Message,
                    envelope.Data,
                    HttpStatusCode: (int)response.StatusCode);
            }
            catch (TaskCanceledException)
            {
                return Failure<TPayload>("Commerce Node catalog request timed out.", ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
            catch (HttpRequestException ex)
            {
                return Failure<TPayload>(ex.Message, ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
            catch (JsonException)
            {
                return Failure<TPayload>("Commerce Node returned malformed JSON.", ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
        }

        private async Task<ControlPlaneCommerceCatalogResult<TPayload>> SendMultipartAsync<TPayload>(
            Guid storePublicId,
            string path,
            ProductImportUploadRequest upload,
            CancellationToken cancellationToken)
        {
            var store = await this.LoadStoreAsync(storePublicId, cancellationToken);
            var validation = ValidateStoreForRemoteCall(store);
            if (validation is not null)
            {
                return validation.ToResult<TPayload>();
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, AppendPath(GetControlApiUrl(store!.Node!), path));
                request.Headers.TryAddWithoutValidation("X-Node-Key", store.Node!.NodeKey);
                request.Headers.TryAddWithoutValidation("X-Node-Secret", store.Node.NodeSecret);
                request.Headers.TryAddWithoutValidation("X-Store-Key", store.StoreKey);

                using var form = new MultipartFormDataContent();
                using var fileContent = new StreamContent(upload.Content);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
                form.Add(fileContent, "file", string.IsNullOrWhiteSpace(upload.FileName) ? "products.csv" : upload.FileName);
                form.Add(new StringContent(string.IsNullOrWhiteSpace(upload.Mode) ? ProductImportModes.CreateOnly : upload.Mode), "mode");
                request.Content = form;

                using var response = await this.httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    return Failure<TPayload>("Commerce Node returned an empty response.", ControlPlaneCommerceCatalogFailure.RemoteFailure, (int)response.StatusCode);
                }

                var envelope = JsonSerializer.Deserialize<CommerceNodeEnvelope<TPayload>>(responseBody, SerializerOptions);
                if (envelope is null)
                {
                    return Failure<TPayload>("Commerce Node returned a malformed response envelope.", ControlPlaneCommerceCatalogFailure.RemoteFailure, (int)response.StatusCode);
                }

                if (!response.IsSuccessStatusCode || !envelope.Success)
                {
                    return new ControlPlaneCommerceCatalogResult<TPayload>(
                        false,
                        string.IsNullOrWhiteSpace(envelope.Message) ? "Commerce Node catalog request failed." : envelope.Message,
                        envelope.Data,
                        ToFailure(response.StatusCode),
                        (int)response.StatusCode);
                }

                return new ControlPlaneCommerceCatalogResult<TPayload>(
                    true,
                    envelope.Message,
                    envelope.Data,
                    HttpStatusCode: (int)response.StatusCode);
            }
            catch (TaskCanceledException)
            {
                return Failure<TPayload>("Commerce Node catalog request timed out.", ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
            catch (HttpRequestException ex)
            {
                return Failure<TPayload>(ex.Message, ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
            catch (JsonException)
            {
                return Failure<TPayload>("Commerce Node returned malformed JSON.", ControlPlaneCommerceCatalogFailure.RemoteFailure);
            }
        }

        private async Task<StoreRegistry?> LoadStoreAsync(Guid publicId, CancellationToken cancellationToken)
        {
            return await this.dbContext.Stores
                .AsNoTracking()
                .Include(store => store.Node)
                    .ThenInclude(node => node!.Endpoints)
                .FirstOrDefaultAsync(store => store.PublicId == publicId, cancellationToken);
        }

        private static StoreValidationFailure? ValidateStoreForRemoteCall(StoreRegistry? store)
        {
            if (store is null)
            {
                return new StoreValidationFailure(ControlPlaneCommerceCatalogFailure.NotFound, "Store was not found.");
            }

            if (store.Status == "archived")
            {
                return new StoreValidationFailure(ControlPlaneCommerceCatalogFailure.Validation, "Archived stores cannot be managed.");
            }

            if (store.Node is null || store.Node.Status == "disabled")
            {
                return new StoreValidationFailure(ControlPlaneCommerceCatalogFailure.Validation, "Store node is missing or disabled.");
            }

            if (string.IsNullOrWhiteSpace(store.Node.NodeSecret))
            {
                return new StoreValidationFailure(ControlPlaneCommerceCatalogFailure.Validation, "Store node does not have a node secret configured.");
            }

            if (string.IsNullOrWhiteSpace(GetControlApiUrl(store.Node)))
            {
                return new StoreValidationFailure(ControlPlaneCommerceCatalogFailure.Validation, "Store node does not have an active Control API endpoint.");
            }

            return null;
        }

        private static string GetControlApiUrl(CommerceNode node)
        {
            return node.Endpoints.FirstOrDefault(endpoint =>
                endpoint.Kind == ControlApiEndpointKind &&
                endpoint.IsPrimary &&
                endpoint.DisabledAt is null)?.Url ?? string.Empty;
        }

        private static Uri AppendPath(string baseUrl, string path)
        {
            return new Uri(baseUrl.TrimEnd('/') + "/" + path.TrimStart('/'), UriKind.Absolute);
        }

        private static string BuildProductQuery(ProductCatalogQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", query.GetNormalizedPageNumber().ToString(CultureInfo.InvariantCulture)),
                new("pageSize", query.GetNormalizedPageSize().ToString(CultureInfo.InvariantCulture)),
                new("sortBy", query.SortBy.ToString()),
            };

            AddIfPresent(values, "searchTerm", query.SearchTerm);
            AddIfPresent(values, "categoryId", query.CategoryId?.ToString("D"));
            AddIfPresent(values, "minPrice", query.MinPrice?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "maxPrice", query.MaxPrice?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "inStock", query.InStock?.ToString());

            return ToQueryString(values);
        }

        private static string BuildInventoryQuery(AdminInventoryQueryDto query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
                new("lowStockOnly", query.LowStockOnly.ToString()),
                new("outOfStockOnly", query.OutOfStockOnly.ToString()),
                new("lowStockThreshold", Math.Max(0, query.LowStockThreshold).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "searchTerm", query.SearchTerm);
            return ToQueryString(values);
        }

        private static string BuildProductImportQuery(ProductImportJobListQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("skip", Math.Max(0, query.Skip).ToString(CultureInfo.InvariantCulture)),
                new("take", Math.Clamp(query.Take, 1, 200).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "status", query.Status);
            return ToQueryString(values);
        }

        private static string BuildProductImportRowsQuery(ProductImportRowsQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("skip", Math.Max(0, query.Skip).ToString(CultureInfo.InvariantCulture)),
                new("take", Math.Clamp(query.Take, 1, 200).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "status", query.Status);
            return ToQueryString(values);
        }

        private static void AddIfPresent(List<KeyValuePair<string, string>> values, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(new KeyValuePair<string, string>(key, value.Trim()));
            }
        }

        private static string ToQueryString(IReadOnlyCollection<KeyValuePair<string, string>> values)
        {
            if (values.Count == 0)
            {
                return string.Empty;
            }

            return "?" + string.Join(
                "&",
                values.Select(value =>
                    Uri.EscapeDataString(value.Key) + "=" + Uri.EscapeDataString(value.Value)));
        }

        private static ControlPlaneCommerceCatalogFailure ToFailure(System.Net.HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                System.Net.HttpStatusCode.NotFound => ControlPlaneCommerceCatalogFailure.NotFound,
                System.Net.HttpStatusCode.BadRequest => ControlPlaneCommerceCatalogFailure.Validation,
                System.Net.HttpStatusCode.Conflict => ControlPlaneCommerceCatalogFailure.Validation,
                _ => ControlPlaneCommerceCatalogFailure.RemoteFailure,
            };
        }

        private static ControlPlaneCommerceCatalogResult<TPayload> Failure<TPayload>(
            string message,
            ControlPlaneCommerceCatalogFailure failure,
            int? httpStatusCode = null)
        {
            return new ControlPlaneCommerceCatalogResult<TPayload>(
                false,
                message,
                Failure: failure,
                HttpStatusCode: httpStatusCode);
        }

        private sealed record CommerceNodeEnvelope<TPayload>(
            bool Success,
            string? Message,
            TPayload? Data);

        private sealed record StoreValidationFailure(ControlPlaneCommerceCatalogFailure Failure, string Message)
        {
            public ControlPlaneCommerceCatalogResult<TPayload> ToResult<TPayload>()
            {
                return new ControlPlaneCommerceCatalogResult<TPayload>(
                    false,
                    this.Message,
                    Failure: this.Failure);
            }
        }
    }
}
