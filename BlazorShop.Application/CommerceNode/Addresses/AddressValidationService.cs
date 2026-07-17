namespace BlazorShop.Application.CommerceNode.Addresses
{
    using System.Net.Mail;

    public sealed class AddressValidationService : IAddressValidationService
    {
        private static readonly HashSet<string> CountriesRequiringState = new(StringComparer.Ordinal)
        {
            "AU",
            "CA",
            "US",
        };

        public AddressValidationResult ValidateAndNormalize(CustomerAddressCreateRequest request)
        {
            var normalized = Normalize(request);
            return new AddressValidationResult(normalized, Validate(normalized));
        }

        public AddressValidationResult ValidateAndNormalize(CustomerAddressUpdateRequest request)
        {
            var normalized = Normalize(new CustomerAddressCreateRequest(
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
                request.IsDefaultBilling));

            return new AddressValidationResult(normalized, Validate(normalized));
        }

        private static CustomerAddressCreateRequest Normalize(CustomerAddressCreateRequest request)
        {
            return request with
            {
                FirstName = NormalizeRequired(request.FirstName),
                LastName = NormalizeRequired(request.LastName),
                Company = NormalizeOptional(request.Company),
                Address1 = NormalizeRequired(request.Address1),
                Address2 = NormalizeOptional(request.Address2),
                City = NormalizeRequired(request.City),
                PostalCode = NormalizeRequired(request.PostalCode),
                CountryCode = NormalizeRequired(request.CountryCode).ToUpperInvariant(),
                StateProvinceCode = NormalizeOptional(request.StateProvinceCode)?.ToUpperInvariant(),
                StateProvinceName = NormalizeOptional(request.StateProvinceName),
                Phone = NormalizeOptional(request.Phone),
                Email = NormalizeOptional(request.Email),
            };
        }

        private static IReadOnlyList<AddressValidationIssue> Validate(CustomerAddressCreateRequest address)
        {
            var issues = new List<AddressValidationIssue>();
            AddRequiredIssue(issues, address.FirstName, "first_name_required", "First name is required.", "firstName");
            AddRequiredIssue(issues, address.LastName, "last_name_required", "Last name is required.", "lastName");
            AddRequiredIssue(issues, address.Address1, "address1_required", "Address line 1 is required.", "address1");
            AddRequiredIssue(issues, address.City, "city_required", "City is required.", "city");
            AddRequiredIssue(issues, address.PostalCode, "postal_code_required", "Postal code is required.", "postalCode");

            if (!IsCountryCode(address.CountryCode))
            {
                issues.Add(new AddressValidationIssue("country_invalid", "Country code must be a two-letter ISO code.", "countryCode"));
            }
            else if (CountriesRequiringState.Contains(address.CountryCode)
                     && string.IsNullOrWhiteSpace(address.StateProvinceCode)
                     && string.IsNullOrWhiteSpace(address.StateProvinceName))
            {
                issues.Add(new AddressValidationIssue("state_province_required", "State or province is required for this country.", "stateProvinceCode"));
            }

            AddMaxLengthIssue(issues, address.FirstName, 120, "first_name_too_long", "First name must be 120 characters or fewer.", "firstName");
            AddMaxLengthIssue(issues, address.LastName, 120, "last_name_too_long", "Last name must be 120 characters or fewer.", "lastName");
            AddMaxLengthIssue(issues, address.Company, 160, "company_too_long", "Company must be 160 characters or fewer.", "company");
            AddMaxLengthIssue(issues, address.Address1, 240, "address1_too_long", "Address line 1 must be 240 characters or fewer.", "address1");
            AddMaxLengthIssue(issues, address.Address2, 240, "address2_too_long", "Address line 2 must be 240 characters or fewer.", "address2");
            AddMaxLengthIssue(issues, address.City, 120, "city_too_long", "City must be 120 characters or fewer.", "city");
            AddMaxLengthIssue(issues, address.PostalCode, 32, "postal_code_too_long", "Postal code must be 32 characters or fewer.", "postalCode");
            AddMaxLengthIssue(issues, address.StateProvinceCode, 64, "state_province_code_too_long", "State or province code must be 64 characters or fewer.", "stateProvinceCode");
            AddMaxLengthIssue(issues, address.StateProvinceName, 120, "state_province_name_too_long", "State or province name must be 120 characters or fewer.", "stateProvinceName");
            AddMaxLengthIssue(issues, address.Phone, 32, "phone_too_long", "Phone must be 32 characters or fewer.", "phone");
            AddMaxLengthIssue(issues, address.Email, 256, "email_too_long", "Email must be 256 characters or fewer.", "email");

            if (address.Email is not null && !IsEmail(address.Email))
            {
                issues.Add(new AddressValidationIssue("email_invalid", "Email is invalid.", "email"));
            }

            return issues;
        }

        private static void AddRequiredIssue(
            ICollection<AddressValidationIssue> issues,
            string? value,
            string code,
            string message,
            string field)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                issues.Add(new AddressValidationIssue(code, message, field));
            }
        }

        private static void AddMaxLengthIssue(
            ICollection<AddressValidationIssue> issues,
            string? value,
            int maxLength,
            string code,
            string message,
            string field)
        {
            if (value is { Length: > 0 } && value.Length > maxLength)
            {
                issues.Add(new AddressValidationIssue(code, message, field));
            }
        }

        private static bool IsCountryCode(string value)
        {
            return value.Length == 2 && value.All(char.IsLetter);
        }

        private static bool IsEmail(string value)
        {
            try
            {
                _ = new MailAddress(value);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string NormalizeRequired(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static string? NormalizeOptional(string? value)
        {
            var normalized = value?.Trim();
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }
    }
}
