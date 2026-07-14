namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Net.Mail;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class StorefrontCheckoutService : IStorefrontCheckoutService
    {
        private const string DefaultCurrencyCode = "USD";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly CommerceNodeDbContext context;
        private readonly IStorefrontCartService cartService;
        private readonly IStorefrontCustomerService customerService;
        private readonly IPaymentHandlerResolver paymentHandlerResolver;
        private readonly IStorefrontPaymentProviderResolver paymentProviderResolver;

        public StorefrontCheckoutService(
            CommerceNodeDbContext context,
            IStorefrontCartService cartService,
            IStorefrontCustomerService customerService,
            IPaymentHandlerResolver paymentHandlerResolver,
            IStorefrontPaymentProviderResolver paymentProviderResolver)
        {
            this.context = context;
            this.cartService = cartService;
            this.customerService = customerService;
            this.paymentHandlerResolver = paymentHandlerResolver;
            this.paymentProviderResolver = paymentProviderResolver;
        }

        public async Task<ServiceResponse<StorefrontCheckoutPreviewResult>> PreviewAsync(
            StorefrontCheckoutPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.StoreId == Guid.Empty)
            {
                return Failed(ServiceResponseType.ValidationError, "Store is required.");
            }

            if (string.IsNullOrWhiteSpace(request.CartToken))
            {
                return Failed(ServiceResponseType.ValidationError, "Cart token is required.");
            }

            var cartResult = await this.cartService.GetAsync(request.StoreId, request.CartToken, cancellationToken);
            if (!cartResult.Success || cartResult.Payload is null)
            {
                return Failed(cartResult.ResponseType, cartResult.Message ?? "Cart could not be resolved.");
            }

            var cart = cartResult.Payload;
            if (request.ExpectedCartVersion != cart.Version)
            {
                return Failed(ServiceResponseType.Conflict, "Cart version is stale.");
            }

            var issues = ValidateCheckoutFields(request).ToList();
            if (!string.Equals(cart.State, CartSessionStates.Active, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new StorefrontCheckoutValidationIssue("cart.inactive", "Cart is not active.", "cart"));
            }

            if (cart.Lines.Count == 0)
            {
                issues.Add(new StorefrontCheckoutValidationIssue("cart.empty", "Cart is empty.", "cart"));
            }

            var cartValidation = await this.cartService.ValidateAsync(request.StoreId, request.CartToken, cancellationToken);
            if (!cartValidation.Success || cartValidation.Payload is null)
            {
                issues.Add(new StorefrontCheckoutValidationIssue(
                    "cart.validation_failed",
                    cartValidation.Message ?? "Cart could not be validated.",
                    "cart"));
            }
            else
            {
                issues.AddRange(cartValidation.Payload.Issues.Select(issue =>
                    new StorefrontCheckoutValidationIssue(
                        issue.Code,
                        issue.Message,
                        Field: "cart.lines",
                        LineId: issue.LineId,
                        ProductId: issue.ProductId)));
            }

            var paymentMethodKey = NormalizeKey(request.PaymentMethodKey);
            if (!await this.IsPaymentMethodEnabledAsync(request.StoreId, paymentMethodKey, cancellationToken))
            {
                issues.Add(new StorefrontCheckoutValidationIssue(
                    "payment.method_unavailable",
                    "Payment method is not available.",
                    "paymentMethodKey"));
            }

            StorefrontCustomerProfile? customer = null;
            if (!issues.Any(issue => issue.Field is "customerEmail" or "customerName"))
            {
                var customerResult = await this.customerService.ResolveOrCreateAsync(
                    new StorefrontCustomerResolutionRequest(
                        request.StoreId,
                        request.CustomerEmail,
                        request.CustomerName,
                        request.ShippingAddress.Phone),
                    cancellationToken);
                if (customerResult.Success)
                {
                    customer = customerResult.Payload;
                }
                else
                {
                    issues.Add(new StorefrontCheckoutValidationIssue(
                        "customer.resolve_failed",
                        customerResult.Message ?? "Customer could not be resolved.",
                        "customerEmail"));
                }
            }

            var currencyCode = NormalizeCurrency(cart.Lines.FirstOrDefault()?.CurrencyCodeSnapshot) ?? DefaultCurrencyCode;
            var lines = cart.Lines.Select(line =>
            {
                var unitPrice = line.UnitPriceSnapshot ?? 0m;
                return new StorefrontCheckoutLineSummary(
                    line.Id,
                    line.ProductId,
                    line.ProductVariantId,
                    Math.Max(0, line.Quantity),
                    unitPrice,
                    unitPrice * Math.Max(0, line.Quantity),
                    NormalizeCurrency(line.CurrencyCodeSnapshot) ?? currencyCode);
            }).ToArray();

            var subtotal = lines.Sum(line => line.LineTotal);
            var isValid = issues.Count == 0;
            var now = DateTimeOffset.UtcNow;
            var session = new CheckoutSession
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = request.StoreId,
                CartSessionId = cart.Id,
                CustomerId = customer?.Id,
                State = isValid ? CheckoutSessionStates.Ready : CheckoutSessionStates.Draft,
                CartVersion = cart.Version,
                CustomerEmail = NormalizeNullable(request.CustomerEmail) ?? string.Empty,
                CustomerName = NormalizeNullable(request.CustomerName) ?? string.Empty,
                CustomerPhone = NormalizeNullable(request.ShippingAddress.Phone),
                ShippingFullName = NormalizeNullable(request.ShippingAddress.FullName) ?? string.Empty,
                ShippingEmail = NormalizeNullable(request.ShippingAddress.Email) ?? string.Empty,
                ShippingPhone = NormalizeNullable(request.ShippingAddress.Phone),
                ShippingAddress1 = NormalizeNullable(request.ShippingAddress.Address1) ?? string.Empty,
                ShippingAddress2 = NormalizeNullable(request.ShippingAddress.Address2),
                ShippingCity = NormalizeNullable(request.ShippingAddress.City) ?? string.Empty,
                ShippingState = NormalizeNullable(request.ShippingAddress.State),
                ShippingPostalCode = NormalizeNullable(request.ShippingAddress.PostalCode) ?? string.Empty,
                ShippingCountryCode = NormalizeNullable(request.ShippingAddress.CountryCode)?.ToUpperInvariant() ?? string.Empty,
                PaymentMethodKey = paymentMethodKey,
                Subtotal = subtotal,
                ShippingTotal = 0m,
                TaxTotal = 0m,
                DiscountTotal = 0m,
                GrandTotal = subtotal,
                CurrencyCode = currencyCode,
                ValidationIssuesJson = issues.Count == 0 ? null : JsonSerializer.Serialize(issues, JsonOptions),
                NextAction = isValid ? "placeOrder" : "review",
                ExpiresAtUtc = now.AddHours(1),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            this.context.CheckoutSessions.Add(session);
            await this.context.SaveChangesAsync(cancellationToken);

            return Succeeded(
                isValid ? "Checkout preview is ready." : "Checkout preview has validation issues.",
                new StorefrontCheckoutPreviewResult(
                    session.PublicId,
                    cart.PublicId,
                    cart.Version,
                    session.State,
                    isValid,
                    session.NextAction,
                    session.CustomerEmail,
                    session.CustomerName,
                    session.PaymentMethodKey,
                    session.Subtotal,
                    session.ShippingTotal,
                    session.TaxTotal,
                    session.DiscountTotal,
                    session.GrandTotal,
                    session.CurrencyCode,
                    session.ExpiresAtUtc,
                    lines,
                issues));
        }

        public async Task<ServiceResponse<StorefrontPlaceOrderResult>> PlaceOrderAsync(
            StorefrontPlaceOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.StoreId == Guid.Empty)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.ValidationError, "Store is required.");
            }

            if (request.CheckoutSessionId == Guid.Empty)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.ValidationError, "Checkout session is required.");
            }

            if (request.ExpectedCartVersion < 1)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.ValidationError, "Cart version is required.");
            }

            var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey);
            if (idempotencyKey is null)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.ValidationError, "Idempotency key is required.");
            }

            var existing = await this.FindCompletedByIdempotencyKeyAsync(request.StoreId, idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                return Succeeded("Order already placed.", existing);
            }

            var session = await this.context.CheckoutSessions
                .Include(checkout => checkout.CartSession!)
                    .ThenInclude(cart => cart.Lines)
                .Include(checkout => checkout.Order)
                .FirstOrDefaultAsync(
                    checkout => checkout.StoreId == request.StoreId
                        && checkout.PublicId == request.CheckoutSessionId,
                    cancellationToken);

            if (session is null)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.NotFound, "Checkout session was not found.");
            }

            if (session.OrderId.HasValue
                && string.Equals(session.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)
                && session.Order is not null)
            {
                var paymentAttemptId = await this.context.PaymentAttempts
                    .AsNoTracking()
                    .Where(attempt => attempt.StoreId == session.StoreId
                        && attempt.IdempotencyKey == idempotencyKey)
                    .Select(attempt => attempt.PublicId)
                    .FirstOrDefaultAsync(cancellationToken);
                return Succeeded("Order already placed.", ToPlaceOrderResult(session, session.Order, paymentAttemptId, idempotencyKey));
            }

            if (string.Equals(session.IdempotencyKey, idempotencyKey, StringComparison.Ordinal)
                && string.Equals(session.State, CheckoutSessionStates.OrderPending, StringComparison.OrdinalIgnoreCase))
            {
                var existingAttempt = await this.context.PaymentAttempts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        attempt => attempt.StoreId == session.StoreId
                            && attempt.IdempotencyKey == idempotencyKey,
                        cancellationToken);
                if (existingAttempt is not null)
                {
                    return Succeeded("Payment session already exists.", ToOnlinePlaceOrderResult(session, existingAttempt, idempotencyKey));
                }
            }

            if (!string.Equals(session.State, CheckoutSessionStates.Ready, StringComparison.OrdinalIgnoreCase))
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Checkout session is not ready for order placement.");
            }

            if (session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                session.State = CheckoutSessionStates.Expired;
                session.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await this.context.SaveChangesAsync(cancellationToken);
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Checkout session has expired.");
            }

            var cart = session.CartSession;
            if (cart is null)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.NotFound, "Cart session was not found.");
            }

            if (!string.Equals(cart.State, CartSessionStates.Active, StringComparison.OrdinalIgnoreCase))
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Cart is not active.");
            }

            if (cart.Version != request.ExpectedCartVersion || cart.Version != session.CartVersion)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Cart version is stale.");
            }

            if (cart.Lines.Count == 0)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.ValidationError, "Cart is empty.");
            }

            var paymentMethodKey = NormalizeKey(session.PaymentMethodKey);
            if (!await this.IsPaymentMethodEnabledAsync(request.StoreId, paymentMethodKey, cancellationToken))
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Payment method is not available.");
            }

            var isCod = string.Equals(paymentMethodKey, PaymentMethodKeys.Cod, StringComparison.OrdinalIgnoreCase);
            var isStripe = string.Equals(paymentMethodKey, PaymentMethodKeys.Stripe, StringComparison.OrdinalIgnoreCase);
            if (!isCod && !isStripe)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Payment provider is not available for order placement.");
            }

            var lineResolution = await this.ResolveOrderLinesAsync(request.StoreId, cart.Lines, cancellationToken);
            if (!lineResolution.Success)
            {
                return Failed<StorefrontPlaceOrderResult>(lineResolution.ResponseType, lineResolution.Message);
            }

            var lines = lineResolution.Lines;
            var totalAmount = lines.Sum(line => line.CartLine.Quantity * line.UnitPrice);
            if (totalAmount <= 0m)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.ValidationError, "Cart total must be greater than zero.");
            }

            var currencyCode = NormalizeCurrency(session.CurrencyCode) ?? DefaultCurrencyCode;
            if (isStripe)
            {
                return await this.CreateOnlinePaymentSessionAsync(
                    request,
                    session,
                    cart,
                    lines,
                    totalAmount,
                    currencyCode,
                    idempotencyKey,
                    cancellationToken);
            }

            PaymentHandlerResult paymentResult;
            try
            {
                var handler = this.paymentHandlerResolver.Resolve(paymentMethodKey);
                paymentResult = await handler.ProcessAsync(
                    new PaymentHandlerContext(
                        request.StoreId,
                        Guid.Empty,
                        paymentMethodKey,
                        totalAmount,
                        currencyCode,
                        JsonSerializer.Serialize(new
                        {
                            handler = paymentMethodKey,
                            checkoutSessionId = session.PublicId,
                            idempotencyKey,
                            mode = "test",
                            processedAt = DateTimeOffset.UtcNow,
                        }, JsonOptions)),
                    cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, ex.Message);
            }

            if (!paymentResult.Success)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, paymentResult.Message);
            }

            var now = DateTimeOffset.UtcNow;
            var paymentAttempt = new PaymentAttempt
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = request.StoreId,
                CheckoutSessionId = session.Id,
                PaymentMethodKey = paymentMethodKey,
                ProviderKey = paymentMethodKey,
                State = PaymentAttemptStates.Created,
                Amount = totalAmount,
                CurrencyCode = currencyCode,
                IdempotencyKey = idempotencyKey,
                ExpiresAtUtc = now.AddMinutes(30),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = string.Empty,
                CustomerId = session.CustomerId,
                StoreId = request.StoreId,
                Reference = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
                OrderStatus = OrderStatuses.Processing,
                PaymentStatus = paymentResult.PaymentStatus,
                PaymentMethodKey = paymentMethodKey,
                PaymentAt = paymentResult.PaymentAt,
                PaymentMetadataJson = paymentResult.MetadataJson,
                CurrencyCode = currencyCode,
                TotalAmount = totalAmount,
                CustomerName = session.CustomerName,
                CustomerEmail = session.CustomerEmail,
                ShippingFullName = session.ShippingFullName,
                ShippingEmail = session.ShippingEmail,
                ShippingPhone = session.ShippingPhone,
                ShippingAddress1 = session.ShippingAddress1,
                ShippingAddress2 = session.ShippingAddress2,
                ShippingCity = session.ShippingCity,
                ShippingState = session.ShippingState,
                ShippingPostalCode = session.ShippingPostalCode,
                ShippingCountryCode = session.ShippingCountryCode,
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
                }).ToList(),
            };

            await using var transaction = this.context.Database.IsRelational()
                ? await this.context.Database.BeginTransactionAsync(cancellationToken)
                : null;

            this.context.Orders.Add(order);
            this.context.PaymentAttempts.Add(paymentAttempt);
            foreach (var line in lines)
            {
                DeductTrackedStock(line);
            }

            cart.State = CartSessionStates.Ordered;
            cart.ConvertedOrderId = order.Id;
            cart.ExpiresAtUtc = now;
            cart.LastActivityAtUtc = now;
            cart.UpdatedAtUtc = now;

            session.State = CheckoutSessionStates.Completed;
            session.OrderId = order.Id;
            session.IdempotencyKey = idempotencyKey;
            session.NextAction = "complete";
            session.PlacedAtUtc = now;
            session.UpdatedAtUtc = now;

            paymentAttempt.OrderId = order.Id;
            paymentAttempt.State = PaymentAttemptStates.Captured;
            paymentAttempt.ProviderReference = paymentResult.ProviderReference;
            paymentAttempt.MetadataJson = paymentResult.MetadataJson;
            paymentAttempt.UpdatedAtUtc = now;

            try
            {
                await this.context.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
            }
            catch (DbUpdateException)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                var duplicate = await this.FindCompletedByIdempotencyKeyAsync(request.StoreId, idempotencyKey, cancellationToken);
                if (duplicate is not null)
                {
                    return Succeeded("Order already placed.", duplicate);
                }

                throw;
            }

            return Succeeded("Order placed successfully.", ToPlaceOrderResult(session, order, paymentAttempt.PublicId, idempotencyKey));
        }

        private async Task<ServiceResponse<StorefrontPlaceOrderResult>> CreateOnlinePaymentSessionAsync(
            StorefrontPlaceOrderRequest request,
            CheckoutSession session,
            CartSession cart,
            IReadOnlyList<OrderLineSnapshot> lines,
            decimal totalAmount,
            string currencyCode,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var existingAttempt = await this.context.PaymentAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    attempt => attempt.StoreId == request.StoreId
                        && attempt.IdempotencyKey == idempotencyKey,
                    cancellationToken);
            if (existingAttempt is not null)
            {
                return Succeeded("Payment session already exists.", ToOnlinePlaceOrderResult(session, existingAttempt, idempotencyKey));
            }

            var now = DateTimeOffset.UtcNow;
            var paymentAttempt = new PaymentAttempt
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = request.StoreId,
                CheckoutSessionId = session.Id,
                PaymentMethodKey = session.PaymentMethodKey,
                ProviderKey = session.PaymentMethodKey,
                State = PaymentAttemptStates.Created,
                Amount = totalAmount,
                CurrencyCode = currencyCode,
                IdempotencyKey = idempotencyKey,
                ExpiresAtUtc = now.AddMinutes(30),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            this.context.PaymentAttempts.Add(paymentAttempt);
            try
            {
                await this.context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                var duplicate = await this.context.PaymentAttempts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        attempt => attempt.StoreId == request.StoreId
                            && attempt.IdempotencyKey == idempotencyKey,
                        cancellationToken);
                if (duplicate is not null)
                {
                    return Succeeded("Payment session already exists.", ToOnlinePlaceOrderResult(session, duplicate, idempotencyKey));
                }

                throw;
            }

            ServiceResponse<PaymentProviderSessionResult> providerResult;
            try
            {
                var provider = this.paymentProviderResolver.Resolve(paymentAttempt.ProviderKey);
                providerResult = await provider.CreateHostedSessionAsync(
                    new CreatePaymentProviderSessionRequest(
                        request.StoreId,
                        session.PublicId,
                        paymentAttempt.PublicId,
                        paymentAttempt.PaymentMethodKey,
                        paymentAttempt.ProviderKey,
                        totalAmount,
                        currencyCode,
                        idempotencyKey,
                        lines.Select(line => new PaymentProviderSessionLine(
                            line.Product.Id,
                            line.Product.Name ?? "Product",
                            line.CartLine.Quantity,
                            line.UnitPrice)).ToArray()),
                    cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                providerResult = new ServiceResponse<PaymentProviderSessionResult>(false, ex.Message)
                {
                    ResponseType = ServiceResponseType.Conflict,
                };
            }

            if (!providerResult.Success || providerResult.Payload is null)
            {
                paymentAttempt.State = PaymentAttemptStates.Failed;
                paymentAttempt.FailureCode = "provider_session_failed";
                paymentAttempt.FailureMessage = providerResult.Message ?? "Payment provider session could not be created.";
                paymentAttempt.UpdatedAtUtc = DateTimeOffset.UtcNow;
                await this.context.SaveChangesAsync(cancellationToken);
                return Failed<StorefrontPlaceOrderResult>(
                    providerResult.ResponseType is ServiceResponseType.Success ? ServiceResponseType.Conflict : providerResult.ResponseType,
                    paymentAttempt.FailureMessage);
            }

            var providerSession = providerResult.Payload;
            paymentAttempt.State = PaymentAttemptStates.RequiresAction;
            paymentAttempt.ProviderSessionId = providerSession.ProviderSessionId;
            paymentAttempt.ProviderReference = providerSession.ProviderReference;
            paymentAttempt.NextActionType = providerSession.NextActionType;
            paymentAttempt.NextActionUrl = providerSession.NextActionUrl;
            paymentAttempt.MetadataJson = providerSession.MetadataJson;
            paymentAttempt.UpdatedAtUtc = DateTimeOffset.UtcNow;

            session.State = CheckoutSessionStates.OrderPending;
            session.IdempotencyKey = idempotencyKey;
            session.NextAction = "paymentRedirect";
            session.UpdatedAtUtc = DateTimeOffset.UtcNow;
            cart.LastActivityAtUtc = DateTimeOffset.UtcNow;
            cart.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            return Succeeded("Payment session created.", ToOnlinePlaceOrderResult(session, paymentAttempt, idempotencyKey));
        }

        private async Task<bool> IsPaymentMethodEnabledAsync(Guid storeId, string paymentMethodKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(paymentMethodKey))
            {
                return false;
            }

            return await this.context.StorePaymentMethods
                .AsNoTracking()
                .AnyAsync(
                    method => method.StoreId == storeId
                        && method.PaymentMethodKey == paymentMethodKey
                        && method.Enabled,
                    cancellationToken);
        }

        private async Task<StorefrontPlaceOrderResult?> FindCompletedByIdempotencyKeyAsync(
            Guid storeId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var session = await this.context.CheckoutSessions
                .AsNoTracking()
                .Include(checkout => checkout.Order)
                .FirstOrDefaultAsync(
                    checkout => checkout.StoreId == storeId
                        && checkout.IdempotencyKey == idempotencyKey
                        && checkout.OrderId != null,
                    cancellationToken);

            if (session?.Order is null)
            {
                return null;
            }

            var paymentAttemptId = await this.context.PaymentAttempts
                .AsNoTracking()
                .Where(attempt => attempt.StoreId == session.StoreId
                    && attempt.IdempotencyKey == idempotencyKey)
                .Select(attempt => attempt.PublicId)
                .FirstOrDefaultAsync(cancellationToken);

            return ToPlaceOrderResult(session, session.Order, paymentAttemptId, idempotencyKey);
        }

        private async Task<OrderLineResolution> ResolveOrderLinesAsync(
            Guid storeId,
            IEnumerable<CartLine> cartLines,
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
                if (product is null || !IsStorefrontAvailable(product, storeId))
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

                var availableStock = variant?.Stock ?? product.Quantity;
                if (availableStock < cartLine.Quantity)
                {
                    return OrderLineResolution.Failed(ServiceResponseType.Conflict, "One or more cart items are out of stock.");
                }

                var unitPrice = cartLine.UnitPriceSnapshot ?? variant?.Price ?? product.Price;
                if (unitPrice <= 0m)
                {
                    return OrderLineResolution.Failed(ServiceResponseType.ValidationError, "Cart line price is invalid.");
                }

                results.Add(new OrderLineSnapshot(cartLine, product, variant, unitPrice));
            }

            return OrderLineResolution.Succeeded(results);
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

        private static IEnumerable<StorefrontCheckoutValidationIssue> ValidateCheckoutFields(StorefrontCheckoutPreviewRequest request)
        {
            var customerEmail = NormalizeNullable(request.CustomerEmail);
            if (customerEmail is null || !IsEmail(customerEmail))
            {
                yield return new StorefrontCheckoutValidationIssue("customer.email_invalid", "Customer email is invalid.", "customerEmail");
            }

            if (NormalizeNullable(request.CustomerName) is null)
            {
                yield return new StorefrontCheckoutValidationIssue("customer.name_required", "Customer name is required.", "customerName");
            }

            if (string.IsNullOrWhiteSpace(request.PaymentMethodKey))
            {
                yield return new StorefrontCheckoutValidationIssue("payment.method_required", "Payment method is required.", "paymentMethodKey");
            }

            var shipping = request.ShippingAddress;
            if (NormalizeNullable(shipping.FullName) is null)
            {
                yield return new StorefrontCheckoutValidationIssue("shipping.full_name_required", "Shipping full name is required.", "shippingAddress.fullName");
            }

            var shippingEmail = NormalizeNullable(shipping.Email);
            if (shippingEmail is null || !IsEmail(shippingEmail))
            {
                yield return new StorefrontCheckoutValidationIssue("shipping.email_invalid", "Shipping email is invalid.", "shippingAddress.email");
            }

            if (NormalizeNullable(shipping.Address1) is null)
            {
                yield return new StorefrontCheckoutValidationIssue("shipping.address1_required", "Shipping address line 1 is required.", "shippingAddress.address1");
            }

            if (NormalizeNullable(shipping.City) is null)
            {
                yield return new StorefrontCheckoutValidationIssue("shipping.city_required", "Shipping city is required.", "shippingAddress.city");
            }

            if (NormalizeNullable(shipping.PostalCode) is null)
            {
                yield return new StorefrontCheckoutValidationIssue("shipping.postal_required", "Shipping postal code is required.", "shippingAddress.postalCode");
            }

            var countryCode = NormalizeNullable(shipping.CountryCode);
            if (countryCode is null || countryCode.Length != 2 || !countryCode.All(char.IsLetter))
            {
                yield return new StorefrontCheckoutValidationIssue("shipping.country_invalid", "Shipping country code must be a two-letter ISO code.", "shippingAddress.countryCode");
            }
        }

        private static bool IsEmail(string value)
        {
            try
            {
                _ = new MailAddress(value);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string NormalizeKey(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? NormalizeIdempotencyKey(string? value)
        {
            var normalized = NormalizeNullable(value);
            if (normalized is null)
            {
                return null;
            }

            return normalized.Length > 128 ? null : normalized;
        }

        private static string? NormalizeCurrency(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized is null ? null : normalized.ToUpperInvariant();
        }

        private static StorefrontPlaceOrderResult ToPlaceOrderResult(
            CheckoutSession session,
            Order order,
            Guid paymentAttemptId,
            string idempotencyKey)
        {
            return new StorefrontPlaceOrderResult(
                session.PublicId,
                paymentAttemptId,
                order.Id,
                order.Reference,
                order.OrderStatus,
                order.PaymentStatus,
                order.PaymentMethodKey,
                order.TotalAmount,
                NormalizeCurrency(order.CurrencyCode) ?? DefaultCurrencyCode,
                idempotencyKey,
                order.CreatedOn);
        }

        private static StorefrontPlaceOrderResult ToOnlinePlaceOrderResult(
            CheckoutSession session,
            PaymentAttempt paymentAttempt,
            string idempotencyKey)
        {
            return new StorefrontPlaceOrderResult(
                session.PublicId,
                paymentAttempt.PublicId,
                paymentAttempt.OrderId,
                null,
                null,
                PaymentStatuses.Pending,
                paymentAttempt.PaymentMethodKey,
                paymentAttempt.Amount,
                NormalizeCurrency(paymentAttempt.CurrencyCode) ?? DefaultCurrencyCode,
                idempotencyKey,
                paymentAttempt.CreatedAtUtc.UtcDateTime,
                paymentAttempt.NextActionType,
                paymentAttempt.NextActionUrl);
        }

        private static ServiceResponse<StorefrontCheckoutPreviewResult> Succeeded(
            string message,
            StorefrontCheckoutPreviewResult payload)
        {
            return new ServiceResponse<StorefrontCheckoutPreviewResult>(true, message, payload.CheckoutSessionId)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<StorefrontCheckoutPreviewResult> Failed(
            ServiceResponseType responseType,
            string message)
        {
            return new ServiceResponse<StorefrontCheckoutPreviewResult>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private static ServiceResponse<TPayload> Succeeded<TPayload>(
            string message,
            TPayload payload)
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

        private sealed record OrderLineResolution(
            bool Success,
            ServiceResponseType ResponseType,
            string Message,
            IReadOnlyList<OrderLineSnapshot> Lines)
        {
            public static OrderLineResolution Succeeded(IReadOnlyList<OrderLineSnapshot> lines)
            {
                return new OrderLineResolution(true, ServiceResponseType.Success, "Cart lines resolved.", lines);
            }

            public static OrderLineResolution Failed(ServiceResponseType responseType, string message)
            {
                return new OrderLineResolution(false, responseType, message, []);
            }
        }
    }
}
