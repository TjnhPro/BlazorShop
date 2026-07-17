namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class OrderPlacementService : IOrderPlacementService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly CommerceNodeDbContext context;
        private readonly IMoneyRoundingService moneyRoundingService;
        private readonly IProductSellabilityResolver sellabilityResolver;

        public OrderPlacementService(
            CommerceNodeDbContext context,
            IMoneyRoundingService? moneyRoundingService = null,
            IProductSellabilityResolver? sellabilityResolver = null)
        {
            this.context = context;
            this.moneyRoundingService = moneyRoundingService ?? new MoneyRoundingService(new CurrencyMetadataService());
            this.sellabilityResolver = sellabilityResolver ?? new ProductSellabilityResolver();
        }

        public async Task<OrderPlacementResult> PlaceAsync(
            OrderPlacementRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.CheckoutSession);
            ArgumentNullException.ThrowIfNull(request.Snapshot);

            if (request.StoreId == Guid.Empty || request.CheckoutSession.StoreId != request.StoreId)
            {
                return OrderPlacementResult.Failed(ServiceResponseType.ValidationError, "Store is required.");
            }

            var cart = request.CheckoutSession.CartSession;
            if (cart is null)
            {
                return OrderPlacementResult.Failed(ServiceResponseType.NotFound, "Cart session was not found.");
            }

            if (!string.Equals(cart.State, CartSessionStates.Active, StringComparison.OrdinalIgnoreCase))
            {
                return OrderPlacementResult.Failed(ServiceResponseType.Conflict, "Cart is not active.");
            }

            if (cart.Lines.Count == 0)
            {
                return OrderPlacementResult.Failed(ServiceResponseType.Conflict, "Cart is empty.");
            }

            var lines = await this.ResolveOrderLinesAsync(
                request.StoreId,
                cart.Lines,
                request.Snapshot.CurrencyCode,
                cancellationToken);
            if (!lines.Success)
            {
                return OrderPlacementResult.Failed(lines.ResponseType, lines.Message);
            }

            var store = await this.context.CommerceStores
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == request.StoreId, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var order = CreateOrder(request, lines.Lines, store, now);
            this.context.Orders.Add(order);

            foreach (var line in lines.Lines)
            {
                DeductTrackedStock(line);
            }

            CompleteCheckout(request.CheckoutSession, cart, order, now);
            if (request.PaymentAttempt is not null)
            {
                request.PaymentAttempt.OrderId = order.Id;
            }

            return OrderPlacementResult.Succeeded(order);
        }

        private Order CreateOrder(
            OrderPlacementRequest request,
            IReadOnlyList<OrderLineSnapshot> lines,
            CommerceStore? store,
            DateTimeOffset now)
        {
            var snapshot = request.Snapshot;
            var shippingOption = snapshot.ShippingOption ?? ParseShippingOption(request.CheckoutSession.SelectedShippingOptionJson);
            return new Order
            {
                Id = Guid.NewGuid(),
                UserId = string.Empty,
                CustomerId = request.CheckoutSession.CustomerId,
                StoreId = request.StoreId,
                StorePublicId = store?.PublicId,
                StoreKeySnapshot = store?.StoreKey,
                StoreNameSnapshot = store?.Name,
                StoreBaseUrlSnapshot = store?.BaseUrl,
                StoreCompanyNameSnapshot = store?.CompanyName,
                StoreCompanyEmailSnapshot = store?.CompanyEmail,
                StoreCompanyPhoneSnapshot = store?.CompanyPhone,
                StoreCompanyAddressSnapshot = store?.CompanyAddress,
                Reference = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
                OrderStatus = snapshot.OrderStatus,
                PaymentStatus = snapshot.PaymentStatus,
                PaymentMethodKey = snapshot.PaymentMethodKey,
                PaymentAt = snapshot.PaymentAtUtc?.UtcDateTime,
                PaymentMetadataJson = NormalizeNullable(snapshot.PaymentMetadataJson),
                CurrencyCode = snapshot.CurrencyCode,
                TotalAmount = snapshot.TotalAmount,
                SubtotalAmount = request.CheckoutSession.Subtotal,
                ShippingTotalAmount = request.CheckoutSession.ShippingTotal,
                TaxTotalAmount = request.CheckoutSession.TaxTotal,
                DiscountTotalAmount = request.CheckoutSession.DiscountTotal,
                GrandTotalAmount = snapshot.TotalAmount,
                BaseCurrencyCode = snapshot.CurrencySnapshot.BaseCurrencyCode,
                BaseTotalAmount = snapshot.CurrencySnapshot.BaseTotalAmount,
                BaseSubtotalAmount = request.CheckoutSession.BaseSubtotal,
                BaseGrandTotalAmount = snapshot.CurrencySnapshot.BaseTotalAmount ?? request.CheckoutSession.BaseGrandTotal,
                ExchangeRate = snapshot.CurrencySnapshot.ExchangeRate,
                ExchangeRateProviderKey = snapshot.CurrencySnapshot.ExchangeRateProviderKey,
                ExchangeRateSource = snapshot.CurrencySnapshot.ExchangeRateSource,
                ExchangeRateEffectiveAtUtc = snapshot.CurrencySnapshot.ExchangeRateEffectiveAtUtc,
                ExchangeRateExpiresAtUtc = snapshot.CurrencySnapshot.ExchangeRateExpiresAtUtc,
                CustomerName = request.CheckoutSession.CustomerName,
                CustomerEmail = request.CheckoutSession.CustomerEmail,
                BillingAddressSnapshotJson = NormalizeNullable(request.CheckoutSession.BillingAddressSnapshotJson),
                ShippingAddressSnapshotJson = BuildShippingAddressSnapshotJson(request.CheckoutSession),
                ShippingFullName = request.CheckoutSession.ShippingFullName,
                ShippingEmail = request.CheckoutSession.ShippingEmail,
                ShippingPhone = request.CheckoutSession.ShippingPhone,
                ShippingAddress1 = request.CheckoutSession.ShippingAddress1,
                ShippingAddress2 = request.CheckoutSession.ShippingAddress2,
                ShippingCity = request.CheckoutSession.ShippingCity,
                ShippingState = request.CheckoutSession.ShippingState,
                ShippingPostalCode = request.CheckoutSession.ShippingPostalCode,
                ShippingCountryCode = request.CheckoutSession.ShippingCountryCode,
                ShippingStatus = snapshot.ShippingStatus,
                ShippingMethodKey = shippingOption?.Key,
                ShippingProviderSystemName = shippingOption?.ProviderSystemName,
                ShippingMethodCode = shippingOption?.MethodCode,
                ShippingMethodName = shippingOption?.DisplayName,
                ShippingTotal = shippingOption?.Price ?? request.CheckoutSession.ShippingTotal,
                ShippingCurrencyCode = shippingOption?.CurrencyCode ?? snapshot.CurrencyCode,
                ShippingDeliveryEstimateText = shippingOption?.DeliveryEstimateText,
                ShippingMethodSnapshotJson = shippingOption is null
                    ? NormalizeNullable(request.CheckoutSession.SelectedShippingOptionJson)
                    : JsonSerializer.Serialize(shippingOption, JsonOptions),
                CreatedOn = now.UtcDateTime,
                UpdatedAt = now.UtcDateTime,
                Lines = lines.Select(line => new OrderLine
                {
                    Id = Guid.NewGuid(),
                    ProductId = line.Product.Id,
                    ProductName = line.Product.Name,
                    Sku = line.Variant?.Sku ?? line.Product.Sku,
                    Image = line.Product.Image,
                    ProductVariantId = line.Variant?.Id,
                    VariantAttributesJson = line.CartLine.SelectedAttributesJson ?? line.Variant?.AttributesJson,
                    PersonalizationHash = line.CartLine.PersonalizationHash,
                    PersonalizationJson = line.CartLine.PersonalizationJson,
                    ArtworkAssetId = line.CartLine.ArtworkAssetId,
                    ArtworkVersion = line.CartLine.ArtworkVersion,
                    FulfillmentProviderKey = line.CartLine.FulfillmentProviderKey,
                    Quantity = line.CartLine.Quantity,
                    UnitPrice = line.UnitPrice,
                    CurrencyCode = snapshot.CurrencyCode,
                    BaseUnitPrice = line.CartLine.BaseUnitPriceSnapshot,
                    ConvertedUnitPrice = line.UnitPrice,
                    LineTotal = this.moneyRoundingService.RoundLineTotal(line.CartLine.Quantity * line.UnitPrice, snapshot.CurrencyCode),
                    BaseLineTotal = line.CartLine.BaseUnitPriceSnapshot.HasValue && snapshot.CurrencySnapshot.BaseCurrencyCode is not null
                        ? this.moneyRoundingService.RoundLineTotal(line.CartLine.Quantity * line.CartLine.BaseUnitPriceSnapshot.Value, snapshot.CurrencySnapshot.BaseCurrencyCode)
                        : null,
                }).ToList(),
            };
        }

        private async Task<OrderLineResolution> ResolveOrderLinesAsync(
            Guid storeId,
            IEnumerable<CartLine> cartLines,
            string currencyCode,
            CancellationToken cancellationToken)
        {
            var results = new List<OrderLineSnapshot>();

            foreach (var cartLine in cartLines)
            {
                if (cartLine.Quantity < 1)
                {
                    return OrderLineResolution.Failed(ServiceResponseType.ValidationError, "Cart line quantity must be at least 1.");
                }

                var product = await this.context.Products
                    .Include(item => item.Category)
                    .Include(item => item.Variants)
                    .FirstOrDefaultAsync(item => item.Id == cartLine.ProductId, cancellationToken);
                if (product is null)
                {
                    return OrderLineResolution.Failed(ServiceResponseType.Conflict, "Product is not available for this store.");
                }

                var variant = cartLine.ProductVariantId.HasValue
                    ? product.Variants.FirstOrDefault(candidate => candidate.Id == cartLine.ProductVariantId.Value)
                    : null;
                if (cartLine.ProductVariantId.HasValue && variant is null)
                {
                    return OrderLineResolution.Failed(ServiceResponseType.Conflict, "Selected product variant was not found.");
                }

                var sellability = this.sellabilityResolver.Resolve(new ProductSellabilityRequest(
                    storeId,
                    product,
                    variant,
                    cartLine.Quantity,
                    Mode: ProductSellabilityMode.Storefront));
                if (!sellability.Purchasable)
                {
                    return OrderLineResolution.Failed(
                        ResolveOrderLineFailureResponseType(sellability.PurchaseBlockReasons),
                        sellability.PurchaseBlockMessages.FirstOrDefault() ?? "Product cannot be purchased right now.");
                }

                var snapshotCurrency = NormalizeCurrency(cartLine.CurrencyCodeSnapshot);
                if (snapshotCurrency is not null
                    && !string.Equals(snapshotCurrency, currencyCode, StringComparison.Ordinal))
                {
                    return OrderLineResolution.Failed(ServiceResponseType.Conflict, "Cart line currency does not match checkout currency.");
                }

                if (!cartLine.UnitPriceSnapshot.HasValue)
                {
                    return OrderLineResolution.Failed(ServiceResponseType.ValidationError, "Cart line price is invalid.");
                }

                var unitPrice = this.moneyRoundingService.RoundUnitPrice(cartLine.UnitPriceSnapshot.Value, currencyCode);
                if (unitPrice <= 0m)
                {
                    return OrderLineResolution.Failed(ServiceResponseType.ValidationError, "Cart line price is invalid.");
                }

                results.Add(new OrderLineSnapshot(cartLine, product, variant, unitPrice));
            }

            return OrderLineResolution.Succeeded(results);
        }

        private static void CompleteCheckout(
            CheckoutSession checkout,
            CartSession cart,
            Order order,
            DateTimeOffset now)
        {
            checkout.State = CheckoutSessionStates.Completed;
            checkout.CurrentStep = CheckoutSteps.Complete;
            checkout.CheckoutVersion = Math.Max(1, checkout.CheckoutVersion) + 1;
            checkout.OrderId = order.Id;
            checkout.NextAction = "complete";
            checkout.PlacedAtUtc = now;
            checkout.UpdatedAtUtc = now;

            cart.State = CartSessionStates.Ordered;
            cart.ConvertedOrderId = order.Id;
            cart.ExpiresAtUtc = now;
            cart.LastActivityAtUtc = now;
            cart.UpdatedAtUtc = now;
        }

        private static string BuildShippingAddressSnapshotJson(CheckoutSession checkout)
        {
            return JsonSerializer.Serialize(
                new StorefrontCheckoutShippingAddressDto(
                    checkout.ShippingFullName,
                    checkout.ShippingEmail,
                    checkout.ShippingPhone,
                    checkout.ShippingAddress1,
                    checkout.ShippingAddress2,
                    checkout.ShippingCity,
                    checkout.ShippingState,
                    checkout.ShippingPostalCode,
                    checkout.ShippingCountryCode),
                JsonOptions);
        }

        private static StorefrontCheckoutShippingOption? ParseShippingOption(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<StorefrontCheckoutShippingOption>(json, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static void DeductTrackedStock(OrderLineSnapshot line)
        {
            if (!string.IsNullOrWhiteSpace(line.CartLine.FulfillmentProviderKey))
            {
                return;
            }

            if (!line.Product.ManageStock)
            {
                return;
            }

            if (line.Variant is not null)
            {
                line.Variant.Stock -= line.CartLine.Quantity;
                return;
            }

            line.Product.Quantity -= line.CartLine.Quantity;
        }

        private static ServiceResponseType ResolveOrderLineFailureResponseType(IReadOnlyList<string> reasons)
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
            var normalized = NormalizeNullable(value)?.ToUpperInvariant();
            return normalized is { Length: 3 } ? normalized : null;
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private sealed record OrderLineSnapshot(
            CartLine CartLine,
            Product Product,
            ProductVariant? Variant,
            decimal UnitPrice);

        private sealed record OrderLineResolution(
            bool Success,
            ServiceResponseType ResponseType,
            string Message,
            IReadOnlyList<OrderLineSnapshot> Lines)
        {
            public static OrderLineResolution Succeeded(IReadOnlyList<OrderLineSnapshot> lines)
            {
                return new OrderLineResolution(true, ServiceResponseType.Success, "Order lines resolved.", lines);
            }

            public static OrderLineResolution Failed(ServiceResponseType responseType, string message)
            {
                return new OrderLineResolution(false, responseType, message, []);
            }
        }
    }
}
