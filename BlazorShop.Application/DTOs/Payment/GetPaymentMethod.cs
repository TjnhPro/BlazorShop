namespace BlazorShop.Application.DTOs.Payment
{
    public class GetPaymentMethod
    {
        public required Guid Id { get; set; }

        public string Key { get; set; } = string.Empty;

        public required string Name { get; set; }

        public string? Description { get; set; }

        public string? ShortDisplayText { get; set; }

        public string? IconUrl { get; set; }

        public IReadOnlyList<string> SupportedCurrencyCodes { get; set; } = [];

        public IReadOnlyList<string> SupportedCountryCodes { get; set; } = [];
    }
}
