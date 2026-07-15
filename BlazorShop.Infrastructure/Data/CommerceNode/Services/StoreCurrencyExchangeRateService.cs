namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;
    using System.Text.RegularExpressions;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class StoreCurrencyExchangeRateService :
        IStoreCurrencyExchangeRateService,
        IMoneyConversionService
    {
        private const string ManualProviderKey = "manual";

        private static readonly Regex CurrencyCodeRegex = new("^[A-Z]{3}$", RegexOptions.Compiled);

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly IAdminAuditService auditService;
        private readonly IMoneyRoundingService moneyRoundingService;

        public StoreCurrencyExchangeRateService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            IAdminAuditService auditService,
            IMoneyRoundingService moneyRoundingService)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.auditService = auditService;
            this.moneyRoundingService = moneyRoundingService;
        }

        public async Task<IReadOnlyList<StoreCurrencyExchangeRateDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            var store = await this.GetCurrentStoreAsync(cancellationToken);
            if (store is null)
            {
                return [];
            }

            var now = DateTimeOffset.UtcNow;
            var baseCurrencyCode = NormalizeCurrencyCode(store.DefaultCurrencyCode) ?? "USD";
            var rates = await this.context.StoreCurrencyExchangeRates
                .AsNoTracking()
                .Where(rate => rate.StoreId == store.Id && rate.BaseCurrencyCode == baseCurrencyCode)
                .OrderBy(rate => rate.TargetCurrencyCode)
                .ThenBy(rate => rate.ProviderKey)
                .ToArrayAsync(cancellationToken);

            return rates.Select(rate => Map(rate, now)).ToArray();
        }

        public async Task<ServiceResponse<StoreCurrencyExchangeRateDto>> UpsertAsync(
            string targetCurrencyCode,
            UpsertStoreCurrencyExchangeRateRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var normalizedTargetCode = NormalizeCurrencyCode(targetCurrencyCode);
            if (normalizedTargetCode is null)
            {
                return RateFailure("Target currency code must be a three-letter ISO-like code.", ServiceResponseType.ValidationError);
            }

            if (request.Rate <= 0m)
            {
                return RateFailure("Exchange rate must be greater than zero.", ServiceResponseType.ValidationError);
            }

            var now = DateTimeOffset.UtcNow;
            var effectiveAt = request.EffectiveAt ?? now;
            if (request.ExpiresAt is DateTimeOffset expiresAt && expiresAt <= effectiveAt)
            {
                return RateFailure("Exchange rate expiry must be after the effective time.", ServiceResponseType.ValidationError);
            }

            var store = await this.GetCurrentStoreAsync(cancellationToken);
            if (store is null)
            {
                return RateFailure("Current store could not be resolved.", ServiceResponseType.NotFound);
            }

            var baseCurrencyCode = NormalizeCurrencyCode(store.DefaultCurrencyCode) ?? "USD";
            if (string.Equals(normalizedTargetCode, baseCurrencyCode, StringComparison.Ordinal))
            {
                return RateFailure("Base currency conversion uses rate 1 and does not need a manual exchange-rate row.", ServiceResponseType.ValidationError);
            }

            if (!await this.IsEnabledCurrencyAsync(store.Id, normalizedTargetCode, cancellationToken))
            {
                return RateFailure($"Target currency '{normalizedTargetCode}' must be enabled for the store before a rate can be configured.", ServiceResponseType.ValidationError);
            }

            var rate = await this.context.StoreCurrencyExchangeRates
                .FirstOrDefaultAsync(
                    candidate => candidate.StoreId == store.Id
                        && candidate.BaseCurrencyCode == baseCurrencyCode
                        && candidate.TargetCurrencyCode == normalizedTargetCode
                        && candidate.ProviderKey == ManualProviderKey,
                    cancellationToken);

            if (rate is null)
            {
                rate = new StoreCurrencyExchangeRate
                {
                    StoreId = store.Id,
                    BaseCurrencyCode = baseCurrencyCode,
                    TargetCurrencyCode = normalizedTargetCode,
                    ProviderKey = ManualProviderKey,
                    IsManual = true,
                    CreatedAt = now,
                };
                this.context.StoreCurrencyExchangeRates.Add(rate);
            }

            rate.Rate = request.Rate;
            rate.Source = NormalizeNullable(request.Source);
            rate.EffectiveAt = effectiveAt;
            rate.ExpiresAt = request.ExpiresAt;
            rate.IsEnabled = request.IsEnabled;
            rate.UpdatedAt = now;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = "StoreCurrencyExchangeRate.Upserted",
                EntityType = "StoreCurrencyExchangeRate",
                EntityId = rate.Id.ToString(),
                Summary = $"Manual exchange rate '{baseCurrencyCode}->{normalizedTargetCode}' updated.",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    rate.StoreId,
                    rate.BaseCurrencyCode,
                    rate.TargetCurrencyCode,
                    rate.Rate,
                    rate.IsEnabled,
                    rate.EffectiveAt,
                    rate.ExpiresAt,
                }),
            });

            return RateSuccess(Map(rate, now), "Store currency exchange rate saved successfully.");
        }

        public async Task<ServiceResponse<StoreCurrencyExchangeRateDto>> DisableAsync(
            string targetCurrencyCode,
            CancellationToken cancellationToken = default)
        {
            var normalizedTargetCode = NormalizeCurrencyCode(targetCurrencyCode);
            if (normalizedTargetCode is null)
            {
                return RateFailure("Target currency code must be a three-letter ISO-like code.", ServiceResponseType.ValidationError);
            }

            var store = await this.GetCurrentStoreAsync(cancellationToken);
            if (store is null)
            {
                return RateFailure("Current store could not be resolved.", ServiceResponseType.NotFound);
            }

            var baseCurrencyCode = NormalizeCurrencyCode(store.DefaultCurrencyCode) ?? "USD";
            var rate = await this.context.StoreCurrencyExchangeRates
                .FirstOrDefaultAsync(
                    candidate => candidate.StoreId == store.Id
                        && candidate.BaseCurrencyCode == baseCurrencyCode
                        && candidate.TargetCurrencyCode == normalizedTargetCode
                        && candidate.ProviderKey == ManualProviderKey,
                    cancellationToken);
            if (rate is null)
            {
                return RateFailure($"Manual exchange rate '{baseCurrencyCode}->{normalizedTargetCode}' could not be found.", ServiceResponseType.NotFound);
            }

            var now = DateTimeOffset.UtcNow;
            rate.IsEnabled = false;
            rate.ExpiresAt ??= now;
            rate.UpdatedAt = now;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = "StoreCurrencyExchangeRate.Disabled",
                EntityType = "StoreCurrencyExchangeRate",
                EntityId = rate.Id.ToString(),
                Summary = $"Manual exchange rate '{baseCurrencyCode}->{normalizedTargetCode}' disabled.",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    rate.StoreId,
                    rate.BaseCurrencyCode,
                    rate.TargetCurrencyCode,
                }),
            });

            return RateSuccess(Map(rate, now), "Store currency exchange rate disabled successfully.");
        }

        public async Task<ServiceResponse<MoneyConversionResult>> ConvertFromBaseAsync(
            Guid storeId,
            decimal amount,
            string targetCurrencyCode,
            CancellationToken cancellationToken = default)
        {
            var normalizedTargetCode = NormalizeCurrencyCode(targetCurrencyCode);
            if (normalizedTargetCode is null)
            {
                return ConversionFailure("Target currency code must be a three-letter ISO-like code.", ServiceResponseType.ValidationError);
            }

            var store = await this.context.CommerceStores
                .AsNoTracking()
                .Where(candidate => candidate.Id == storeId)
                .Select(candidate => new StoreProjection(candidate.Id, candidate.DefaultCurrencyCode))
                .FirstOrDefaultAsync(cancellationToken);
            if (store is null)
            {
                return ConversionFailure("Current store could not be resolved.", ServiceResponseType.NotFound);
            }

            var baseCurrencyCode = NormalizeCurrencyCode(store.DefaultCurrencyCode) ?? "USD";
            if (string.Equals(normalizedTargetCode, baseCurrencyCode, StringComparison.Ordinal))
            {
                var roundedBaseAmount = this.moneyRoundingService.RoundOrderTotal(amount, baseCurrencyCode);
                return ConversionSuccess(
                    new MoneyConversionResult(
                        amount,
                        baseCurrencyCode,
                        roundedBaseAmount,
                        baseCurrencyCode,
                        1m,
                        DateTimeOffset.UtcNow,
                        null,
                        "base",
                        "same-currency"),
                    "Same-currency conversion resolved with rate 1.");
            }

            if (!await this.IsEnabledCurrencyAsync(store.Id, normalizedTargetCode, cancellationToken))
            {
                return ConversionFailure($"Target currency '{normalizedTargetCode}' is not enabled for this store.", ServiceResponseType.ValidationError);
            }

            var now = DateTimeOffset.UtcNow;
            var rate = await this.context.StoreCurrencyExchangeRates
                .AsNoTracking()
                .Where(candidate => candidate.StoreId == store.Id
                    && candidate.BaseCurrencyCode == baseCurrencyCode
                    && candidate.TargetCurrencyCode == normalizedTargetCode
                    && candidate.IsEnabled
                    && candidate.EffectiveAt <= now
                    && (candidate.ExpiresAt == null || candidate.ExpiresAt > now))
                .OrderByDescending(candidate => candidate.EffectiveAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (rate is null)
            {
                return ConversionFailure($"No active exchange rate is configured for '{baseCurrencyCode}->{normalizedTargetCode}'.", ServiceResponseType.Conflict);
            }

            var convertedAmount = this.moneyRoundingService.RoundOrderTotal(amount * rate.Rate, normalizedTargetCode);
            return ConversionSuccess(
                new MoneyConversionResult(
                    amount,
                    baseCurrencyCode,
                    convertedAmount,
                    normalizedTargetCode,
                    rate.Rate,
                    rate.EffectiveAt,
                    rate.ExpiresAt,
                    rate.ProviderKey,
                    rate.Source),
                "Currency conversion resolved successfully.");
        }

        private async Task<StoreProjection?> GetCurrentStoreAsync(CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return null;
            }

            return await this.context.CommerceStores
                .AsNoTracking()
                .Where(candidate => candidate.Id == storeResult.Payload)
                .Select(candidate => new StoreProjection(candidate.Id, candidate.DefaultCurrencyCode))
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<bool> IsEnabledCurrencyAsync(
            Guid storeId,
            string currencyCode,
            CancellationToken cancellationToken)
        {
            return await this.context.StoreCurrencies
                .AsNoTracking()
                .AnyAsync(
                    currency => currency.StoreId == storeId
                        && currency.CurrencyCode == currencyCode
                        && currency.IsEnabled,
                    cancellationToken);
        }

        private static StoreCurrencyExchangeRateDto Map(StoreCurrencyExchangeRate rate, DateTimeOffset now)
        {
            return new StoreCurrencyExchangeRateDto(
                rate.Id,
                rate.BaseCurrencyCode,
                rate.TargetCurrencyCode,
                rate.Rate,
                rate.ProviderKey,
                rate.Source,
                rate.EffectiveAt,
                rate.ExpiresAt,
                rate.IsManual,
                rate.IsEnabled,
                IsActive(rate, now),
                rate.CreatedAt,
                rate.UpdatedAt);
        }

        private static bool IsActive(StoreCurrencyExchangeRate rate, DateTimeOffset now)
        {
            return rate.IsEnabled
                && rate.EffectiveAt <= now
                && (rate.ExpiresAt is null || rate.ExpiresAt > now);
        }

        private static string? NormalizeCurrencyCode(string? value)
        {
            var normalized = NormalizeNullable(value)?.ToUpperInvariant();
            return normalized is not null && CurrencyCodeRegex.IsMatch(normalized) ? normalized : null;
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static ServiceResponse<StoreCurrencyExchangeRateDto> RateSuccess(
            StoreCurrencyExchangeRateDto payload,
            string message)
        {
            return new ServiceResponse<StoreCurrencyExchangeRateDto>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<StoreCurrencyExchangeRateDto> RateFailure(
            string message,
            ServiceResponseType responseType)
        {
            return new ServiceResponse<StoreCurrencyExchangeRateDto>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private static ServiceResponse<MoneyConversionResult> ConversionSuccess(
            MoneyConversionResult payload,
            string message)
        {
            return new ServiceResponse<MoneyConversionResult>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<MoneyConversionResult> ConversionFailure(
            string message,
            ServiceResponseType responseType)
        {
            return new ServiceResponse<MoneyConversionResult>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private sealed record StoreProjection(Guid Id, string DefaultCurrencyCode);
    }
}
