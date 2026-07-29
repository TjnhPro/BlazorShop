using BlazorShop.Storefront.Components.Browser;
using BlazorShop.Storefront.Components.Headless.Account;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorShop.Storefront.Browser.Account;

public sealed class StorefrontBrowserAccountController : IStorefrontBrowserAccountController
{
    private readonly IServiceProvider _services;
    private StorefrontFeatureDataMode _profileDataMode = StorefrontFeatureDataMode.BrowserFetch;
    private StorefrontFeatureDataMode _addressDataMode = StorefrontFeatureDataMode.BrowserFetch;
    private StorefrontFeatureDataMode _ordersDataMode = StorefrontFeatureDataMode.BrowserFetch;
    private StorefrontFeatureDataMode _orderDetailDataMode = StorefrontFeatureDataMode.BrowserFetch;
    private StorefrontAccountProfileActionDescriptor _profileActions = StorefrontAccountProfileActionDescriptor.Empty;
    private StorefrontAccountAddressActionDescriptor _addressActions = StorefrontAccountAddressActionDescriptor.Empty;
    private StorefrontAccountOrderActionDescriptor _orderActions = StorefrontAccountOrderActionDescriptor.Empty;
    private StorefrontAccountPasswordActionDescriptor _passwordActions = StorefrontAccountPasswordActionDescriptor.Empty;
    private int _ordersPageNumber = 1;
    private string? _orderReference;
    private bool _receiptMode;
    private bool _profileInitialized;
    private bool _addressesInitialized;
    private bool _ordersInitialized;
    private bool _orderDetailInitialized;

    public StorefrontBrowserAccountController(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public StorefrontBrowserAccountState State { get; } = new();

    public void InitializeProfile(StorefrontBrowserCustomerProfile? initialProfile, string? initialError, string? initialSuccess, StorefrontFeatureDataMode dataMode, StorefrontAccountProfileActionDescriptor actions)
    {
        _profileDataMode = dataMode;
        _profileActions = actions ?? StorefrontAccountProfileActionDescriptor.Empty;
        if (_profileInitialized)
        {
            return;
        }

        State.Profile = initialProfile;
        State.ProfileError = initialError;
        State.ProfileSuccess = initialSuccess;
        if (initialProfile is not null)
        {
            CopyProfileToForm(initialProfile);
        }

        _profileInitialized = true;
    }

    public async Task<bool> HydrateProfileAsync(CancellationToken cancellationToken = default)
    {
        if (_profileDataMode == StorefrontFeatureDataMode.InitialSnapshot)
        {
            return false;
        }

        var apiClient = ResolveApiClient();
        if (apiClient is null || string.IsNullOrWhiteSpace(_profileActions.LoadProfileRoute))
        {
            return false;
        }

        var result = await apiClient.GetAsync<StorefrontBrowserCustomerProfile>(_profileActions.LoadProfileRoute, cancellationToken).ConfigureAwait(false);
        if (result.Success && result.Data is not null)
        {
            State.Profile = result.Data;
            State.ProfileError = null;
            CopyProfileToForm(result.Data);
            return true;
        }

        State.ProfileError = result.Message;
        return true;
    }

    public async Task<bool> SaveProfileAsync(CancellationToken cancellationToken = default)
    {
        var apiClient = ResolveApiClient();
        if (apiClient is null)
        {
            return false;
        }

        State.ProfileSaving = true;
        State.ProfileError = null;
        State.ProfileSuccess = null;
        var result = await apiClient.PutJsonAsync<StorefrontBrowserCustomerProfileUpdateRequest, StorefrontBrowserCustomerProfile>(
            _profileActions.SaveProfileRoute,
            new StorefrontBrowserCustomerProfileUpdateRequest
            {
                Email = State.ProfileForm.Email,
                FullName = State.ProfileForm.FullName,
                FirstName = State.ProfileForm.FirstName,
                LastName = State.ProfileForm.LastName,
                Company = State.ProfileForm.Company,
                PhoneNumber = State.ProfileForm.PhoneNumber,
                PreferredLanguage = State.ProfileForm.PreferredLanguage,
                PreferredCurrencyCode = State.ProfileForm.PreferredCurrencyCode,
            },
            cancellationToken).ConfigureAwait(false);
        State.ProfileSaving = false;
        if (result.Success && result.Data is not null)
        {
            State.Profile = result.Data;
            CopyProfileToForm(result.Data);
            State.ProfileSuccess = "Profile updated.";
            return true;
        }

        State.ProfileError = result.Message;
        return true;
    }

    public void InitializeAddresses(IReadOnlyList<StorefrontBrowserCustomerAddress> initialAddresses, string? initialError, string? initialSuccess, StorefrontFeatureDataMode dataMode, StorefrontAccountAddressActionDescriptor actions)
    {
        _addressDataMode = dataMode;
        _addressActions = actions ?? StorefrontAccountAddressActionDescriptor.Empty;
        if (_addressesInitialized)
        {
            return;
        }

        State.Addresses = initialAddresses;
        State.AddressError = initialError;
        State.AddressSuccess = initialSuccess;
        SyncAddressForms();
        _addressesInitialized = true;
    }

    public async Task<bool> HydrateAddressesAsync(CancellationToken cancellationToken = default)
    {
        if (_addressDataMode == StorefrontFeatureDataMode.InitialSnapshot)
        {
            return false;
        }

        return await RefreshAddressesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> CreateAddressAsync(CancellationToken cancellationToken = default)
    {
        var changed = await MutateAddressAsync(
            client => client.PostJsonAsync<StorefrontBrowserCustomerAddressRequest, StorefrontBrowserCustomerAddress>(
                _addressActions.CreateAddressRoute,
                ToAddressRequest(State.NewAddress),
                cancellationToken),
            cancellationToken).ConfigureAwait(false);
        if (changed && State.AddressError is null)
        {
            State.NewAddress = new StorefrontBrowserAccountAddressForm();
        }

        return changed;
    }

    public Task<bool> UpdateAddressAsync(Guid addressId, CancellationToken cancellationToken = default)
    {
        return State.AddressForms.TryGetValue(addressId, out var form)
            ? MutateAddressAsync(
                client => client.PutJsonAsync<StorefrontBrowserCustomerAddressRequest, StorefrontBrowserCustomerAddress>(
                    _addressActions.UpdateAddressRoute(addressId),
                    ToAddressRequest(form),
                    cancellationToken),
                cancellationToken)
            : Task.FromResult(false);
    }

    public Task<bool> DeleteAddressAsync(Guid addressId, CancellationToken cancellationToken = default)
    {
        return MutateAddressAsync(
            client => client.DeleteAsync<StorefrontBrowserAccountCommandResult>(
                _addressActions.DeleteAddressRoute(addressId),
                cancellationToken),
            cancellationToken);
    }

    public Task<bool> SetDefaultAddressAsync(Guid addressId, bool shipping, CancellationToken cancellationToken = default)
    {
        var route = shipping ? _addressActions.DefaultShippingRoute(addressId) : _addressActions.DefaultBillingRoute(addressId);
        return MutateAddressAsync(
            client => client.PostJsonAsync<object, StorefrontBrowserCustomerAddress>(
                route,
                new { },
                cancellationToken),
            cancellationToken);
    }

    public void InitializeOrders(StorefrontBrowserAccountOrderList initialOrders, string? initialError, StorefrontFeatureDataMode dataMode, StorefrontAccountOrderActionDescriptor actions, int pageNumber)
    {
        _ordersDataMode = dataMode;
        _orderActions = actions ?? StorefrontAccountOrderActionDescriptor.Empty;
        _ordersPageNumber = pageNumber;
        if (_ordersInitialized)
        {
            return;
        }

        State.Orders = initialOrders;
        State.OrdersError = initialError;
        _ordersInitialized = true;
    }

    public async Task<bool> HydrateOrdersAsync(CancellationToken cancellationToken = default)
    {
        if (_ordersDataMode == StorefrontFeatureDataMode.InitialSnapshot)
        {
            return false;
        }

        var apiClient = ResolveApiClient();
        if (apiClient is null)
        {
            return false;
        }

        var result = await apiClient.GetAsync<StorefrontBrowserAccountOrderList>(_orderActions.OrderListRoute(_ordersPageNumber), cancellationToken).ConfigureAwait(false);
        if (result.Success && result.Data is not null)
        {
            State.Orders = result.Data;
            State.OrdersError = null;
            return true;
        }

        State.OrdersError = result.Message;
        return true;
    }

    public void InitializeOrderDetail(StorefrontBrowserAccountOrderDetail? initialOrder, string? initialError, StorefrontFeatureDataMode dataMode, StorefrontAccountOrderActionDescriptor actions, string? orderReference, bool receiptMode)
    {
        _orderDetailDataMode = dataMode;
        _orderActions = actions ?? StorefrontAccountOrderActionDescriptor.Empty;
        _orderReference = orderReference;
        _receiptMode = receiptMode;
        if (_orderDetailInitialized)
        {
            return;
        }

        State.OrderDetail = initialOrder;
        State.OrderDetailError = initialError;
        _orderDetailInitialized = true;
    }

    public async Task<bool> HydrateOrderDetailAsync(CancellationToken cancellationToken = default)
    {
        if (_orderDetailDataMode == StorefrontFeatureDataMode.InitialSnapshot || string.IsNullOrWhiteSpace(_orderReference))
        {
            return false;
        }

        var apiClient = ResolveApiClient();
        if (apiClient is null)
        {
            return false;
        }

        var route = _receiptMode
            ? _orderActions.ReceiptRoute(_orderReference)
            : _orderActions.OrderDetailRoute(_orderReference);
        var result = await apiClient.GetAsync<StorefrontBrowserAccountOrderDetail>(route, cancellationToken).ConfigureAwait(false);
        if (result.Success && result.Data is not null)
        {
            State.OrderDetail = result.Data;
            State.OrderDetailError = null;
            return true;
        }

        State.OrderDetailError = result.Message;
        return true;
    }

    public void InitializePassword(string? initialError, string? initialSuccess, StorefrontAccountPasswordActionDescriptor actions)
    {
        _passwordActions = actions ?? StorefrontAccountPasswordActionDescriptor.Empty;
        State.PasswordError = initialError;
        State.PasswordSuccess = initialSuccess;
    }

    public async Task<bool> ChangePasswordAsync(CancellationToken cancellationToken = default)
    {
        var apiClient = ResolveApiClient();
        if (apiClient is null)
        {
            return false;
        }

        State.PasswordSaving = true;
        State.PasswordError = null;
        State.PasswordSuccess = null;
        if (!string.Equals(State.PasswordForm.NewPassword, State.PasswordForm.ConfirmPassword, StringComparison.Ordinal))
        {
            State.PasswordSaving = false;
            State.PasswordError = "Passwords do not match.";
            return true;
        }

        var result = await apiClient.PostJsonAsync<object, StorefrontBrowserAccountCommandResult>(
            _passwordActions.ChangePasswordRoute,
            new
            {
                State.PasswordForm.CurrentPassword,
                State.PasswordForm.NewPassword,
                State.PasswordForm.ConfirmPassword,
            },
            cancellationToken).ConfigureAwait(false);
        State.PasswordSaving = false;
        if (result.Success && result.Data?.Success == true)
        {
            State.PasswordForm.Clear();
            State.PasswordSuccess = result.Data.Message;
            return true;
        }

        State.PasswordError = result.Message;
        return true;
    }

    private async Task<bool> MutateAddressAsync<T>(
        Func<StorefrontLocalApiClient, Task<StorefrontLocalApiResult<T>>> action,
        CancellationToken cancellationToken)
    {
        var apiClient = ResolveApiClient();
        if (apiClient is null)
        {
            return false;
        }

        State.AddressSaving = true;
        State.AddressError = null;
        State.AddressSuccess = null;
        var result = await action(apiClient).ConfigureAwait(false);
        State.AddressSaving = false;
        if (!result.Success)
        {
            State.AddressError = result.Message;
            return true;
        }

        State.AddressSuccess = "Address book updated.";
        await RefreshAddressesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async Task<bool> RefreshAddressesAsync(CancellationToken cancellationToken)
    {
        var apiClient = ResolveApiClient();
        if (apiClient is null)
        {
            return false;
        }

        var result = await apiClient.GetAsync<IReadOnlyList<StorefrontBrowserCustomerAddress>>(_addressActions.CurrentAddressesRoute, cancellationToken).ConfigureAwait(false);
        if (result.Success && result.Data is not null)
        {
            State.Addresses = result.Data;
            SyncAddressForms();
            return true;
        }

        State.AddressError = result.Message;
        return true;
    }

    private void SyncAddressForms()
    {
        State.AddressForms.Clear();
        foreach (var address in State.Addresses)
        {
            State.AddressForms[address.PublicId] = ToAddressForm(address);
        }
    }

    private void CopyProfileToForm(StorefrontBrowserCustomerProfile profile)
    {
        State.ProfileForm.Email = profile.Email;
        State.ProfileForm.FullName = profile.FullName;
        State.ProfileForm.FirstName = profile.FirstName;
        State.ProfileForm.LastName = profile.LastName;
        State.ProfileForm.Company = profile.Company;
        State.ProfileForm.PhoneNumber = profile.PhoneNumber;
        State.ProfileForm.PreferredLanguage = profile.PreferredLanguage;
        State.ProfileForm.PreferredCurrencyCode = profile.PreferredCurrencyCode;
    }

    private static StorefrontBrowserAccountAddressForm ToAddressForm(StorefrontBrowserCustomerAddress address)
    {
        return new StorefrontBrowserAccountAddressForm
        {
            FullName = address.FullName,
            Company = address.Company,
            Email = address.Email,
            Phone = address.Phone,
            Address1 = address.Address1,
            Address2 = address.Address2,
            City = address.City,
            PostalCode = address.PostalCode,
            CountryCode = address.CountryCode,
            StateProvinceCode = address.StateProvinceCode,
            StateProvinceName = address.StateProvinceName,
            IsDefaultShipping = address.IsDefaultShipping,
            IsDefaultBilling = address.IsDefaultBilling,
        };
    }

    private static StorefrontBrowserCustomerAddressRequest ToAddressRequest(StorefrontBrowserAccountAddressForm form)
    {
        return new StorefrontBrowserCustomerAddressRequest
        {
            FullName = form.FullName,
            Company = form.Company,
            Email = form.Email,
            Phone = form.Phone,
            Address1 = form.Address1,
            Address2 = form.Address2,
            City = form.City,
            PostalCode = form.PostalCode,
            CountryCode = form.CountryCode,
            StateProvinceCode = form.StateProvinceCode,
            StateProvinceName = form.StateProvinceName,
            IsDefaultShipping = form.IsDefaultShipping,
            IsDefaultBilling = form.IsDefaultBilling,
        };
    }

    private StorefrontLocalApiClient? ResolveApiClient()
    {
        return _services.GetService<StorefrontLocalApiClient>();
    }
}
