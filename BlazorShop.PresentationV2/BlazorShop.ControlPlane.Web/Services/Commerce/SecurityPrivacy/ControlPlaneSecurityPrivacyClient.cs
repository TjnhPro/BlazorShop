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

        public sealed class ControlPlaneSecurityPrivacyClient : ControlPlaneCommerceClientBase, IControlPlaneSecurityPrivacyClient
    {
        public ControlPlaneSecurityPrivacyClient(IControlPlaneApiClient apiClient)
            : base(apiClient)
        {
        }
        public Task<ControlPlaneClientResult<StoreSecurityPrivacySettingsDto>> GetSecurityPrivacySettingsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<StoreSecurityPrivacySettingsDto>(
                CommerceRoute(storePublicId, "security-privacy"),
                "Unable to load security and privacy settings.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreSecurityPrivacySettingsDto>> UpdateSecurityPrivacySettingsAsync(
            Guid storePublicId,
            UpdateStoreSecurityPrivacySettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpdateStoreSecurityPrivacySettingsRequest, StoreSecurityPrivacySettingsDto>(
                CommerceRoute(storePublicId, "security-privacy"),
                request,
                "Unable to update security and privacy settings.",
                cancellationToken);
        }
    }
}

