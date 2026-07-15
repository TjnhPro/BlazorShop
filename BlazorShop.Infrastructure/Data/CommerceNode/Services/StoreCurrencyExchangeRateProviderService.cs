namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;
    using System.Text.RegularExpressions;

    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Settings;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    public sealed class StoreCurrencyExchangeRateProviderService : IStoreCurrencyExchangeRateProviderService
    {
        private const string ManualProviderKey = "manual";
        private const string PayloadSchemaVersion = "v1";
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private static readonly Regex CurrencyCodeRegex = new("^[A-Z]{3}$", RegexOptions.Compiled);

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly IAdminAuditService auditService;
        private readonly ICommerceTaskService taskService;
        private readonly IStorefrontPublicConfigurationCache publicConfigurationCache;
        private readonly IReadOnlyDictionary<string, IExchangeRateProvider> providers;

        public StoreCurrencyExchangeRateProviderService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            IAdminAuditService auditService,
            ICommerceTaskService taskService,
            IStorefrontPublicConfigurationCache publicConfigurationCache,
            IEnumerable<IExchangeRateProvider> providers)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.auditService = auditService;
            this.taskService = taskService;
            this.publicConfigurationCache = publicConfigurationCache;
            this.providers = providers.ToDictionary(
                provider => provider.ProviderKey,
                StringComparer.OrdinalIgnoreCase);
        }

        public async Task<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>> GetProvidersAsync(
            CancellationToken cancellationToken = default)
        {
            var statuses = new List<StoreCurrencyExchangeRateProviderDto>();
            foreach (var provider in this.providers.Values.OrderBy(provider => provider.ProviderKey, StringComparer.Ordinal))
            {
                statuses.Add(await provider.GetStatusAsync(cancellationToken));
            }

            return statuses;
        }

        public async Task<ServiceResponse<StoreCurrencyExchangeRateProviderFetchResult>> FetchAsync(
            FetchStoreCurrencyExchangeRatesRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var providerKey = NormalizeProviderKey(request.ProviderKey);
            if (providerKey is null)
            {
                return Failed("Provider key is required.", ServiceResponseType.ValidationError);
            }

            if (string.Equals(providerKey, ManualProviderKey, StringComparison.Ordinal))
            {
                return Failed("Manual rates cannot be fetched. Use the manual exchange-rate upsert endpoint.", ServiceResponseType.ValidationError);
            }

            if (!this.providers.TryGetValue(providerKey, out var provider))
            {
                return Failed($"Exchange-rate provider '{providerKey}' is not configured.", ServiceResponseType.NotFound);
            }

            var store = await this.GetCurrentStoreAsync(cancellationToken);
            if (store is null)
            {
                return Failed("Current store could not be resolved.", ServiceResponseType.NotFound);
            }

            return await this.FetchForStoreAsync(store.Id, request, cancellationToken);
        }

        public async Task<ServiceResponse<StoreCurrencyExchangeRateProviderFetchResult>> FetchForStoreAsync(
            Guid storeId,
            FetchStoreCurrencyExchangeRatesRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (storeId == Guid.Empty)
            {
                return Failed("Store id is required.", ServiceResponseType.ValidationError);
            }

            var providerKey = NormalizeProviderKey(request.ProviderKey);
            if (providerKey is null)
            {
                return Failed("Provider key is required.", ServiceResponseType.ValidationError);
            }

            if (string.Equals(providerKey, ManualProviderKey, StringComparison.Ordinal))
            {
                return Failed("Manual rates cannot be fetched. Use the manual exchange-rate upsert endpoint.", ServiceResponseType.ValidationError);
            }

            if (!this.providers.TryGetValue(providerKey, out var provider))
            {
                return Failed($"Exchange-rate provider '{providerKey}' is not configured.", ServiceResponseType.NotFound);
            }

            var store = await this.GetStoreAsync(storeId, cancellationToken);
            if (store is null)
            {
                return Failed("Store could not be resolved.", ServiceResponseType.NotFound);
            }

            var baseCurrencyCode = NormalizeCurrencyCode(store.DefaultCurrencyCode) ?? "USD";
            var targetCurrencyCodes = await this.ResolveTargetCurrencyCodesAsync(
                store.Id,
                baseCurrencyCode,
                request.TargetCurrencyCodes,
                cancellationToken);
            if (!targetCurrencyCodes.Success)
            {
                return Failed(targetCurrencyCodes.Message, targetCurrencyCodes.ResponseType);
            }

            var fetch = await provider.FetchAsync(
                new ExchangeRateProviderFetchRequest(store.Id, baseCurrencyCode, targetCurrencyCodes.TargetCurrencyCodes),
                cancellationToken);
            if (!fetch.Success || fetch.Payload is null)
            {
                return Failed(fetch.Message ?? "Exchange-rate provider did not return rates.", fetch.ResponseType);
            }

            var now = DateTimeOffset.UtcNow;
            var providerRates = fetch.Payload.Rates
                .Where(rate => string.Equals(rate.BaseCurrencyCode, baseCurrencyCode, StringComparison.Ordinal))
                .ToDictionary(rate => rate.TargetCurrencyCode, StringComparer.Ordinal);

            var missingCurrencyCodes = targetCurrencyCodes.TargetCurrencyCodes
                .Where(target => !providerRates.ContainsKey(target))
                .ToArray();
            if (missingCurrencyCodes.Length > 0)
            {
                return Failed(
                    $"Exchange-rate provider '{providerKey}' did not return rates for: {string.Join(", ", missingCurrencyCodes)}.",
                    ServiceResponseType.Conflict);
            }

            var updatedRates = new List<StoreCurrencyExchangeRateDto>();
            foreach (var targetCurrencyCode in targetCurrencyCodes.TargetCurrencyCodes)
            {
                var providerRate = providerRates[targetCurrencyCode];
                var rate = await this.context.StoreCurrencyExchangeRates
                    .FirstOrDefaultAsync(
                        candidate => candidate.StoreId == store.Id
                            && candidate.BaseCurrencyCode == baseCurrencyCode
                            && candidate.TargetCurrencyCode == targetCurrencyCode
                            && candidate.ProviderKey == providerKey,
                        cancellationToken);

                if (rate is null)
                {
                    rate = new StoreCurrencyExchangeRate
                    {
                        StoreId = store.Id,
                        BaseCurrencyCode = baseCurrencyCode,
                        TargetCurrencyCode = targetCurrencyCode,
                        ProviderKey = providerKey,
                        IsManual = false,
                        CreatedAt = now,
                    };
                    this.context.StoreCurrencyExchangeRates.Add(rate);
                }

                rate.Rate = providerRate.Rate;
                rate.Source = NormalizeNullable(providerRate.Source);
                rate.EffectiveAt = providerRate.EffectiveAt;
                rate.ExpiresAt = providerRate.ExpiresAt;
                rate.IsEnabled = request.IsEnabled;
                rate.UpdatedAt = now;

                updatedRates.Add(Map(rate, now));
            }

            await this.context.SaveChangesAsync(cancellationToken);
            await this.publicConfigurationCache.InvalidateAsync(store.Id, cancellationToken);
            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = "StoreCurrencyExchangeRate.ProviderFetched",
                EntityType = "StoreCurrencyExchangeRate",
                Summary = $"Exchange rates fetched from provider '{providerKey}'.",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    store.Id,
                    BaseCurrencyCode = baseCurrencyCode,
                    ProviderKey = providerKey,
                    TargetCurrencyCodes = targetCurrencyCodes.TargetCurrencyCodes,
                    request.IsEnabled,
                }),
            });

            return Succeeded(
                new StoreCurrencyExchangeRateProviderFetchResult(providerKey, updatedRates.Count, updatedRates),
                "Exchange rates fetched successfully.");
        }

        public async Task<ServiceResponse<CommerceTaskSummary>> QueueUpdateAsync(
            QueueStoreCurrencyExchangeRateUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var providerKey = NormalizeProviderKey(request.ProviderKey);
            if (providerKey is null)
            {
                return TaskFailure("Provider key is required.", ServiceResponseType.ValidationError);
            }

            if (string.Equals(providerKey, ManualProviderKey, StringComparison.Ordinal))
            {
                return TaskFailure("Manual rates cannot be fetched. Use the manual exchange-rate upsert endpoint.", ServiceResponseType.ValidationError);
            }

            if (!this.providers.ContainsKey(providerKey))
            {
                return TaskFailure($"Exchange-rate provider '{providerKey}' is not configured.", ServiceResponseType.NotFound);
            }

            var store = await this.GetCurrentStoreAsync(cancellationToken);
            if (store is null)
            {
                return TaskFailure("Current store could not be resolved.", ServiceResponseType.NotFound);
            }

            var baseCurrencyCode = NormalizeCurrencyCode(store.DefaultCurrencyCode) ?? "USD";
            var targetCurrencyCodes = await this.ResolveTargetCurrencyCodesAsync(
                store.Id,
                baseCurrencyCode,
                request.TargetCurrencyCodes,
                cancellationToken);
            if (!targetCurrencyCodes.Success)
            {
                return TaskFailure(targetCurrencyCodes.Message, targetCurrencyCodes.ResponseType);
            }

            var payload = new StoreCurrencyExchangeRateUpdateTaskPayload(
                PayloadSchemaVersion,
                store.Id,
                providerKey,
                targetCurrencyCodes.TargetCurrencyCodes,
                request.IsEnabled,
                DateTimeOffset.UtcNow);

            var enqueue = await this.taskService.EnqueueAsync(
                new EnqueueCommerceTaskRequest(
                    CurrencyExchangeRateTaskTypes.Update,
                    NormalizeNullable(request.IdempotencyKey),
                    PayloadSchemaVersion,
                    JsonSerializer.Serialize(payload, SerializerOptions),
                    $"currency-exchange-rate:{store.Id:D}:{providerKey}",
                    Math.Clamp(request.MaxAttempts, 1, 10),
                    CorrelationId: NormalizeNullable(request.CorrelationId)),
                cancellationToken);
            if (!enqueue.Success || enqueue.Payload is null)
            {
                return TaskFailure(
                    enqueue.Message ?? "Exchange-rate update task could not be queued.",
                    ServiceResponseType.Failure);
            }

            return TaskSuccess(enqueue.Payload, enqueue.AlreadyExists
                ? "Exchange-rate update task already exists."
                : "Exchange-rate update task queued.");
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

        private async Task<StoreProjection?> GetStoreAsync(Guid storeId, CancellationToken cancellationToken)
        {
            return await this.context.CommerceStores
                .AsNoTracking()
                .Where(candidate => candidate.Id == storeId)
                .Select(candidate => new StoreProjection(candidate.Id, candidate.DefaultCurrencyCode))
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<TargetCurrencyResolution> ResolveTargetCurrencyCodesAsync(
            Guid storeId,
            string baseCurrencyCode,
            IReadOnlyList<string>? requestedCurrencyCodes,
            CancellationToken cancellationToken)
        {
            var enabledCurrencyCodes = await this.context.StoreCurrencies
                .AsNoTracking()
                .Where(currency => currency.StoreId == storeId && currency.IsEnabled)
                .Select(currency => currency.CurrencyCode)
                .ToArrayAsync(cancellationToken);

            var enabledSet = enabledCurrencyCodes
                .Select(NormalizeCurrencyCode)
                .Where(code => code is not null)
                .Select(code => code!)
                .ToHashSet(StringComparer.Ordinal);

            var targets = requestedCurrencyCodes is { Count: > 0 }
                ? requestedCurrencyCodes.Select(NormalizeCurrencyCode).ToArray()
                : enabledSet.Where(code => !string.Equals(code, baseCurrencyCode, StringComparison.Ordinal)).ToArray();

            if (targets.Any(code => code is null))
            {
                return TargetCurrencyResolution.Failed(
                    "Target currency codes must be three-letter ISO-like codes.",
                    ServiceResponseType.ValidationError);
            }

            var normalizedTargets = targets
                .Select(code => code!)
                .Where(code => !string.Equals(code, baseCurrencyCode, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToArray();
            if (normalizedTargets.Length == 0)
            {
                return TargetCurrencyResolution.Failed(
                    "At least one non-base enabled target currency is required.",
                    ServiceResponseType.ValidationError);
            }

            var disabledTargets = normalizedTargets.Where(code => !enabledSet.Contains(code)).ToArray();
            if (disabledTargets.Length > 0)
            {
                return TargetCurrencyResolution.Failed(
                    $"Target currencies must be enabled for the store before rates can be fetched: {string.Join(", ", disabledTargets)}.",
                    ServiceResponseType.ValidationError);
            }

            return TargetCurrencyResolution.Succeeded(normalizedTargets);
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
                rate.IsEnabled && rate.EffectiveAt <= now && (rate.ExpiresAt is null || rate.ExpiresAt > now),
                rate.CreatedAt,
                rate.UpdatedAt);
        }

        private static ServiceResponse<StoreCurrencyExchangeRateProviderFetchResult> Succeeded(
            StoreCurrencyExchangeRateProviderFetchResult payload,
            string message)
        {
            return new ServiceResponse<StoreCurrencyExchangeRateProviderFetchResult>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<StoreCurrencyExchangeRateProviderFetchResult> Failed(
            string message,
            ServiceResponseType responseType)
        {
            return new ServiceResponse<StoreCurrencyExchangeRateProviderFetchResult>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private static ServiceResponse<CommerceTaskSummary> TaskSuccess(
            CommerceTaskSummary payload,
            string message)
        {
            return new ServiceResponse<CommerceTaskSummary>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<CommerceTaskSummary> TaskFailure(
            string message,
            ServiceResponseType responseType)
        {
            return new ServiceResponse<CommerceTaskSummary>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private static string? NormalizeProviderKey(string? value)
        {
            return NormalizeNullable(value)?.ToLowerInvariant();
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

        private sealed record StoreProjection(Guid Id, string DefaultCurrencyCode);

        private sealed record TargetCurrencyResolution(
            bool Success,
            string Message,
            ServiceResponseType ResponseType,
            IReadOnlyList<string> TargetCurrencyCodes)
        {
            public static TargetCurrencyResolution Succeeded(IReadOnlyList<string> targetCurrencyCodes)
            {
                return new TargetCurrencyResolution(true, "Target currencies resolved.", ServiceResponseType.Success, targetCurrencyCodes);
            }

            public static TargetCurrencyResolution Failed(string message, ServiceResponseType responseType)
            {
                return new TargetCurrencyResolution(false, message, responseType, []);
            }
        }
    }

    public sealed class ManualExchangeRateProvider : IExchangeRateProvider
    {
        public string ProviderKey => "manual";

        public Task<StoreCurrencyExchangeRateProviderDto> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StoreCurrencyExchangeRateProviderDto(
                this.ProviderKey,
                Enabled: true,
                SecretsConfigured: false,
                Status: "Manual rates are managed through the upsert endpoint.",
                Source: "commerce-node"));
        }

        public Task<ServiceResponse<ExchangeRateProviderFetchResult>> FetchAsync(
            ExchangeRateProviderFetchRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ServiceResponse<ExchangeRateProviderFetchResult>(
                false,
                "Manual rates cannot be fetched. Use the manual exchange-rate upsert endpoint.")
            {
                ResponseType = ServiceResponseType.ValidationError,
            });
        }
    }

    public sealed class ConfigurationExchangeRateProvider : IExchangeRateProvider
    {
        private static readonly Regex CurrencyCodeRegex = new("^[A-Z]{3}$", RegexOptions.Compiled);

        private readonly IOptionsMonitor<ConfigurationExchangeRateProviderOptions> options;

        public ConfigurationExchangeRateProvider(IOptionsMonitor<ConfigurationExchangeRateProviderOptions> options)
        {
            this.options = options;
        }

        public string ProviderKey => "configuration";

        public Task<StoreCurrencyExchangeRateProviderDto> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            var current = this.options.CurrentValue;
            return Task.FromResult(new StoreCurrencyExchangeRateProviderDto(
                this.ProviderKey,
                current.Enabled,
                SecretsConfigured: false,
                current.Enabled ? "Configuration provider is enabled." : "Configuration provider is disabled.",
                NormalizeNullable(current.Source) ?? "configuration"));
        }

        public Task<ServiceResponse<ExchangeRateProviderFetchResult>> FetchAsync(
            ExchangeRateProviderFetchRequest request,
            CancellationToken cancellationToken = default)
        {
            var current = this.options.CurrentValue;
            if (!current.Enabled)
            {
                return Task.FromResult(Failed("Configuration exchange-rate provider is disabled.", ServiceResponseType.Conflict));
            }

            var baseCurrencyCode = NormalizeCurrencyCode(request.BaseCurrencyCode);
            if (baseCurrencyCode is null)
            {
                return Task.FromResult(Failed("Base currency code is invalid.", ServiceResponseType.ValidationError));
            }

            var now = DateTimeOffset.UtcNow;
            var maxAge = TimeSpan.FromHours(Math.Max(1, current.MaxRateAgeHours));
            var configuredRates = current.Rates
                .Select(rate => ToProviderRate(rate, current.Source, now))
                .Where(rate => rate is not null)
                .Select(rate => rate!)
                .Where(rate => string.Equals(rate.BaseCurrencyCode, baseCurrencyCode, StringComparison.Ordinal))
                .ToDictionary(rate => rate.TargetCurrencyCode, StringComparer.Ordinal);

            var results = new List<ExchangeRateProviderRate>();
            foreach (var targetCurrencyCode in request.TargetCurrencyCodes)
            {
                var normalizedTarget = NormalizeCurrencyCode(targetCurrencyCode);
                if (normalizedTarget is null)
                {
                    return Task.FromResult(Failed("Target currency code is invalid.", ServiceResponseType.ValidationError));
                }

                if (!configuredRates.TryGetValue(normalizedTarget, out var rate))
                {
                    return Task.FromResult(Failed(
                        $"Configuration provider did not define a rate for '{baseCurrencyCode}->{normalizedTarget}'.",
                        ServiceResponseType.Conflict));
                }

                if (rate.EffectiveAt < now.Subtract(maxAge))
                {
                    return Task.FromResult(Failed(
                        $"Configuration provider rate '{baseCurrencyCode}->{normalizedTarget}' is stale.",
                        ServiceResponseType.Conflict));
                }

                if (rate.ExpiresAt is DateTimeOffset expiresAt && expiresAt <= now)
                {
                    return Task.FromResult(Failed(
                        $"Configuration provider rate '{baseCurrencyCode}->{normalizedTarget}' has expired.",
                        ServiceResponseType.Conflict));
                }

                results.Add(rate);
            }

            return Task.FromResult(new ServiceResponse<ExchangeRateProviderFetchResult>(true, "Configuration rates fetched.")
            {
                Payload = new ExchangeRateProviderFetchResult(results),
                ResponseType = ServiceResponseType.Success,
            });
        }

        private static ExchangeRateProviderRate? ToProviderRate(
            ConfigurationExchangeRateEntry rate,
            string? defaultSource,
            DateTimeOffset now)
        {
            var baseCurrencyCode = NormalizeCurrencyCode(rate.BaseCurrencyCode);
            var targetCurrencyCode = NormalizeCurrencyCode(rate.TargetCurrencyCode);
            if (baseCurrencyCode is null || targetCurrencyCode is null || rate.Rate <= 0m)
            {
                return null;
            }

            var effectiveAt = rate.EffectiveAt ?? now;
            if (rate.ExpiresAt is DateTimeOffset expiresAt && expiresAt <= effectiveAt)
            {
                return null;
            }

            return new ExchangeRateProviderRate(
                baseCurrencyCode,
                targetCurrencyCode,
                rate.Rate,
                NormalizeNullable(rate.Source) ?? NormalizeNullable(defaultSource) ?? "configuration",
                effectiveAt,
                rate.ExpiresAt);
        }

        private static ServiceResponse<ExchangeRateProviderFetchResult> Failed(
            string message,
            ServiceResponseType responseType)
        {
            return new ServiceResponse<ExchangeRateProviderFetchResult>(false, message)
            {
                ResponseType = responseType,
            };
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
    }

    public sealed class ConfigurationExchangeRateProviderOptions
    {
        public bool Enabled { get; set; }

        public string? Source { get; set; }

        public int MaxRateAgeHours { get; set; } = 24;

        public List<ConfigurationExchangeRateEntry> Rates { get; set; } = [];
    }

    public sealed class ConfigurationExchangeRateEntry
    {
        public string BaseCurrencyCode { get; set; } = string.Empty;

        public string TargetCurrencyCode { get; set; } = string.Empty;

        public decimal Rate { get; set; }

        public string? Source { get; set; }

        public DateTimeOffset? EffectiveAt { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }
    }
}
