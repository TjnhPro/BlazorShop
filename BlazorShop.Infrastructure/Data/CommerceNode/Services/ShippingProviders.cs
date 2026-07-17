namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.DTOs;

    public sealed class InternalFreeStandardShippingProvider : IShippingProvider
    {
        public const string SystemName = "free_standard";
        public const string MethodCode = "standard";
        private readonly IStoreShippingSettingsService? settingsService;

        public InternalFreeStandardShippingProvider(IStoreShippingSettingsService? settingsService = null)
        {
            this.settingsService = settingsService;
        }

        public string ProviderSystemName => SystemName;

        public async Task<ShippingProviderResult> GetOptionsAsync(
            ShippingOptionsRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!ShippingCalculator.RequiresShipping(request.PackageLines))
            {
                return ShippingProviderResult.Empty;
            }

            var settings = await this.ResolveSettingsAsync(request.StoreId, cancellationToken);
            if (!SupportsDestination(settings, request.Address))
            {
                return new ShippingProviderResult(
                    Options: [],
                    Warnings: [],
                    Errors: ["Shipping is not available for the selected country."]);
            }

            var currencyCode = NormalizeCurrency(request.CurrencyCode);
            var option = new ShippingOptionDto(
                Key: SystemName,
                ProviderSystemName: SystemName,
                MethodCode: MethodCode,
                DisplayName: "Free standard",
                Description: "Standard shipping for MVP stores.",
                Rate: 0m,
                CurrencyCode: currencyCode,
                DeliveryEstimateText: "Standard delivery",
                Warnings: [],
                Errors: [],
                RuleMatch: "internal.free_standard");

            return new ShippingProviderResult([option], [], []);
        }

        private async Task<StoreShippingRuntimeSettings> ResolveSettingsAsync(Guid storeId, CancellationToken cancellationToken)
        {
            return this.settingsService is null
                ? new StoreShippingRuntimeSettings(new StoreShippingOriginDto(null, null, null, null, null, null, null, null), [], null, null, StoreShippingSurchargePolicies.Sum, null)
                : await this.settingsService.ResolveAsync(storeId, cancellationToken);
        }

        private static string NormalizeCurrency(string? value)
        {
            var normalized = value?.Trim().ToUpperInvariant();
            return string.IsNullOrWhiteSpace(normalized) ? "USD" : normalized;
        }

        internal static bool SupportsDestination(StoreShippingRuntimeSettings settings, ShippingAddressSnapshot? address)
        {
            if (settings.EnabledCountryCodes.Count == 0)
            {
                return true;
            }

            var destinationCountry = address?.CountryCode?.Trim().ToUpperInvariant();
            return !string.IsNullOrWhiteSpace(destinationCountry)
                && settings.EnabledCountryCodes.Contains(destinationCountry, StringComparer.OrdinalIgnoreCase);
        }
    }

    public sealed class InternalFlatRateShippingProvider : IShippingProvider
    {
        public const string SystemName = "flat_rate";
        public const string MethodCode = "flat_rate";

        private readonly IStoreShippingSettingsService settingsService;

        public InternalFlatRateShippingProvider(IStoreShippingSettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        public string ProviderSystemName => SystemName;

        public async Task<ShippingProviderResult> GetOptionsAsync(
            ShippingOptionsRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (!ShippingCalculator.RequiresShipping(request.PackageLines))
            {
                return ShippingProviderResult.Empty;
            }

            var settings = await this.settingsService.ResolveAsync(request.StoreId, cancellationToken);
            if (!InternalFreeStandardShippingProvider.SupportsDestination(settings, request.Address))
            {
                return new ShippingProviderResult([], [], ["Shipping is not available for the selected country."]);
            }

            if (!settings.DefaultFlatRate.HasValue)
            {
                return ShippingProviderResult.Empty;
            }

            var rate = settings.FreeShippingThreshold.HasValue && request.Subtotal >= settings.FreeShippingThreshold.Value
                ? 0m
                : settings.DefaultFlatRate.Value;
            var estimate = settings.DefaultDeliveryEstimateText ?? "Standard delivery";
            var currencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "USD" : request.CurrencyCode.Trim().ToUpperInvariant();

            var option = new ShippingOptionDto(
                Key: SystemName,
                ProviderSystemName: SystemName,
                MethodCode: MethodCode,
                DisplayName: "Flat rate",
                Description: "Store configured flat-rate shipping.",
                Rate: rate,
                CurrencyCode: currencyCode,
                DeliveryEstimateText: estimate,
                Warnings: [],
                Errors: [],
                RuleMatch: rate == 0m ? "store.free_shipping_threshold" : "store.flat_rate");

            return new ShippingProviderResult([option], [], []);
        }
    }

    public sealed class ShippingProviderResolver : IShippingProviderResolver
    {
        private readonly IReadOnlyDictionary<string, IShippingProvider> providers;

        public ShippingProviderResolver(IEnumerable<IShippingProvider> providers)
        {
            this.providers = providers.ToDictionary(
                provider => provider.ProviderSystemName,
                StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<string> ListProviderSystemNames()
        {
            return this.providers.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public ServiceResponse<IShippingProvider> Resolve(string providerSystemName)
        {
            var normalized = providerSystemName?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return Failure("Shipping provider is required.");
            }

            if (this.providers.TryGetValue(normalized, out var provider))
            {
                return new ServiceResponse<IShippingProvider>(true, "Shipping provider loaded.")
                {
                    Payload = provider,
                    ResponseType = ServiceResponseType.Success,
                };
            }

            return Failure("Shipping provider is not supported.");
        }

        private static ServiceResponse<IShippingProvider> Failure(string message)
        {
            return new ServiceResponse<IShippingProvider>(false, message)
            {
                ResponseType = ServiceResponseType.ValidationError,
            };
        }
    }

    public sealed class ShippingCalculator : IShippingCalculator
    {
        private readonly IEnumerable<IShippingProvider> providers;

        public ShippingCalculator(IEnumerable<IShippingProvider> providers)
        {
            this.providers = providers;
        }

        public async Task<ServiceResponse<ShippingCalculationResult>> GetOptionsAsync(
            ShippingOptionsRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var shippingRequired = RequiresShipping(request.PackageLines);
            if (!shippingRequired)
            {
                return Success(new ShippingCalculationResult(false, [], [], []));
            }

            var options = new List<ShippingOptionDto>();
            var warnings = new List<string>();
            var errors = new List<string>();

            foreach (var provider in this.providers)
            {
                var result = await provider.GetOptionsAsync(request, cancellationToken);
                options.AddRange(result.Options);
                warnings.AddRange(result.Warnings);
                errors.AddRange(result.Errors);
            }

            return Success(new ShippingCalculationResult(true, options, warnings, errors));
        }

        internal static bool RequiresShipping(IReadOnlyList<ShippingPackageLine> packageLines)
        {
            return packageLines.Any(line => line.Quantity > 0 && line.ShippingRequired);
        }

        private static ServiceResponse<ShippingCalculationResult> Success(ShippingCalculationResult payload)
        {
            return new ServiceResponse<ShippingCalculationResult>(true, "Shipping options calculated.")
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }
    }
}
