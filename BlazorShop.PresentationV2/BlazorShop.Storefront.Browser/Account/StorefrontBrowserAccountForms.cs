namespace BlazorShop.Storefront.Browser.Account;

public sealed class StorefrontBrowserAccountProfileForm
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Company { get; set; }

    public string? PhoneNumber { get; set; }

    public string? PreferredLanguage { get; set; }

    public string? PreferredCurrencyCode { get; set; }
}

public sealed class StorefrontBrowserAccountAddressForm
{
    public string FullName { get; set; } = string.Empty;

    public string? Company { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Address1 { get; set; } = string.Empty;

    public string? Address2 { get; set; }

    public string City { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string? StateProvinceCode { get; set; }

    public string? StateProvinceName { get; set; }

    public bool IsDefaultShipping { get; set; }

    public bool IsDefaultBilling { get; set; }
}

public sealed class StorefrontBrowserAccountPasswordForm
{
    public string? CurrentPassword { get; set; }

    public string? NewPassword { get; set; }

    public string? ConfirmPassword { get; set; }

    public void Clear()
    {
        CurrentPassword = null;
        NewPassword = null;
        ConfirmPassword = null;
    }
}
