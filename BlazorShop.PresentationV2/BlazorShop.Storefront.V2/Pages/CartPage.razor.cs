namespace BlazorShop.Storefront.Pages
{
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2.Models.Product;

    using Microsoft.AspNetCore.Components;

    public partial class CartPage
    {
        private readonly List<CartAlert> _alerts = [];
        private IReadOnlyList<CartLine> _lines = [];
        private StorefrontDisplayContext _displayContext = StorefrontDisplayContext.Fallback;

        [CascadingParameter]
        private HttpContext? HttpContext { get; set; }

        private IReadOnlyList<CartAlert> Alerts => _alerts;

        private IReadOnlyList<CartLine> Lines => _lines;

        private int ItemCount => _lines.Sum(line => line.Quantity);

        private string GrandTotalDisplay => FormatPrice(_lines.Sum(line => line.LineTotal));

        private string CheckoutUrl => StorefrontRoutes.Checkout;

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
                _lines = [];
                return;
            }

            var cartItems = cartResolution.Cart?.Lines ?? [];
            var productsById = await LoadProductsAsync(cartItems);
            _lines = BuildLines(cartItems, productsById);
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

            var results = await Task.WhenAll(productIds.Select(id => ApiClient.GetProductByIdAsync(id)));
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
            var lines = new List<CartLine>();
            var unavailableItems = 0;

            foreach (var cartItem in cartItems)
            {
                var quantity = Math.Max(1, cartItem.Quantity);

                if (productsById.TryGetValue(cartItem.ProductId, out var product))
                {
                    var selectedVariantId = cartItem.ProductVariantId;
                    var selectedVariant = selectedVariantId is null
                        ? null
                        : product.Variants.FirstOrDefault(variant => variant.Id == selectedVariantId.Value);
                    var variantLabel = selectedVariant is null
                        ? null
                        : GetVariantLabel(selectedVariant);
                    var unitPrice = cartItem.UnitPriceSnapshot
                        ?? (selectedVariant?.EffectivePrice > 0 ? selectedVariant.EffectivePrice : selectedVariant?.Price)
                        ?? product.Price;
                    lines.Add(new CartLine(
                        LineId: cartItem.LineId,
                        ProductId: cartItem.ProductId,
                        ProductVariantId: selectedVariantId,
                        DisplayName: string.IsNullOrWhiteSpace(product.Name) ? "Product" : product.Name,
                        ProductUrl: string.IsNullOrWhiteSpace(product.Slug) ? null : StorefrontRoutes.Product(product.Slug),
                        ImageUrl: product.Image,
                        Quantity: quantity,
                        UnitPrice: unitPrice,
                        VariantLabel: variantLabel,
                        IsUnavailable: false));
                    continue;
                }

                unavailableItems++;
                lines.Add(new CartLine(
                    LineId: cartItem.LineId,
                    ProductId: cartItem.ProductId,
                    ProductVariantId: cartItem.ProductVariantId,
                    DisplayName: "Unavailable item",
                    ProductUrl: null,
                    ImageUrl: null,
                    Quantity: quantity,
                    UnitPrice: cartItem.UnitPriceSnapshot ?? 0m,
                    VariantLabel: null,
                    IsUnavailable: true));
            }

            if (unavailableItems > 0)
            {
                _alerts.Add(new CartAlert(
                    "warning",
                    unavailableItems == 1
                        ? "One cart item could not be refreshed from the catalog and is shown as unavailable."
                        : $"{unavailableItems} cart items could not be refreshed from the catalog and are shown as unavailable."));
            }

            return lines;
        }

        private string FormatPrice(decimal amount) => PriceFormatter.Format(amount, _displayContext);

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
            string? VariantLabel,
            bool IsUnavailable)
        {
            public decimal LineTotal => UnitPrice * Quantity;
        }

        private static string? GetVariantLabel(GetProductVariant variant)
        {
            if (!string.IsNullOrWhiteSpace(variant.DisplayName))
            {
                return variant.DisplayName;
            }

            var attributeText = string.Join(" / ", variant.Attributes.Select(attribute => $"{attribute.Name}: {attribute.Value}"));
            return string.IsNullOrWhiteSpace(attributeText)
                ? variant.SizeValue
                : attributeText;
        }
    }
}
