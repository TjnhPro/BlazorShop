namespace BlazorShop.Infrastructure.Data.ControlPlane
{
    using System.Globalization;

    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.ControlPlane.Catalog;
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
    public sealed class ControlPlaneSecurityPrivacyGateway : ControlPlaneCommerceGatewayBase, BlazorShop.Application.ControlPlane.CommerceGateway.SecurityPrivacy.IControlPlaneSecurityPrivacyGateway
    {
        public ControlPlaneSecurityPrivacyGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
        }

        public Task<ApplicationResult<StoreSecurityPrivacySettingsDto>> GetSecurityPrivacySettingsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<StoreSecurityPrivacySettingsDto>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/security-privacy",
                null,
                cancellationToken);
        }

        public Task<ApplicationResult<StoreSecurityPrivacySettingsDto>> UpdateSecurityPrivacySettingsAsync(
            Guid storePublicId,
            UpdateStoreSecurityPrivacySettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<StoreSecurityPrivacySettingsDto>(
                storePublicId,
                HttpMethod.Put,
                "api/commerce/admin/security-privacy",
                request,
                cancellationToken);
        }
    }
}

