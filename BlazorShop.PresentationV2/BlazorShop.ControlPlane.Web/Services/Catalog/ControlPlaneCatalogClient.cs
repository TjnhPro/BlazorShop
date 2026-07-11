namespace BlazorShop.ControlPlane.Web.Services.Catalog
{
    using System.Globalization;
    using System.Net.Http.Headers;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
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

        Task<ControlPlaneClientResult<ProductSeoDto>> GetProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductSeoDto>> UpdateProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductSeoDto request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneFileResult> DownloadProductImportTemplateAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductImportUploadResponse>> UploadProductImportAsync(
            Guid storePublicId,
            Stream content,
            string fileName,
            string? mode,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductImportJobListResponse>> ListProductImportsAsync(
            Guid storePublicId,
            ProductImportJobListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductImportJobDetailDto>> GetProductImportAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductImportRowsResponse>> ListProductImportRowsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            ProductImportRowsQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneFileResult> DownloadProductImportErrorsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<ProductMediaListResponse>> ListProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ProductMediaListQuery query,
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

        Task<ControlPlaneFileResult> GetProductMediaPreviewAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            ProductMediaPreviewQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<PagedResult<GetCategory>>> ListCategoriesAsync(
            Guid storePublicId,
            int pageNumber = 1,
            int pageSize = 25,
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

        Task<ControlPlaneClientResult<PagedResult<GetProductVariant>>> ListVariantsAsync(
            Guid storePublicId,
            Guid productId,
            int pageNumber = 1,
            int pageSize = 25,
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

        Task<ControlPlaneClientResult<VariationTemplateListResponse>> ListVariationTemplatesAsync(
            Guid storePublicId,
            VariationTemplateListQuery query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateDetailDto>> GetVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateDetailDto>> CreateVariationTemplateAsync(
            Guid storePublicId,
            CreateVariationTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateDetailDto>> UpdateVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateVariationTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateDetailDto>> CreateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CreateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateDetailDto>> UpdateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            UpdateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateDetailDto>> CreateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            CreateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<VariationTemplateDetailDto>> UpdateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            Guid valuePublicId,
            UpdateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<PagedResult<GetOrder>>> QueryOrdersAsync(
            Guid storePublicId,
            AdminOrderQueryDto query,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<GetOrder>> GetOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<GetOrder>> UpdateOrderAdminNoteAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateOrderAdminNoteRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<GetOrder>> UpdateOrderShippingStatusAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateShippingStatusRequest request,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<GetShipment>> GetShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<ControlPlaneClientResult<GetShipment>> UpsertShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            UpsertShipmentRequest request,
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
                CommerceRoute(storePublicId, "products") + BuildProductQuery(query),
                "Unable to load catalog products.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetProduct>> GetProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<GetProduct>(
                CommerceRoute(storePublicId, $"products/{productId:D}"),
                "Unable to load product.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> CreateProductAsync(
            Guid storePublicId,
            CreateProduct request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateProduct, object>(
                CommerceRoute(storePublicId, "products"),
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
                CommerceRoute(storePublicId, $"products/{productId:D}"),
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
                CommerceRoute(storePublicId, $"products/{productId:D}"),
                "Unable to archive product.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductSeoDto>> GetProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<ProductSeoDto>(
                CommerceRoute(storePublicId, $"products/{productId:D}/seo"),
                "Unable to load product SEO.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductSeoDto>> UpdateProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductSeoDto request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateProductSeoDto, ProductSeoDto>(
                CommerceRoute(storePublicId, $"products/{productId:D}/seo"),
                request,
                "Unable to update product SEO.",
                cancellationToken);
        }

        public Task<ControlPlaneFileResult> DownloadProductImportTemplateAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateFileAsync(
                CommerceRoute(storePublicId, "product-imports/template"),
                "Unable to download product import template.",
                cancellationToken);
        }

        public async Task<ControlPlaneClientResult<ProductImportUploadResponse>> UploadProductImportAsync(
            Guid storePublicId,
            Stream content,
            string fileName,
            string? mode,
            CancellationToken cancellationToken = default)
        {
            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            form.Add(fileContent, "file", string.IsNullOrWhiteSpace(fileName) ? "products.csv" : fileName);
            form.Add(new StringContent(string.IsNullOrWhiteSpace(mode) ? ProductImportModes.CreateOnly : mode), "mode");

            return await this.apiClient.PostPrivateMultipartAsync<ProductImportUploadResponse>(
                CommerceRoute(storePublicId, "product-imports"),
                form,
                "Unable to upload product import.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductImportJobListResponse>> ListProductImportsAsync(
            Guid storePublicId,
            ProductImportJobListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<ProductImportJobListResponse>(
                CommerceRoute(storePublicId, "product-imports") + BuildProductImportQuery(query),
                "Unable to load product import jobs.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductImportJobDetailDto>> GetProductImportAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<ProductImportJobDetailDto>(
                CommerceRoute(storePublicId, $"product-imports/{jobPublicId:D}"),
                "Unable to load product import job.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductImportRowsResponse>> ListProductImportRowsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            ProductImportRowsQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<ProductImportRowsResponse>(
                CommerceRoute(storePublicId, $"product-imports/{jobPublicId:D}/rows") + BuildProductImportRowsQuery(query),
                "Unable to load product import rows.",
                cancellationToken);
        }

        public Task<ControlPlaneFileResult> DownloadProductImportErrorsAsync(
            Guid storePublicId,
            Guid jobPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateFileAsync(
                CommerceRoute(storePublicId, $"product-imports/{jobPublicId:D}/errors.csv"),
                "Unable to download product import errors.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductMediaListResponse>> ListProductMediaAsync(
            Guid storePublicId,
            Guid productId,
            ProductMediaListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<ProductMediaListResponse>(
                CommerceRoute(storePublicId, $"products/{productId:D}/media") + BuildPageQuery(query.PageNumber, query.PageSize),
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
                CommerceRoute(storePublicId, $"products/{productId:D}/media/import"),
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
                CommerceRoute(storePublicId, $"products/{productId:D}/media/order"),
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
                CommerceRoute(storePublicId, $"products/{productId:D}/media/{mediaPublicId:D}/primary"),
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
                CommerceRoute(storePublicId, $"products/{productId:D}/media/{mediaPublicId:D}"),
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
                CommerceRoute(storePublicId, $"products/{productId:D}/media/{mediaPublicId:D}/retry"),
                "Unable to retry product media.",
                cancellationToken);
        }

        public Task<ControlPlaneFileResult> GetProductMediaPreviewAsync(
            Guid storePublicId,
            Guid productId,
            Guid mediaPublicId,
            ProductMediaPreviewQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateFileAsync(
                CommerceRoute(storePublicId, $"products/{productId:D}/media/{mediaPublicId:D}/preview") + BuildMediaPreviewQuery(query),
                "Unable to load product media preview.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<PagedResult<GetCategory>>> ListCategoriesAsync(
            Guid storePublicId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<PagedResult<GetCategory>>(
                CommerceRoute(storePublicId, "categories") + BuildPageQuery(pageNumber, pageSize),
                "Unable to load categories.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<GetCategoryTreeNode>>> GetCategoryTreeAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<IReadOnlyList<GetCategoryTreeNode>>(
                CommerceRoute(storePublicId, "categories/tree"),
                "Unable to load category tree.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> CreateCategoryAsync(
            Guid storePublicId,
            CreateCategory request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateCategory, object>(
                CommerceRoute(storePublicId, "categories"),
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
                CommerceRoute(storePublicId, $"categories/{categoryId:D}"),
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
                CommerceRoute(storePublicId, $"categories/{categoryId:D}"),
                "Unable to archive category.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<PagedResult<GetProductVariant>>> ListVariantsAsync(
            Guid storePublicId,
            Guid productId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<PagedResult<GetProductVariant>>(
                CommerceRoute(storePublicId, $"products/{productId:D}/variants") + BuildPageQuery(pageNumber, pageSize),
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
                CommerceRoute(storePublicId, $"products/{productId:D}/variants"),
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
                CommerceRoute(storePublicId, $"products/{productId:D}/variants/{variantId:D}"),
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
                CommerceRoute(storePublicId, $"products/{productId:D}/variants/{variantId:D}"),
                "Unable to delete variant.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<PagedResult<AdminInventoryItemDto>>> QueryInventoryAsync(
            Guid storePublicId,
            AdminInventoryQueryDto query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<PagedResult<AdminInventoryItemDto>>(
                CommerceRoute(storePublicId, "inventory") + BuildInventoryQuery(query),
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
                CommerceRoute(storePublicId, $"products/{productId:D}/inventory"),
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
                CommerceRoute(storePublicId, $"inventory/variants/{variantId:D}"),
                request,
                "Unable to update variant stock.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateListResponse>> ListVariationTemplatesAsync(
            Guid storePublicId,
            VariationTemplateListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<VariationTemplateListResponse>(
                CommerceRoute(storePublicId, "variation-templates") + BuildPageQuery(query.PageNumber, query.PageSize),
                "Unable to load variation templates.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> GetVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, $"variation-templates/{templatePublicId:D}"),
                "Unable to load variation template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> CreateVariationTemplateAsync(
            Guid storePublicId,
            CreateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateVariationTemplateRequest, VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, "variation-templates"),
                request,
                "Unable to create variation template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> UpdateVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateVariationTemplateRequest, VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, $"variation-templates/{templatePublicId:D}"),
                request,
                "Unable to update variation template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> CreateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CreateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateVariationTemplateOptionRequest, VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, $"variation-templates/{templatePublicId:D}/options"),
                request,
                "Unable to create variation option.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> UpdateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            UpdateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateVariationTemplateOptionRequest, VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, $"variation-templates/{templatePublicId:D}/options/{optionPublicId:D}"),
                request,
                "Unable to update variation option.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> CreateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            CreateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PostPrivateAsync<CreateVariationTemplateValueRequest, VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, $"variation-templates/{templatePublicId:D}/options/{optionPublicId:D}/values"),
                request,
                "Unable to create variation value.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> UpdateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            Guid valuePublicId,
            UpdateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateVariationTemplateValueRequest, VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, $"variation-templates/{templatePublicId:D}/options/{optionPublicId:D}/values/{valuePublicId:D}"),
                request,
                "Unable to update variation value.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<PagedResult<GetOrder>>> QueryOrdersAsync(
            Guid storePublicId,
            AdminOrderQueryDto query,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<PagedResult<GetOrder>>(
                CommerceRoute(storePublicId, "orders") + BuildOrderQuery(query),
                "Unable to load orders.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetOrder>> GetOrderAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<GetOrder>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}"),
                "Unable to load order.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetOrder>> UpdateOrderAdminNoteAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateOrderAdminNoteRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateOrderAdminNoteRequest, GetOrder>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/admin-note"),
                request,
                "Unable to update order note.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetOrder>> UpdateOrderShippingStatusAsync(
            Guid storePublicId,
            Guid orderId,
            UpdateShippingStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpdateShippingStatusRequest, GetOrder>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/shipping-status"),
                request,
                "Unable to update shipping status.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetShipment>> GetShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.GetPrivateAsync<GetShipment>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/shipment"),
                "Unable to load shipment.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetShipment>> UpsertShipmentAsync(
            Guid storePublicId,
            Guid orderId,
            UpsertShipmentRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.apiClient.PutPrivateAsync<UpsertShipmentRequest, GetShipment>(
                CommerceRoute(storePublicId, $"orders/{orderId:D}/shipment"),
                request,
                "Unable to save shipment.",
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
            AddIfPresent(values, "isPublished", query.IsPublished?.ToString());
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

        private static string BuildProductImportQuery(ProductImportJobListQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "status", query.Status);
            return ToQueryString(values);
        }

        private static string BuildPageQuery(int pageNumber, int pageSize)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, pageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(pageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            return ToQueryString(values);
        }

        private static string BuildProductImportRowsQuery(ProductImportRowsQuery query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "status", query.Status);
            return ToQueryString(values);
        }

        private static string BuildOrderQuery(AdminOrderQueryDto query)
        {
            var values = new List<KeyValuePair<string, string>>
            {
                new("pageNumber", Math.Max(1, query.PageNumber).ToString(CultureInfo.InvariantCulture)),
                new("pageSize", Math.Clamp(query.PageSize, 1, 100).ToString(CultureInfo.InvariantCulture)),
            };

            AddIfPresent(values, "searchTerm", query.SearchTerm);
            AddIfPresent(values, "status", query.Status);
            AddIfPresent(values, "shippingStatus", query.ShippingStatus);
            AddIfPresent(values, "fromUtc", query.FromUtc?.ToString("O", CultureInfo.InvariantCulture));
            AddIfPresent(values, "toUtc", query.ToUtc?.ToString("O", CultureInfo.InvariantCulture));
            return ToQueryString(values);
        }

        private static string BuildMediaPreviewQuery(ProductMediaPreviewQuery query)
        {
            var values = new List<KeyValuePair<string, string>>();
            AddIfPresent(values, "w", query.Width?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "h", query.Height?.ToString(CultureInfo.InvariantCulture));
            AddIfPresent(values, "fit", query.Fit);
            AddIfPresent(values, "format", query.Format);
            AddIfPresent(values, "v", query.Version?.ToString(CultureInfo.InvariantCulture));
            return ToQueryString(values);
        }

        private static string CommerceRoute(Guid storePublicId, string path)
        {
            return $"api/controlplane/commerce/stores/{storePublicId:D}/{path.TrimStart('/')}";
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
