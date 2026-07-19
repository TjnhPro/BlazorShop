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
    public sealed class ControlPlaneNavigationGateway : ControlPlaneCommerceGatewayBase, BlazorShop.Application.ControlPlane.CommerceGateway.Navigation.IControlPlaneNavigationGateway
    {
        public ControlPlaneNavigationGateway(ICommerceNodeAdminGatewayTransport transport)
            : base(transport)
        {
        }

        public Task<ApplicationResult<IReadOnlyList<StoreNavigationMenuSummaryDto>>> ListNavigationMenusAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<IReadOnlyList<StoreNavigationMenuSummaryDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/navigation/menus",
                null,
                cancellationToken);
        }

        public Task<ApplicationResult<StoreNavigationMenuDetailDto>> GetNavigationMenuAsync(
            Guid storePublicId,
            Guid menuPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<StoreNavigationMenuDetailDto>(
                storePublicId,
                HttpMethod.Get,
                $"api/commerce/admin/navigation/menus/{menuPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ApplicationResult<StoreNavigationMenuDetailDto>> CreateNavigationMenuAsync(
            Guid storePublicId,
            CreateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<StoreNavigationMenuDetailDto>(
                storePublicId,
                HttpMethod.Post,
                "api/commerce/admin/navigation/menus",
                request,
                cancellationToken);
        }

        public Task<ApplicationResult<StoreNavigationMenuDetailDto>> UpdateNavigationMenuAsync(
            Guid storePublicId,
            Guid menuPublicId,
            UpdateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<StoreNavigationMenuDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/navigation/menus/{menuPublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ApplicationResult<StoreNavigationMenuDetailDto>> CreateNavigationItemAsync(
            Guid storePublicId,
            Guid menuPublicId,
            CreateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<StoreNavigationMenuDetailDto>(
                storePublicId,
                HttpMethod.Post,
                $"api/commerce/admin/navigation/menus/{menuPublicId:D}/items",
                request,
                cancellationToken);
        }

        public Task<ApplicationResult<StoreNavigationMenuDetailDto>> UpdateNavigationItemAsync(
            Guid storePublicId,
            Guid itemPublicId,
            UpdateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<StoreNavigationMenuDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/navigation/items/{itemPublicId:D}",
                request,
                cancellationToken);
        }

        public Task<ApplicationResult<StoreNavigationMenuDetailDto>> ArchiveNavigationItemAsync(
            Guid storePublicId,
            Guid itemPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<StoreNavigationMenuDetailDto>(
                storePublicId,
                HttpMethod.Delete,
                $"api/commerce/admin/navigation/items/{itemPublicId:D}",
                null,
                cancellationToken);
        }

        public Task<ApplicationResult<StoreNavigationMenuDetailDto>> UpdateNavigationItemOrderAsync(
            Guid storePublicId,
            Guid menuPublicId,
            UpdateStoreNavigationMenuItemOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<StoreNavigationMenuDetailDto>(
                storePublicId,
                HttpMethod.Put,
                $"api/commerce/admin/navigation/menus/{menuPublicId:D}/items/order",
                request,
                cancellationToken);
        }

        public Task<ApplicationResult<IReadOnlyList<StoreNavigationTargetOptionDto>>> ListNavigationSystemTargetsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.SendApplicationAsync<IReadOnlyList<StoreNavigationTargetOptionDto>>(
                storePublicId,
                HttpMethod.Get,
                "api/commerce/admin/navigation/system-targets",
                null,
                cancellationToken);
        }
    }
}

