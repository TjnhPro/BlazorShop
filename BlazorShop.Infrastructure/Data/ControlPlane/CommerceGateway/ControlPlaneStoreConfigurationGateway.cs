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
    public sealed class ControlPlaneStoreConfigurationGateway : ControlPlaneCommerceGatewayBase, BlazorShop.Application.ControlPlane.CommerceGateway.StoreConfiguration.IControlPlaneStoreConfigurationGateway
    {
        public ControlPlaneStoreConfigurationGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
        }

        public async Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> GetRuntimeStoreAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            var storeKey = await this.ResolveStoreKeyAsync(storePublicId, cancellationToken);
            if (!storeKey.Success)
            {
                return new ControlPlaneCommerceCatalogResult<CommerceStoreDetail>(
                    false,
                    storeKey.Message,
                    Failure: storeKey.Failure,
                    HttpStatusCode: storeKey.HttpStatusCode);
            }

            var result = await this.SendAsync<CommerceStoreListResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/stores",
                null,
                cancellationToken);

            if (!result.Success)
            {
                return new ControlPlaneCommerceCatalogResult<CommerceStoreDetail>(
                    false,
                    result.Message,
                    Failure: result.Failure,
                    HttpStatusCode: result.HttpStatusCode);
            }

            var runtimeStore = result.Payload?.Items.FirstOrDefault(item =>
                string.Equals(item.StoreKey, storeKey.Payload, StringComparison.OrdinalIgnoreCase));
            if (runtimeStore is null)
            {
                return Failure<CommerceStoreDetail>("Runtime store was not found.", ControlPlaneCommerceCatalogFailure.NotFound);
            }

            return await this.SendAsync<CommerceStoreDetail>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/stores/{runtimeStore.PublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> UpdateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            UpdateCommerceStoreRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceStoreDetail>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/stores/{runtimeStorePublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> ActivateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceStoreDetail>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/stores/{runtimeStorePublicId:D}/activate",
                null,
                cancellationToken);
        }

        public Task<ControlPlaneCommerceCatalogResult<CommerceStoreDetail>> DeactivateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendAsync<CommerceStoreDetail>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/stores/{runtimeStorePublicId:D}/deactivate",
                null,
                cancellationToken);
        }
    }
}

