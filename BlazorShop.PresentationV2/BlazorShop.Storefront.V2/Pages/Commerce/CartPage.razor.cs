namespace BlazorShop.Storefront.Pages.Commerce
{
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;

    using Microsoft.AspNetCore.Components;

    public partial class CartPage
    {
        private readonly List<StorefrontBrowserCartAlert> _alerts = [];
        private StorefrontDisplayContext _displayContext = StorefrontDisplayContext.Fallback;
        private StorefrontBrowserCart? _cart;

        [CascadingParameter]
        private HttpContext? HttpContext { get; set; }

        [Inject]
        private StorefrontCartTokenService CartTokenService { get; set; } = default!;

        [Inject]
        private IStorefrontDisplayContextProvider DisplayContextProvider { get; set; } = default!;

        [Inject]
        private IStorefrontPriceFormatter PriceFormatter { get; set; } = default!;

        protected override async Task OnParametersSetAsync()
        {
            _alerts.Clear();
            StorefrontResponseHeaders.ApplyPrivatePage(HttpContext);
            _displayContext = await DisplayContextProvider.GetAsync();

            var cartResolution = await CartTokenService.ResolveAsync(HttpContext);
            if (!cartResolution.Success)
            {
                _alerts.Add(new StorefrontBrowserCartAlert("error", cartResolution.Message));
                _cart = null;
                return;
            }

            _cart = BuildCart(cartResolution.Cart);
            foreach (var warning in _cart?.Warnings ?? [])
            {
                _alerts.Add(new StorefrontBrowserCartAlert("warning", warning.Message));
            }
        }

        private StorefrontBrowserCart BuildCart(StorefrontCartResponse? cart)
        {
            var lines = BuildLines(cart?.Lines ?? [], cart?.CurrencyCode);
            var count = cart is not null && cart.SummaryCount > 0
                ? cart.SummaryCount
                : lines.Sum(line => Math.Max(0, line.Quantity));
            var currencyCode = NormalizeCurrencyCode(cart?.CurrencyCode) ?? lines
                .Select(line => line.CurrencyCode)
                .Distinct(StringComparer.Ordinal)
                .SingleOrDefault()
                ?? _displayContext.CurrencyCode;
            var subtotal = cart?.Subtotal ?? lines.Sum(line => line.LineTotal);
            var grandTotal = cart?.GrandTotal ?? lines.Sum(line => line.LineTotal);

            return new StorefrontBrowserCart(
                count,
                cart?.Version ?? 0,
                lines,
                currencyCode,
                subtotal,
                FormatPrice(subtotal, currencyCode),
                grandTotal,
                FormatPrice(grandTotal, currencyCode),
                cart?.CheckoutAllowed ?? lines.All(line => !line.IsUnavailable),
                (cart?.Warnings ?? [])
                    .Select(warning => new StorefrontBrowserCartWarning(warning.Message))
                    .ToArray(),
                (cart?.Adjustments ?? [])
                    .Select(adjustment => new StorefrontBrowserCartAdjustment(
                        adjustment.Label,
                        adjustment.Amount,
                        FormatPrice(adjustment.Amount, currencyCode)))
                    .ToArray());
        }

        private IReadOnlyList<StorefrontBrowserCartLine> BuildLines(IEnumerable<StorefrontCartLineResponse> cartItems, string? cartCurrencyCode)
        {
            var lines = new List<StorefrontBrowserCartLine>();

            foreach (var cartItem in cartItems)
            {
                var quantity = Math.Max(1, cartItem.Quantity);
                var currencyCode = NormalizeCurrencyCode(cartItem.CurrencyCodeSnapshot) ?? NormalizeCurrencyCode(cartCurrencyCode) ?? _displayContext.CurrencyCode;
                var unitPrice = cartItem.UnitPrice ?? cartItem.UnitPriceSnapshot ?? 0m;
                var lineTotal = cartItem.LineTotal ?? cartItem.LineSubtotal ?? (unitPrice * quantity);
                lines.Add(new StorefrontBrowserCartLine(
                    LineId: cartItem.LineId,
                    ProductId: cartItem.ProductId,
                    ProductVariantId: cartItem.ProductVariantId,
                    DisplayName: string.IsNullOrWhiteSpace(cartItem.DisplayName) ? "Cart item" : cartItem.DisplayName,
                    ProductUrl: ResolveProductUrl(cartItem),
                    ImageUrl: cartItem.ImageUrl,
                    Quantity: quantity,
                    UnitPrice: unitPrice,
                    UnitPriceDisplay: FormatPrice(unitPrice, currencyCode),
                    LineTotal: lineTotal,
                    LineTotalDisplay: FormatPrice(lineTotal, currencyCode),
                    CurrencyCode: currencyCode,
                    VariantLabel: ResolveSelectedAttributes(cartItem.SelectedAttributes),
                    QuantityMinimum: Math.Max(1, cartItem.QuantityMinimum),
                    QuantityMaximum: cartItem.QuantityMaximum,
                    QuantityStep: Math.Max(1, cartItem.QuantityStep),
                    Warnings: (cartItem.Warnings ?? [])
                        .Select(warning => warning.Message)
                        .Where(message => !string.IsNullOrWhiteSpace(message))
                        .Select(message => new StorefrontBrowserCartWarning(message))
                        .ToArray(),
                    IsUnavailable: !cartItem.Purchasable || (cartItem.Warnings?.Count ?? 0) > 0));
            }

            return lines;
        }

        private string FormatPrice(decimal amount, string currencyCode) => PriceFormatter.Format(amount, _displayContext with { CurrencyCode = currencyCode });

        private static string? NormalizeCurrencyCode(string? currencyCode)
        {
            var normalized = currencyCode?.Trim().ToUpperInvariant();
            return normalized is { Length: 3 } && normalized.All(char.IsLetter)
                ? normalized
                : null;
        }

        private static string? ResolveProductUrl(StorefrontCartLineResponse cartItem)
        {
            if (!string.IsNullOrWhiteSpace(cartItem.ProductSlug))
            {
                return StorefrontRoutes.Product(cartItem.ProductSlug);
            }

            return string.IsNullOrWhiteSpace(cartItem.ProductUrl) ? null : cartItem.ProductUrl;
        }

        private static string? ResolveSelectedAttributes(IReadOnlyList<StorefrontCartSelectedAttributeResponse>? attributes)
        {
            var attributeText = string.Join(
                " / ",
                (attributes ?? [])
                    .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Name) || !string.IsNullOrWhiteSpace(attribute.Value))
                    .Select(attribute => $"{attribute.Name}: {attribute.Value}"));
            return string.IsNullOrWhiteSpace(attributeText) ? null : attributeText;
        }
    }
}
