namespace BlazorShop.Storefront.Pages
{
    using System.Globalization;

    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Web.SharedV2.Models.Product;

    using Microsoft.AspNetCore.Components;

    public partial class CheckoutPage
    {
        private readonly List<CartLine> lines = [];
        private IReadOnlyList<GetPaymentMethod> paymentMethods = [];

        [CascadingParameter]
        private HttpContext? HttpContext { get; set; }

        [SupplyParameterFromQuery(Name = "error")]
        public string? Error { get; set; }

        [SupplyParameterFromQuery(Name = "orderReference")]
        public string? OrderReference { get; set; }

        private IReadOnlyList<CartLine> Lines => lines;

        private IReadOnlyList<GetPaymentMethod> PaymentMethods => paymentMethods;

        private int CartVersion { get; set; }

        private string GrandTotalDisplay => lines.Sum(line => line.LineTotal).ToString("0.00", CultureInfo.InvariantCulture);

        protected override async Task OnParametersSetAsync()
        {
            StorefrontResponseHeaders.ApplyPrivatePage(HttpContext);

            if (!string.IsNullOrWhiteSpace(OrderReference))
            {
                lines.Clear();
                paymentMethods = [];
                return;
            }

            var cartResolution = await CartTokenService.ResolveAsync(HttpContext);
            if (!cartResolution.Success)
            {
                Error = cartResolution.Message;
                lines.Clear();
                paymentMethods = [];
                return;
            }

            var cartItems = cartResolution.Cart?.Lines ?? [];
            CartVersion = cartResolution.Cart?.Version ?? 0;
            var productsById = await LoadProductsAsync(cartItems);
            lines.Clear();
            lines.AddRange(BuildLines(cartItems, productsById));

            var paymentResult = await ApiClient.GetPaymentMethodsAsync();
            paymentMethods = paymentResult.IsSuccess && paymentResult.Value is not null
                ? paymentResult.Value
                : [];
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

        private static IReadOnlyList<CartLine> BuildLines(IEnumerable<StorefrontCartLineResponse> cartItems, IReadOnlyDictionary<Guid, GetProduct> productsById)
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
                    unitPrice));
            }

            return result;
        }

        private sealed record CartLine(string DisplayName, int Quantity, decimal UnitPrice)
        {
            public decimal LineTotal => UnitPrice * Quantity;

            public string LineTotalDisplay => LineTotal.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
