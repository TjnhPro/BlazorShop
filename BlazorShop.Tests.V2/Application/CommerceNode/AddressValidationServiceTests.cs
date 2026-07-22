namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Addresses;

    using Xunit;

    public sealed class AddressValidationServiceTests
    {
        private readonly AddressValidationService service = new();

        [Fact]
        public void ValidateAndNormalize_WhenAddressIsValid_TrimsAndNormalizesCodes()
        {
            var result = this.service.ValidateAndNormalize(new CustomerAddressCreateRequest(
                " Ada ",
                " Lovelace ",
                " Example Co ",
                " 100 Main St ",
                " Suite 4 ",
                " New York ",
                " 10001 ",
                " us ",
                " ny ",
                " New York ",
                " 5550100 ",
                " ada@example.test ",
                IsDefaultShipping: true,
                IsDefaultBilling: false));

            Assert.True(result.IsValid);
            Assert.Empty(result.Issues);
            Assert.Equal("Ada", result.Address.FirstName);
            Assert.Equal("Lovelace", result.Address.LastName);
            Assert.Equal("Example Co", result.Address.Company);
            Assert.Equal("100 Main St", result.Address.Address1);
            Assert.Equal("Suite 4", result.Address.Address2);
            Assert.Equal("New York", result.Address.City);
            Assert.Equal("10001", result.Address.PostalCode);
            Assert.Equal("US", result.Address.CountryCode);
            Assert.Equal("NY", result.Address.StateProvinceCode);
            Assert.Equal("5550100", result.Address.Phone);
            Assert.Equal("ada@example.test", result.Address.Email);
            Assert.True(result.Address.IsDefaultShipping);
            Assert.False(result.Address.IsDefaultBilling);
        }

        [Fact]
        public void ValidateAndNormalize_WhenRequiredFieldsAreMissing_ReturnsStableIssues()
        {
            var result = this.service.ValidateAndNormalize(new CustomerAddressCreateRequest(
                " ",
                "",
                null,
                " ",
                null,
                "",
                " ",
                "VN",
                null,
                null,
                null,
                null,
                IsDefaultShipping: false,
                IsDefaultBilling: false));

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.Code == "first_name_required" && issue.Field == "firstName");
            Assert.Contains(result.Issues, issue => issue.Code == "last_name_required" && issue.Field == "lastName");
            Assert.Contains(result.Issues, issue => issue.Code == "address1_required" && issue.Field == "address1");
            Assert.Contains(result.Issues, issue => issue.Code == "city_required" && issue.Field == "city");
            Assert.Contains(result.Issues, issue => issue.Code == "postal_code_required" && issue.Field == "postalCode");
        }

        [Fact]
        public void ValidateAndNormalize_WhenCountryOrEmailIsInvalid_ReturnsStableIssues()
        {
            var result = this.service.ValidateAndNormalize(ValidAddress() with
            {
                CountryCode = "USA",
                Email = "not-an-email",
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.Code == "country_invalid" && issue.Field == "countryCode");
            Assert.Contains(result.Issues, issue => issue.Code == "email_invalid" && issue.Field == "email");
        }

        [Fact]
        public void ValidateAndNormalize_WhenCountryRequiresState_ReturnsStateIssue()
        {
            var result = this.service.ValidateAndNormalize(ValidAddress() with
            {
                CountryCode = "ca",
                StateProvinceCode = " ",
                StateProvinceName = null,
            });

            Assert.False(result.IsValid);
            Assert.Equal("CA", result.Address.CountryCode);
            Assert.Contains(result.Issues, issue => issue.Code == "state_province_required" && issue.Field == "stateProvinceCode");
        }

        [Fact]
        public void ValidateAndNormalize_WhenOptionalFieldsAreBlank_CollapsesThemToNull()
        {
            var result = this.service.ValidateAndNormalize(ValidAddress() with
            {
                Company = " ",
                Address2 = "",
                Phone = " ",
                Email = null,
                StateProvinceName = " ",
            });

            Assert.True(result.IsValid);
            Assert.Null(result.Address.Company);
            Assert.Null(result.Address.Address2);
            Assert.Null(result.Address.Phone);
            Assert.Null(result.Address.Email);
            Assert.Null(result.Address.StateProvinceName);
        }

        [Fact]
        public void ValidateAndNormalize_WhenFieldsExceedMaxLength_ReturnsStableIssues()
        {
            var result = this.service.ValidateAndNormalize(ValidAddress() with
            {
                Phone = new string('1', 33),
                Address1 = new string('a', 241),
            });

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.Code == "phone_too_long" && issue.Field == "phone");
            Assert.Contains(result.Issues, issue => issue.Code == "address1_too_long" && issue.Field == "address1");
        }

        private static CustomerAddressCreateRequest ValidAddress()
        {
            return new CustomerAddressCreateRequest(
                "Ada",
                "Lovelace",
                null,
                "100 Main St",
                null,
                "New York",
                "10001",
                "US",
                "NY",
                null,
                "5550100",
                "ada@example.test",
                IsDefaultShipping: false,
                IsDefaultBilling: false);
        }
    }
}
