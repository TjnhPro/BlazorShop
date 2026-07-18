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

        public sealed class ControlPlaneNavigationClient : ControlPlaneCommerceClientBase, IControlPlaneNavigationClient
    {
        public ControlPlaneNavigationClient(IControlPlaneApiClient apiClient)
            : base(apiClient)
        {
        }
        public Task<ControlPlaneClientResult<IReadOnlyList<StoreNavigationMenuSummaryDto>>> ListNavigationMenusAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<IReadOnlyList<StoreNavigationMenuSummaryDto>>(
                CommerceRoute(storePublicId, "navigation/menus"),
                "Unable to load navigation menus.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> GetNavigationMenuAsync(
            Guid storePublicId,
            Guid menuPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<StoreNavigationMenuDetailDto>(
                CommerceRoute(storePublicId, $"navigation/menus/{menuPublicId:D}"),
                "Unable to load navigation menu.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> CreateNavigationMenuAsync(
            Guid storePublicId,
            CreateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<CreateStoreNavigationMenuRequest, StoreNavigationMenuDetailDto>(
                CommerceRoute(storePublicId, "navigation/menus"),
                request,
                "Unable to create navigation menu.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> UpdateNavigationMenuAsync(
            Guid storePublicId,
            Guid menuPublicId,
            UpdateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpdateStoreNavigationMenuRequest, StoreNavigationMenuDetailDto>(
                CommerceRoute(storePublicId, $"navigation/menus/{menuPublicId:D}"),
                request,
                "Unable to update navigation menu.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> CreateNavigationItemAsync(
            Guid storePublicId,
            Guid menuPublicId,
            CreateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PostPrivateAsync<CreateStoreNavigationMenuItemRequest, StoreNavigationMenuDetailDto>(
                CommerceRoute(storePublicId, $"navigation/menus/{menuPublicId:D}/items"),
                request,
                "Unable to create navigation item.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> UpdateNavigationItemAsync(
            Guid storePublicId,
            Guid itemPublicId,
            UpdateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpdateStoreNavigationMenuItemRequest, StoreNavigationMenuDetailDto>(
                CommerceRoute(storePublicId, $"navigation/items/{itemPublicId:D}"),
                request,
                "Unable to update navigation item.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> ArchiveNavigationItemAsync(
            Guid storePublicId,
            Guid itemPublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.DeletePrivateAsync<StoreNavigationMenuDetailDto>(
                CommerceRoute(storePublicId, $"navigation/items/{itemPublicId:D}"),
                "Unable to archive navigation item.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<StoreNavigationMenuDetailDto>> UpdateNavigationItemOrderAsync(
            Guid storePublicId,
            Guid menuPublicId,
            UpdateStoreNavigationMenuItemOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.PutPrivateAsync<UpdateStoreNavigationMenuItemOrderRequest, StoreNavigationMenuDetailDto>(
                CommerceRoute(storePublicId, $"navigation/menus/{menuPublicId:D}/items/order"),
                request,
                "Unable to update navigation item order.",
                cancellationToken);
        }

        public Task<ControlPlaneClientResult<IReadOnlyList<StoreNavigationTargetOptionDto>>> ListNavigationSystemTargetsAsync(
            Guid storePublicId,
            CancellationToken cancellationToken = default)
        {
            return this.ApiClient.GetPrivateAsync<IReadOnlyList<StoreNavigationTargetOptionDto>>(
                CommerceRoute(storePublicId, "navigation/system-targets"),
                "Unable to load navigation system targets.",
                cancellationToken);
        }
    }
}

