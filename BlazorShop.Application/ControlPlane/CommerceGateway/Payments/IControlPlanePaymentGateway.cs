namespace BlazorShop.Application.ControlPlane.CommerceGateway.Payments
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
    public interface IControlPlanePaymentGateway
    {
        
                Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StorePaymentMethodDto>>> ListPaymentMethodsAsync(
                    Guid storePublicId,
                    CancellationToken cancellationToken = default);

        
                Task<ControlPlaneCommerceCatalogResult<StorePaymentMethodDto>> UpdatePaymentMethodAsync(
                    Guid storePublicId,
                    string paymentMethodKey,
                    UpdateStorePaymentMethodRequest request,
                    CancellationToken cancellationToken = default);
    }
}

