namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Security.Cryptography;
    using System.Text;

    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class PaymentAttemptService : IPaymentAttemptService
    {
        private static readonly IReadOnlyDictionary<string, ISet<string>> AllowedTransitions =
            new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                [PaymentAttemptStates.Created] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    PaymentAttemptStates.RequiresAction,
                    PaymentAttemptStates.Authorized,
                    PaymentAttemptStates.Captured,
                    PaymentAttemptStates.Failed,
                    PaymentAttemptStates.Cancelled,
                    PaymentAttemptStates.Expired,
                },
                [PaymentAttemptStates.RequiresAction] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    PaymentAttemptStates.Authorized,
                    PaymentAttemptStates.Captured,
                    PaymentAttemptStates.Failed,
                    PaymentAttemptStates.Cancelled,
                    PaymentAttemptStates.Expired,
                },
                [PaymentAttemptStates.Authorized] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    PaymentAttemptStates.Captured,
                    PaymentAttemptStates.Failed,
                    PaymentAttemptStates.Cancelled,
                    PaymentAttemptStates.Expired,
                },
                [PaymentAttemptStates.Captured] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                [PaymentAttemptStates.Failed] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                [PaymentAttemptStates.Cancelled] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                [PaymentAttemptStates.Expired] = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            };

        private readonly CommerceNodeDbContext context;

        public PaymentAttemptService(CommerceNodeDbContext context)
        {
            this.context = context;
        }

        public async Task<ServiceResponse<PaymentAttemptDto>> GetAsync(
            Guid storeId,
            Guid paymentAttemptId,
            CancellationToken cancellationToken = default)
        {
            if (storeId == Guid.Empty)
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.ValidationError, "Store is required.");
            }

            if (paymentAttemptId == Guid.Empty)
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.ValidationError, "Payment attempt is required.");
            }

            var attempt = await this.context.PaymentAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    candidate => candidate.StoreId == storeId
                        && candidate.PublicId == paymentAttemptId,
                    cancellationToken);

            return attempt is null
                ? Failed<PaymentAttemptDto>(ServiceResponseType.NotFound, "Payment attempt was not found.")
                : Succeeded("Payment attempt loaded.", ToDto(attempt));
        }

        public async Task<ServiceResponse<PaymentAttemptDto>> CreateAsync(
            CreatePaymentAttemptRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = ValidateCreateRequest(request);
            if (validation is not null)
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.ValidationError, validation);
            }

            var idempotencyKey = NormalizeRequired(request.IdempotencyKey)!;
            var existing = await this.FindAttemptByIdempotencyKeyAsync(request.StoreId, idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                return Succeeded("Payment attempt already exists.", existing);
            }

            var now = DateTimeOffset.UtcNow;
            var attempt = new PaymentAttempt
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = request.StoreId,
                CheckoutSessionId = request.CheckoutSessionId,
                OrderId = request.OrderId,
                PaymentMethodKey = NormalizeKey(request.PaymentMethodKey)!,
                ProviderKey = NormalizeKey(request.ProviderKey)!,
                State = PaymentAttemptStates.Created,
                Amount = request.Amount,
                CurrencyCode = NormalizeCurrency(request.CurrencyCode)!,
                BaseCurrencyCode = NormalizeCurrency(request.BaseCurrencyCode),
                BaseAmount = request.BaseAmount,
                ExchangeRate = request.ExchangeRate,
                ExchangeRateProviderKey = NormalizeKey(request.ExchangeRateProviderKey),
                ExchangeRateSource = NormalizeNullable(request.ExchangeRateSource),
                ExchangeRateEffectiveAtUtc = request.ExchangeRateEffectiveAtUtc,
                ExchangeRateExpiresAtUtc = request.ExchangeRateExpiresAtUtc,
                IdempotencyKey = idempotencyKey,
                MetadataJson = NormalizeNullable(request.MetadataJson),
                ExpiresAtUtc = request.ExpiresAtUtc ?? now.AddMinutes(30),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            this.context.PaymentAttempts.Add(attempt);
            try
            {
                await this.context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                var duplicate = await this.FindAttemptByIdempotencyKeyAsync(request.StoreId, idempotencyKey, cancellationToken);
                if (duplicate is not null)
                {
                    return Succeeded("Payment attempt already exists.", duplicate);
                }

                throw;
            }

            return Succeeded("Payment attempt created.", ToDto(attempt));
        }

        public async Task<ServiceResponse<PaymentAttemptDto>> TransitionAsync(
            TransitionPaymentAttemptRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.StoreId == Guid.Empty)
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.ValidationError, "Store is required.");
            }

            if (request.PaymentAttemptId == Guid.Empty)
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.ValidationError, "Payment attempt is required.");
            }

            var newState = NormalizeKey(request.NewState);
            if (newState is null || !AllowedTransitions.ContainsKey(newState))
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.ValidationError, "Payment attempt state is invalid.");
            }

            var attempt = await this.context.PaymentAttempts
                .FirstOrDefaultAsync(
                    candidate => candidate.StoreId == request.StoreId
                        && candidate.PublicId == request.PaymentAttemptId,
                    cancellationToken);
            if (attempt is null)
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.NotFound, "Payment attempt was not found.");
            }

            if (string.Equals(attempt.State, newState, StringComparison.OrdinalIgnoreCase))
            {
                return Succeeded("Payment attempt already has the requested state.", ToDto(attempt));
            }

            if (!AllowedTransitions.TryGetValue(attempt.State, out var nextStates) || !nextStates.Contains(newState))
            {
                return Failed<PaymentAttemptDto>(ServiceResponseType.Conflict, "Payment attempt state transition is not allowed.");
            }

            if (string.Equals(newState, PaymentAttemptStates.Captured, StringComparison.OrdinalIgnoreCase)
                && !attempt.OrderId.HasValue)
            {
                var orderResult = await this.CreateCapturedOrderAsync(attempt, request.MetadataJson, cancellationToken);
                if (!orderResult.Success)
                {
                    return Failed<PaymentAttemptDto>(orderResult.ResponseType, orderResult.Message);
                }
            }

            attempt.State = newState;
            attempt.ProviderReference = NormalizeNullable(request.ProviderReference) ?? attempt.ProviderReference;
            attempt.ProviderSessionId = NormalizeNullable(request.ProviderSessionId) ?? attempt.ProviderSessionId;
            attempt.NextActionType = NormalizeNullable(request.NextActionType);
            attempt.NextActionUrl = NormalizeNullable(request.NextActionUrl);
            attempt.FailureCode = NormalizeNullable(request.FailureCode);
            attempt.FailureMessage = NormalizeNullable(request.FailureMessage);
            attempt.MetadataJson = NormalizeNullable(request.MetadataJson) ?? attempt.MetadataJson;
            attempt.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            return Succeeded("Payment attempt updated.", ToDto(attempt));
        }

        private async Task<OrderCreationResult> CreateCapturedOrderAsync(
            PaymentAttempt attempt,
            string? paymentMetadataJson,
            CancellationToken cancellationToken)
        {
            var checkout = await this.context.CheckoutSessions
                .Include(session => session.CartSession!)
                    .ThenInclude(cart => cart.Lines)
                .FirstOrDefaultAsync(
                    session => session.Id == attempt.CheckoutSessionId
                        && session.StoreId == attempt.StoreId,
                    cancellationToken);
            if (checkout?.CartSession is null)
            {
                return OrderCreationResult.Failed(ServiceResponseType.NotFound, "Checkout session was not found.");
            }

            if (checkout.CartSession.Lines.Count == 0)
            {
                return OrderCreationResult.Failed(ServiceResponseType.Conflict, "Cart is empty.");
            }

            var lines = new List<OrderLineSnapshot>();
            foreach (var cartLine in checkout.CartSession.Lines)
            {
                var product = await this.context.Products
                    .Include(item => item.Category)
                    .Include(item => item.Variants)
                    .FirstOrDefaultAsync(item => item.Id == cartLine.ProductId, cancellationToken);
                if (product is null || !IsStorefrontAvailable(product, attempt.StoreId))
                {
                    return OrderCreationResult.Failed(ServiceResponseType.Conflict, "Product is not available for this store.");
                }

                var variant = cartLine.ProductVariantId.HasValue
                    ? product.Variants.FirstOrDefault(candidate => candidate.Id == cartLine.ProductVariantId.Value)
                    : null;
                if (cartLine.ProductVariantId.HasValue && variant is null)
                {
                    return OrderCreationResult.Failed(ServiceResponseType.Conflict, "Selected product variant was not found.");
                }

                if ((variant?.Stock ?? product.Quantity) < cartLine.Quantity)
                {
                    return OrderCreationResult.Failed(ServiceResponseType.Conflict, "One or more cart items are out of stock.");
                }

                var unitPrice = cartLine.UnitPriceSnapshot ?? variant?.Price ?? product.Price;
                lines.Add(new OrderLineSnapshot(cartLine, product, variant, unitPrice));
            }

            var now = DateTimeOffset.UtcNow;
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = string.Empty,
                CustomerId = checkout.CustomerId,
                StoreId = attempt.StoreId,
                Reference = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
                OrderStatus = OrderStatuses.Processing,
                PaymentStatus = PaymentStatuses.Paid,
                PaymentMethodKey = attempt.PaymentMethodKey,
                PaymentAt = now.UtcDateTime,
                PaymentMetadataJson = NormalizeNullable(paymentMetadataJson) ?? attempt.MetadataJson,
                CurrencyCode = attempt.CurrencyCode,
                TotalAmount = attempt.Amount,
                BaseCurrencyCode = attempt.BaseCurrencyCode,
                BaseTotalAmount = attempt.BaseAmount,
                ExchangeRate = attempt.ExchangeRate,
                ExchangeRateProviderKey = attempt.ExchangeRateProviderKey,
                ExchangeRateSource = attempt.ExchangeRateSource,
                ExchangeRateEffectiveAtUtc = attempt.ExchangeRateEffectiveAtUtc,
                ExchangeRateExpiresAtUtc = attempt.ExchangeRateExpiresAtUtc,
                CustomerName = checkout.CustomerName,
                CustomerEmail = checkout.CustomerEmail,
                ShippingFullName = checkout.ShippingFullName,
                ShippingEmail = checkout.ShippingEmail,
                ShippingPhone = checkout.ShippingPhone,
                ShippingAddress1 = checkout.ShippingAddress1,
                ShippingAddress2 = checkout.ShippingAddress2,
                ShippingCity = checkout.ShippingCity,
                ShippingState = checkout.ShippingState,
                ShippingPostalCode = checkout.ShippingPostalCode,
                ShippingCountryCode = checkout.ShippingCountryCode,
                ShippingStatus = ShippingStatuses.NotYetShipped,
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
                    CurrencyCode = attempt.CurrencyCode,
                    BaseUnitPrice = line.CartLine.BaseUnitPriceSnapshot,
                    ConvertedUnitPrice = line.UnitPrice,
                    LineTotal = line.UnitPrice * line.CartLine.Quantity,
                    BaseLineTotal = line.CartLine.BaseUnitPriceSnapshot.HasValue
                        ? line.CartLine.BaseUnitPriceSnapshot.Value * line.CartLine.Quantity
                        : null,
                }).ToList(),
            };

            foreach (var line in lines)
            {
                DeductTrackedStock(line);
            }

            this.context.Orders.Add(order);
            checkout.State = CheckoutSessionStates.Completed;
            checkout.OrderId = order.Id;
            checkout.NextAction = "complete";
            checkout.PlacedAtUtc = now;
            checkout.UpdatedAtUtc = now;
            checkout.CartSession.State = CartSessionStates.Ordered;
            checkout.CartSession.ConvertedOrderId = order.Id;
            checkout.CartSession.ExpiresAtUtc = now;
            checkout.CartSession.LastActivityAtUtc = now;
            checkout.CartSession.UpdatedAtUtc = now;
            attempt.OrderId = order.Id;

            return OrderCreationResult.Succeeded();
        }

        public async Task<ServiceResponse<PaymentProviderEventDto>> RecordProviderEventAsync(
            RecordPaymentProviderEventRequest request,
            CancellationToken cancellationToken = default)
        {
            var validation = ValidateProviderEventRequest(request);
            if (validation is not null)
            {
                return Failed<PaymentProviderEventDto>(ServiceResponseType.ValidationError, validation);
            }

            var providerKey = NormalizeKey(request.ProviderKey)!;
            var eventId = NormalizeNullable(request.EventId);
            Guid? paymentAttemptInternalId = null;
            if (request.PaymentAttemptId.HasValue)
            {
                var attempt = await this.context.PaymentAttempts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        candidate => candidate.StoreId == request.StoreId
                            && candidate.PublicId == request.PaymentAttemptId.Value,
                        cancellationToken);
                if (attempt is null)
                {
                    return Failed<PaymentProviderEventDto>(ServiceResponseType.NotFound, "Payment attempt was not found.");
                }

                paymentAttemptInternalId = attempt.Id;
            }

            if (eventId is not null)
            {
                var existing = await this.context.PaymentProviderEvents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        candidate => candidate.ProviderKey == providerKey
                            && candidate.EventId == eventId,
                        cancellationToken);
                if (existing is not null)
                {
                    return Succeeded("Payment provider event already recorded.", ToDto(existing, isDuplicate: true));
                }
            }

            var paymentEvent = new PaymentProviderEvent
            {
                Id = Guid.NewGuid(),
                StoreId = request.StoreId,
                PaymentAttemptId = paymentAttemptInternalId,
                ProviderKey = providerKey,
                EventId = eventId,
                EventType = NormalizeRequired(request.EventType)!,
                PayloadHash = ComputeSha256(request.PayloadJson),
                PayloadJson = request.PayloadJson,
                ProcessedAtUtc = request.ProcessedAtUtc,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };

            this.context.PaymentProviderEvents.Add(paymentEvent);
            try
            {
                await this.context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                if (eventId is not null)
                {
                    var duplicate = await this.context.PaymentProviderEvents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            candidate => candidate.ProviderKey == providerKey
                                && candidate.EventId == eventId,
                            cancellationToken);
                    if (duplicate is not null)
                    {
                        return Succeeded("Payment provider event already recorded.", ToDto(duplicate, isDuplicate: true));
                    }
                }

                throw;
            }

            return Succeeded("Payment provider event recorded.", ToDto(paymentEvent, isDuplicate: false));
        }

        private async Task<PaymentAttemptDto?> FindAttemptByIdempotencyKeyAsync(
            Guid storeId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var attempt = await this.context.PaymentAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    candidate => candidate.StoreId == storeId
                        && candidate.IdempotencyKey == idempotencyKey,
                    cancellationToken);

            return attempt is null ? null : ToDto(attempt);
        }

        private static string? ValidateCreateRequest(CreatePaymentAttemptRequest request)
        {
            if (request.StoreId == Guid.Empty)
            {
                return "Store is required.";
            }

            if (request.CheckoutSessionId == Guid.Empty)
            {
                return "Checkout session is required.";
            }

            if (NormalizeKey(request.PaymentMethodKey) is null)
            {
                return "Payment method is required.";
            }

            if (NormalizeKey(request.ProviderKey) is null)
            {
                return "Payment provider is required.";
            }

            if (request.Amount <= 0m)
            {
                return "Payment amount must be greater than zero.";
            }

            if (NormalizeCurrency(request.CurrencyCode) is null)
            {
                return "Currency code is required.";
            }

            if (NormalizeRequired(request.IdempotencyKey) is null)
            {
                return "Idempotency key is required.";
            }

            return null;
        }

        private static string? ValidateProviderEventRequest(RecordPaymentProviderEventRequest request)
        {
            if (request.StoreId == Guid.Empty)
            {
                return "Store is required.";
            }

            if (NormalizeKey(request.ProviderKey) is null)
            {
                return "Payment provider is required.";
            }

            if (NormalizeRequired(request.EventType) is null)
            {
                return "Provider event type is required.";
            }

            if (NormalizeNullable(request.PayloadJson) is null)
            {
                return "Provider event payload is required.";
            }

            return null;
        }

        private static string? NormalizeRequired(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized is { Length: <= 128 } ? normalized : null;
        }

        private static string? NormalizeKey(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized is { Length: <= 64 } ? normalized.ToLowerInvariant() : null;
        }

        private static string? NormalizeCurrency(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized is { Length: 3 } ? normalized.ToUpperInvariant() : null;
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string ComputeSha256(string value)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static bool IsStorefrontAvailable(Product product, Guid storeId)
        {
            return product.StoreId == storeId
                && product.ArchivedAt is null
                && product.IsPublished
                && product.PublishedOn is not null
                && !string.IsNullOrWhiteSpace(product.Slug)
                && product.Category is not null
                && product.Category.StoreId == product.StoreId
                && product.Category.ArchivedAt is null
                && product.Category.IsPublished;
        }

        private static void DeductTrackedStock(OrderLineSnapshot line)
        {
            if (!string.IsNullOrWhiteSpace(line.CartLine.FulfillmentProviderKey))
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

        private static PaymentAttemptDto ToDto(PaymentAttempt attempt)
        {
            return new PaymentAttemptDto(
                attempt.PublicId,
                attempt.StoreId,
                attempt.CheckoutSessionId,
                attempt.OrderId,
                attempt.PaymentMethodKey,
                attempt.ProviderKey,
                attempt.State,
                attempt.Amount,
                attempt.CurrencyCode,
                attempt.IdempotencyKey,
                attempt.ProviderReference,
                attempt.ProviderSessionId,
                attempt.NextActionType,
                attempt.NextActionUrl,
                attempt.FailureCode,
                attempt.FailureMessage,
                attempt.BaseCurrencyCode,
                attempt.BaseAmount,
                attempt.ExchangeRate,
                attempt.ExchangeRateProviderKey,
                attempt.ExchangeRateSource,
                attempt.ExchangeRateEffectiveAtUtc,
                attempt.ExchangeRateExpiresAtUtc,
                attempt.ExpiresAtUtc,
                attempt.CreatedAtUtc,
                attempt.UpdatedAtUtc);
        }

        private static PaymentProviderEventDto ToDto(PaymentProviderEvent paymentEvent, bool isDuplicate)
        {
            return new PaymentProviderEventDto(
                paymentEvent.Id,
                paymentEvent.StoreId,
                paymentEvent.PaymentAttemptId,
                paymentEvent.ProviderKey,
                paymentEvent.EventId,
                paymentEvent.EventType,
                paymentEvent.PayloadHash,
                isDuplicate,
                paymentEvent.ProcessedAtUtc,
                paymentEvent.CreatedAtUtc);
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

        private sealed record OrderLineSnapshot(
            CartLine CartLine,
            Product Product,
            ProductVariant? Variant,
            decimal UnitPrice);

        private sealed record OrderCreationResult(
            bool Success,
            ServiceResponseType ResponseType,
            string Message)
        {
            public static OrderCreationResult Succeeded()
            {
                return new OrderCreationResult(true, ServiceResponseType.Success, "Order created.");
            }

            public static OrderCreationResult Failed(ServiceResponseType responseType, string message)
            {
                return new OrderCreationResult(false, responseType, message);
            }
        }
    }
}
