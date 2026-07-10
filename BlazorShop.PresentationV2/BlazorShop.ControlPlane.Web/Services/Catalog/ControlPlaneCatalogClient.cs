namespace BlazorShop.ControlPlane.Web.Services.Catalog
{
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.ControlPlane.Web.Services.Common;
    using BlazorShop.Domain.Contracts;

    public interface IControlPlaneCatalogClient
    {
        Task<ControlPlaneClientResult<PagedResult<GetCatalogProduct>>> QueryProductsAsync(
            Guid storePublicId,
            ProductCatalogQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<GetProduct>> GetProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> CreateProductAsync(
            Guid storePublicId,
            CreateProduct request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> UpdateProductAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProduct request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> ArchiveProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductMediaListResponse>> ListProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ImportProductMediaResponse>> ImportProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ImportProductMediaRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductMediaListResponse>> UpdateProductMediaOrderAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductMediaDto>> SetPrimaryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductMediaListResponse>> DeleteProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ImportProductMediaResponse>> RetryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<GetCategory>>> ListCategoriesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<GetCategoryTreeNode>>> GetCategoryTreeAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> CreateCategoryAsync(
            Guid storePublicId,
            CreateCategory request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> UpdateCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategory request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> ArchiveCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<IReadOnlyList<GetProductVariant>>> ListVariantsAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> CreateVariantAsync(
            Guid storePublicId,
            Guid productId,
            CreateProductVariant request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> UpdateVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            UpdateProductVariant request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<object>> DeleteVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<PagedResult<AdminInventoryItemDto>>> QueryInventoryAsync(
            Guid storePublicId,
            AdminInventoryQueryDto query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<AdminInventoryItemDto>> UpdateProductStockAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductStockDto request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<AdminInventoryVariantDto>> UpdateVariantStockAsync(
            Guid storePublicId,
            Guid variantId,
            UpdateVariantStockDto request,
            CancellationToken cancellationToken = default);
    }

    public sealed class ControlPlaneCatalogClient : IControlPlaneCatalogClient
    {
        private readonly IControlPlaneApiClient apiClient;

        public ControlPlaneCatalogClient(IControlPlaneApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public Task<ControlPlaneClientResult<PagedResult<GetCatalogProduct>>> QueryProductsAsync(
            Guid storePublicId,
            ProductCatalogQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<PagedResult<GetCatalogProduct>>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products{BuildProductQuery(query)}",
                "Unable to load catalog products.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetProduct>> GetProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<GetProduct>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products/{productId:D}",
                "Unable to load product.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> CreateProductAsync(
            Guid storePublicId,
            CreateProduct request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateProduct, object>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products",
                request,
                "Unable to create product.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> UpdateProductAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProduct request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateProduct, object>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products/{productId:D}",
                request,
                "Unable to update product.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> ArchiveProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.DeletePrivateAsync<object>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products/{productId:D}",
                "Unable to archive product.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductMediaListResponse>> ListProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<ProductMediaListResponse>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products/{productId:D}/media",
                "Unable to load product media.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ImportProductMediaResponse>> ImportProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ImportProductMediaRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<ImportProductMediaRequest, ImportProductMediaResponse>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products/{productId:D}/media/import",
                request,
                "Unable to import product media.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductMediaListResponse>> UpdateProductMediaOrderAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductMediaOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateProductMediaOrderRequest, ProductMediaListResponse>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products/{productId:D}/media/order",
                request,
                "Unable to update product media order.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductMediaDto>> SetPrimaryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<ProductMediaDto>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products/{productId:D}/media/{mediaPublicId:D}/primary",
                "Unable to set primary product media.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductMediaListResponse>> DeleteProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.DeletePrivateAsync<ProductMediaListResponse>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products/{productId:D}/media/{mediaPublicId:D}",
                "Unable to delete product media.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ImportProductMediaResponse>> RetryProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<ImportProductMediaResponse>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products/{productId:D}/media/{mediaPublicId:D}/retry",
                "Unable to retry product media.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<GetCategory>>> ListCategoriesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<GetCategory>>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/categories",
                "Unable to load categories.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<GetCategoryTreeNode>>> GetCategoryTreeAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<GetCategoryTreeNode>>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/categories/tree",
                "Unable to load category tree.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> CreateCategoryAsync(
            Guid storePublicId,
            CreateCategory request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateCategory, object>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/categories",
                request,
                "Unable to create category.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> UpdateCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            UpdateCategory request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateCategory, object>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/categories/{categoryId:D}",
                request,
                "Unable to update category.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> ArchiveCategoryAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.DeletePrivateAsync<object>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/categories/{categoryId:D}",
                "Unable to archive category.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<GetProductVariant>>> ListVariantsAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<GetProductVariant>>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products/{productId:D}/variants",
                "Unable to load variants.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> CreateVariantAsync(
            Guid storePublicId,
            Guid productId,
            CreateProductVariant request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateProductVariant, object>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products/{productId:D}/variants",
                request,
                "Unable to create variant.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> UpdateVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            UpdateProductVariant request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateProductVariant, object>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products/{productId:D}/variants/{variantId:D}",
                request,
                "Unable to update variant.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> DeleteVariantAsync(
            Guid storePublicId,
            Guid productId,
            Guid variantId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.DeletePrivateAsync<object>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/products/{productId:D}/variants/{variantId:D}",
                "Unable to delete variant.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<PagedResult<AdminInventoryItemDto>>> QueryInventoryAsync(
            Guid storePublicId,
            AdminInventoryQueryDto query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<PagedResult<AdminInventoryItemDto>>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/inventory{BuildInventoryQuery(query)}",
                "Unable to load inventory.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<AdminInventoryItemDto>> UpdateProductStockAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductStockDto request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateProductStockDto, AdminInventoryItemDto>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/inventory/products/{productId:D}",
                request,
                "Unable to update product stock.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<AdminInventoryVariantDto>> UpdateVariantStockAsync(
            Guid storePublicId,
            Guid variantId,
            UpdateVariantStockDto request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateVariantStockDto, AdminInventoryVariantDto>(
                $"api/control-plane/stores/{storePublicId:D}/catalog/inventory/variants/{variantId:D}",
                request,
                "Unable to update variant stock.",
                cancellationToken);
        }

        private static string BuildProductQuery(ProductCatalogQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", query.GetNormalizedPageNumber().ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("pageSize", query.GetNormalizedPageSize().ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("sortBy", query.SortBy.ToString()),
            };

            AddIfPresent(values, "searchTerm", query.SearchTerm);
            AddIfPresent(values, "categoryId", query.CategoryId?.ToString("D"));
            AddIfPresent(values, "minPrice", query.MinPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture));
            AddIfPresent(values, "maxPrice", query.MaxPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture));
            AddIfPresent(values, "inStock", query.InStock?.ToString());
            return ToQueryString(values);
        }

        private static string BuildInventoryQuery(AdminInventoryQueryDto query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("lowStockOnly", query.LowStockOnly.ToString()),
                new("outOfStockOnly", query.OutOfStockOnly.ToString()),
                new("lowStockThreshold", Math.Max(0, query.LowStockThreshold).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "searchTerm", query.SearchTerm);
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
            return values.Count == 0
                ? string.Empty
                : "?" + string.Join(
                    "&",
                    values.Select(value => Uri.EscapeDataString(value.Key) + "=" + Uri.EscapeDataString(value.Value)));
        }
    }
}
