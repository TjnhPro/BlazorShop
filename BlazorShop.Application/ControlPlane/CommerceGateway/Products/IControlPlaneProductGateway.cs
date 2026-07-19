namespace BlazorShop.Application.ControlPlane.CommerceGateway.Products
{
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Domain.Contracts;
    public interface IControlPlaneProductGateway
    {
        
                Task<ControlPlaneCommerceCatalogResult<PagedResult<GetCatalogProduct>>> QueryProductsAsync(
                    Guid storePublicId,
                    ProductCatalogQuery query,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<GetProduct>> GetProductAsync(
                    Guid storePublicId,
                    Guid productId,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<object>> CreateProductAsync(
                    Guid storePublicId,
                    CreateProduct request,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<object>> UpdateProductAsync(
                    Guid storePublicId,
                    Guid productId,
                    UpdateProduct request,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<object>> ArchiveProductAsync(
                    Guid storePublicId,
                    Guid productId,
                    CancellationToken cancellationToken = default);

                Task<ControlPlaneCommerceCatalogResult<PagedResult<GetProductVariant>>> ListVariantsAsync(
                    Guid storePublicId,
                    Guid productId,
                    int pageNumber = 1,
                    int pageSize = 25,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<object>> CreateVariantAsync(
                    Guid storePublicId,
                    Guid productId,
                    CreateProductVariant request,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<object>> UpdateVariantAsync(
                    Guid storePublicId,
                    Guid productId,
                    Guid variantId,
                    UpdateProductVariant request,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<object>> DeleteVariantAsync(
                    Guid storePublicId,
                    Guid productId,
                    Guid variantId,
                    CancellationToken cancellationToken = default);

    }
}

