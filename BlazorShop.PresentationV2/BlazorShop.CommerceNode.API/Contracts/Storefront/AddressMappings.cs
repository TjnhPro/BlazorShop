namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.IdentityModel.Tokens.Jwt;

    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.CommerceNode.Captcha;
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Consent;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Domain.Contracts;
    public static partial class StorefrontContractMappings
    {
        public static CheckoutShippingAddress ToApplicationRequest(this StorefrontCheckoutShippingAddress request)
        {
            return new CheckoutShippingAddress
            {
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                Address1 = request.Address1,
                Address2 = request.Address2,
                City = request.City,
                State = request.State,
                PostalCode = request.PostalCode,
                CountryCode = request.CountryCode,
            };
        }
        public static StorefrontCheckoutShippingAddress ToStorefrontContract(this CheckoutShippingAddress address)
        {
            return new StorefrontCheckoutShippingAddress
            {
                FullName = address.FullName,
                Email = address.Email,
                Phone = address.Phone,
                Address1 = address.Address1,
                Address2 = address.Address2,
                City = address.City,
                State = address.State,
                PostalCode = address.PostalCode,
                CountryCode = address.CountryCode,
            };
        }
        public static StorefrontAddressCountryResponse ToStorefrontContract(this AddressCountryDto country)
        {
            return new StorefrontAddressCountryResponse(
                country.Code,
                country.Name,
                country.PostalCodeRequired,
                country.StateProvinceRequired);
        }
        public static StorefrontAddressStateProvinceResponse ToStorefrontContract(this AddressStateProvinceDto state)
        {
            return new StorefrontAddressStateProvinceResponse(state.Code, state.Name);
        }
        public static StorefrontAddressFieldConfigurationResponse ToStorefrontContract(this AddressFieldConfigurationDto configuration)
        {
            return new StorefrontAddressFieldConfigurationResponse(
                configuration.CompanyEnabled,
                configuration.PhoneEnabled,
                configuration.PhoneRequired,
                configuration.PostalCodeRequired,
                configuration.BillingAddressEnabled,
                configuration.UseShippingAddressAsBillingDefault,
                configuration.FirstNameMaxLength,
                configuration.LastNameMaxLength,
                configuration.CompanyMaxLength,
                configuration.AddressLineMaxLength,
                configuration.CityMaxLength,
                configuration.PostalCodeMaxLength,
                configuration.StateProvinceCodeMaxLength,
                configuration.StateProvinceNameMaxLength,
                configuration.PhoneMaxLength,
                configuration.EmailMaxLength,
                configuration.StateProvinceRequiredCountryCodes);
        }
        public static CustomerAddressCreateRequest ToApplicationRequest(this StorefrontCustomerAddressRequest request)
        {
            return new CustomerAddressCreateRequest(
                request.FirstName,
                request.LastName,
                request.Company,
                request.Address1,
                request.Address2,
                request.City,
                request.PostalCode,
                request.CountryCode,
                request.StateProvinceCode,
                request.StateProvinceName,
                request.Phone,
                request.Email,
                request.IsDefaultShipping,
                request.IsDefaultBilling);
        }
        public static StorefrontCustomerAddressResponse ToStorefrontContract(this CustomerAddressDto address)
        {
            return new StorefrontCustomerAddressResponse(
                address.PublicId,
                address.FirstName,
                address.LastName,
                address.Company,
                address.Address1,
                address.Address2,
                address.City,
                address.PostalCode,
                address.CountryCode,
                address.StateProvinceCode,
                address.StateProvinceName,
                address.Phone,
                address.Email,
                address.IsDefaultShipping,
                address.IsDefaultBilling,
                address.CreatedAtUtc,
                address.UpdatedAtUtc);
        }
        public static StorefrontCheckoutShippingAddressDto ToPreviewShippingAddress(this StorefrontCheckoutShippingAddress request)
        {
            return new StorefrontCheckoutShippingAddressDto(
                request.FullName,
                request.Email,
                request.Phone,
                request.Address1,
                request.Address2,
                request.City,
                request.State,
                request.PostalCode,
                request.CountryCode);
        }
        public static StorefrontCheckoutShippingAddress ToStorefrontContract(this StorefrontCheckoutShippingAddressDto address)
        {
            return new StorefrontCheckoutShippingAddress
            {
                FullName = address.FullName,
                Email = address.Email,
                Phone = address.Phone,
                Address1 = address.Address1,
                Address2 = address.Address2,
                City = address.City,
                State = address.State,
                PostalCode = address.PostalCode,
                CountryCode = address.CountryCode,
            };
        }
    }
}
