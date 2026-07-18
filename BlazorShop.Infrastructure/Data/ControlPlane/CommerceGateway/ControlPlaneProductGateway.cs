namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Globalization;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.ControlPlane.CommerceGateway;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Domain.Contracts;
    public sealed class ControlPlaneProductGateway : ControlPlaneCommerceGatewayBase, BlazorShop.Application.ControlPlane.CommerceGateway.Products.IControlPlaneProductGateway
    {
        public ControlPlaneProductGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
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

        public Task<ControlPlaneCommerceCatalogResult<ProductSeoDto>> GetProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<ProductSeoDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/{productId:D}/seo",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<ProductSeoDto>> UpdateProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            UpdateProductSeoDto request,
            CancellationToken cancellationToken = default)
        {
            request.ProductId = productId;
            return this.SendAsync<ProductSeoDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/products/{productId:D}/seo",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreSeoSlugPolicyResult>> GenerateSeoSlugAsync(
            Guid storePublicId,
            StoreSeoSlugGenerateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreSeoSlugPolicyResult>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/seo/slugs/generate",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StoreSeoSlugPolicyResult>> ValidateSeoSlugAsync(
            Guid storePublicId,
            StoreSeoSlugValidateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StoreSeoSlugPolicyResult>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/seo/slugs/validate",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StoreSeoSlugHistoryDto>>> ListSeoSlugHistoryAsync(
            Guid storePublicId,
            StoreSeoSlugHistoryQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StoreSeoSlugHistoryDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/seo/slugs/history" + BuildSeoSlugHistoryQuery(query),
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

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateListResponse>> ListVariationTemplatesAsync(
            Guid storePublicId,
            VariationTemplateListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateListResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/variation-templates" + BuildPageQuery(query.PageNumber, query.PageSize),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> GetVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> CreateVariationTemplateAsync(
            Guid storePublicId,
            CreateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/variation-templates",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> UpdateVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            UpdateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> CreateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CreateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}/options",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> UpdateVariationTemplateOptionAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            UpdateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}/options/{optionPublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> CreateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            CreateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}/options/{optionPublicId:D}/values",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<VariationTemplateDetailDto>> UpdateVariationTemplateValueAsync(
            Guid storePublicId,
            Guid templatePublicId,
            Guid optionPublicId,
            Guid valuePublicId,
            UpdateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<VariationTemplateDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/variation-templates/{templatePublicId:D}/options/{optionPublicId:D}/values/{valuePublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CategoryMediaAssignmentDto>> SetCategoryPrimaryMediaAsync(
            Guid storePublicId,
            Guid categoryId,
            SetCategoryPrimaryMediaRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CategoryMediaAssignmentDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/categories/{categoryId:D}/media/primary",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CategoryMediaAssignmentDto>> ClearCategoryPrimaryMediaAsync(
            Guid storePublicId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CategoryMediaAssignmentDto>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/categories/{categoryId:D}/media/primary",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<PagedResult<GetProductVariant>>> ListVariantsAsync(
            Guid storePublicId,
            Guid productId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<PagedResult<GetProductVariant>>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/products/{productId:D}/variants" + BuildPageQuery(pageNumber, pageSize),
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
    }
}

