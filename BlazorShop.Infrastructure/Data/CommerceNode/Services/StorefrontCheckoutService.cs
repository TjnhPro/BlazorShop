namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Net.Mail;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Addresses;
    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.CommerceNode.Features;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.ProductSelections;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.EntityFrameworkCore;

    public sealed class StorefrontCheckoutService : IStorefrontCheckoutService
    {
        private const string DefaultCurrencyCode = "USD";
        private const string FreeStandardShippingOptionKey = "free_standard";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly CommerceNodeDbContext context;
        private readonly IStorefrontCartService cartService;
        private readonly IStoreCurrencyResolver storeCurrencyResolver;
        private readonly IMoneyRoundingService moneyRoundingService;
        private readonly IStorefrontCustomerService customerService;
        private readonly IStoreFeatureStateService featureStateService;
        private readonly IPaymentHandlerResolver paymentHandlerResolver;
        private readonly IStorefrontPaymentProviderResolver paymentProviderResolver;
        private readonly IProductSellabilityResolver sellabilityResolver;
        private readonly IAddressValidationService addressValidationService;

        public StorefrontCheckoutService(
            CommerceNodeDbContext context,
            IStorefrontCartService cartService,
            IStoreCurrencyResolver storeCurrencyResolver,
            IMoneyRoundingService moneyRoundingService,
            IStorefrontCustomerService customerService,
            IStoreFeatureStateService featureStateService,
            IPaymentHandlerResolver paymentHandlerResolver,
            IStorefrontPaymentProviderResolver paymentProviderResolver,
            IProductSellabilityResolver? sellabilityResolver = null,
            IAddressValidationService? addressValidationService = null)
        {
            this.context = context;
            this.cartService = cartService;
            this.storeCurrencyResolver = storeCurrencyResolver;
            this.moneyRoundingService = moneyRoundingService;
            this.customerService = customerService;
            this.featureStateService = featureStateService;
            this.paymentHandlerResolver = paymentHandlerResolver;
            this.paymentProviderResolver = paymentProviderResolver;
            this.sellabilityResolver = sellabilityResolver ?? new ProductSellabilityResolver();
            this.addressValidationService = addressValidationService ?? new AddressValidationService();
        }

        public async Task<ServiceResponse<StorefrontCheckoutSessionResult>> StartAsync(
            StorefrontCheckoutStartRequest request,
            CancellationToken cancellationToken = default)
        {
            var cartResult = await this.ResolveStartCartAsync(request.StoreId, request.CartToken, cancellationToken);
            if (!cartResult.Success || cartResult.Payload is null)
            {
                return Failed<StorefrontCheckoutSessionResult>(
                    cartResult.ResponseType,
                    cartResult.Message ?? "Cart could not be resolved.");
            }

            var cart = cartResult.Payload;
            var entryValidation = await this.ValidateCheckoutEntryAsync(request.StoreId, request.CartToken, cart, cancellationToken);
            if (entryValidation.Issues.Count > 0)
            {
                return Failed<StorefrontCheckoutSessionResult>(
                    ResolveEntryValidationResponseType(entryValidation.Issues),
                    entryValidation.Issues[0].Message);
            }

            var now = DateTimeOffset.UtcNow;
            var activeSession = await this.context.CheckoutSessions
                .FirstOrDefaultAsync(
                    session => session.StoreId == request.StoreId
                        && session.CartSessionId == cart.Id
                        && (session.State == CheckoutSessionStates.Draft
                            || session.State == CheckoutSessionStates.Ready
                            || session.State == CheckoutSessionStates.OrderPending),
                    cancellationToken);

            if (activeSession is not null)
            {
                if (activeSession.ExpiresAtUtc <= now)
                {
                    MarkExpired(activeSession, now);
                    await this.context.SaveChangesAsync(cancellationToken);
                    return Failed<StorefrontCheckoutSessionResult>(ServiceResponseType.Conflict, "Checkout session has expired.");
                }

                return Succeeded("Checkout session resumed.", await this.ToSessionResultAsync(activeSession, cart, cancellationToken));
            }

            var session = new CheckoutSession
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = request.StoreId,
                CartSessionId = cart.Id,
                State = CheckoutSessionStates.Draft,
                CheckoutVersion = 1,
                CurrentStep = CheckoutSteps.Entry,
                CompletedStepsJson = "[]",
                CartVersion = cart.Version,
                LastValidatedCartVersion = cart.Version,
                CustomerEmail = string.Empty,
                CustomerName = string.Empty,
                ShippingFullName = string.Empty,
                ShippingEmail = string.Empty,
                ShippingAddress1 = string.Empty,
                ShippingCity = string.Empty,
                ShippingPostalCode = string.Empty,
                ShippingCountryCode = string.Empty,
                PaymentMethodKey = string.Empty,
                Subtotal = cart.Subtotal,
                ShippingTotal = cart.ShippingEstimate,
                TaxTotal = cart.TaxEstimate,
                DiscountTotal = cart.DiscountTotal,
                GrandTotal = cart.GrandTotal,
                CurrencyCode = NormalizeCurrency(cart.CurrencyCode) ?? DefaultCurrencyCode,
                NextAction = CheckoutSteps.ShippingAddress,
                ExpiresAtUtc = now.AddHours(1),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            this.context.CheckoutSessions.Add(session);
            await this.context.SaveChangesAsync(cancellationToken);

            return Succeeded("Checkout session started.", await this.ToSessionResultAsync(session, cart, cancellationToken));
        }

        public async Task<ServiceResponse<StorefrontCheckoutSessionResult>> LoadAsync(
            StorefrontCheckoutSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            var resolution = await this.ResolveActiveSessionAsync(request, cancellationToken);
            if (!resolution.Success)
            {
                return Failed<StorefrontCheckoutSessionResult>(resolution.ResponseType, resolution.Message);
            }

            return Succeeded(
                "Checkout session loaded.",
                await this.ToSessionResultAsync(resolution.Session!, resolution.Cart!, cancellationToken));
        }

        public async Task<ServiceResponse<StorefrontCheckoutSessionResult>> CancelAsync(
            StorefrontCheckoutSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            var resolution = await this.ResolveActiveSessionAsync(request, cancellationToken);
            if (!resolution.Success)
            {
                return Failed<StorefrontCheckoutSessionResult>(resolution.ResponseType, resolution.Message);
            }

            Touch(resolution.Session!, CheckoutSessionStates.Cancelled, CheckoutSteps.Entry, DateTimeOffset.UtcNow);
            await this.context.SaveChangesAsync(cancellationToken);

            return Succeeded(
                "Checkout session cancelled.",
                await this.ToSessionResultAsync(resolution.Session!, resolution.Cart!, cancellationToken));
        }

        public async Task<ServiceResponse<StorefrontCheckoutSessionResult>> ExpireAsync(
            StorefrontCheckoutSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            var resolution = await this.ResolveSessionForCartAsync(request, cancellationToken);
            if (!resolution.Success)
            {
                return Failed<StorefrontCheckoutSessionResult>(resolution.ResponseType, resolution.Message);
            }

            MarkExpired(resolution.Session!, DateTimeOffset.UtcNow);
            await this.context.SaveChangesAsync(cancellationToken);

            return Succeeded(
                "Checkout session expired.",
                await this.ToSessionResultAsync(resolution.Session!, resolution.Cart!, cancellationToken));
        }

        public async Task<ServiceResponse<StorefrontCheckoutSessionResult>> UpdateAddressesAsync(
            StorefrontCheckoutAddressStepRequest request,
            CancellationToken cancellationToken = default)
        {
            var resolution = await this.ResolveActiveSessionAsync(
                new StorefrontCheckoutSessionRequest(request.StoreId, request.CheckoutSessionId, request.CartToken),
                cancellationToken);
            if (!resolution.Success)
            {
                return Failed<StorefrontCheckoutSessionResult>(resolution.ResponseType, resolution.Message);
            }

            var billingResolution = await this.ResolveCheckoutAddressAsync(
                request.StoreId,
                request.BillingAddressId,
                request.BillingAddress,
                request.CustomerAppUserId,
                "billingAddressId",
                cancellationToken);
            var billingAddress = billingResolution.Address;
            var issues = billingResolution.Issues.ToList();
            if (billingAddress is null)
            {
                issues.Add(new StorefrontCheckoutValidationIssue(
                    "billing.address_required",
                    "Billing address is required.",
                    "billingAddress"));
            }

            var shippingResolution = request.UseBillingAddressAsShippingAddress
                ? ShippingAddressResolution.Succeeded(billingAddress)
                : await this.ResolveCheckoutAddressAsync(
                    request.StoreId,
                    request.ShippingAddressId,
                    request.ShippingAddress,
                    request.CustomerAppUserId,
                    "shippingAddressId",
                    cancellationToken);
            var shippingAddress = shippingResolution.Address;
            issues.AddRange(shippingResolution.Issues);
            if (shippingAddress is null)
            {
                issues.Add(new StorefrontCheckoutValidationIssue(
                    "shipping.address_required",
                    "Shipping address is required.",
                    "shippingAddress"));
            }

            if (billingAddress is not null)
            {
                issues.AddRange(this.ValidateAddressFields("billing", "billingAddress", billingAddress));
            }

            if (shippingAddress is not null)
            {
                issues.AddRange(this.ValidateAddressFields("shipping", "shippingAddress", shippingAddress));
            }

            if (issues.Count > 0)
            {
                return Failed<StorefrontCheckoutSessionResult>(ServiceResponseType.ValidationError, issues[0].Message);
            }

            var session = resolution.Session!;
            session.BillingAddressSnapshotJson = JsonSerializer.Serialize(billingAddress, JsonOptions);
            session.ShippingAddressSource = request.UseBillingAddressAsShippingAddress
                ? "billing"
                : request.ShippingAddressId.HasValue ? "saved" : "direct";
            session.ShippingFullName = NormalizeNullable(shippingAddress!.FullName) ?? string.Empty;
            session.ShippingEmail = NormalizeNullable(shippingAddress.Email) ?? string.Empty;
            session.ShippingPhone = NormalizeNullable(shippingAddress.Phone);
            session.ShippingAddress1 = NormalizeNullable(shippingAddress.Address1) ?? string.Empty;
            session.ShippingAddress2 = NormalizeNullable(shippingAddress.Address2);
            session.ShippingCity = NormalizeNullable(shippingAddress.City) ?? string.Empty;
            session.ShippingState = NormalizeNullable(shippingAddress.State);
            session.ShippingPostalCode = NormalizeNullable(shippingAddress.PostalCode) ?? string.Empty;
            session.ShippingCountryCode = NormalizeNullable(shippingAddress.CountryCode)?.ToUpperInvariant() ?? string.Empty;
            session.PaymentMethodKey = string.Empty;
            ClearTermsAcknowledgement(session);
            session.CompletedStepsJson = JsonSerializer.Serialize(
                new[] { CheckoutSteps.Entry, CheckoutSteps.BillingAddress, CheckoutSteps.ShippingAddress },
                JsonOptions);
            session.ValidationIssuesJson = null;
            session.SelectedShippingOptionJson = null;
            session.NextAction = CheckoutSteps.ShippingMethod;
            Touch(session, CheckoutSessionStates.Draft, CheckoutSteps.ShippingMethod, DateTimeOffset.UtcNow);
            await this.context.SaveChangesAsync(cancellationToken);

            return Succeeded(
                "Checkout addresses updated.",
                await this.ToSessionResultAsync(session, resolution.Cart!, cancellationToken));
        }

        public async Task<ServiceResponse<StorefrontCheckoutSessionResult>> SelectShippingMethodAsync(
            StorefrontCheckoutShippingMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            var resolution = await this.ResolveActiveSessionAsync(
                new StorefrontCheckoutSessionRequest(request.StoreId, request.CheckoutSessionId, request.CartToken),
                cancellationToken);
            if (!resolution.Success)
            {
                return Failed<StorefrontCheckoutSessionResult>(resolution.ResponseType, resolution.Message);
            }

            var session = resolution.Session!;
            if (string.IsNullOrWhiteSpace(session.ShippingAddress1)
                || string.IsNullOrWhiteSpace(session.ShippingCity)
                || string.IsNullOrWhiteSpace(session.ShippingPostalCode)
                || string.IsNullOrWhiteSpace(session.ShippingCountryCode))
            {
                return Failed<StorefrontCheckoutSessionResult>(ServiceResponseType.Conflict, "Shipping address must be selected first.");
            }

            var option = ResolveShippingOptions(session, selectedKey: request.ShippingOptionKey)
                .FirstOrDefault(candidate => string.Equals(candidate.Key, request.ShippingOptionKey, StringComparison.OrdinalIgnoreCase));
            if (option is null)
            {
                return Failed<StorefrontCheckoutSessionResult>(ServiceResponseType.ValidationError, "Shipping option is not available.");
            }

            session.SelectedShippingOptionJson = JsonSerializer.Serialize(option with { Selected = true }, JsonOptions);
            session.ShippingTotal = option.Price;
            session.GrandTotal = this.moneyRoundingService.RoundOrderTotal(
                session.Subtotal + option.Price + session.TaxTotal - session.DiscountTotal,
                session.CurrencyCode);
            session.PaymentMethodKey = string.Empty;
            ClearTermsAcknowledgement(session);
            session.ValidationIssuesJson = null;
            session.CompletedStepsJson = JsonSerializer.Serialize(
                new[] { CheckoutSteps.Entry, CheckoutSteps.BillingAddress, CheckoutSteps.ShippingAddress, CheckoutSteps.ShippingMethod },
                JsonOptions);
            session.NextAction = CheckoutSteps.PaymentMethod;
            Touch(session, CheckoutSessionStates.Draft, CheckoutSteps.PaymentMethod, DateTimeOffset.UtcNow);
            await this.context.SaveChangesAsync(cancellationToken);

            return Succeeded(
                "Checkout shipping method selected.",
                await this.ToSessionResultAsync(session, resolution.Cart!, cancellationToken));
        }

        public async Task<ServiceResponse<StorefrontCheckoutSessionResult>> SelectPaymentMethodAsync(
            StorefrontCheckoutPaymentMethodRequest request,
            CancellationToken cancellationToken = default)
        {
            var resolution = await this.ResolveActiveSessionAsync(
                new StorefrontCheckoutSessionRequest(request.StoreId, request.CheckoutSessionId, request.CartToken),
                cancellationToken);
            if (!resolution.Success)
            {
                return Failed<StorefrontCheckoutSessionResult>(resolution.ResponseType, resolution.Message);
            }

            var session = resolution.Session!;
            if (ParseSelectedShippingOption(session.SelectedShippingOptionJson) is null)
            {
                return Failed<StorefrontCheckoutSessionResult>(ServiceResponseType.Conflict, "Shipping method must be selected first.");
            }

            var paymentMethodKey = NormalizeKey(request.PaymentMethodKey);
            var methods = await this.ResolvePaymentMethodOptionsAsync(session, paymentMethodKey, cancellationToken);
            var selected = methods.FirstOrDefault(method => string.Equals(method.Key, paymentMethodKey, StringComparison.OrdinalIgnoreCase));
            if (selected is null)
            {
                return Failed<StorefrontCheckoutSessionResult>(ServiceResponseType.ValidationError, "Payment method is not available.");
            }

            session.PaymentMethodKey = selected.Key;
            ClearTermsAcknowledgement(session);
            session.ValidationIssuesJson = null;
            session.CompletedStepsJson = JsonSerializer.Serialize(
                new[]
                {
                    CheckoutSteps.Entry,
                    CheckoutSteps.BillingAddress,
                    CheckoutSteps.ShippingAddress,
                    CheckoutSteps.ShippingMethod,
                    CheckoutSteps.PaymentMethod,
                },
                JsonOptions);
            session.NextAction = CheckoutSteps.Review;
            Touch(session, CheckoutSessionStates.Draft, CheckoutSteps.Review, DateTimeOffset.UtcNow);
            await this.context.SaveChangesAsync(cancellationToken);

            return Succeeded(
                "Checkout payment method selected.",
                await this.ToSessionResultAsync(session, resolution.Cart!, cancellationToken, methods));
        }

        public async Task<ServiceResponse<StorefrontCheckoutReviewResult>> ReviewAsync(
            StorefrontCheckoutReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            var resolution = await this.ResolveActiveSessionAsync(
                new StorefrontCheckoutSessionRequest(request.StoreId, request.CheckoutSessionId, request.CartToken),
                cancellationToken);
            if (!resolution.Success)
            {
                return Failed<StorefrontCheckoutReviewResult>(resolution.ResponseType, resolution.Message);
            }

            var session = resolution.Session!;
            var cart = resolution.Cart!;
            var selectedShippingOption = ParseSelectedShippingOption(session.SelectedShippingOptionJson);
            var paymentMethods = selectedShippingOption is null
                ? []
                : await this.ResolvePaymentMethodOptionsAsync(session, session.PaymentMethodKey, cancellationToken);
            var selectedPaymentMethod = paymentMethods.FirstOrDefault(method => method.Selected);
            var billingAddress = ParseBillingAddress(session.BillingAddressSnapshotJson);
            var shippingAddress = CreateShippingAddress(session);
            var issues = ParseValidationIssues(session.ValidationIssuesJson).ToList();
            var entryValidation = await this.ValidateCheckoutEntryAsync(request.StoreId, request.CartToken, cart, cancellationToken);
            issues.AddRange(entryValidation.Issues);

            if (billingAddress is null)
            {
                issues.Add(new StorefrontCheckoutValidationIssue(
                    "billing.address_required",
                    "Billing address is required.",
                    "billingAddress"));
            }

            if (shippingAddress is null)
            {
                issues.Add(new StorefrontCheckoutValidationIssue(
                    "shipping.address_required",
                    "Shipping address is required.",
                    "shippingAddress"));
            }

            if (selectedShippingOption is null)
            {
                issues.Add(new StorefrontCheckoutValidationIssue(
                    "shipping.method_required",
                    "Shipping method is required.",
                    "shippingOptionKey"));
            }

            if (selectedPaymentMethod is null)
            {
                issues.Add(new StorefrontCheckoutValidationIssue(
                    "payment.method_required",
                    "Payment method is required.",
                    "paymentMethodKey"));
            }

            var termsRequired = false;
            if (request.TermsAccepted)
            {
                session.TermsAccepted = true;
                session.TermsVersion = NormalizeTermsVersion(request.TermsVersion) ?? "default";
                session.TermsAcceptedAtUtc = DateTimeOffset.UtcNow;
            }
            else if (termsRequired && !session.TermsAccepted)
            {
                issues.Add(new StorefrontCheckoutValidationIssue(
                    "terms.required",
                    "Terms must be accepted before placing the order.",
                    "termsAccepted"));
            }

            var distinctIssues = DeduplicateIssues(issues);
            var placeOrderAllowed = distinctIssues.Count == 0
                && selectedShippingOption is not null
                && selectedPaymentMethod is not null;
            var nextRequiredStep = placeOrderAllowed
                ? CheckoutSteps.PlaceOrder
                : ResolveNextRequiredStep(billingAddress, shippingAddress, selectedShippingOption, selectedPaymentMethod);
            session.ValidationIssuesJson = distinctIssues.Count == 0 ? null : JsonSerializer.Serialize(distinctIssues, JsonOptions);
            session.CompletedStepsJson = JsonSerializer.Serialize(
                placeOrderAllowed
                    ? [
                        CheckoutSteps.Entry,
                        CheckoutSteps.BillingAddress,
                        CheckoutSteps.ShippingAddress,
                        CheckoutSteps.ShippingMethod,
                        CheckoutSteps.PaymentMethod,
                        CheckoutSteps.Review,
                    ]
                    : ParseCompletedSteps(session.CompletedStepsJson),
                JsonOptions);
            session.NextAction = nextRequiredStep;
            Touch(session, placeOrderAllowed ? CheckoutSessionStates.Ready : CheckoutSessionStates.Draft, CheckoutSteps.Review, DateTimeOffset.UtcNow);
            await this.context.SaveChangesAsync(cancellationToken);

            return Succeeded(
                placeOrderAllowed ? "Checkout review is ready." : "Checkout review has validation issues.",
                ToReviewResult(
                    session,
                    cart,
                    billingAddress,
                    shippingAddress,
                    selectedShippingOption,
                    selectedPaymentMethod,
                    termsRequired,
                    placeOrderAllowed,
                    nextRequiredStep,
                    distinctIssues));
        }

        public async Task<ServiceResponse<StorefrontCheckoutPreviewResult>> PreviewAsync(
            StorefrontCheckoutPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.StoreId == Guid.Empty)
            {
                return Failed(ServiceResponseType.ValidationError, "Store is required.");
            }

            if (!await this.featureStateService.IsEnabledAsync(request.StoreId, StoreFeatureKeys.Checkout, cancellationToken))
            {
                return Failed(ServiceResponseType.Conflict, "Checkout is disabled for this store.");
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

            var shippingResolution = await this.ResolveShippingAddressAsync(request, cancellationToken);
            var shippingAddress = shippingResolution.Address;
            var issues = shippingResolution.Issues.ToList();
            issues.AddRange(this.ValidateCheckoutFields(request, shippingAddress));
            var entryValidation = await this.ValidateCheckoutEntryAsync(request.StoreId, request.CartToken, cart, cancellationToken);
            issues.AddRange(entryValidation.Issues);

            var paymentMethodKey = NormalizeKey(request.PaymentMethodKey);
            StorefrontCustomerProfile? customer = null;
            if (!issues.Any(issue => issue.Field is "customerEmail" or "customerName"))
            {
                var customerResult = await this.customerService.ResolveOrCreateAsync(
                    new StorefrontCustomerResolutionRequest(
                        request.StoreId,
                        request.CustomerEmail,
                        request.CustomerName,
                        shippingAddress?.Phone),
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

            var currencyCode = entryValidation.CartValidation is not null
                ? entryValidation.CartValidation.CurrencyCode
                : await this.storeCurrencyResolver.ResolveDefaultCurrencyCodeAsync(request.StoreId, cancellationToken);
            var lines = cart.Lines.Select(line =>
            {
                var unitPrice = this.moneyRoundingService.RoundUnitPrice(line.UnitPriceSnapshot ?? 0m, currencyCode);
                return new StorefrontCheckoutLineSummary(
                    line.Id,
                    line.ProductId,
                    line.ProductVariantId,
                    Math.Max(0, line.Quantity),
                    unitPrice,
                    this.moneyRoundingService.RoundLineTotal(unitPrice * Math.Max(0, line.Quantity), currencyCode),
                    currencyCode);
            }).ToArray();

            var subtotal = this.moneyRoundingService.RoundOrderTotal(lines.Sum(line => line.LineTotal), currencyCode);
            var rateSnapshot = this.ResolveCurrencyRateSnapshot(cart.Lines, currencyCode, subtotal);
            if (!rateSnapshot.Success)
            {
                issues.Add(new StorefrontCheckoutValidationIssue(
                    "cart.currency_rate_snapshot_invalid",
                    rateSnapshot.Message,
                    "cart.lines"));
            }

            if (!await this.IsPaymentMethodAvailableAsync(
                request.StoreId,
                paymentMethodKey,
                currencyCode,
                shippingAddress?.CountryCode ?? string.Empty,
                subtotal,
                cancellationToken))
            {
                issues.Add(new StorefrontCheckoutValidationIssue(
                    "payment.method_unavailable",
                    "Payment method is not available.",
                    "paymentMethodKey"));
            }

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
                CheckoutVersion = 1,
                CurrentStep = isValid ? CheckoutSteps.Review : CheckoutSteps.ShippingAddress,
                CompletedStepsJson = isValid
                    ? JsonSerializer.Serialize(new[] { CheckoutSteps.Entry, CheckoutSteps.ShippingAddress, CheckoutSteps.PaymentMethod }, JsonOptions)
                    : "[]",
                CartVersion = cart.Version,
                LastValidatedCartVersion = cart.Version,
                CustomerEmail = NormalizeNullable(request.CustomerEmail) ?? string.Empty,
                CustomerName = NormalizeNullable(request.CustomerName) ?? string.Empty,
                CustomerPhone = NormalizeNullable(shippingAddress?.Phone),
                ShippingFullName = NormalizeNullable(shippingAddress?.FullName) ?? string.Empty,
                ShippingEmail = NormalizeNullable(shippingAddress?.Email) ?? string.Empty,
                ShippingPhone = NormalizeNullable(shippingAddress?.Phone),
                ShippingAddress1 = NormalizeNullable(shippingAddress?.Address1) ?? string.Empty,
                ShippingAddress2 = NormalizeNullable(shippingAddress?.Address2),
                ShippingCity = NormalizeNullable(shippingAddress?.City) ?? string.Empty,
                ShippingState = NormalizeNullable(shippingAddress?.State),
                ShippingPostalCode = NormalizeNullable(shippingAddress?.PostalCode) ?? string.Empty,
                ShippingCountryCode = NormalizeNullable(shippingAddress?.CountryCode)?.ToUpperInvariant() ?? string.Empty,
                PaymentMethodKey = paymentMethodKey,
                Subtotal = subtotal,
                ShippingTotal = 0m,
                TaxTotal = 0m,
                DiscountTotal = 0m,
                GrandTotal = subtotal,
                CurrencyCode = currencyCode,
                BaseCurrencyCode = rateSnapshot.BaseCurrencyCode,
                BaseSubtotal = rateSnapshot.BaseTotalAmount,
                BaseGrandTotal = rateSnapshot.BaseTotalAmount,
                ExchangeRate = rateSnapshot.ExchangeRate,
                ExchangeRateProviderKey = rateSnapshot.ExchangeRateProviderKey,
                ExchangeRateSource = rateSnapshot.ExchangeRateSource,
                ExchangeRateEffectiveAtUtc = rateSnapshot.ExchangeRateEffectiveAtUtc,
                ExchangeRateExpiresAtUtc = rateSnapshot.ExchangeRateExpiresAtUtc,
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
                    session.CheckoutVersion,
                    cart.Version,
                    session.LastValidatedCartVersion,
                    session.State,
                    session.CurrentStep,
                    ParseCompletedSteps(session.CompletedStepsJson),
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

            if (!await this.featureStateService.IsEnabledAsync(request.StoreId, StoreFeatureKeys.Checkout, cancellationToken))
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Checkout is disabled for this store.");
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
                MarkExpired(session, DateTimeOffset.UtcNow);
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
            var isCod = string.Equals(paymentMethodKey, PaymentMethodKeys.Cod, StringComparison.OrdinalIgnoreCase);
            var isStripe = string.Equals(paymentMethodKey, PaymentMethodKeys.Stripe, StringComparison.OrdinalIgnoreCase);
            if (!isCod && !isStripe)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Payment provider is not available for order placement.");
            }

            var currencyCode = NormalizeCurrency(session.CurrencyCode)
                ?? await this.storeCurrencyResolver.ResolveDefaultCurrencyCodeAsync(request.StoreId, cancellationToken);
            var lineResolution = await this.ResolveOrderLinesAsync(request.StoreId, cart.Lines, currencyCode, cancellationToken);
            if (!lineResolution.Success)
            {
                return Failed<StorefrontPlaceOrderResult>(lineResolution.ResponseType, lineResolution.Message);
            }

            var lines = lineResolution.Lines;
            var totalAmount = this.moneyRoundingService.RoundOrderTotal(
                lines.Sum(line => this.moneyRoundingService.RoundLineTotal(line.CartLine.Quantity * line.UnitPrice, currencyCode)),
                currencyCode);
            var rateSnapshot = this.ResolveCurrencyRateSnapshot(lines, currencyCode, totalAmount);
            if (!rateSnapshot.Success)
            {
                return Failed<StorefrontPlaceOrderResult>(rateSnapshot.ResponseType, rateSnapshot.Message);
            }

            if (totalAmount <= 0m)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.ValidationError, "Cart total must be greater than zero.");
            }

            var paymentAmount = this.moneyRoundingService.RoundPaymentAmount(totalAmount, currencyCode);
            if (!await this.IsPaymentMethodAvailableAsync(
                request.StoreId,
                paymentMethodKey,
                currencyCode,
                session.ShippingCountryCode,
                totalAmount,
                cancellationToken))
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Payment method is not available.");
            }

            if (isStripe)
            {
                return await this.CreateOnlinePaymentSessionAsync(
                    request,
                    session,
                    cart,
                    lines,
                    paymentAmount,
                    currencyCode,
                    rateSnapshot,
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
                        paymentAmount,
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
                Amount = paymentAmount,
                CurrencyCode = currencyCode,
                BaseCurrencyCode = rateSnapshot.BaseCurrencyCode,
                BaseAmount = rateSnapshot.BaseTotalAmount,
                ExchangeRate = rateSnapshot.ExchangeRate,
                ExchangeRateProviderKey = rateSnapshot.ExchangeRateProviderKey,
                ExchangeRateSource = rateSnapshot.ExchangeRateSource,
                ExchangeRateEffectiveAtUtc = rateSnapshot.ExchangeRateEffectiveAtUtc,
                ExchangeRateExpiresAtUtc = rateSnapshot.ExchangeRateExpiresAtUtc,
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
                BaseCurrencyCode = rateSnapshot.BaseCurrencyCode,
                BaseTotalAmount = rateSnapshot.BaseTotalAmount,
                ExchangeRate = rateSnapshot.ExchangeRate,
                ExchangeRateProviderKey = rateSnapshot.ExchangeRateProviderKey,
                ExchangeRateSource = rateSnapshot.ExchangeRateSource,
                ExchangeRateEffectiveAtUtc = rateSnapshot.ExchangeRateEffectiveAtUtc,
                ExchangeRateExpiresAtUtc = rateSnapshot.ExchangeRateExpiresAtUtc,
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
                    CurrencyCode = currencyCode,
                    BaseUnitPrice = line.CartLine.BaseUnitPriceSnapshot,
                    ConvertedUnitPrice = line.UnitPrice,
                    LineTotal = this.moneyRoundingService.RoundLineTotal(line.CartLine.Quantity * line.UnitPrice, currencyCode),
                    BaseLineTotal = line.CartLine.BaseUnitPriceSnapshot.HasValue && rateSnapshot.BaseCurrencyCode is not null
                        ? this.moneyRoundingService.RoundLineTotal(line.CartLine.Quantity * line.CartLine.BaseUnitPriceSnapshot.Value, rateSnapshot.BaseCurrencyCode)
                        : null,
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

            Touch(session, CheckoutSessionStates.Completed, CheckoutSteps.Complete, now);
            session.OrderId = order.Id;
            session.IdempotencyKey = idempotencyKey;
            session.NextAction = "complete";
            session.PlacedAtUtc = now;
            session.BaseCurrencyCode = rateSnapshot.BaseCurrencyCode;
            session.BaseSubtotal = rateSnapshot.BaseTotalAmount;
            session.BaseGrandTotal = rateSnapshot.BaseTotalAmount;
            session.ExchangeRate = rateSnapshot.ExchangeRate;
            session.ExchangeRateProviderKey = rateSnapshot.ExchangeRateProviderKey;
            session.ExchangeRateSource = rateSnapshot.ExchangeRateSource;
            session.ExchangeRateEffectiveAtUtc = rateSnapshot.ExchangeRateEffectiveAtUtc;
            session.ExchangeRateExpiresAtUtc = rateSnapshot.ExchangeRateExpiresAtUtc;

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
            CurrencyRateSnapshot rateSnapshot,
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
                BaseCurrencyCode = rateSnapshot.BaseCurrencyCode,
                BaseAmount = rateSnapshot.BaseTotalAmount,
                ExchangeRate = rateSnapshot.ExchangeRate,
                ExchangeRateProviderKey = rateSnapshot.ExchangeRateProviderKey,
                ExchangeRateSource = rateSnapshot.ExchangeRateSource,
                ExchangeRateEffectiveAtUtc = rateSnapshot.ExchangeRateEffectiveAtUtc,
                ExchangeRateExpiresAtUtc = rateSnapshot.ExchangeRateExpiresAtUtc,
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

            Touch(session, CheckoutSessionStates.OrderPending, CheckoutSteps.PlaceOrder, DateTimeOffset.UtcNow);
            session.IdempotencyKey = idempotencyKey;
            session.NextAction = "paymentRedirect";
            session.BaseCurrencyCode = rateSnapshot.BaseCurrencyCode;
            session.BaseSubtotal = rateSnapshot.BaseTotalAmount;
            session.BaseGrandTotal = rateSnapshot.BaseTotalAmount;
            session.ExchangeRate = rateSnapshot.ExchangeRate;
            session.ExchangeRateProviderKey = rateSnapshot.ExchangeRateProviderKey;
            session.ExchangeRateSource = rateSnapshot.ExchangeRateSource;
            session.ExchangeRateEffectiveAtUtc = rateSnapshot.ExchangeRateEffectiveAtUtc;
            session.ExchangeRateExpiresAtUtc = rateSnapshot.ExchangeRateExpiresAtUtc;
            cart.LastActivityAtUtc = DateTimeOffset.UtcNow;
            cart.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            return Succeeded("Payment session created.", ToOnlinePlaceOrderResult(session, paymentAttempt, idempotencyKey));
        }

        private async Task<ServiceResponse<StorefrontCartSessionDto>> ResolveStartCartAsync(
            Guid storeId,
            string cartToken,
            CancellationToken cancellationToken)
        {
            if (storeId == Guid.Empty)
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.ValidationError, "Store is required.");
            }

            if (!await this.featureStateService.IsEnabledAsync(storeId, StoreFeatureKeys.Checkout, cancellationToken))
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.Conflict, "Checkout is disabled for this store.");
            }

            if (string.IsNullOrWhiteSpace(cartToken))
            {
                return Failed<StorefrontCartSessionDto>(ServiceResponseType.ValidationError, "Cart token is required.");
            }

            var cartResult = await this.cartService.GetAsync(storeId, cartToken, cancellationToken);
            if (!cartResult.Success || cartResult.Payload is null)
            {
                return Failed<StorefrontCartSessionDto>(
                    cartResult.ResponseType,
                    cartResult.Message ?? "Cart could not be resolved.");
            }

            return Succeeded("Cart resolved.", cartResult.Payload);
        }

        private async Task<CheckoutEntryValidation> ValidateCheckoutEntryAsync(
            Guid storeId,
            string cartToken,
            StorefrontCartSessionDto cart,
            CancellationToken cancellationToken)
        {
            var issues = new List<StorefrontCheckoutValidationIssue>();
            if (!string.Equals(cart.State, CartSessionStates.Active, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new StorefrontCheckoutValidationIssue("cart.inactive", "Cart is not active.", "cart"));
            }

            if (cart.Lines.Count == 0)
            {
                issues.Add(new StorefrontCheckoutValidationIssue("cart.empty", "Cart is empty.", "cart"));
            }

            var cartValidation = await this.cartService.ValidateAsync(storeId, cartToken, cancellationToken);
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

            return new CheckoutEntryValidation(issues, cartValidation.Payload);
        }

        private async Task MarkCartChangedIfNeededAsync(
            CheckoutSession session,
            StorefrontCartSessionDto cart,
            CancellationToken cancellationToken)
        {
            if (session.CartVersion == cart.Version
                && session.LastValidatedCartVersion == cart.Version)
            {
                return;
            }

            session.CartVersion = cart.Version;
            session.LastValidatedCartVersion = cart.Version;
            session.CompletedStepsJson = "[]";
            session.PaymentMethodKey = string.Empty;
            session.ValidationIssuesJson = JsonSerializer.Serialize(
                new[]
                {
                    new StorefrontCheckoutValidationIssue(
                        "cart.version_changed",
                        "Cart changed after checkout started. Review checkout details again.",
                        "cart"),
                },
                JsonOptions);
            session.NextAction = "review";
            Touch(session, CheckoutSessionStates.Draft, CheckoutSteps.Entry, DateTimeOffset.UtcNow);
            await this.context.SaveChangesAsync(cancellationToken);
        }

        private async Task<CheckoutSessionResolution> ResolveActiveSessionAsync(
            StorefrontCheckoutSessionRequest request,
            CancellationToken cancellationToken)
        {
            var resolution = await this.ResolveSessionForCartAsync(request, cancellationToken);
            if (!resolution.Success)
            {
                return resolution;
            }

            var session = resolution.Session!;
            var now = DateTimeOffset.UtcNow;
            if (session.ExpiresAtUtc <= now)
            {
                MarkExpired(session, now);
                await this.context.SaveChangesAsync(cancellationToken);
                return CheckoutSessionResolution.Failed(ServiceResponseType.Conflict, "Checkout session has expired.");
            }

            if (!IsActiveCheckoutState(session.State))
            {
                return CheckoutSessionResolution.Failed(ServiceResponseType.Conflict, "Checkout session is not active.");
            }

            await this.MarkCartChangedIfNeededAsync(session, resolution.Cart!, cancellationToken);

            return resolution;
        }

        private async Task<CheckoutSessionResolution> ResolveSessionForCartAsync(
            StorefrontCheckoutSessionRequest request,
            CancellationToken cancellationToken)
        {
            if (request.CheckoutSessionId == Guid.Empty)
            {
                return CheckoutSessionResolution.Failed(ServiceResponseType.ValidationError, "Checkout session is required.");
            }

            var cartResult = await this.ResolveStartCartAsync(request.StoreId, request.CartToken, cancellationToken);
            if (!cartResult.Success || cartResult.Payload is null)
            {
                return CheckoutSessionResolution.Failed(
                    cartResult.ResponseType,
                    cartResult.Message ?? "Cart could not be resolved.");
            }

            var cart = cartResult.Payload;
            var session = await this.context.CheckoutSessions
                .FirstOrDefaultAsync(
                    checkout => checkout.StoreId == request.StoreId
                        && checkout.CartSessionId == cart.Id
                        && checkout.PublicId == request.CheckoutSessionId,
                    cancellationToken);
            if (session is null)
            {
                return CheckoutSessionResolution.Failed(ServiceResponseType.NotFound, "Checkout session was not found.");
            }

            return CheckoutSessionResolution.Succeeded(session, cart);
        }

        private async Task<StorefrontCheckoutSessionResult> ToSessionResultAsync(
            CheckoutSession session,
            StorefrontCartSessionDto cart,
            CancellationToken cancellationToken,
            IReadOnlyList<StorefrontCheckoutPaymentMethodOption>? paymentMethods = null)
        {
            var selectedShippingOption = ParseSelectedShippingOption(session.SelectedShippingOptionJson);
            var resolvedPaymentMethods = paymentMethods
                ?? (selectedShippingOption is null
                    ? []
                    : await this.ResolvePaymentMethodOptionsAsync(session, session.PaymentMethodKey, cancellationToken));

            return ToSessionResult(session, cart, selectedShippingOption, resolvedPaymentMethods);
        }

        private static StorefrontCheckoutSessionResult ToSessionResult(
            CheckoutSession session,
            StorefrontCartSessionDto cart,
            StorefrontCheckoutShippingOption? selectedShippingOption,
            IReadOnlyList<StorefrontCheckoutPaymentMethodOption> paymentMethods)
        {
            var selectedPaymentMethod = paymentMethods.FirstOrDefault(method => method.Selected)
                ?? CreateSelectedPaymentMethod(session.PaymentMethodKey);

            return new StorefrontCheckoutSessionResult(
                session.PublicId,
                cart.PublicId,
                session.CheckoutVersion,
                cart.Version,
                session.LastValidatedCartVersion,
                session.State,
                session.CurrentStep,
                ParseCompletedSteps(session.CompletedStepsJson),
                IsActiveCheckoutState(session.State) && session.ExpiresAtUtc > DateTimeOffset.UtcNow,
                session.NextAction,
                session.CustomerEmail,
                session.CustomerName,
                session.PaymentMethodKey,
                session.Subtotal,
                session.ShippingTotal,
                session.TaxTotal,
                session.DiscountTotal,
                session.GrandTotal,
                NormalizeCurrency(session.CurrencyCode) ?? NormalizeCurrency(cart.CurrencyCode) ?? DefaultCurrencyCode,
                session.ExpiresAtUtc,
                ShippingRequired: true,
                selectedShippingOption,
                ResolveShippingOptions(session, selectedShippingOption?.Key),
                selectedPaymentMethod,
                paymentMethods,
                cart.Lines.Select(line => new StorefrontCheckoutLineSummary(
                    line.Id,
                    line.ProductId,
                    line.ProductVariantId,
                    Math.Max(0, line.Quantity),
                    line.UnitPrice ?? line.UnitPriceSnapshot ?? 0m,
                    line.LineTotal ?? line.LineSubtotal ?? 0m,
                    NormalizeCurrency(line.CurrencyCodeSnapshot) ?? NormalizeCurrency(cart.CurrencyCode) ?? DefaultCurrencyCode)).ToArray(),
                ParseValidationIssues(session.ValidationIssuesJson));
        }

        private static StorefrontCheckoutReviewResult ToReviewResult(
            CheckoutSession session,
            StorefrontCartSessionDto cart,
            StorefrontCheckoutShippingAddressDto? billingAddress,
            StorefrontCheckoutShippingAddressDto? shippingAddress,
            StorefrontCheckoutShippingOption? selectedShippingOption,
            StorefrontCheckoutPaymentMethodOption? selectedPaymentMethod,
            bool termsRequired,
            bool placeOrderAllowed,
            string nextRequiredStep,
            IReadOnlyList<StorefrontCheckoutValidationIssue> issues)
        {
            return new StorefrontCheckoutReviewResult(
                session.PublicId,
                cart.PublicId,
                session.CheckoutVersion,
                cart.Version,
                session.LastValidatedCartVersion,
                session.State,
                session.CurrentStep,
                ParseCompletedSteps(session.CompletedStepsJson),
                IsActiveCheckoutState(session.State) && session.ExpiresAtUtc > DateTimeOffset.UtcNow,
                session.NextAction,
                session.CustomerEmail,
                session.CustomerName,
                billingAddress,
                shippingAddress,
                selectedShippingOption,
                selectedPaymentMethod,
                cart.Lines.Select(line => new StorefrontCheckoutLineSummary(
                    line.Id,
                    line.ProductId,
                    line.ProductVariantId,
                    Math.Max(0, line.Quantity),
                    line.UnitPrice ?? line.UnitPriceSnapshot ?? 0m,
                    line.LineTotal ?? line.LineSubtotal ?? 0m,
                    NormalizeCurrency(line.CurrencyCodeSnapshot) ?? NormalizeCurrency(cart.CurrencyCode) ?? DefaultCurrencyCode)).ToArray(),
                session.Subtotal,
                session.ShippingTotal,
                session.TaxTotal,
                session.DiscountTotal,
                session.GrandTotal,
                NormalizeCurrency(session.CurrencyCode) ?? NormalizeCurrency(cart.CurrencyCode) ?? DefaultCurrencyCode,
                termsRequired,
                session.TermsAccepted,
                session.TermsVersion,
                session.TermsAcceptedAtUtc,
                placeOrderAllowed,
                nextRequiredStep,
                issues);
        }

        private static StorefrontCheckoutShippingAddressDto? ParseBillingAddress(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<StorefrontCheckoutShippingAddressDto>(json, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static StorefrontCheckoutShippingAddressDto? CreateShippingAddress(CheckoutSession session)
        {
            if (string.IsNullOrWhiteSpace(session.ShippingFullName)
                || string.IsNullOrWhiteSpace(session.ShippingEmail)
                || string.IsNullOrWhiteSpace(session.ShippingAddress1)
                || string.IsNullOrWhiteSpace(session.ShippingCity)
                || string.IsNullOrWhiteSpace(session.ShippingPostalCode)
                || string.IsNullOrWhiteSpace(session.ShippingCountryCode))
            {
                return null;
            }

            return new StorefrontCheckoutShippingAddressDto(
                session.ShippingFullName,
                session.ShippingEmail,
                session.ShippingPhone,
                session.ShippingAddress1,
                session.ShippingAddress2,
                session.ShippingCity,
                session.ShippingState,
                session.ShippingPostalCode,
                session.ShippingCountryCode);
        }

        private static IReadOnlyList<StorefrontCheckoutValidationIssue> DeduplicateIssues(
            IReadOnlyList<StorefrontCheckoutValidationIssue> issues)
        {
            return issues
                .GroupBy(issue => new { issue.Code, issue.Field, issue.LineId, issue.ProductId })
                .Select(group => group.First())
                .ToArray();
        }

        private static string ResolveNextRequiredStep(
            StorefrontCheckoutShippingAddressDto? billingAddress,
            StorefrontCheckoutShippingAddressDto? shippingAddress,
            StorefrontCheckoutShippingOption? selectedShippingOption,
            StorefrontCheckoutPaymentMethodOption? selectedPaymentMethod)
        {
            if (billingAddress is null)
            {
                return CheckoutSteps.BillingAddress;
            }

            if (shippingAddress is null)
            {
                return CheckoutSteps.ShippingAddress;
            }

            if (selectedShippingOption is null)
            {
                return CheckoutSteps.ShippingMethod;
            }

            return selectedPaymentMethod is null ? CheckoutSteps.PaymentMethod : CheckoutSteps.Review;
        }

        private static void ClearTermsAcknowledgement(CheckoutSession session)
        {
            session.TermsAccepted = false;
            session.TermsVersion = null;
            session.TermsAcceptedAtUtc = null;
        }

        private static string? NormalizeTermsVersion(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized is null || normalized.Length <= 64 ? normalized : normalized[..64];
        }

        private static void Touch(
            CheckoutSession session,
            string state,
            string currentStep,
            DateTimeOffset now)
        {
            session.State = state;
            session.CurrentStep = currentStep;
            session.CheckoutVersion = Math.Max(1, session.CheckoutVersion) + 1;
            session.UpdatedAtUtc = now;
        }

        private static void MarkExpired(CheckoutSession session, DateTimeOffset now)
        {
            Touch(session, CheckoutSessionStates.Expired, CheckoutSteps.Entry, now);
            session.NextAction = "expired";
        }

        private static bool IsActiveCheckoutState(string state)
        {
            return string.Equals(state, CheckoutSessionStates.Draft, StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, CheckoutSessionStates.Ready, StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, CheckoutSessionStates.OrderPending, StringComparison.OrdinalIgnoreCase);
        }

        private static ServiceResponseType ResolveEntryValidationResponseType(
            IReadOnlyList<StorefrontCheckoutValidationIssue> issues)
        {
            return issues.Any(issue => issue.Code is "cart.inactive" or "cart.validation_failed")
                ? ServiceResponseType.Conflict
                : ServiceResponseType.ValidationError;
        }

        private static IReadOnlyList<StorefrontCheckoutShippingOption> ResolveShippingOptions(
            CheckoutSession session,
            string? selectedKey)
        {
            var currencyCode = NormalizeCurrency(session.CurrencyCode) ?? DefaultCurrencyCode;
            var selected = string.Equals(selectedKey, FreeStandardShippingOptionKey, StringComparison.OrdinalIgnoreCase);
            return
            [
                new StorefrontCheckoutShippingOption(
                    FreeStandardShippingOptionKey,
                    "Free standard",
                    "Standard shipping for MVP stores.",
                    0m,
                    currencyCode,
                    "Standard delivery",
                    selected),
            ];
        }

        private async Task<IReadOnlyList<StorefrontCheckoutPaymentMethodOption>> ResolvePaymentMethodOptionsAsync(
            CheckoutSession session,
            string selectedKey,
            CancellationToken cancellationToken)
        {
            var currencyCode = NormalizeCurrency(session.CurrencyCode) ?? DefaultCurrencyCode;
            var countryCode = NormalizeCountry(session.ShippingCountryCode);
            var orderTotal = session.GrandTotal;
            var methods = await this.context.StorePaymentMethods
                .AsNoTracking()
                .Where(method => method.StoreId == session.StoreId && method.Enabled)
                .OrderBy(method => method.DisplayOrder)
                .ThenBy(method => method.DisplayName)
                .ToArrayAsync(cancellationToken);

            return methods
                .Where(method =>
                    SupportsValue(ParseCodes(method.SupportedCurrencyCodesJson), currencyCode)
                    && SupportsValue(ParseCodes(method.SupportedCountryCodesJson), countryCode)
                    && (!method.MinOrderTotal.HasValue || orderTotal >= method.MinOrderTotal.Value)
                    && (!method.MaxOrderTotal.HasValue || orderTotal <= method.MaxOrderTotal.Value))
                .Select(method => new StorefrontCheckoutPaymentMethodOption(
                    NormalizeKey(method.PaymentMethodKey),
                    method.DisplayName,
                    method.Description,
                    method.ShortDisplayText,
                    method.IconUrl,
                    NormalizeKey(method.PaymentMethodKey),
                    ResolvePaymentNextActionKind(method.PaymentMethodKey),
                    string.Equals(method.PaymentMethodKey, selectedKey, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }

        private static StorefrontCheckoutPaymentMethodOption? CreateSelectedPaymentMethod(string? paymentMethodKey)
        {
            var key = NormalizeKey(paymentMethodKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return new StorefrontCheckoutPaymentMethodOption(
                key,
                key,
                null,
                null,
                null,
                key,
                ResolvePaymentNextActionKind(key),
                Selected: true);
        }

        private static bool SupportsValue(IReadOnlyList<string> supportedValues, string? value)
        {
            return supportedValues.Count == 0
                || (value is not null && supportedValues.Contains(value, StringComparer.OrdinalIgnoreCase));
        }

        private static string ResolvePaymentNextActionKind(string paymentMethodKey)
        {
            return string.Equals(paymentMethodKey, PaymentMethodKeys.Stripe, StringComparison.OrdinalIgnoreCase)
                ? "redirect"
                : "none";
        }

        private static StorefrontCheckoutShippingOption? ParseSelectedShippingOption(string? json)
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

        private static IReadOnlyList<string> ParseCompletedSteps(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<IReadOnlyList<string>>(json, JsonOptions)
                    ?.Where(step => CheckoutSteps.All.Contains(step))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray() ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static IReadOnlyList<StorefrontCheckoutValidationIssue> ParseValidationIssues(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<IReadOnlyList<StorefrontCheckoutValidationIssue>>(json, JsonOptions) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private async Task<bool> IsPaymentMethodAvailableAsync(
            Guid storeId,
            string paymentMethodKey,
            string? currencyCode,
            string? countryCode,
            decimal orderTotal,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(paymentMethodKey))
            {
                return false;
            }

            var method = await this.context.StorePaymentMethods
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    method => method.StoreId == storeId
                        && method.PaymentMethodKey == paymentMethodKey,
                    cancellationToken);

            if (method is null || !method.Enabled)
            {
                return false;
            }

            var currencyCodes = ParseCodes(method.SupportedCurrencyCodesJson);
            var normalizedCurrency = NormalizeCurrency(currencyCode);
            if (currencyCodes.Count > 0
                && (normalizedCurrency is null || !currencyCodes.Contains(normalizedCurrency, StringComparer.Ordinal)))
            {
                return false;
            }

            var countryCodes = ParseCodes(method.SupportedCountryCodesJson);
            var normalizedCountry = NormalizeCountry(countryCode);
            if (countryCodes.Count > 0
                && (normalizedCountry is null || !countryCodes.Contains(normalizedCountry, StringComparer.Ordinal)))
            {
                return false;
            }

            if (method.MinOrderTotal.HasValue && orderTotal < method.MinOrderTotal.Value)
            {
                return false;
            }

            if (method.MaxOrderTotal.HasValue && orderTotal > method.MaxOrderTotal.Value)
            {
                return false;
            }

            return true;
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

        private CurrencyRateSnapshot ResolveCurrencyRateSnapshot(
            IEnumerable<StorefrontCartLineDto> cartLines,
            string currencyCode,
            decimal totalAmount)
        {
            var snapshots = cartLines.Select(line => new CurrencyRateLineSnapshot(
                NormalizeCurrency(line.BaseCurrencyCodeSnapshot),
                line.BaseUnitPriceSnapshot,
                NormalizeCurrency(line.CurrencyCodeSnapshot) ?? currencyCode,
                line.ExchangeRateSnapshot,
                NormalizeKey(line.ExchangeRateProviderKey),
                NormalizeNullable(line.ExchangeRateSource),
                line.ExchangeRateEffectiveAtUtc,
                line.ExchangeRateExpiresAtUtc,
                line.Quantity)).ToArray();

            return this.ResolveCurrencyRateSnapshot(snapshots, currencyCode, totalAmount);
        }

        private CurrencyRateSnapshot ResolveCurrencyRateSnapshot(
            IEnumerable<OrderLineSnapshot> orderLines,
            string currencyCode,
            decimal totalAmount)
        {
            var snapshots = orderLines.Select(line => new CurrencyRateLineSnapshot(
                NormalizeCurrency(line.CartLine.BaseCurrencyCodeSnapshot),
                line.CartLine.BaseUnitPriceSnapshot,
                NormalizeCurrency(line.CartLine.CurrencyCodeSnapshot) ?? currencyCode,
                line.CartLine.ExchangeRateSnapshot,
                NormalizeKey(line.CartLine.ExchangeRateProviderKey),
                NormalizeNullable(line.CartLine.ExchangeRateSource),
                line.CartLine.ExchangeRateEffectiveAtUtc,
                line.CartLine.ExchangeRateExpiresAtUtc,
                line.CartLine.Quantity)).ToArray();

            return this.ResolveCurrencyRateSnapshot(snapshots, currencyCode, totalAmount);
        }

        private CurrencyRateSnapshot ResolveCurrencyRateSnapshot(
            IReadOnlyList<CurrencyRateLineSnapshot> snapshots,
            string currencyCode,
            decimal totalAmount)
        {
            var convertedSnapshots = snapshots
                .Where(line => line.ExchangeRate.HasValue)
                .ToArray();
            if (convertedSnapshots.Length == 0)
            {
                return CurrencyRateSnapshot.None();
            }

            if (convertedSnapshots.Any(line => line.BaseCurrencyCode is null || line.BaseUnitPrice is null))
            {
                return CurrencyRateSnapshot.Failed(
                    ServiceResponseType.Conflict,
                    "Cart line currency rate snapshot is missing. Re-add the affected items.");
            }

            var first = convertedSnapshots[0];
            var hasMixedSnapshot = convertedSnapshots.Any(line =>
                !string.Equals(line.BaseCurrencyCode, first.BaseCurrencyCode, StringComparison.Ordinal)
                || line.ExchangeRate != first.ExchangeRate
                || !string.Equals(line.ProviderKey, first.ProviderKey, StringComparison.Ordinal)
                || !string.Equals(line.Source, first.Source, StringComparison.Ordinal)
                || line.EffectiveAtUtc != first.EffectiveAtUtc
                || line.ExpiresAtUtc != first.ExpiresAtUtc);
            if (hasMixedSnapshot)
            {
                return CurrencyRateSnapshot.Failed(
                    ServiceResponseType.Conflict,
                    "Cart lines use mixed exchange-rate snapshots. Re-add items before checkout.");
            }

            var normalizedCurrency = NormalizeCurrency(currencyCode) ?? DefaultCurrencyCode;
            if (!snapshots.All(line => string.Equals(line.CurrencyCode, normalizedCurrency, StringComparison.Ordinal)))
            {
                return CurrencyRateSnapshot.Failed(
                    ServiceResponseType.Conflict,
                    "Cart line currency does not match checkout currency.");
            }

            var baseTotal = this.moneyRoundingService.RoundOrderTotal(
                snapshots.Sum(line => (line.BaseUnitPrice ?? 0m) * line.Quantity),
                first.BaseCurrencyCode!);

            return CurrencyRateSnapshot.Converted(
                first.BaseCurrencyCode!,
                baseTotal,
                totalAmount,
                first.ExchangeRate!.Value,
                first.ProviderKey,
                first.Source,
                first.EffectiveAtUtc,
                first.ExpiresAtUtc);
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

        private async Task<ShippingAddressResolution> ResolveShippingAddressAsync(
            StorefrontCheckoutPreviewRequest request,
            CancellationToken cancellationToken)
        {
            if (!request.ShippingAddressId.HasValue)
            {
                return ShippingAddressResolution.Succeeded(request.ShippingAddress);
            }

            if (string.IsNullOrWhiteSpace(request.CustomerAppUserId))
            {
                return ShippingAddressResolution.Failed(new StorefrontCheckoutValidationIssue(
                    "shipping.address_auth_required",
                    "Sign in before using a saved shipping address.",
                    "shippingAddressId"));
            }

            var savedAddress = await this.context.CommerceCustomerAddresses
                .AsNoTracking()
                .Include(address => address.Customer)
                .FirstOrDefaultAsync(
                    address =>
                        address.StoreId == request.StoreId
                        && address.PublicId == request.ShippingAddressId.Value
                        && address.DeletedAtUtc == null
                        && address.Customer != null
                        && address.Customer.StoreId == request.StoreId
                        && address.Customer.AppUserId == request.CustomerAppUserId,
                    cancellationToken);

            if (savedAddress is null)
            {
                return ShippingAddressResolution.Failed(new StorefrontCheckoutValidationIssue(
                    "shipping.address_not_found",
                    "Saved shipping address was not found.",
                    "shippingAddressId"));
            }

            return ShippingAddressResolution.Succeeded(new StorefrontCheckoutShippingAddressDto(
                JoinName(savedAddress.FirstName, savedAddress.LastName),
                NormalizeNullable(savedAddress.Email) ?? NormalizeNullable(request.CustomerEmail) ?? string.Empty,
                savedAddress.Phone,
                savedAddress.Address1,
                savedAddress.Address2,
                savedAddress.City,
                NormalizeNullable(savedAddress.StateProvinceName) ?? NormalizeNullable(savedAddress.StateProvinceCode),
                savedAddress.PostalCode,
                savedAddress.CountryCode));
        }

        private async Task<ShippingAddressResolution> ResolveCheckoutAddressAsync(
            Guid storeId,
            Guid? addressId,
            StorefrontCheckoutShippingAddressDto? directAddress,
            string? customerAppUserId,
            string addressIdField,
            CancellationToken cancellationToken)
        {
            if (!addressId.HasValue)
            {
                return ShippingAddressResolution.Succeeded(directAddress);
            }

            var purpose = addressIdField.StartsWith("billing", StringComparison.OrdinalIgnoreCase)
                ? "billing"
                : "shipping";
            if (string.IsNullOrWhiteSpace(customerAppUserId))
            {
                return ShippingAddressResolution.Failed(new StorefrontCheckoutValidationIssue(
                    $"{purpose}.address_auth_required",
                    $"Sign in before using a saved {purpose} address.",
                    addressIdField));
            }

            var savedAddress = await this.context.CommerceCustomerAddresses
                .AsNoTracking()
                .Include(address => address.Customer)
                .FirstOrDefaultAsync(
                    address =>
                        address.StoreId == storeId
                        && address.PublicId == addressId.Value
                        && address.DeletedAtUtc == null
                        && address.Customer != null
                        && address.Customer.StoreId == storeId
                        && address.Customer.AppUserId == customerAppUserId,
                    cancellationToken);

            if (savedAddress is null)
            {
                return ShippingAddressResolution.Failed(new StorefrontCheckoutValidationIssue(
                    $"{purpose}.address_not_found",
                    $"Saved {purpose} address was not found.",
                    addressIdField));
            }

            return ShippingAddressResolution.Succeeded(new StorefrontCheckoutShippingAddressDto(
                JoinName(savedAddress.FirstName, savedAddress.LastName),
                NormalizeNullable(savedAddress.Email) ?? string.Empty,
                savedAddress.Phone,
                savedAddress.Address1,
                savedAddress.Address2,
                savedAddress.City,
                NormalizeNullable(savedAddress.StateProvinceName) ?? NormalizeNullable(savedAddress.StateProvinceCode),
                savedAddress.PostalCode,
                savedAddress.CountryCode));
        }

        private IEnumerable<StorefrontCheckoutValidationIssue> ValidateAddressFields(
            string issuePrefix,
            string fieldPrefix,
            StorefrontCheckoutShippingAddressDto address)
        {
            if (NormalizeNullable(address.FullName) is null)
            {
                yield return new StorefrontCheckoutValidationIssue($"{issuePrefix}.full_name_required", "Full name is required.", $"{fieldPrefix}.fullName");
            }

            var validation = this.addressValidationService.ValidateAndNormalize(new CustomerAddressCreateRequest(
                "checkout",
                "checkout",
                null,
                address.Address1,
                address.Address2,
                address.City,
                address.PostalCode,
                address.CountryCode,
                address.State,
                address.State,
                address.Phone,
                address.Email,
                IsDefaultShipping: false,
                IsDefaultBilling: false));

            foreach (var issue in validation.Issues)
            {
                yield return issue.Code switch
                {
                    "email_invalid" => new StorefrontCheckoutValidationIssue($"{issuePrefix}.email_invalid", "Email is invalid.", $"{fieldPrefix}.email"),
                    "address1_required" => new StorefrontCheckoutValidationIssue($"{issuePrefix}.address1_required", "Address line 1 is required.", $"{fieldPrefix}.address1"),
                    "city_required" => new StorefrontCheckoutValidationIssue($"{issuePrefix}.city_required", "City is required.", $"{fieldPrefix}.city"),
                    "postal_code_required" => new StorefrontCheckoutValidationIssue($"{issuePrefix}.postal_required", "Postal code is required.", $"{fieldPrefix}.postalCode"),
                    "country_invalid" => new StorefrontCheckoutValidationIssue($"{issuePrefix}.country_invalid", "Country code must be a two-letter ISO code.", $"{fieldPrefix}.countryCode"),
                    "state_province_required" => new StorefrontCheckoutValidationIssue($"{issuePrefix}.state_required", "State or province is required.", $"{fieldPrefix}.state"),
                    _ => new StorefrontCheckoutValidationIssue($"{issuePrefix}.{issue.Code}", issue.Message, $"{fieldPrefix}.{issue.Field}"),
                };
            }
        }

        private IEnumerable<StorefrontCheckoutValidationIssue> ValidateCheckoutFields(
            StorefrontCheckoutPreviewRequest request,
            StorefrontCheckoutShippingAddressDto? shipping)
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

            if (shipping is null)
            {
                yield return new StorefrontCheckoutValidationIssue("shipping.address_required", "Shipping address is required.", "shippingAddress");
                yield break;
            }

            if (NormalizeNullable(shipping.FullName) is null)
            {
                yield return new StorefrontCheckoutValidationIssue("shipping.full_name_required", "Shipping full name is required.", "shippingAddress.fullName");
            }

            var validation = this.addressValidationService.ValidateAndNormalize(new CustomerAddressCreateRequest(
                "checkout",
                "checkout",
                null,
                shipping.Address1,
                shipping.Address2,
                shipping.City,
                shipping.PostalCode,
                shipping.CountryCode,
                shipping.State,
                shipping.State,
                shipping.Phone,
                shipping.Email,
                IsDefaultShipping: false,
                IsDefaultBilling: false));

            foreach (var issue in validation.Issues)
            {
                yield return issue.Code switch
                {
                    "email_invalid" => new StorefrontCheckoutValidationIssue("shipping.email_invalid", "Shipping email is invalid.", "shippingAddress.email"),
                    "address1_required" => new StorefrontCheckoutValidationIssue("shipping.address1_required", "Shipping address line 1 is required.", "shippingAddress.address1"),
                    "city_required" => new StorefrontCheckoutValidationIssue("shipping.city_required", "Shipping city is required.", "shippingAddress.city"),
                    "postal_code_required" => new StorefrontCheckoutValidationIssue("shipping.postal_required", "Shipping postal code is required.", "shippingAddress.postalCode"),
                    "country_invalid" => new StorefrontCheckoutValidationIssue("shipping.country_invalid", "Shipping country code must be a two-letter ISO code.", "shippingAddress.countryCode"),
                    "state_province_required" => new StorefrontCheckoutValidationIssue("shipping.state_required", "Shipping state or province is required.", "shippingAddress.state"),
                    _ => new StorefrontCheckoutValidationIssue($"shipping.{issue.Code}", issue.Message, $"shippingAddress.{issue.Field}"),
                };
            }
        }

        private static string JoinName(string firstName, string lastName)
        {
            return string.Join(
                ' ',
                new[] { NormalizeNullable(firstName), NormalizeNullable(lastName) }
                    .Where(value => value is not null)
                    .Select(value => value!));
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

        private static string? NormalizeCountry(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized is null ? null : normalized.ToUpperInvariant();
        }

        private static IReadOnlyList<string> ParseCodes(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            try
            {
                return JsonSerializer.Deserialize<IReadOnlyList<string>>(json)
                    ?.Select(NormalizeCountry)
                    .Where(code => code is not null)
                    .Select(code => code!)
                    .ToArray() ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
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

        private sealed record CurrencyRateLineSnapshot(
            string? BaseCurrencyCode,
            decimal? BaseUnitPrice,
            string CurrencyCode,
            decimal? ExchangeRate,
            string? ProviderKey,
            string? Source,
            DateTimeOffset? EffectiveAtUtc,
            DateTimeOffset? ExpiresAtUtc,
            int Quantity);

        private sealed record CurrencyRateSnapshot(
            bool Success,
            ServiceResponseType ResponseType,
            string Message,
            string? BaseCurrencyCode,
            decimal? BaseTotalAmount,
            decimal? ConvertedTotalAmount,
            decimal? ExchangeRate,
            string? ExchangeRateProviderKey,
            string? ExchangeRateSource,
            DateTimeOffset? ExchangeRateEffectiveAtUtc,
            DateTimeOffset? ExchangeRateExpiresAtUtc)
        {
            public static CurrencyRateSnapshot None()
            {
                return new CurrencyRateSnapshot(
                    true,
                    ServiceResponseType.Success,
                    "Currency conversion snapshot is not required.",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null);
            }

            public static CurrencyRateSnapshot Converted(
                string baseCurrencyCode,
                decimal baseTotalAmount,
                decimal convertedTotalAmount,
                decimal exchangeRate,
                string? exchangeRateProviderKey,
                string? exchangeRateSource,
                DateTimeOffset? exchangeRateEffectiveAtUtc,
                DateTimeOffset? exchangeRateExpiresAtUtc)
            {
                return new CurrencyRateSnapshot(
                    true,
                    ServiceResponseType.Success,
                    "Currency conversion snapshot resolved.",
                    baseCurrencyCode,
                    baseTotalAmount,
                    convertedTotalAmount,
                    exchangeRate,
                    exchangeRateProviderKey,
                    exchangeRateSource,
                    exchangeRateEffectiveAtUtc,
                    exchangeRateExpiresAtUtc);
            }

            public static CurrencyRateSnapshot Failed(ServiceResponseType responseType, string message)
            {
                return new CurrencyRateSnapshot(
                    false,
                    responseType,
                    message,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null);
            }
        }

        private sealed record ShippingAddressResolution(
            StorefrontCheckoutShippingAddressDto? Address,
            IReadOnlyList<StorefrontCheckoutValidationIssue> Issues)
        {
            public static ShippingAddressResolution Succeeded(StorefrontCheckoutShippingAddressDto? address)
            {
                return new ShippingAddressResolution(address, []);
            }

            public static ShippingAddressResolution Failed(StorefrontCheckoutValidationIssue issue)
            {
                return new ShippingAddressResolution(null, [issue]);
            }
        }

        private sealed record CheckoutSessionResolution(
            bool Success,
            ServiceResponseType ResponseType,
            string Message,
            CheckoutSession? Session,
            StorefrontCartSessionDto? Cart)
        {
            public static CheckoutSessionResolution Succeeded(
                CheckoutSession session,
                StorefrontCartSessionDto cart)
            {
                return new CheckoutSessionResolution(true, ServiceResponseType.Success, "Checkout session resolved.", session, cart);
            }

            public static CheckoutSessionResolution Failed(ServiceResponseType responseType, string message)
            {
                return new CheckoutSessionResolution(false, responseType, message, null, null);
            }
        }

        private sealed record CheckoutEntryValidation(
            IReadOnlyList<StorefrontCheckoutValidationIssue> Issues,
            StorefrontCartValidationResult? CartValidation);
    }
}
