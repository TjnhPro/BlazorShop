namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;


    public sealed record StorefrontAddressCountryResponse(
        string Code,
        string Name,
        bool PostalCodeRequired,
        bool StateProvinceRequired);

    public sealed record StorefrontAddressStateProvinceResponse(
        string Code,
        string Name);

    public sealed record StorefrontAddressFieldConfigurationResponse(
        bool CompanyEnabled,
        bool PhoneEnabled,
        bool PhoneRequired,
        bool PostalCodeRequired,
        bool BillingAddressEnabled,
        bool UseShippingAddressAsBillingDefault,
        int FirstNameMaxLength,
        int LastNameMaxLength,
        int CompanyMaxLength,
        int AddressLineMaxLength,
        int CityMaxLength,
        int PostalCodeMaxLength,
        int StateProvinceCodeMaxLength,
        int StateProvinceNameMaxLength,
        int PhoneMaxLength,
        int EmailMaxLength,
        IReadOnlyList<string> StateProvinceRequiredCountryCodes);

    public sealed class StorefrontCustomerAddressRequest
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Company { get; set; }

        public string Address1 { get; set; } = string.Empty;

        public string? Address2 { get; set; }

        public string City { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string CountryCode { get; set; } = string.Empty;

        public string? StateProvinceCode { get; set; }

        public string? StateProvinceName { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public bool IsDefaultShipping { get; set; }

        public bool IsDefaultBilling { get; set; }
    }

    public sealed record StorefrontCustomerAddressResponse(
        Guid PublicId,
        string FirstName,
        string LastName,
        string? Company,
        string Address1,
        string? Address2,
        string City,
        string PostalCode,
        string CountryCode,
        string? StateProvinceCode,
        string? StateProvinceName,
        string? Phone,
        string? Email,
        bool IsDefaultShipping,
        bool IsDefaultBilling,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc)
    {
        public string FullName => string.Join(" ", new[] { FirstName, LastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    public sealed record StorefrontShippingAddressResponse(
        string? FullName,
        string? Email,
        string? Phone,
        string? Address1,
        string? Address2,
        string? City,
        string? State,
        string? PostalCode,
        string? CountryCode);
}
