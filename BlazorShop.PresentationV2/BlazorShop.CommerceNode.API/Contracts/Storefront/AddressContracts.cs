namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    public sealed class StorefrontCheckoutShippingAddress
    {
        [Required]
        [MaxLength(160)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(StorefrontContractValidation.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [MaxLength(32)]
        public string? Phone { get; set; }

        [Required]
        [MaxLength(240)]
        public string Address1 { get; set; } = string.Empty;

        [MaxLength(240)]
        public string? Address2 { get; set; }

        [Required]
        [MaxLength(120)]
        public string City { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? State { get; set; }

        [Required]
        [MaxLength(32)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string CountryCode { get; set; } = string.Empty;
    }

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
        [Required]
        [StringLength(120)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(160)]
        public string? Company { get; set; }

        [Required]
        [StringLength(240)]
        public string Address1 { get; set; } = string.Empty;

        [StringLength(240)]
        public string? Address2 { get; set; }

        [Required]
        [StringLength(120)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(32)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^[A-Za-z]{2}$")]
        public string CountryCode { get; set; } = string.Empty;

        [StringLength(64)]
        public string? StateProvinceCode { get; set; }

        [StringLength(120)]
        public string? StateProvinceName { get; set; }

        [StringLength(32)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(256)]
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
        DateTimeOffset UpdatedAtUtc);

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
