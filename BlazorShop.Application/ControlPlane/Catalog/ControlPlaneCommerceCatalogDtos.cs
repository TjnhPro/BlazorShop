namespace BlazorShop.Application.ControlPlane.Catalog
{
    using BlazorShop.Application.ControlPlane.CommerceGateway.Categories;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Content;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Currencies;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Media;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Messages;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Navigation;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Orders;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Payments;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Products;
    using BlazorShop.Application.ControlPlane.CommerceGateway.SecurityPrivacy;
    using BlazorShop.Application.ControlPlane.CommerceGateway.Shipping;
    using BlazorShop.Application.ControlPlane.CommerceGateway.StoreConfiguration;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Contracts;

    public interface IControlPlaneCommerceCatalogService :
        IControlPlaneProductGateway,
        IControlPlaneCategoryGateway,
        IControlPlaneMediaGateway,
        IControlPlaneOrderGateway,
        IControlPlaneContentGateway,
        IControlPlaneNavigationGateway,
        IControlPlaneStoreConfigurationGateway,
        IControlPlaneCurrencyGateway,
        IControlPlanePaymentGateway,
        IControlPlaneShippingGateway,
        IControlPlaneSecurityPrivacyGateway,
        IControlPlaneMessageGateway
    {
    }

    public sealed record ControlPlaneCommerceCatalogResult<TPayload>(
        bool Success,
        string? Message = null,
        TPayload? Payload = default,
        ControlPlaneCommerceCatalogFailure? Failure = null,
        int? HttpStatusCode = null);

    public sealed record ProductMediaPreviewQuery(
        int? Width = null,
        int? Height = null,
        string? Fit = null,
        string? Format = null,
        int? Version = null);

    public sealed record MediaAssetPreviewQuery(
        int? Width = null,
        int? Height = null,
        string? Fit = null,
        string? Format = null,
        long? Version = null);

    public sealed record ControlPlaneCommerceMediaResult(
        bool Success,
        string? Message = null,
        byte[]? Content = null,
        string? ContentType = null,
        ControlPlaneCommerceCatalogFailure? Failure = null,
        int? HttpStatusCode = null);

    public enum ControlPlaneCommerceCatalogFailure
    {
        Validation,
        NotFound,
        RemoteFailure
    }
}

