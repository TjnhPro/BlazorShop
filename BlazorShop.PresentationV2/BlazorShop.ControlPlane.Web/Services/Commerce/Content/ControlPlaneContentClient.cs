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

        public sealed class ControlPlaneContentClient : ControlPlaneCommerceClientBase, IControlPlaneContentClient
    {
        public ControlPlaneContentClient(IControlPlaneApiClient apiClient)
            : base(apiClient)
        {
        }
        public Task<ControlPlaneClientResult<StorefrontPageListResponse>> ListStorefrontPagesAsync(
            Guid storePublicId,
            StorefrontPageListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<StorefrontPageListResponse>(
                CommerceRoute(storePublicId, "pages") + BuildStorefrontPageQuery(query),
                "Unable to load storefront pages.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<StorefrontPageTemplateDefinitionDto>>> ListStorefrontPageTemplatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<IReadOnlyList<StorefrontPageTemplateDefinitionDto>>(
                CommerceRoute(storePublicId, "pages/templates"),
                "Unable to load storefront page templates.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<StorefrontPageTemplateStatusDto>>> GetStorefrontPageTemplateStatusAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<IReadOnlyList<StorefrontPageTemplateStatusDto>>(
                CommerceRoute(storePublicId, "pages/template-status"),
                "Unable to load storefront page template status.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> GetStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, $"pages/{pagePublicId:D}"),
                "Unable to load storefront page.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> CreateStorefrontPageAsync(
            Guid storePublicId,
            CreateStorefrontPageRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<CreateStorefrontPageRequest, StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, "pages"),
                request,
                "Unable to create storefront page.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> UpdateStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            UpdateStorefrontPageRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpdateStorefrontPageRequest, StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, $"pages/{pagePublicId:D}"),
                request,
                "Unable to update storefront page.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> ArchiveStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.DeletePrivateAsync<StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, $"pages/{pagePublicId:D}"),
                "Unable to archive storefront page.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> CreateStorefrontPageDraftFromTemplateAsync(
            Guid storePublicId,
            string pageKey,
            CreatePageFromTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<CreatePageFromTemplateRequest, StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, $"pages/templates/{Uri.EscapeDataString(pageKey)}/draft"),
                request,
                "Unable to create storefront page draft.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> MapStorefrontPageTemplateAsync(
            Guid storePublicId,
            Guid pagePublicId,
            MapPageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<MapPageTemplateRequest, StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, $"pages/{pagePublicId:D}/template"),
                request,
                "Unable to map storefront page template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> ClearStorefrontPageTemplateAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.DeletePrivateAsync<StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, $"pages/{pagePublicId:D}/template"),
                "Unable to clear storefront page template.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StorefrontPageDetailDto>> UpdateStorefrontPageNavigationAsync(
            Guid storePublicId,
            Guid pagePublicId,
            UpdatePageNavigationRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpdatePageNavigationRequest, StorefrontPageDetailDto>(
                CommerceRoute(storePublicId, $"pages/{pagePublicId:D}/navigation"),
                request,
                "Unable to update storefront page navigation.",
                cancellationToken);
        }
    }
}

