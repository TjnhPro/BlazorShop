namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;
    using System.Text.RegularExpressions;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class StoreCurrencyService : IStoreCurrencyService
    {
        private static readonly Regex CurrencyCodeRegex = new("^[A-Z]{3}$", RegexOptions.Compiled);
        private static readonly string[] SupportedRoundingModes =
        [
            "halfAwayFromZero",
            "halfToEven",
            "towardZero",
            "awayFromZero",
        ];

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly IAdminAuditService auditService;
        private readonly IStorefrontPublicConfigurationCache publicConfigurationCache;

        public StoreCurrencyService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            IAdminAuditService auditService,
            IStorefrontPublicConfigurationCache publicConfigurationCache)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.auditService = auditService;
            this.publicConfigurationCache = publicConfigurationCache;
        }

        public async Task<IReadOnlyList<StoreCurrencyDto>> GetAsync(CancellationToken cancellationToken = default)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return [];
            }

            var store = await this.context.CommerceStores
                .AsNoTracking()
                .Where(candidate => candidate.Id == storeResult.Payload)
                .Select(candidate => new { candidate.Id, candidate.DefaultCurrencyCode, candidate.DefaultCulture })
                .FirstOrDefaultAsync(cancellationToken);
            if (store is null)
            {
                return [];
            }

            await this.EnsureBaseCurrencyAsync(store.Id, store.DefaultCurrencyCode, store.DefaultCulture, cancellationToken);

            var currencies = await this.context.StoreCurrencies
                .AsNoTracking()
                .Where(currency => currency.StoreId == store.Id)
                .OrderByDescending(currency => currency.CurrencyCode == store.DefaultCurrencyCode)
                .ThenBy(currency => currency.DisplayOrder)
                .ThenBy(currency => currency.CurrencyCode)
                .ToArrayAsync(cancellationToken);

            return currencies
                .Select(currency => Map(currency, store.DefaultCurrencyCode))
                .ToArray();
        }

        public async Task<ServiceResponse<StoreCurrencyDto>> UpdateAsync(
            string currencyCode,
            UpdateStoreCurrencyRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var normalizedCode = NormalizeCurrencyCode(currencyCode);
            if (normalizedCode is null)
            {
                return Failure("Currency code must be a three-letter ISO-like code.", ServiceResponseType.ValidationError);
            }

            var validation = Validate(request);
            if (validation is not null)
            {
                return Failure(validation, ServiceResponseType.ValidationError);
            }

            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success)
            {
                return Failure("Current store could not be resolved.", ServiceResponseType.NotFound);
            }

            var store = await this.context.CommerceStores
                .Where(candidate => candidate.Id == storeResult.Payload)
                .Select(candidate => new { candidate.Id, candidate.DefaultCurrencyCode, candidate.DefaultCulture })
                .FirstOrDefaultAsync(cancellationToken);
            if (store is null)
            {
                return Failure("Current store could not be resolved.", ServiceResponseType.NotFound);
            }

            var baseCurrencyCode = NormalizeCurrencyCode(store.DefaultCurrencyCode) ?? "USD";
            var isBaseCurrency = string.Equals(normalizedCode, baseCurrencyCode, StringComparison.Ordinal);
            var now = DateTimeOffset.UtcNow;
            var currency = await this.context.StoreCurrencies
                .FirstOrDefaultAsync(
                    candidate => candidate.StoreId == store.Id && candidate.CurrencyCode == normalizedCode,
                    cancellationToken);

            if (currency is null)
            {
                currency = new StoreCurrency
                {
                    StoreId = store.Id,
                    CurrencyCode = normalizedCode,
                    CreatedAt = now,
                };
                this.context.StoreCurrencies.Add(currency);
            }

            if (request.IsDefaultDisplayCurrency)
            {
                var defaultDisplayCurrencies = await this.context.StoreCurrencies
                    .Where(candidate => candidate.StoreId == store.Id
                        && candidate.CurrencyCode != normalizedCode
                        && candidate.IsDefaultDisplayCurrency)
                    .ToArrayAsync(cancellationToken);
                foreach (var defaultDisplayCurrency in defaultDisplayCurrencies)
                {
                    defaultDisplayCurrency.IsDefaultDisplayCurrency = false;
                    defaultDisplayCurrency.UpdatedAt = now;
                }
            }

            currency.IsEnabled = isBaseCurrency || request.IsEnabled;
            currency.IsDefaultDisplayCurrency = request.IsDefaultDisplayCurrency || isBaseCurrency;
            currency.DisplayOrder = request.DisplayOrder;
            currency.CultureName = NormalizeNullable(request.CultureName) ?? (isBaseCurrency ? NormalizeNullable(store.DefaultCulture) : null);
            currency.Symbol = NormalizeNullable(request.Symbol);
            currency.DecimalDigits = request.DecimalDigits;
            currency.UnitPriceRoundingMode = request.UnitPriceRoundingMode;
            currency.UnitPriceRoundingIncrement = request.UnitPriceRoundingIncrement;
            currency.LineTotalRoundingMode = request.LineTotalRoundingMode;
            currency.LineTotalRoundingIncrement = request.LineTotalRoundingIncrement;
            currency.OrderTotalRoundingMode = request.OrderTotalRoundingMode;
            currency.OrderTotalRoundingIncrement = request.OrderTotalRoundingIncrement;
            currency.UpdatedAt = now;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = "StoreCurrency.Updated",
                EntityType = "StoreCurrency",
                EntityId = currency.Id.ToString(),
                Summary = $"Currency '{currency.CurrencyCode}' updated.",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    currency.StoreId,
                    currency.CurrencyCode,
                    currency.IsEnabled,
                    currency.IsDefaultDisplayCurrency,
                    IsBaseCurrency = isBaseCurrency,
                }),
            });
            await this.publicConfigurationCache.InvalidateAsync(store.Id, cancellationToken);

            return Success(Map(currency, baseCurrencyCode), "Store currency updated successfully.");
        }

        public async Task<IReadOnlyList<string>> ResolveSupportedCurrencyCodesAsync(
            Guid storeId,
            CancellationToken cancellationToken = default)
        {
            var store = await this.context.CommerceStores
                .AsNoTracking()
                .Where(candidate => candidate.Id == storeId)
                .Select(candidate => new { candidate.DefaultCurrencyCode })
                .FirstOrDefaultAsync(cancellationToken);
            var baseCurrencyCode = NormalizeCurrencyCode(store?.DefaultCurrencyCode) ?? "USD";

            var enabledCodes = await this.context.StoreCurrencies
                .AsNoTracking()
                .Where(currency => currency.StoreId == storeId && currency.IsEnabled)
                .OrderByDescending(currency => currency.CurrencyCode == baseCurrencyCode)
                .ThenBy(currency => currency.DisplayOrder)
                .ThenBy(currency => currency.CurrencyCode)
                .Select(currency => currency.CurrencyCode)
                .ToArrayAsync(cancellationToken);

            return enabledCodes
                .Prepend(baseCurrencyCode)
                .Select(code => NormalizeCurrencyCode(code) ?? baseCurrencyCode)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private async Task EnsureBaseCurrencyAsync(
            Guid storeId,
            string defaultCurrencyCode,
            string? defaultCulture,
            CancellationToken cancellationToken)
        {
            var baseCurrencyCode = NormalizeCurrencyCode(defaultCurrencyCode) ?? "USD";
            var exists = await this.context.StoreCurrencies
                .AnyAsync(
                    currency => currency.StoreId == storeId && currency.CurrencyCode == baseCurrencyCode,
                    cancellationToken);
            if (exists)
            {
                return;
            }

            this.context.StoreCurrencies.Add(new StoreCurrency
            {
                StoreId = storeId,
                CurrencyCode = baseCurrencyCode,
                IsEnabled = true,
                IsDefaultDisplayCurrency = true,
                DisplayOrder = 0,
                CultureName = NormalizeNullable(defaultCulture),
            });
            await this.context.SaveChangesAsync(cancellationToken);
        }

        private static StoreCurrencyDto Map(StoreCurrency currency, string defaultCurrencyCode)
        {
            var baseCurrencyCode = NormalizeCurrencyCode(defaultCurrencyCode) ?? "USD";
            return new StoreCurrencyDto(
                currency.Id,
                currency.CurrencyCode,
                currency.IsEnabled,
                string.Equals(currency.CurrencyCode, baseCurrencyCode, StringComparison.Ordinal),
                currency.IsDefaultDisplayCurrency,
                currency.DisplayOrder,
                currency.CultureName,
                currency.Symbol,
                currency.DecimalDigits,
                currency.UnitPriceRoundingMode,
                currency.UnitPriceRoundingIncrement,
                currency.LineTotalRoundingMode,
                currency.LineTotalRoundingIncrement,
                currency.OrderTotalRoundingMode,
                currency.OrderTotalRoundingIncrement,
                currency.CreatedAt,
                currency.UpdatedAt);
        }

        private static string? Validate(UpdateStoreCurrencyRequest request)
        {
            if (!SupportedRoundingModes.Contains(request.UnitPriceRoundingMode, StringComparer.Ordinal)
                || !SupportedRoundingModes.Contains(request.LineTotalRoundingMode, StringComparer.Ordinal)
                || !SupportedRoundingModes.Contains(request.OrderTotalRoundingMode, StringComparer.Ordinal))
            {
                return "Rounding mode is not supported.";
            }

            if (request.DecimalDigits is < 0 or > 4)
            {
                return "Decimal digits must be between 0 and 4.";
            }

            if (request.UnitPriceRoundingIncrement <= 0m
                || request.LineTotalRoundingIncrement <= 0m
                || request.OrderTotalRoundingIncrement <= 0m)
            {
                return "Rounding increment must be greater than zero.";
            }

            return null;
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

        private static ServiceResponse<StoreCurrencyDto> Success(StoreCurrencyDto payload, string message)
        {
            return new ServiceResponse<StoreCurrencyDto>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<StoreCurrencyDto> Failure(string message, ServiceResponseType responseType)
        {
            return new ServiceResponse<StoreCurrencyDto>(false, message)
            {
                ResponseType = responseType,
            };
        }
    }
}
