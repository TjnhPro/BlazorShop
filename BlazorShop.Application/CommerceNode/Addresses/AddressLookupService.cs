namespace BlazorShop.Application.CommerceNode.Addresses
{
    using BlazorShop.Application.DTOs;

    public sealed class AddressLookupService : IAddressLookupService
    {
        private static readonly AddressCountryDto[] Countries =
        [
            new("AU", "Australia", PostalCodeRequired: true, StateProvinceRequired: true),
            new("CA", "Canada", PostalCodeRequired: true, StateProvinceRequired: true),
            new("DE", "Germany", PostalCodeRequired: true, StateProvinceRequired: false),
            new("FR", "France", PostalCodeRequired: true, StateProvinceRequired: false),
            new("GB", "United Kingdom", PostalCodeRequired: true, StateProvinceRequired: false),
            new("US", "United States", PostalCodeRequired: true, StateProvinceRequired: true),
            new("VN", "Vietnam", PostalCodeRequired: true, StateProvinceRequired: false),
        ];

        private static readonly IReadOnlyDictionary<string, AddressStateProvinceDto[]> StatesByCountry =
            new Dictionary<string, AddressStateProvinceDto[]>(StringComparer.Ordinal)
            {
                ["AU"] =
                [
                    new("ACT", "Australian Capital Territory"),
                    new("NSW", "New South Wales"),
                    new("NT", "Northern Territory"),
                    new("QLD", "Queensland"),
                    new("SA", "South Australia"),
                    new("TAS", "Tasmania"),
                    new("VIC", "Victoria"),
                    new("WA", "Western Australia"),
                ],
                ["CA"] =
                [
                    new("AB", "Alberta"),
                    new("BC", "British Columbia"),
                    new("MB", "Manitoba"),
                    new("NB", "New Brunswick"),
                    new("NL", "Newfoundland and Labrador"),
                    new("NS", "Nova Scotia"),
                    new("NT", "Northwest Territories"),
                    new("NU", "Nunavut"),
                    new("ON", "Ontario"),
                    new("PE", "Prince Edward Island"),
                    new("QC", "Quebec"),
                    new("SK", "Saskatchewan"),
                    new("YT", "Yukon"),
                ],
                ["US"] =
                [
                    new("AL", "Alabama"),
                    new("AK", "Alaska"),
                    new("AZ", "Arizona"),
                    new("AR", "Arkansas"),
                    new("CA", "California"),
                    new("CO", "Colorado"),
                    new("CT", "Connecticut"),
                    new("DE", "Delaware"),
                    new("FL", "Florida"),
                    new("GA", "Georgia"),
                    new("HI", "Hawaii"),
                    new("ID", "Idaho"),
                    new("IL", "Illinois"),
                    new("IN", "Indiana"),
                    new("IA", "Iowa"),
                    new("KS", "Kansas"),
                    new("KY", "Kentucky"),
                    new("LA", "Louisiana"),
                    new("ME", "Maine"),
                    new("MD", "Maryland"),
                    new("MA", "Massachusetts"),
                    new("MI", "Michigan"),
                    new("MN", "Minnesota"),
                    new("MS", "Mississippi"),
                    new("MO", "Missouri"),
                    new("MT", "Montana"),
                    new("NE", "Nebraska"),
                    new("NV", "Nevada"),
                    new("NH", "New Hampshire"),
                    new("NJ", "New Jersey"),
                    new("NM", "New Mexico"),
                    new("NY", "New York"),
                    new("NC", "North Carolina"),
                    new("ND", "North Dakota"),
                    new("OH", "Ohio"),
                    new("OK", "Oklahoma"),
                    new("OR", "Oregon"),
                    new("PA", "Pennsylvania"),
                    new("RI", "Rhode Island"),
                    new("SC", "South Carolina"),
                    new("SD", "South Dakota"),
                    new("TN", "Tennessee"),
                    new("TX", "Texas"),
                    new("UT", "Utah"),
                    new("VT", "Vermont"),
                    new("VA", "Virginia"),
                    new("WA", "Washington"),
                    new("WV", "West Virginia"),
                    new("WI", "Wisconsin"),
                    new("WY", "Wyoming"),
                    new("DC", "District of Columbia"),
                ],
            };

        private static readonly AddressFieldConfigurationDto Configuration = new(
            CompanyEnabled: true,
            PhoneEnabled: true,
            PhoneRequired: false,
            PostalCodeRequired: true,
            BillingAddressEnabled: false,
            UseShippingAddressAsBillingDefault: true,
            FirstNameMaxLength: 120,
            LastNameMaxLength: 120,
            CompanyMaxLength: 160,
            AddressLineMaxLength: 240,
            CityMaxLength: 120,
            PostalCodeMaxLength: 32,
            StateProvinceCodeMaxLength: 64,
            StateProvinceNameMaxLength: 120,
            PhoneMaxLength: 32,
            EmailMaxLength: 256,
            StateProvinceRequiredCountryCodes: ["AU", "CA", "US"]);

        public Task<IReadOnlyList<AddressCountryDto>> GetCountriesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AddressCountryDto>>(Countries);
        }

        public Task<ServiceResponse<IReadOnlyList<AddressStateProvinceDto>>> GetStatesAsync(
            string countryCode,
            CancellationToken cancellationToken = default)
        {
            var normalized = NormalizeCountryCode(countryCode);
            if (normalized is null || Countries.All(country => country.Code != normalized))
            {
                return Task.FromResult(new ServiceResponse<IReadOnlyList<AddressStateProvinceDto>>(
                    false,
                    "Country was not found.")
                {
                    ResponseType = ServiceResponseType.NotFound,
                    Payload = [],
                });
            }

            var states = StatesByCountry.TryGetValue(normalized, out var configuredStates)
                ? configuredStates
                : [];
            return Task.FromResult(new ServiceResponse<IReadOnlyList<AddressStateProvinceDto>>(
                true,
                "Address states resolved.")
            {
                ResponseType = ServiceResponseType.Success,
                Payload = states,
            });
        }

        public Task<AddressFieldConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Configuration);
        }

        private static string? NormalizeCountryCode(string? countryCode)
        {
            var normalized = countryCode?.Trim().ToUpperInvariant();
            return normalized is { Length: 2 } && normalized.All(char.IsLetter) ? normalized : null;
        }
    }
}
