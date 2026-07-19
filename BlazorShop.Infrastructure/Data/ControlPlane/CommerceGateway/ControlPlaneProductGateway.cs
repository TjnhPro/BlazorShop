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

    }
}

