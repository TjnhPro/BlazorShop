namespace BlazorShop.Application.CommerceNode.Currencies
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.DTOs;

    public sealed record StoreCurrencyDto(
        Guid Id,
        string CurrencyCode,
        bool IsEnabled,
        bool IsBaseCurrency,
        bool IsDefaultDisplayCurrency,
        int DisplayOrder,
        string? CultureName,
        string? Symbol,
        int DecimalDigits,
        string UnitPriceRoundingMode,
        decimal UnitPriceRoundingIncrement,
        string LineTotalRoundingMode,
        decimal LineTotalRoundingIncrement,
        string OrderTotalRoundingMode,
        decimal OrderTotalRoundingIncrement,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed record UpdateStoreCurrencyRequest(
        bool IsEnabled = true,
        bool IsDefaultDisplayCurrency = false,
        [property: Range(0, 10000)] int DisplayOrder = 0,
        [property: StringLength(32)] string? CultureName = null,
        [property: StringLength(16)] string? Symbol = null,
        [property: Range(0, 4)] int DecimalDigits = 2,
        [property: StringLength(32)] string UnitPriceRoundingMode = "halfAwayFromZero",
        [property: Range(typeof(decimal), "0.0001", "1000000")] decimal UnitPriceRoundingIncrement = 0.01m,
        [property: StringLength(32)] string LineTotalRoundingMode = "halfAwayFromZero",
        [property: Range(typeof(decimal), "0.0001", "1000000")] decimal LineTotalRoundingIncrement = 0.01m,
        [property: StringLength(32)] string OrderTotalRoundingMode = "halfAwayFromZero",
        [property: Range(typeof(decimal), "0.0001", "1000000")] decimal OrderTotalRoundingIncrement = 0.01m);

    public interface IStoreCurrencyService
    {
        Task<IReadOnlyList<StoreCurrencyDto>> GetAsync(CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreCurrencyDto>> UpdateAsync(
            string currencyCode,
            UpdateStoreCurrencyRequest request,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> ResolveSupportedCurrencyCodesAsync(
            Guid storeId,
            CancellationToken cancellationToken = default);
    }
}
