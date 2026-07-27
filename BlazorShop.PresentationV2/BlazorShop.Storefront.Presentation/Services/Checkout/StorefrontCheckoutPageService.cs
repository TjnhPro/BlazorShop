namespace BlazorShop.Storefront.Presentation.Services.Checkout
{
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Presentation.Endpoints;
    using BlazorShop.Storefront.Presentation.PagePatterns;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Storefront.Runtime;
    using BlazorShop.Storefront.Client;
    using Microsoft.AspNetCore.Http;
    using StorefrontAddressCountryResponse = BlazorShop.Storefront.Client.StorefrontAddressCountryResponse;
    using StorefrontAddressFieldConfigurationResponse = BlazorShop.Storefront.Client.StorefrontAddressFieldConfigurationResponse;
    using StorefrontAddressStateProvinceResponse = BlazorShop.Storefront.Client.StorefrontAddressStateProvinceResponse;
    using static BlazorShop.Storefront.Presentation.Endpoints.StorefrontLocalEndpointSupport;
    using StorefrontCartLineResponse = BlazorShop.Storefront.Services.StorefrontCartLineResponse;

    public sealed class StorefrontCheckoutPageService
    {
        private readonly StorefrontCartTokenService cartTokenService;
        private readonly IStorefrontDisplayContextProvider displayContextProvider;
        private readonly IStorefrontPriceFormatter priceFormatter;
        private readonly IStorefrontRuntimeCheckoutFacade checkoutFacade;
        private readonly IStorefrontRuntimePaymentFacade paymentFacade;
        private readonly IStorefrontRuntimeAddressFacade addressFacade;
        private readonly IStorefrontRuntimeCatalogFacade catalogFacade;

        public StorefrontCheckoutPageService(
            StorefrontCartTokenService cartTokenService,
            IStorefrontDisplayContextProvider displayContextProvider,
            IStorefrontPriceFormatter priceFormatter,
            IStorefrontRuntimeCheckoutFacade checkoutFacade,
            IStorefrontRuntimePaymentFacade paymentFacade,
            IStorefrontRuntimeAddressFacade addressFacade,
            IStorefrontRuntimeCatalogFacade catalogFacade)
        {
            this.cartTokenService = cartTokenService;
            this.displayContextProvider = displayContextProvider;
            this.priceFormatter = priceFormatter;
            this.checkoutFacade = checkoutFacade;
            this.paymentFacade = paymentFacade;
            this.addressFacade = addressFacade;
            this.catalogFacade = catalogFacade;
        }

        public async Task<StorefrontCheckoutPageContext> GetAsync(
            HttpContext? httpContext,
            string? error,
            string? orderReference,
            CancellationToken cancellationToken = default)
        {
            var displayContext = await this.displayContextProvider.GetAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(orderReference))
            {
                return CreateOrderPlacedContext(orderReference.Trim(), displayContext);
            }

            var cartResolution = await this.cartTokenService.ResolveAsync(httpContext, cancellationToken: cancellationToken);
            if (!cartResolution.Success)
            {
                return CreateEmptyContext(
                    error ?? cartResolution.Message,
                    displayContext);
            }

            var cartItems = cartResolution.Cart?.Lines ?? [];
            var checkoutSession = default(StorefrontCheckoutSessionResponse);
            var cartVersion = cartResolution.Cart?.Version ?? 0;
            var checkoutState = CreateEmptyCheckoutState(error ?? "Checkout is not available yet.");
            if (!string.IsNullOrWhiteSpace(cartResolution.CartToken) && cartItems.Count > 0)
            {
                var checkoutResult = await this.checkoutFacade.StartAsync(cartResolution.CartToken, cancellationToken);
                if (checkoutResult.Success && checkoutResult.Value is not null)
                {
                    checkoutSession = checkoutResult.Value;
                    cartVersion = NormalizeVersion(checkoutResult.Value.CartVersion);
                    checkoutState = ToBrowserCheckoutState(checkoutResult.Value, displayContext, this.priceFormatter);
                }
                else if (string.IsNullOrWhiteSpace(error))
                {
                    error = checkoutResult.Error?.Message ?? "Checkout is not available yet.";
                }
            }

            var productsById = await LoadProductsAsync(cartItems, displayContext, cancellationToken);
            var lines = BuildLines(cartItems, productsById, displayContext);

            var paymentMethods = checkoutSession is not null && checkoutSession.PaymentMethods.Count > 0
                ? checkoutSession.PaymentMethods.ToArray()
                : await LoadPaymentMethodsAsync(checkoutSession?.CurrencyCode ?? displayContext.CurrencyCode, cancellationToken);

            var addressCountries = await LoadAddressCountriesAsync(cancellationToken);
            var addressConfiguration = await LoadAddressConfigurationAsync(cancellationToken);
            var addressStates = await LoadAddressStatesAsync(addressCountries, cancellationToken);
            var grandTotalCurrencyCode = checkoutSession?.CurrencyCode
                ?? lines.Select(line => line.CurrencyCode).Distinct(StringComparer.Ordinal).SingleOrDefault()
                ?? displayContext.CurrencyCode;
            var totalDisplayContext = displayContext with { CurrencyCode = grandTotalCurrencyCode };
            var grandTotalDisplay = checkoutSession is not null
                ? this.priceFormatter.Format(ToMoney(checkoutSession.GrandTotal), totalDisplayContext)
                : this.priceFormatter.Format(lines.Sum(line => line.LineTotal), totalDisplayContext);

            return new StorefrontCheckoutPageContext(
                error,
                null,
                checkoutState,
                lines,
                paymentMethods,
                addressCountries,
                addressStates,
                addressConfiguration,
                cartVersion,
                Guid.NewGuid().ToString("N"),
                grandTotalDisplay,
                grandTotalCurrencyCode,
                ToNullableMoney(checkoutSession?.Subtotal),
                ToNullableMoney(checkoutSession?.ShippingTotal),
                ToNullableMoney(checkoutSession?.TaxTotal),
                ToNullableMoney(checkoutSession?.DiscountTotal),
                checkoutSession is null ? null : this.priceFormatter.Format(ToMoney(checkoutSession.Subtotal), totalDisplayContext),
                checkoutSession is null ? null : this.priceFormatter.Format(ToMoney(checkoutSession.ShippingTotal), totalDisplayContext),
                checkoutSession is null ? null : this.priceFormatter.Format(ToMoney(checkoutSession.TaxTotal), totalDisplayContext),
                checkoutSession is null ? null : this.priceFormatter.Format(ToMoney(checkoutSession.DiscountTotal), totalDisplayContext),
                addressCountries.FirstOrDefault()?.Code ?? "US",
                string.Empty);
        }

        private static StorefrontCheckoutPageContext CreateOrderPlacedContext(
            string orderReference,
            StorefrontDisplayContext displayContext)
        {
            return new StorefrontCheckoutPageContext(
                null,
                orderReference,
                CreateEmptyCheckoutState("Order placed."),
                [],
                [],
                [],
                [],
                null,
                0,
                Guid.NewGuid().ToString("N"),
                string.Empty,
                displayContext.CurrencyCode,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "US",
                string.Empty);
        }

        private static StorefrontCheckoutPageContext CreateEmptyContext(
            string? error,
            StorefrontDisplayContext displayContext)
        {
            return new StorefrontCheckoutPageContext(
                error,
                null,
                CreateEmptyCheckoutState(error ?? "Checkout is not available yet."),
                [],
                [],
                [],
                [],
                null,
                0,
                Guid.NewGuid().ToString("N"),
                string.Empty,
                displayContext.CurrencyCode,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "US",
                string.Empty);
        }

        private async Task<IReadOnlyList<StorefrontCheckoutPaymentMethodOptionResponse>> LoadPaymentMethodsAsync(
            string currencyCode,
            CancellationToken cancellationToken)
        {
            var paymentResult = await this.paymentFacade.ListMethodsAsync(cancellationToken);
            if (!paymentResult.Success || paymentResult.Value is null)
            {
                return [];
            }

            return paymentResult.Value
                .Where(method => SupportsCurrency(method, currencyCode))
                .Select(ToCheckoutPaymentOption)
                .ToArray();
        }

        private async Task<IReadOnlyList<StorefrontAddressCountryResponse>> LoadAddressCountriesAsync(
            CancellationToken cancellationToken)
        {
            var countriesResult = await this.addressFacade.ListCountriesAsync(cancellationToken);
            return countriesResult.Success && countriesResult.Value is not null
                ? countriesResult.Value
                : [];
        }

        private async Task<StorefrontAddressFieldConfigurationResponse?> LoadAddressConfigurationAsync(
            CancellationToken cancellationToken)
        {
            var configurationResult = await this.addressFacade.GetConfigurationAsync(cancellationToken);
            return configurationResult.Success
                ? configurationResult.Value
                : null;
        }

        private async Task<IReadOnlyList<StorefrontAddressStateProvinceResponse>> LoadAddressStatesAsync(
            IReadOnlyList<StorefrontAddressCountryResponse> addressCountries,
            CancellationToken cancellationToken)
        {
            var countryCode = addressCountries.FirstOrDefault()?.Code;
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return [];
            }

            var statesResult = await this.addressFacade.ListStatesAsync(countryCode, cancellationToken);
            return statesResult.Success && statesResult.Value is not null
                ? statesResult.Value
                : [];
        }

        private async Task<Dictionary<Guid, StorefrontProductResponse>> LoadProductsAsync(
            IEnumerable<StorefrontCartLineResponse> cartItems,
            StorefrontDisplayContext displayContext,
            CancellationToken cancellationToken)
        {
            var productIds = cartItems
                .Select(item => item.ProductId)
                .Where(productId => productId != Guid.Empty)
                .Distinct()
                .ToArray();

            if (productIds.Length == 0)
            {
                return [];
            }

            var results = await Task.WhenAll(productIds.Select(id => this.catalogFacade.GetProductByIdAsync(id, displayContext.CurrencyCode, cancellationToken)));
            var productsById = new Dictionary<Guid, StorefrontProductResponse>();

            for (var index = 0; index < productIds.Length; index++)
            {
                var result = results[index];
                if (result.Success && result.Value is not null)
                {
                    productsById[productIds[index]] = result.Value;
                }
            }

            return productsById;
        }

        private IReadOnlyList<StorefrontCheckoutPageLine> BuildLines(
            IEnumerable<StorefrontCartLineResponse> cartItems,
            IReadOnlyDictionary<Guid, StorefrontProductResponse> productsById,
            StorefrontDisplayContext displayContext)
        {
            var result = new List<StorefrontCheckoutPageLine>();

            foreach (var cartItem in cartItems)
            {
                if (!productsById.TryGetValue(cartItem.ProductId, out var product))
                {
                    continue;
                }

                var selectedVariantId = cartItem.ProductVariantId;
                var selectedVariant = selectedVariantId is null
                    ? null
                    : product.Variants.FirstOrDefault(variant => variant.Id == selectedVariantId);
                var selectedVariantPrice = selectedVariant?.EffectivePrice > 0
                    ? ToNullableMoney(selectedVariant.EffectivePrice)
                    : ToNullableMoney(selectedVariant?.Price);
                var unitPrice = cartItem.UnitPriceSnapshot
                    ?? selectedVariantPrice
                    ?? ToMoney(product.Price);

                var currencyCode = NormalizeCurrencyCode(cartItem.CurrencyCodeSnapshot) ?? displayContext.CurrencyCode;
                var quantity = Math.Max(1, cartItem.Quantity);
                result.Add(new StorefrontCheckoutPageLine(
                    string.IsNullOrWhiteSpace(product.Name) ? "Product" : product.Name,
                    quantity,
                    unitPrice,
                    currencyCode,
                    this.priceFormatter.Format(unitPrice * quantity, displayContext with { CurrencyCode = currencyCode })));
            }

            return result;
        }

        private static StorefrontCheckoutPaymentMethodOptionResponse ToCheckoutPaymentOption(StorefrontPaymentMethodResponse method)
        {
            return new StorefrontCheckoutPaymentMethodOptionResponse
            {
                Key = method.Key ?? string.Empty,
                DisplayName = method.Name ?? method.Key ?? "Payment method",
                Description = method.Description,
                ShortDisplayText = method.ShortDisplayText,
                IconUrl = method.IconUrl,
                ProviderKey = method.Key,
                NextActionKind = "none",
                Selected = false,
            };
        }

        private static bool SupportsCurrency(StorefrontPaymentMethodResponse method, string currencyCode)
        {
            var supportedCodes = (method.SupportedCurrencyCodes ?? [])
                .Select(NormalizeCurrencyCode)
                .Where(code => code is not null)
                .Select(code => code!)
                .ToArray();

            return supportedCodes.Length == 0 || supportedCodes.Contains(currencyCode, StringComparer.Ordinal);
        }

        private static string? NormalizeCurrencyCode(string? currencyCode)
        {
            var normalized = currencyCode?.Trim().ToUpperInvariant();
            return normalized is { Length: 3 } && normalized.All(char.IsLetter)
                ? normalized
                : null;
        }
    }
}
