namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class CheckoutOrderLineResolver
    {
        private readonly CommerceNodeDbContext context;
        private readonly IMoneyRoundingService moneyRoundingService;
        private readonly IProductSellabilityResolver sellabilityResolver;

        public CheckoutOrderLineResolver(
            CommerceNodeDbContext context,
            IMoneyRoundingService moneyRoundingService,
            IProductSellabilityResolver sellabilityResolver)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(moneyRoundingService);
            ArgumentNullException.ThrowIfNull(sellabilityResolver);

            this.context = context;
            this.moneyRoundingService = moneyRoundingService;
            this.sellabilityResolver = sellabilityResolver;
        }

        internal async Task<CheckoutOrderLineResolution> ResolveAsync(
            Guid storeId,
            IEnumerable<CartLine> cartLines,
            string currencyCode,
            CancellationToken cancellationToken)
        {
            var results = new List<CheckoutOrderLineSnapshot>();

            foreach (var cartLine in cartLines)
            {
                if (cartLine.Quantity < 1)
                {
                    return CheckoutOrderLineResolution.Failed(ServiceResponseType.ValidationError, "Cart line quantity must be at least 1.");
                }

                var product = await this.context.Products
                    .Include(item => item.Category)
                    .Include(item => item.Variants)
                    .FirstOrDefaultAsync(item => item.Id == cartLine.ProductId, cancellationToken);
                if (product is null)
                {
                    return CheckoutOrderLineResolution.Failed(ServiceResponseType.Conflict, "Product is not available for this store.");
                }

                var variant = cartLine.ProductVariantId.HasValue
                    ? product.Variants.FirstOrDefault(candidate => candidate.Id == cartLine.ProductVariantId.Value)
                    : null;
                if (cartLine.ProductVariantId.HasValue && variant is null)
                {
                    return CheckoutOrderLineResolution.Failed(ServiceResponseType.Conflict, "Selected product variant was not found.");
                }

                var sellability = this.sellabilityResolver.Resolve(new ProductSellabilityRequest(
                    storeId,
                    product,
                    variant,
                    cartLine.Quantity,
                    Mode: ProductSellabilityMode.Storefront));
                if (!sellability.Purchasable)
                {
                    return CheckoutOrderLineResolution.Failed(
                        ResolveFailureResponseType(sellability.PurchaseBlockReasons),
                        sellability.PurchaseBlockMessages.FirstOrDefault() ?? "Product cannot be purchased right now.");
                }

                var snapshotCurrency = NormalizeCurrency(cartLine.CurrencyCodeSnapshot);
                if (snapshotCurrency is not null
                    && !string.Equals(snapshotCurrency, currencyCode, StringComparison.Ordinal))
                {
                    return CheckoutOrderLineResolution.Failed(ServiceResponseType.Conflict, "Cart line currency does not match checkout currency.");
                }

                if (!cartLine.UnitPriceSnapshot.HasValue)
                {
                    return CheckoutOrderLineResolution.Failed(ServiceResponseType.ValidationError, "Cart line price is invalid.");
                }

                var unitPrice = this.moneyRoundingService.RoundUnitPrice(cartLine.UnitPriceSnapshot.Value, currencyCode);
                if (unitPrice <= 0m)
                {
                    return CheckoutOrderLineResolution.Failed(ServiceResponseType.ValidationError, "Cart line price is invalid.");
                }

                results.Add(new CheckoutOrderLineSnapshot(cartLine, product, variant, unitPrice));
            }

            return CheckoutOrderLineResolution.Succeeded(results);
        }

        private static ServiceResponseType ResolveFailureResponseType(IReadOnlyList<string> reasons)
        {
            return reasons.Any(reason => reason is ProductPurchaseBlockReasons.BelowMinQuantity
                    or ProductPurchaseBlockReasons.AboveMaxQuantity
                    or ProductPurchaseBlockReasons.InvalidQuantityStep
                    or ProductPurchaseBlockReasons.VariantRequired
                    or ProductPurchaseBlockReasons.VariantInactive)
                ? ServiceResponseType.ValidationError
                : ServiceResponseType.Conflict;
        }

        private static string? NormalizeCurrency(string? value)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
            return normalized is { Length: 3 } ? normalized : null;
        }
    }

    internal sealed record CheckoutOrderLineSnapshot(
        CartLine CartLine,
        Product Product,
        ProductVariant? Variant,
        decimal UnitPrice);

    internal sealed record CheckoutOrderLineResolution(
        bool Success,
        ServiceResponseType ResponseType,
        string Message,
        IReadOnlyList<CheckoutOrderLineSnapshot> Lines)
    {
        public static CheckoutOrderLineResolution Succeeded(IReadOnlyList<CheckoutOrderLineSnapshot> lines)
        {
            return new CheckoutOrderLineResolution(true, ServiceResponseType.Success, "Order lines resolved.", lines);
        }

        public static CheckoutOrderLineResolution Failed(ServiceResponseType responseType, string message)
        {
            return new CheckoutOrderLineResolution(false, responseType, message, []);
        }
    }
}
