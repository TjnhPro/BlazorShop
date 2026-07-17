namespace BlazorShop.Application.CommerceNode.Addresses
{
    using BlazorShop.Application.DTOs;

    public sealed record AddressCountryDto(
        string Code,
        string Name,
        bool PostalCodeRequired,
        bool StateProvinceRequired);

    public sealed record AddressStateProvinceDto(
        string Code,
        string Name);

    public sealed record AddressFieldConfigurationDto(
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

    public interface IAddressLookupService
    {
        Task<IReadOnlyList<AddressCountryDto>> GetCountriesAsync(CancellationToken cancellationToken = default);

        Task<ServiceResponse<IReadOnlyList<AddressStateProvinceDto>>> GetStatesAsync(
            string countryCode,
            CancellationToken cancellationToken = default);

        Task<AddressFieldConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default);
    }
}
