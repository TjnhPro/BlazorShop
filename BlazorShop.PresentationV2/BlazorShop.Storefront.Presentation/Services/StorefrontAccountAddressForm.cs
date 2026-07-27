namespace BlazorShop.Storefront.Services
{
    public sealed class StorefrontAccountAddressForm
    {
        public string? Action { get; set; }

        public Guid? AddressId { get; set; }

        public string? FullName { get; set; }

        public string? Company { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Address1 { get; set; }

        public string? Address2 { get; set; }

        public string? City { get; set; }

        public string? StateProvinceCode { get; set; }

        public string? StateProvinceName { get; set; }

        public string? PostalCode { get; set; }

        public string? CountryCode { get; set; }

        public bool IsDefaultShipping { get; set; }

        public bool IsDefaultBilling { get; set; }
    }
}
