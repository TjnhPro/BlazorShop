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
    public sealed class ControlPlaneContentGateway : ControlPlaneCommerceGatewayBase, BlazorShop.Application.ControlPlane.CommerceGateway.Content.IControlPlaneContentGateway
    {
        public ControlPlaneContentGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageListResponse>> ListStorefrontPagesAsync(
            Guid storePublicId,
            StorefrontPageListQuery query,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageListResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/pages" + BuildStorefrontPageQuery(query),
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StorefrontPageTemplateDefinitionDto>>> ListStorefrontPageTemplatesAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StorefrontPageTemplateDefinitionDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/pages/templates",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<IReadOnlyList<StorefrontPageTemplateStatusDto>>> GetStorefrontPageTemplateStatusAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<IReadOnlyList<StorefrontPageTemplateStatusDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/pages/template-status",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> GetStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/pages/{pagePublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> CreateStorefrontPageAsync(
            Guid storePublicId,
            CreateStorefrontPageRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/pages",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> UpdateStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            UpdateStorefrontPageRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/pages/{pagePublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> ArchiveStorefrontPageAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/pages/{pagePublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> CreateStorefrontPageDraftFromTemplateAsync(
            Guid storePublicId,
            string pageKey,
            CreatePageFromTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/pages/templates/{Uri.EscapeDataString(pageKey)}/draft",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> MapStorefrontPageTemplateAsync(
            Guid storePublicId,
            Guid pagePublicId,
            MapPageTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/pages/{pagePublicId:D}/template",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> ClearStorefrontPageTemplateAsync(
            Guid storePublicId,
            Guid pagePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/pages/{pagePublicId:D}/template",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<StorefrontPageDetailDto>> UpdateStorefrontPageNavigationAsync(
            Guid storePublicId,
            Guid pagePublicId,
            UpdatePageNavigationRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<StorefrontPageDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/pages/{pagePublicId:D}/navigation",
                request,
                cancellationToken);
        }
    }
}

