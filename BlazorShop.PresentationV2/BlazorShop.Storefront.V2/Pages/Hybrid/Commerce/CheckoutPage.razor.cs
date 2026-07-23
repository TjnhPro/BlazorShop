namespace BlazorShop.Storefront.Pages.Hybrid.Commerce
{
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2.Models.Product;

    using Microsoft.AspNetCore.Components;

    public partial class CheckoutPage
    {
        private readonly List<CartLine> lines = [];
        private IReadOnlyList<StorefrontCheckoutPaymentMethodOptionResponse> paymentMethods = [];
        private IReadOnlyList<StorefrontAddressCountryResponse> addressCountries = [];
        private IReadOnlyList<StorefrontAddressStateProvinceResponse> addressStates = [];
        private IReadOnlyList<StorefrontCustomerAddressResponse> customerAddresses = [];
        private StorefrontCustomerAddressResponse? selectedShippingAddress;
        private StorefrontAddressFieldConfigurationResponse? addressConfiguration;
        private StorefrontCheckoutSessionResponse? checkoutSession;
        private StorefrontBrowserCheckoutState checkoutState = CreateEmptyCheckoutState("Checkout is not available yet.");
        private StorefrontDisplayContext displayContext = StorefrontDisplayContext.Fallback;

        [CascadingParameter]
        private HttpContext? HttpContext { get; set; }

        [SupplyParameterFromQuery(Name = "error")]
        public string? Error { get; set; }

        [SupplyParameterFromQuery(Name = "orderReference")]
        public string? OrderReference { get; set; }

        private IReadOnlyList<CartLine> Lines => lines;

        private IReadOnlyList<StorefrontCheckoutPaymentMethodOptionResponse> PaymentMethods => paymentMethods;

        private IReadOnlyList<StorefrontAddressCountryResponse> AddressCountries => addressCountries;

        private IReadOnlyList<StorefrontAddressStateProvinceResponse> AddressStates => addressStates;

        private IReadOnlyList<StorefrontCustomerAddressResponse> CustomerAddresses => customerAddresses;

        private StorefrontCustomerAddressResponse? SelectedShippingAddress => selectedShippingAddress;

        private bool HasAddressCountries => addressCountries.Count > 0;

        private bool HasAddressStates => addressStates.Count > 0;

        private bool PhoneEnabled => addressConfiguration?.PhoneEnabled ?? true;

        private bool PhoneRequired => addressConfiguration?.PhoneRequired ?? false;

        private bool PostalCodeRequired => addressConfiguration?.PostalCodeRequired ?? true;

        private string DefaultShippingCountryCode => NormalizeCountryCode(selectedShippingAddress?.CountryCode) ?? "US";

        private string DefaultShippingStateCode => selectedShippingAddress?.StateProvinceCode ?? selectedShippingAddress?.StateProvinceName ?? string.Empty;

        private int CartVersion { get; set; }

        private string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");

        private string GrandTotalDisplay => FormatPrice(checkoutSession?.GrandTotal ?? lines.Sum(line => line.LineTotal), GrandTotalCurrencyCode);

        private string GrandTotalCurrencyCode => checkoutSession?.CurrencyCode ?? lines
            .Select(line => line.CurrencyCode)
            .Distinct(StringComparer.Ordinal)
            .SingleOrDefault()
            ?? displayContext.CurrencyCode;

        private decimal? ServerSubtotal => checkoutSession?.Subtotal;

        private decimal? ServerShippingTotal => checkoutSession?.ShippingTotal;

        private decimal? ServerTaxTotal => checkoutSession?.TaxTotal;

        private decimal? ServerDiscountTotal => checkoutSession?.DiscountTotal;

        private StorefrontBrowserCheckoutState CheckoutState => checkoutState;

        [Inject]
        private IStorefrontDisplayContextProvider DisplayContextProvider { get; set; } = default!;

        [Inject]
        private IStorefrontPriceFormatter PriceFormatter { get; set; } = default!;

        [Inject]
        private IStorefrontSessionResolver SessionResolver { get; set; } = default!;

        [Inject]
        private IStorefrontCheckoutClient CheckoutClient { get; set; } = default!;

        [Inject]
        private IStorefrontPaymentClient PaymentClient { get; set; } = default!;

        [Inject]
        private IStorefrontCatalogClient CatalogClient { get; set; } = default!;

        [Inject]
        private IStorefrontAddressClient AddressClient { get; set; } = default!;

        [Inject]
        private IStorefrontCustomerClient CustomerClient { get; set; } = default!;

        protected override async Task OnParametersSetAsync()
        {
            StorefrontResponseHeaders.ApplyPrivatePage(HttpContext);
            displayContext = await DisplayContextProvider.GetAsync();

            if (!string.IsNullOrWhiteSpace(OrderReference))
            {
                lines.Clear();
                paymentMethods = [];
                checkoutSession = null;
                checkoutState = CreateEmptyCheckoutState("Order placed.");
                return;
            }

            var cartResolution = await CartTokenService.ResolveAsync(HttpContext);
            if (!cartResolution.Success)
            {
                Error = cartResolution.Message;
                lines.Clear();
                paymentMethods = [];
                checkoutSession = null;
                checkoutState = CreateEmptyCheckoutState(Error ?? "Your cart is empty.");
                return;
            }

            var cartItems = cartResolution.Cart?.Lines ?? [];
            CartVersion = cartResolution.Cart?.Version ?? 0;
            checkoutSession = null;
            if (!string.IsNullOrWhiteSpace(cartResolution.CartToken) && cartItems.Count > 0)
            {
                var checkoutResult = await CheckoutClient.StartCheckoutAsync(cartResolution.CartToken);
                if (checkoutResult.Success && checkoutResult.Data is not null)
                {
                    checkoutSession = checkoutResult.Data;
                    CartVersion = checkoutResult.Data.CartVersion;
                }
                else if (string.IsNullOrWhiteSpace(Error))
                {
                    Error = checkoutResult.Message;
                }
            }

            var productsById = await LoadProductsAsync(cartItems);
            lines.Clear();
            lines.AddRange(BuildLines(cartItems, productsById));
            checkoutState = checkoutSession is null
                ? CreateEmptyCheckoutState(Error ?? "Checkout is not available yet.")
                : ToBrowserCheckoutState(checkoutSession);

            if (checkoutSession?.PaymentMethods.Count > 0)
            {
                paymentMethods = checkoutSession.PaymentMethods;
            }
            else
            {
                var paymentResult = await PaymentClient.GetPaymentMethodsAsync();
                paymentMethods = paymentResult.IsSuccess && paymentResult.Value is not null
                    ? paymentResult.Value
                        .Where(method => SupportsCurrency(method, GrandTotalCurrencyCode))
                        .Select(ToCheckoutPaymentOption)
                        .ToArray()
                    : [];
            }

            await LoadAddressMetadataAsync();
        }

        private async Task<Dictionary<Guid, GetProduct>> LoadProductsAsync(IEnumerable<StorefrontCartLineResponse> cartItems)
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

            var results = await Task.WhenAll(productIds.Select(id => CatalogClient.GetProductByIdAsync(id)));
            var productsById = new Dictionary<Guid, GetProduct>();

            for (var index = 0; index < productIds.Length; index++)
            {
                var result = results[index];
                if (result.IsSuccess && result.Value is not null)
                {
                    productsById[productIds[index]] = result.Value;
                }
            }

            return productsById;
        }

        private IReadOnlyList<CartLine> BuildLines(IEnumerable<StorefrontCartLineResponse> cartItems, IReadOnlyDictionary<Guid, GetProduct> productsById)
        {
            var result = new List<CartLine>();

            foreach (var cartItem in cartItems)
            {
                if (!productsById.TryGetValue(cartItem.ProductId, out var product))
                {
                    continue;
                }

                var selectedVariantId = cartItem.ProductVariantId;
                var selectedVariant = selectedVariantId is null
                    ? null
                    : product.Variants.FirstOrDefault(variant => variant.Id == selectedVariantId.Value);
                var unitPrice = cartItem.UnitPriceSnapshot
                    ?? (selectedVariant?.EffectivePrice > 0 ? selectedVariant.EffectivePrice : selectedVariant?.Price)
                    ?? product.Price;

                result.Add(new CartLine(
                    string.IsNullOrWhiteSpace(product.Name) ? "Product" : product.Name,
                    Math.Max(1, cartItem.Quantity),
                    unitPrice,
                    NormalizeCurrencyCode(cartItem.CurrencyCodeSnapshot) ?? displayContext.CurrencyCode));
            }

            return result;
        }

        private string FormatPrice(decimal amount, string currencyCode) => PriceFormatter.Format(amount, displayContext with { CurrencyCode = currencyCode });

        private async Task LoadAddressMetadataAsync()
        {
            var countriesResult = await AddressClient.GetAddressCountriesAsync();
            addressCountries = countriesResult.IsSuccess && countriesResult.Value is not null
                ? countriesResult.Value
                : [];

            var configurationResult = await AddressClient.GetAddressConfigurationAsync();
            addressConfiguration = configurationResult.IsSuccess
                ? configurationResult.Value
                : null;

            customerAddresses = [];
            selectedShippingAddress = null;
            var session = await SessionResolver.GetCurrentUserAsync();
            if (session.IsAuthenticated && !string.IsNullOrWhiteSpace(session.AccessToken))
            {
                var addressesResult = await CustomerClient.GetCustomerAddressesAsync(session.AccessToken);
                customerAddresses = addressesResult.Success && addressesResult.Data is not null
                    ? addressesResult.Data
                    : [];
                selectedShippingAddress = customerAddresses.FirstOrDefault(address => address.IsDefaultShipping)
                    ?? customerAddresses.FirstOrDefault();
            }

            var statesResult = await AddressClient.GetAddressStatesAsync(DefaultShippingCountryCode);
            addressStates = statesResult.IsSuccess && statesResult.Value is not null
                ? statesResult.Value
                : [];
        }

        private string FormatAddressOption(StorefrontCustomerAddressResponse address)
        {
            var region = string.IsNullOrWhiteSpace(address.StateProvinceCode)
                ? address.StateProvinceName
                : address.StateProvinceCode;
            var parts = new[]
            {
                address.FullName,
                address.Address1,
                address.City,
                region,
                address.CountryCode,
            };

            return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        private sealed record CartLine(string DisplayName, int Quantity, decimal UnitPrice, string CurrencyCode)
        {
            public decimal LineTotal => UnitPrice * Quantity;
        }

        private static string? NormalizeCurrencyCode(string? currencyCode)
        {
            var normalized = currencyCode?.Trim().ToUpperInvariant();
            return normalized is { Length: 3 } && normalized.All(char.IsLetter)
                ? normalized
                : null;
        }

        private static string? NormalizeCountryCode(string? countryCode)
        {
            var normalized = countryCode?.Trim().ToUpperInvariant();
            return normalized is { Length: 2 } && normalized.All(char.IsLetter)
                ? normalized
                : null;
        }

        private static StorefrontCheckoutPaymentMethodOptionResponse ToCheckoutPaymentOption(GetPaymentMethod method)
        {
            return new StorefrontCheckoutPaymentMethodOptionResponse(
                method.Key,
                method.Name,
                method.Description,
                method.ShortDisplayText,
                method.IconUrl,
                method.Key,
                "none",
                Selected: false);
        }

        private static bool SupportsCurrency(GetPaymentMethod method, string currencyCode)
        {
            var supportedCodes = method.SupportedCurrencyCodes
                .Select(NormalizeCurrencyCode)
                .Where(code => code is not null)
                .Select(code => code!)
                .ToArray();

            return supportedCodes.Length == 0 || supportedCodes.Contains(currencyCode, StringComparer.Ordinal);
        }

        private StorefrontBrowserCheckoutState ToBrowserCheckoutState(StorefrontCheckoutSessionResponse session)
        {
            var checkoutContext = displayContext with { CurrencyCode = session.CurrencyCode };
            return new StorefrontBrowserCheckoutState(
                true,
                null,
                session.CheckoutSessionId,
                session.CheckoutVersion,
                session.CartVersion,
                session.State,
                session.CurrentStep,
                session.IsActive,
                session.ShippingRequired,
                false,
                PriceFormatter.Format(session.GrandTotal, checkoutContext),
                session.Lines.Select(line => new StorefrontBrowserCheckoutLine(
                    line.LineId,
                    line.ProductId,
                    line.ProductVariantId,
                    line.Quantity,
                    PriceFormatter.Format(line.UnitPrice, checkoutContext with { CurrencyCode = line.CurrencyCode }),
                    PriceFormatter.Format(line.LineTotal, checkoutContext with { CurrencyCode = line.CurrencyCode }))).ToArray(),
                session.ShippingOptions.Select(option => new StorefrontBrowserCheckoutOption(
                    option.Key,
                    option.DisplayName,
                    option.Description,
                    PriceFormatter.Format(option.Price, checkoutContext with { CurrencyCode = option.CurrencyCode }),
                    option.Selected)).ToArray(),
                session.PaymentMethods.Select(method => new StorefrontBrowserCheckoutOption(
                    method.Key,
                    method.DisplayName,
                    method.Description,
                    null,
                    method.Selected)).ToArray(),
                session.Issues.Select(issue => new StorefrontBrowserCheckoutIssue(
                    issue.Code,
                    issue.Message,
                    issue.Field)).ToArray());
        }

        private static StorefrontBrowserCheckoutState CreateEmptyCheckoutState(string message)
        {
            return new StorefrontBrowserCheckoutState(
                false,
                message,
                null,
                0,
                0,
                "empty",
                "cart",
                false,
                false,
                false,
                string.Empty,
                [],
                [],
                [],
                []);
        }
    }
}
