using BlazorShop.Storefront.Components.Browser;

namespace BlazorShop.Storefront.Browser.Account;

public sealed class StorefrontBrowserAccountState
{
    public StorefrontBrowserCustomerProfile? Profile { get; internal set; }

    public StorefrontBrowserAccountProfileForm ProfileForm { get; } = new();

    public string? ProfileError { get; internal set; }

    public string? ProfileSuccess { get; internal set; }

    public bool ProfileSaving { get; internal set; }

    public IReadOnlyList<StorefrontBrowserCustomerAddress> Addresses { get; internal set; } = [];

    public Dictionary<Guid, StorefrontBrowserAccountAddressForm> AddressForms { get; } = [];

    public StorefrontBrowserAccountAddressForm NewAddress { get; internal set; } = new();

    public string? AddressError { get; internal set; }

    public string? AddressSuccess { get; internal set; }

    public bool AddressSaving { get; internal set; }

    public StorefrontBrowserAccountOrderList Orders { get; internal set; } = new([], 1, 10, 0, 0);

    public string? OrdersError { get; internal set; }

    public StorefrontBrowserAccountOrderDetail? OrderDetail { get; internal set; }

    public string? OrderDetailError { get; internal set; }

    public StorefrontBrowserAccountPasswordForm PasswordForm { get; } = new();

    public string? PasswordError { get; internal set; }

    public string? PasswordSuccess { get; internal set; }

    public bool PasswordSaving { get; internal set; }
}
