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
    public sealed class ControlPlaneStoreConfigurationGateway : ControlPlaneCommerceGatewayBase, BlazorShop.Application.ControlPlane.CommerceGateway.StoreConfiguration.IControlPlaneStoreConfigurationGateway
    {
        public ControlPlaneStoreConfigurationGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
        }

        public async Task<ApplicationResult<CommerceStoreDetail>> GetRuntimeStoreAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            var storeKey = await this.ResolveStoreKeyApplicationAsync(storePublicId, cancellationToken);
            if (!storeKey.Success)
            {
                return ApplicationResult<CommerceStoreDetail>.Failed(storeKey.Error!);
            }

            var result = await this.SendApplicationAsync<CommerceStoreListResponse>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/stores",
                null,
                cancellationToken);

            if (!result.Success)
            {
                return ApplicationResult<CommerceStoreDetail>.Failed(result.Error!);
            }

            var runtimeStore = result.Value?.Items.FirstOrDefault(item =>
                string.Equals(item.StoreKey, storeKey.Value, StringComparison.OrdinalIgnoreCase));
            if (runtimeStore is null)
            {
                return ApplicationResult<CommerceStoreDetail>.Failed(
                    ApplicationError.NotFound(
                        "commerce_node.runtime_store_not_found",
                        "Runtime store was not found."));
            }

            return await this.SendApplicationAsync<CommerceStoreDetail>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/stores/{runtimeStore.PublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ApplicationResult<CommerceStoreDetail>> UpdateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            UpdateCommerceStoreRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<CommerceStoreDetail>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/stores/{runtimeStorePublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ApplicationResult<CommerceStoreDetail>> ActivateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<CommerceStoreDetail>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/stores/{runtimeStorePublicId:D}/activate",
                null,
                cancellationToken);
        }

        public Task<ApplicationResult<CommerceStoreDetail>> DeactivateRuntimeStoreAsync(
            Guid storePublicId,
            Guid runtimeStorePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<CommerceStoreDetail>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/stores/{runtimeStorePublicId:D}/deactivate",
                null,
                cancellationToken);
        }
    }
}

