namespace BlazorShop.Storefront.Pages
{
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;

    using Microsoft.AspNetCore.Components;

    public partial class CartPage
    {
        private readonly List<CartAlert> _alerts = [];
        private IReadOnlyList<CartLine> _lines = [];
        private StorefrontDisplayContext _displayContext = StorefrontDisplayContext.Fallback;
        private StorefrontCartResponse? _cart;

        [CascadingParameter]
        private HttpContext? HttpContext { get; set; }

        private IReadOnlyList<CartAlert> Alerts => _alerts;

        private IReadOnlyList<CartLine> Lines => _lines;

        private int ItemCount => _cart?.SummaryCount > 0 ? _cart.SummaryCount : _lines.Sum(line => line.Quantity);

        private string GrandTotalDisplay => FormatPrice(_cart?.GrandTotal ?? _lines.Sum(line => line.LineTotal), GrandTotalCurrencyCode);

        private string GrandTotalCurrencyCode => NormalizeCurrencyCode(_cart?.CurrencyCode) ?? _lines
            .Select(line => line.CurrencyCode)
            .Distinct(StringComparer.Ordinal)
            .SingleOrDefault()
            ?? _displayContext.CurrencyCode;

        private string CheckoutUrl => StorefrontRoutes.Checkout;

        private bool CheckoutAllowed => _cart?.CheckoutAllowed ?? _lines.All(line => !line.IsUnavailable);

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
                _alerts.Add(new CartAlert("error", cartResolution.Message));
                _cart = null;
                _lines = [];
                return;
            }

            _cart = cartResolution.Cart;
            foreach (var warning in _cart?.Warnings ?? [])
            {
                _alerts.Add(new CartAlert("warning", warning.Message));
            }

            _lines = BuildLines(_cart?.Lines ?? []);
        }

        private IReadOnlyList<CartLine> BuildLines(IEnumerable<StorefrontCartLineResponse> cartItems)
        {
            var lines = new List<CartLine>();

            foreach (var cartItem in cartItems)
            {
                var quantity = Math.Max(1, cartItem.Quantity);
                lines.Add(new CartLine(
                    LineId: cartItem.LineId,
                    ProductId: cartItem.ProductId,
                    ProductVariantId: cartItem.ProductVariantId,
                    DisplayName: string.IsNullOrWhiteSpace(cartItem.DisplayName) ? "Cart item" : cartItem.DisplayName,
                    ProductUrl: ResolveProductUrl(cartItem),
                    ImageUrl: cartItem.ImageUrl,
                    Quantity: quantity,
                    UnitPrice: cartItem.UnitPrice ?? cartItem.UnitPriceSnapshot ?? 0m,
                    LineTotal: cartItem.LineTotal ?? cartItem.LineSubtotal ?? ((cartItem.UnitPrice ?? cartItem.UnitPriceSnapshot ?? 0m) * quantity),
                    CurrencyCode: NormalizeCurrencyCode(cartItem.CurrencyCodeSnapshot) ?? NormalizeCurrencyCode(_cart?.CurrencyCode) ?? _displayContext.CurrencyCode,
                    VariantLabel: ResolveSelectedAttributes(cartItem.SelectedAttributes),
                    QuantityMinimum: Math.Max(1, cartItem.QuantityMinimum),
                    QuantityMaximum: cartItem.QuantityMaximum,
                    QuantityStep: Math.Max(1, cartItem.QuantityStep),
                    Warnings: (cartItem.Warnings ?? []).Select(warning => warning.Message).Where(message => !string.IsNullOrWhiteSpace(message)).ToArray(),
                    IsUnavailable: !cartItem.Purchasable || (cartItem.Warnings?.Count ?? 0) > 0));
            }

            return lines;
        }

        private string FormatPrice(decimal amount, string currencyCode) => PriceFormatter.Format(amount, _displayContext with { CurrencyCode = currencyCode });

        private sealed record CartAlert(string Level, string Message);

        private sealed record CartLine(
            Guid LineId,
            Guid ProductId,
            Guid? ProductVariantId,
            string DisplayName,
            string? ProductUrl,
            string? ImageUrl,
            int Quantity,
            decimal UnitPrice,
            decimal LineTotal,
            string CurrencyCode,
            string? VariantLabel,
            int QuantityMinimum,
            int? QuantityMaximum,
            int QuantityStep,
            IReadOnlyList<string> Warnings,
            bool IsUnavailable)
        {
        }

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
