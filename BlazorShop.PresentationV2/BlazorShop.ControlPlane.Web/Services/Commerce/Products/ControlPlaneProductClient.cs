namespace BlazorShop.ControlPlane.Web.Services.Commerce
{
    using System.Globalization;
    using System.Net.Http.Headers;

    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.Tasks;
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

        public sealed class ControlPlaneProductClient : ControlPlaneCommerceClientBase, IControlPlaneProductClient
    {
        public ControlPlaneProductClient(IControlPlaneApiClient apiClient)
            : base(apiClient)
        {
        }
        public Task<ControlPlaneClientResult<PagedResult<GetCatalogProduct>>> QueryProductsAsync(
            Guid storePublicId,
            ProductCatalogQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<PagedResult<GetCatalogProduct>>(
                CommerceRoute(storePublicId, "products") + BuildProductQuery(query),
                "Unable to load catalog products.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<GetProduct>> GetProductAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<GetProduct>(
                CommerceRoute(storePublicId, $"products/{productId:D}"),
                "Unable to load product.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<object>> CreateProductAsync(
            Guid storePublicId,
            CreateProduct request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<CreateProduct, object>(
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
            return this.ApiClient.PutPrivateAsync<UpdateProduct, object>(
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
            return this.ApiClient.DeletePrivateAsync<object>(
                CommerceRoute(storePublicId, $"products/{productId:D}"),
                "Unable to archive product.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<ProductSeoDto>> GetProductSeoAsync(
            Guid storePublicId,
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<ProductSeoDto>(
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
            return this.ApiClient.PutPrivateAsync<UpdateProductSeoDto, ProductSeoDto>(
                CommerceRoute(storePublicId, $"products/{productId:D}/seo"),
                request,
                "Unable to update product SEO.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<PagedResult<GetProductVariant>>> ListVariantsAsync(
            Guid storePublicId,
            Guid productId,
            int pageNumber = 1,
            int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<PagedResult<GetProductVariant>>(
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
            return this.ApiClient.PostPrivateAsync<CreateProductVariant, object>(
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
            return this.ApiClient.PutPrivateAsync<UpdateProductVariant, object>(
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
            return this.ApiClient.DeletePrivateAsync<object>(
                CommerceRoute(storePublicId, $"products/{productId:D}/variants/{variantId:D}"),
                "Unable to delete variant.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<PagedResult<AdminInventoryItemDto>>> QueryInventoryAsync(
            Guid storePublicId,
            AdminInventoryQueryDto query,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<PagedResult<AdminInventoryItemDto>>(
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
            return this.ApiClient.PutPrivateAsync<UpdateProductStockDto, AdminInventoryItemDto>(
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
            return this.ApiClient.PutPrivateAsync<UpdateVariantStockDto, AdminInventoryVariantDto>(
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
            return this.ApiClient.GetPrivateAsync<VariationTemplateListResponse>(
                CommerceRoute(storePublicId, "variation-templates") + BuildPageQuery(query.PageNumber, query.PageSize),
                "Unable to load variation templates.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> GetVariationTemplateAsync(
            Guid storePublicId,
            Guid templatePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, $"variation-templates/{templatePublicId:D}"),
                "Unable to load variation template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<VariationTemplateDetailDto>> CreateVariationTemplateAsync(
            Guid storePublicId,
            CreateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<CreateVariationTemplateRequest, VariationTemplateDetailDto>(
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
            return this.ApiClient.PutPrivateAsync<UpdateVariationTemplateRequest, VariationTemplateDetailDto>(
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
            return this.ApiClient.PostPrivateAsync<CreateVariationTemplateOptionRequest, VariationTemplateDetailDto>(
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
            return this.ApiClient.PutPrivateAsync<UpdateVariationTemplateOptionRequest, VariationTemplateDetailDto>(
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
            return this.ApiClient.PostPrivateAsync<CreateVariationTemplateValueRequest, VariationTemplateDetailDto>(
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
            return this.ApiClient.PutPrivateAsync<UpdateVariationTemplateValueRequest, VariationTemplateDetailDto>(
                CommerceRoute(storePublicId, $"variation-templates/{templatePublicId:D}/options/{optionPublicId:D}/values/{valuePublicId:D}"),
                request,
                "Unable to update variation value.",
                cancellationToken);
        }
    }
}

