namespace BlazorShop.Storefront.Pages
{
    using System.Globalization;
    using System.Text.Json;

    using BlazorShop.Storefront.Services;
    using BlazorShop.Web.SharedV2;
    using BlazorShop.Web.SharedV2.Models.Payment;
    using BlazorShop.Web.SharedV2.Models.Product;

    using Microsoft.AspNetCore.Components;

    public partial class CartPage
    {
        private readonly List<CartAlert> _alerts = [];
        private IReadOnlyList<CartLine> _lines = [];

        [CascadingParameter]
        private HttpContext? HttpContext { get; set; }

        private IReadOnlyList<CartAlert> Alerts => _alerts;

        private IReadOnlyList<CartLine> Lines => _lines;

        private int ItemCount => _lines.Sum(line => line.Quantity);

        private string GrandTotalDisplay => _lines.Sum(line => line.LineTotal).ToString("0.00", CultureInfo.InvariantCulture);

        private string CheckoutUrl => StorefrontRoutes.Checkout;

        protected override async Task OnParametersSetAsync()
        {
            _alerts.Clear();
            StorefrontResponseHeaders.ApplyPrivatePage(HttpContext);

            var cartItems = ReadCartItems(HttpContext?.Request.Cookies[StorefrontCookieNames.Cart]);
            var productsById = await LoadProductsAsync(cartItems);
            _lines = BuildLines(cartItems, productsById);
        }

        private List<ProcessCart> ReadCartItems(string? rawCart)
        {
            if (string.IsNullOrWhiteSpace(rawCart))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<List<ProcessCart>>(rawCart)
                    ?.Where(item => item.ProductId != Guid.Empty && item.Quantity > 0)
                    .ToList()
                    ?? [];
            }
            catch (JsonException)
            {
                _alerts.Add(new CartAlert(
                    "error",
                    "We couldn't read the saved cart cookie. Add the items again to continue."));
                return [];
            }
        }

        private async Task<Dictionary<Guid, GetProduct>> LoadProductsAsync(IEnumerable<ProcessCart> cartItems)
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

        private IReadOnlyList<CartLine> BuildLines(IEnumerable<ProcessCart> cartItems, IReadOnlyDictionary<Guid, GetProduct> productsById)
        {
            var lines = new List<CartLine>();
            var unavailableItems = 0;

            foreach (var cartItem in cartItems)
            {
                var quantity = Math.Max(1, cartItem.Quantity);
                var sizeValue = string.IsNullOrWhiteSpace(cartItem.SizeValue) ? null : cartItem.SizeValue.Trim();

                if (productsById.TryGetValue(cartItem.ProductId, out var product))
                {
                    var selectedVariantId = cartItem.ProductVariantId ?? cartItem.VariantId;
                    var selectedVariant = selectedVariantId is null
                        ? null
                        : product.Variants.FirstOrDefault(variant => variant.Id == selectedVariantId.Value);
                    var variantLabel = selectedVariant is null
                        ? sizeValue
                        : GetVariantLabel(selectedVariant);
                    var unitPrice = cartItem.UnitPrice
                                    ?? (selectedVariant?.EffectivePrice > 0 ? selectedVariant.EffectivePrice : selectedVariant?.Price)
                                    ?? product.Price;
                    lines.Add(new CartLine(
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
                    ProductId: cartItem.ProductId,
                    ProductVariantId: cartItem.ProductVariantId ?? cartItem.VariantId,
                    DisplayName: "Unavailable item",
                    ProductUrl: null,
                    ImageUrl: null,
                    Quantity: quantity,
                    UnitPrice: cartItem.UnitPrice ?? 0m,
                    VariantLabel: sizeValue,
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

        private sealed record CartAlert(string Level, string Message);

        private sealed record CartLine(
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

            public string UnitPriceDisplay => UnitPrice.ToString("0.00", CultureInfo.InvariantCulture);

            public string LineTotalDisplay => LineTotal.ToString("0.00", CultureInfo.InvariantCulture);
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
