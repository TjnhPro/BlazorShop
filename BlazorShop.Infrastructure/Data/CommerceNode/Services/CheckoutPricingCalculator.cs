namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Shipping;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class CheckoutPricingCalculator
    {
        private const string DefaultCurrencyCode = "USD";

        private readonly CommerceNodeDbContext context;
        private readonly IMoneyRoundingService moneyRoundingService;
        private readonly IMoneyConversionService moneyConversionService;
        private readonly IShippingCalculator shippingCalculator;
        private readonly IShippingTaxCalculator shippingTaxCalculator;

        public CheckoutPricingCalculator(
            CommerceNodeDbContext context,
            IMoneyRoundingService moneyRoundingService,
            IMoneyConversionService moneyConversionService,
            IShippingCalculator shippingCalculator,
            IShippingTaxCalculator shippingTaxCalculator)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(moneyRoundingService);
            ArgumentNullException.ThrowIfNull(moneyConversionService);
            ArgumentNullException.ThrowIfNull(shippingCalculator);
            ArgumentNullException.ThrowIfNull(shippingTaxCalculator);

            this.context = context;
            this.moneyRoundingService = moneyRoundingService;
            this.moneyConversionService = moneyConversionService;
            this.shippingCalculator = shippingCalculator;
            this.shippingTaxCalculator = shippingTaxCalculator;
        }

        internal async Task<CheckoutShippingCalculationResult> ResolveShippingOptionsAsync(
            CheckoutSession session,
            StorefrontCartSessionDto cart,
            string? selectedKey,
            CancellationToken cancellationToken)
        {
            var currencyCode = NormalizeCurrency(session.CurrencyCode)
                ?? NormalizeCurrency(cart.CurrencyCode)
                ?? DefaultCurrencyCode;
            var subtotal = this.moneyRoundingService.RoundOrderTotal(
                cart.Lines.Sum(line => line.LineTotal ?? line.LineSubtotal ?? 0m),
                currencyCode);
            var rateCurrency = this.ResolveShippingRateCurrency(cart.Lines, currencyCode, subtotal);
            var packageLines = await this.BuildShippingPackageLinesAsync(session.StoreId, cart.Lines, cancellationToken);
            var result = await this.CalculateShippingAsync(
                session.StoreId,
                cart.Id,
                cart.PublicId,
                CreateShippingAddress(session),
                currencyCode,
                subtotal,
                rateCurrency.BaseCurrencyCode,
                rateCurrency.BaseSubtotal,
                packageLines,
                cancellationToken);

            return result with
            {
                Options = result.Options
                    .Select(option => option with
                    {
                        Selected = string.Equals(option.Key, selectedKey, StringComparison.OrdinalIgnoreCase),
                    })
                    .ToArray(),
            };
        }

        internal async Task<CheckoutShippingCalculationResult> CalculateShippingAsync(
            Guid storeId,
            Guid? cartId,
            Guid? cartPublicId,
            StorefrontCheckoutShippingAddressDto? address,
            string checkoutCurrencyCode,
            decimal checkoutSubtotal,
            string rateCurrencyCode,
            decimal rateSubtotal,
            IReadOnlyList<ShippingPackageLine> packageLines,
            CancellationToken cancellationToken)
        {
            var normalizedCheckoutCurrency = NormalizeCurrency(checkoutCurrencyCode) ?? DefaultCurrencyCode;
            var normalizedRateCurrency = NormalizeCurrency(rateCurrencyCode) ?? normalizedCheckoutCurrency;
            var request = new ShippingOptionsRequest(
                storeId,
                cartId,
                cartPublicId,
                ToShippingAddressSnapshot(address),
                normalizedRateCurrency,
                rateSubtotal,
                packageLines);
            var result = await this.shippingCalculator.GetOptionsAsync(request, cancellationToken);
            if (!result.Success || result.Payload is null)
            {
                return new CheckoutShippingCalculationResult(
                    ShippingRequired: packageLines.Any(line => line.Quantity > 0 && line.ShippingRequired),
                    Options: [],
                    Warnings: [],
                    Errors: [result.Message ?? "Shipping options could not be calculated."]);
            }

            var options = new List<StorefrontCheckoutShippingOption>();
            var errors = new List<string>(result.Payload.Errors);
            foreach (var option in result.Payload.Options)
            {
                var mapped = await this.MapShippingOptionAsync(
                    storeId,
                    option,
                    normalizedCheckoutCurrency,
                    normalizedRateCurrency,
                    cancellationToken);
                if (!mapped.Success || mapped.Payload is null)
                {
                    errors.Add(mapped.Message ?? "Shipping rate currency conversion is not available.");
                    continue;
                }

                options.Add(mapped.Payload);
            }

            return new CheckoutShippingCalculationResult(
                result.Payload.ShippingRequired,
                options,
                result.Payload.Warnings,
                errors);
        }

        internal ShippingRateCurrency ResolveShippingRateCurrency(
            IReadOnlyList<StorefrontCartLineDto> cartLines,
            string checkoutCurrencyCode,
            decimal checkoutSubtotal)
        {
            var convertedLines = cartLines
                .Where(line => line.ExchangeRateSnapshot.HasValue
                    && !string.IsNullOrWhiteSpace(line.BaseCurrencyCodeSnapshot))
                .ToArray();
            if (convertedLines.Length == 0)
            {
                var currencyCode = NormalizeCurrency(checkoutCurrencyCode) ?? DefaultCurrencyCode;
                return new ShippingRateCurrency(currencyCode, checkoutSubtotal);
            }

            var baseCurrencyCode = NormalizeCurrency(convertedLines[0].BaseCurrencyCodeSnapshot) ?? DefaultCurrencyCode;
            var baseSubtotal = this.moneyRoundingService.RoundOrderTotal(
                cartLines.Sum(line => (line.BaseUnitPriceSnapshot ?? line.UnitPriceSnapshot ?? 0m) * Math.Max(0, line.Quantity)),
                baseCurrencyCode);
            return new ShippingRateCurrency(baseCurrencyCode, baseSubtotal);
        }

        internal ShippingRateCurrency ResolveShippingRateCurrency(
            IEnumerable<CartLine> cartLines,
            string checkoutCurrencyCode,
            decimal checkoutSubtotal)
        {
            var materialized = cartLines.ToArray();
            var convertedLines = materialized
                .Where(line => line.ExchangeRateSnapshot.HasValue
                    && !string.IsNullOrWhiteSpace(line.BaseCurrencyCodeSnapshot))
                .ToArray();
            if (convertedLines.Length == 0)
            {
                var currencyCode = NormalizeCurrency(checkoutCurrencyCode) ?? DefaultCurrencyCode;
                return new ShippingRateCurrency(currencyCode, checkoutSubtotal);
            }

            var baseCurrencyCode = NormalizeCurrency(convertedLines[0].BaseCurrencyCodeSnapshot) ?? DefaultCurrencyCode;
            var baseSubtotal = this.moneyRoundingService.RoundOrderTotal(
                materialized.Sum(line => (line.BaseUnitPriceSnapshot ?? line.UnitPriceSnapshot ?? 0m) * Math.Max(0, line.Quantity)),
                baseCurrencyCode);
            return new ShippingRateCurrency(baseCurrencyCode, baseSubtotal);
        }

        internal async Task<ShippingTaxCalculationResult> CalculateShippingTaxAsync(
            Guid storeId,
            StorefrontCheckoutShippingAddressDto? address,
            string currencyCode,
            decimal subtotal,
            decimal shippingTotal,
            CancellationToken cancellationToken)
        {
            var request = new ShippingTaxCalculationRequest(
                storeId,
                ToShippingAddressSnapshot(address),
                NormalizeCurrency(currencyCode) ?? DefaultCurrencyCode,
                subtotal,
                shippingTotal);

            var result = await this.shippingTaxCalculator.CalculateAsync(request, cancellationToken);
            return result with
            {
                TaxTotal = this.moneyRoundingService.RoundOrderTotal(result.TaxTotal, request.CurrencyCode),
            };
        }

        internal async Task<IReadOnlyList<ShippingPackageLine>> BuildShippingPackageLinesAsync(
            Guid storeId,
            IReadOnlyList<StorefrontCartLineDto> cartLines,
            CancellationToken cancellationToken)
        {
            var productIds = cartLines.Select(line => line.ProductId).Distinct().ToArray();
            var products = await this.LoadProductShippingMetadataAsync(storeId, productIds, cancellationToken);

            return cartLines
                .Select(line => ToShippingPackageLine(
                    line.ProductId,
                    line.ProductVariantId,
                    line.Quantity,
                    products.GetValueOrDefault(line.ProductId)))
                .ToArray();
        }

        internal async Task<IReadOnlyList<ShippingPackageLine>> BuildShippingPackageLinesAsync(
            Guid storeId,
            IEnumerable<CartLine> cartLines,
            CancellationToken cancellationToken)
        {
            var materialized = cartLines.ToArray();
            var productIds = materialized.Select(line => line.ProductId).Distinct().ToArray();
            var products = await this.LoadProductShippingMetadataAsync(storeId, productIds, cancellationToken);

            return materialized
                .Select(line => ToShippingPackageLine(
                    line.ProductId,
                    line.ProductVariantId,
                    line.Quantity,
                    products.GetValueOrDefault(line.ProductId)))
                .ToArray();
        }

        internal static IReadOnlyList<StorefrontCheckoutValidationIssue> ToShippingIssues(
            CheckoutShippingCalculationResult result)
        {
            return result.Errors
                .Select(error => new StorefrontCheckoutValidationIssue(
                    "shipping.option_unavailable",
                    error,
                    "shippingOptionKey"))
                .Concat(result.Warnings.Select(warning => new StorefrontCheckoutValidationIssue(
                    "shipping.warning",
                    warning,
                    "shippingOptionKey")))
                .ToArray();
        }

        private async Task<IReadOnlyDictionary<Guid, ProductShippingMetadata>> LoadProductShippingMetadataAsync(
            Guid storeId,
            IReadOnlyList<Guid> productIds,
            CancellationToken cancellationToken)
        {
            return await this.context.Products
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id)
                    && (product.StoreId == storeId || product.StoreId == null))
                .Select(product => new
                {
                    product.Id,
                    product.ShippingRequired,
                    product.FreeShipping,
                    product.Weight,
                    product.Length,
                    product.Width,
                    product.Height,
                    product.ShippingSurcharge,
                })
                .ToDictionaryAsync(
                    product => product.Id,
                    product => new ProductShippingMetadata(
                        product.ShippingRequired,
                        product.FreeShipping,
                        product.Weight,
                        product.Length,
                        product.Width,
                        product.Height,
                        product.ShippingSurcharge),
                    cancellationToken);
        }

        private async Task<ServiceResponse<StorefrontCheckoutShippingOption>> MapShippingOptionAsync(
            Guid storeId,
            ShippingOptionDto option,
            string checkoutCurrencyCode,
            string rateCurrencyCode,
            CancellationToken cancellationToken)
        {
            var optionCurrencyCode = NormalizeCurrency(option.CurrencyCode) ?? rateCurrencyCode;
            var normalizedCheckoutCurrency = NormalizeCurrency(checkoutCurrencyCode) ?? DefaultCurrencyCode;
            var normalizedRateCurrency = NormalizeCurrency(rateCurrencyCode) ?? normalizedCheckoutCurrency;

            if (!string.Equals(optionCurrencyCode, normalizedRateCurrency, StringComparison.Ordinal)
                && !string.Equals(optionCurrencyCode, normalizedCheckoutCurrency, StringComparison.Ordinal))
            {
                return Failed<StorefrontCheckoutShippingOption>(
                    ServiceResponseType.Conflict,
                    "Shipping provider returned an unsupported currency.");
            }

            var price = this.moneyRoundingService.RoundOrderTotal(option.Rate, optionCurrencyCode);
            if (option.Rate == 0m)
            {
                price = 0m;
            }
            else if (!string.Equals(optionCurrencyCode, normalizedCheckoutCurrency, StringComparison.Ordinal))
            {
                var conversion = await this.moneyConversionService.ConvertFromBaseAsync(
                    storeId,
                    option.Rate,
                    normalizedCheckoutCurrency,
                    cancellationToken);
                if (!conversion.Success || conversion.Payload is null)
                {
                    return Failed<StorefrontCheckoutShippingOption>(
                        conversion.ResponseType,
                        conversion.Message ?? "Shipping rate currency conversion is not available.");
                }

                price = this.moneyRoundingService.RoundOrderTotal(
                    conversion.Payload.ConvertedAmount,
                    conversion.Payload.TargetCurrencyCode);
            }

            return Succeeded(
                "Shipping option mapped.",
                new StorefrontCheckoutShippingOption(
                option.Key,
                option.ProviderSystemName,
                option.MethodCode,
                option.DisplayName,
                option.Description,
                price,
                normalizedCheckoutCurrency,
                option.DeliveryEstimateText,
                Selected: false));
        }

        private static ShippingPackageLine ToShippingPackageLine(
            Guid productId,
            Guid? productVariantId,
            int quantity,
            ProductShippingMetadata? product)
        {
            return new ShippingPackageLine(
                productId,
                productVariantId,
                Math.Max(0, quantity),
                product?.ShippingRequired ?? true,
                product?.FreeShipping ?? false,
                product?.Weight,
                product?.Length,
                product?.Width,
                product?.Height,
                product?.ShippingSurcharge);
        }

        private static StorefrontCheckoutShippingAddressDto? CreateShippingAddress(CheckoutSession session)
        {
            return string.IsNullOrWhiteSpace(session.ShippingAddress1)
                ? null
                : new StorefrontCheckoutShippingAddressDto(
                    session.ShippingFullName,
                    session.ShippingEmail,
                    session.ShippingPhone,
                    session.ShippingAddress1,
                    session.ShippingAddress2,
                    session.ShippingCity,
                    session.ShippingState,
                    session.ShippingPostalCode,
                    session.ShippingCountryCode);
        }

        private static ShippingAddressSnapshot? ToShippingAddressSnapshot(StorefrontCheckoutShippingAddressDto? address)
        {
            return address is null
                ? null
                : new ShippingAddressSnapshot(
                    address.FullName,
                    null,
                    address.Address1,
                    address.Address2,
                    address.City,
                    address.State,
                    address.PostalCode,
                    address.CountryCode,
                    address.Phone,
                    address.Email);
        }

        private static string? NormalizeCurrency(string? currencyCode)
        {
            return string.IsNullOrWhiteSpace(currencyCode)
                ? null
                : currencyCode.Trim().ToUpperInvariant();
        }

        private static ServiceResponse<TPayload> Succeeded<TPayload>(string message, TPayload payload)
        {
            return new ServiceResponse<TPayload>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<TPayload> Failed<TPayload>(
            ServiceResponseType responseType,
            string message)
        {
            return new ServiceResponse<TPayload>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private sealed record ProductShippingMetadata(
            bool ShippingRequired,
            bool FreeShipping,
            decimal? Weight,
            decimal? Length,
            decimal? Width,
            decimal? Height,
            decimal? ShippingSurcharge);
    }

    internal sealed record CheckoutShippingCalculationResult(
        bool ShippingRequired,
        IReadOnlyList<StorefrontCheckoutShippingOption> Options,
        IReadOnlyList<string> Warnings,
        IReadOnlyList<string> Errors);

    internal sealed record ShippingRateCurrency(
        string BaseCurrencyCode,
        decimal BaseSubtotal);
}
