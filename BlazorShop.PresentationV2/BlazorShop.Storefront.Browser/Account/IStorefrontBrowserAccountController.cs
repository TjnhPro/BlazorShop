using BlazorShop.Storefront.Components.Browser;
using BlazorShop.Storefront.Components.Headless.Account;

namespace BlazorShop.Storefront.Browser.Account;

public interface IStorefrontBrowserAccountController
{
    StorefrontBrowserAccountState State { get; }

    void InitializeProfile(StorefrontBrowserCustomerProfile? initialProfile, string? initialError, string? initialSuccess, StorefrontFeatureDataMode dataMode, StorefrontAccountProfileActionDescriptor actions);

    Task<bool> HydrateProfileAsync(CancellationToken cancellationToken = default);

    Task<bool> SaveProfileAsync(CancellationToken cancellationToken = default);

    void InitializeAddresses(IReadOnlyList<StorefrontBrowserCustomerAddress> initialAddresses, string? initialError, string? initialSuccess, StorefrontFeatureDataMode dataMode, StorefrontAccountAddressActionDescriptor actions);

    Task<bool> HydrateAddressesAsync(CancellationToken cancellationToken = default);

    Task<bool> CreateAddressAsync(CancellationToken cancellationToken = default);

    Task<bool> UpdateAddressAsync(Guid addressId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAddressAsync(Guid addressId, CancellationToken cancellationToken = default);

    Task<bool> SetDefaultAddressAsync(Guid addressId, bool shipping, CancellationToken cancellationToken = default);

    void InitializeOrders(StorefrontBrowserAccountOrderList initialOrders, string? initialError, StorefrontFeatureDataMode dataMode, StorefrontAccountOrderActionDescriptor actions, int pageNumber);

    Task<bool> HydrateOrdersAsync(CancellationToken cancellationToken = default);

    void InitializeOrderDetail(StorefrontBrowserAccountOrderDetail? initialOrder, string? initialError, StorefrontFeatureDataMode dataMode, StorefrontAccountOrderActionDescriptor actions, string? orderReference, bool receiptMode);

    Task<bool> HydrateOrderDetailAsync(CancellationToken cancellationToken = default);

    void InitializePassword(string? initialError, string? initialSuccess, StorefrontAccountPasswordActionDescriptor actions);

    Task<bool> ChangePasswordAsync(CancellationToken cancellationToken = default);
}
