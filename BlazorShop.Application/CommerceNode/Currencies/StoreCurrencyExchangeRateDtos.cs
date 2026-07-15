namespace BlazorShop.Application.CommerceNode.Currencies
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.DTOs;

    public sealed record StoreCurrencyExchangeRateDto(
        Guid Id,
        string BaseCurrencyCode,
        string TargetCurrencyCode,
        decimal Rate,
        string ProviderKey,
        string? Source,
        DateTimeOffset EffectiveAt,
        DateTimeOffset? ExpiresAt,
        bool IsManual,
        bool IsEnabled,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public sealed record UpsertStoreCurrencyExchangeRateRequest(
        [property: Range(typeof(decimal), "0.000001", "1000000000")] decimal Rate,
        [property: StringLength(256)] string? Source = null,
        DateTimeOffset? EffectiveAt = null,
        DateTimeOffset? ExpiresAt = null,
        bool IsEnabled = true);

    public sealed record FetchStoreCurrencyExchangeRatesRequest(
        [property: Required]
        [property: StringLength(64, MinimumLength = 1)]
        string ProviderKey,
        IReadOnlyList<string>? TargetCurrencyCodes = null,
        bool IsEnabled = true);

    public sealed record StoreCurrencyExchangeRateProviderDto(
        string ProviderKey,
        bool Enabled,
        bool SecretsConfigured,
        string Status,
        string? Source);

    public sealed record StoreCurrencyExchangeRateProviderFetchResult(
        string ProviderKey,
        int UpdatedCount,
        IReadOnlyList<StoreCurrencyExchangeRateDto> Rates);

    public sealed record ExchangeRateProviderFetchRequest(
        Guid StoreId,
        string BaseCurrencyCode,
        IReadOnlyList<string> TargetCurrencyCodes);

    public sealed record ExchangeRateProviderRate(
        string BaseCurrencyCode,
        string TargetCurrencyCode,
        decimal Rate,
        string? Source,
        DateTimeOffset EffectiveAt,
        DateTimeOffset? ExpiresAt);

    public sealed record ExchangeRateProviderFetchResult(
        IReadOnlyList<ExchangeRateProviderRate> Rates);

    public sealed record MoneyConversionResult(
        decimal SourceAmount,
        string SourceCurrencyCode,
        decimal ConvertedAmount,
        string TargetCurrencyCode,
        decimal Rate,
        DateTimeOffset EffectiveAt,
        DateTimeOffset? ExpiresAt,
        string? ProviderKey = null,
        string? Source = null);

    public interface IStoreCurrencyExchangeRateService
    {
        Task<IReadOnlyList<StoreCurrencyExchangeRateDto>> GetAsync(CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreCurrencyExchangeRateDto>> UpsertAsync(
            string targetCurrencyCode,
            UpsertStoreCurrencyExchangeRateRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreCurrencyExchangeRateDto>> DisableAsync(
            string targetCurrencyCode,
            CancellationToken cancellationToken = default);
    }

    public interface IExchangeRateProvider
    {
        string ProviderKey { get; }

        Task<StoreCurrencyExchangeRateProviderDto> GetStatusAsync(CancellationToken cancellationToken = default);

        Task<ServiceResponse<ExchangeRateProviderFetchResult>> FetchAsync(
            ExchangeRateProviderFetchRequest request,
            CancellationToken cancellationToken = default);
    }

    public interface IStoreCurrencyExchangeRateProviderService
    {
        Task<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>> GetProvidersAsync(
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreCurrencyExchangeRateProviderFetchResult>> FetchAsync(
            FetchStoreCurrencyExchangeRatesRequest request,
            CancellationToken cancellationToken = default);
    }

    public interface IMoneyConversionService
    {
        Task<ServiceResponse<MoneyConversionResult>> ConvertFromBaseAsync(
            Guid storeId,
            decimal amount,
            string targetCurrencyCode,
            CancellationToken cancellationToken = default);
    }
}
