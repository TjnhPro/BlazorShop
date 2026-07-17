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
    using BlazorShop.Application.CommerceNode.Shipping;
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
        private const string ShippingNotRequiredOptionKey = "shipping_not_required";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly CommerceNodeDbContext context;
        private readonly IStorefrontCartService cartService;
        private readonly IStoreCurrencyResolver storeCurrencyResolver;
        private readonly IMoneyRoundingService moneyRoundingService;
        private readonly IMoneyConversionService moneyConversionService;
        private readonly IStorefrontCustomerService customerService;
        private readonly IStoreFeatureStateService featureStateService;
        private readonly IPaymentProviderCapabilityRegistry paymentProviderCapabilityRegistry;
        private readonly IStorefrontPaymentProviderResolver paymentProviderResolver;
        private readonly IProductSellabilityResolver sellabilityResolver;
        private readonly IAddressValidationService addressValidationService;
        private readonly IShippingCalculator shippingCalculator;
        private readonly IShippingTaxCalculator shippingTaxCalculator;
        private readonly IOrderPlacementService orderPlacementService;

        public StorefrontCheckoutService(
            CommerceNodeDbContext context,
            IStorefrontCartService cartService,
            IStoreCurrencyResolver storeCurrencyResolver,
            IMoneyRoundingService moneyRoundingService,
            IMoneyConversionService moneyConversionService,
            IStorefrontCustomerService customerService,
            IStoreFeatureStateService featureStateService,
            IPaymentProviderCapabilityRegistry paymentProviderCapabilityRegistry,
            IStorefrontPaymentProviderResolver paymentProviderResolver,
            IProductSellabilityResolver? sellabilityResolver = null,
            IAddressValidationService? addressValidationService = null,
            IShippingCalculator? shippingCalculator = null,
            IShippingTaxCalculator? shippingTaxCalculator = null,
            IOrderPlacementService? orderPlacementService = null)
        {
            this.context = context;
            this.cartService = cartService;
            this.storeCurrencyResolver = storeCurrencyResolver;
            this.moneyRoundingService = moneyRoundingService;
            this.moneyConversionService = moneyConversionService;
            this.customerService = customerService;
            this.featureStateService = featureStateService;
            this.paymentProviderCapabilityRegistry = paymentProviderCapabilityRegistry;
            this.paymentProviderResolver = paymentProviderResolver;
            this.sellabilityResolver = sellabilityResolver ?? new ProductSellabilityResolver();
            this.addressValidationService = addressValidationService ?? new AddressValidationService();
            this.shippingCalculator = shippingCalculator ?? new ShippingCalculator([new InternalFreeStandardShippingProvider()]);
            this.shippingTaxCalculator = shippingTaxCalculator ?? new ZeroShippingTaxCalculator();
            this.orderPlacementService = orderPlacementService ?? new OrderPlacementService(context, this.moneyRoundingService, this.sellabilityResolver);
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
            var shippingOptions = await this.ResolveShippingOptionsAsync(
                session,
                resolution.Cart!,
                selectedKey: null,
                cancellationToken);
            var taxResult = await this.CalculateShippingTaxAsync(
                session.StoreId,
                CreateShippingAddress(session),
                session.CurrencyCode,
                session.Subtotal,
                shippingTotal: 0m,
                cancellationToken);
            session.ShippingTotal = 0m;
            session.TaxTotal = taxResult.TaxTotal;
            session.GrandTotal = this.moneyRoundingService.RoundOrderTotal(
                session.Subtotal + session.TaxTotal - session.DiscountTotal,
                session.CurrencyCode);

            if (shippingOptions.ShippingRequired)
            {
                session.CompletedStepsJson = JsonSerializer.Serialize(
                    new[] { CheckoutSteps.Entry, CheckoutSteps.BillingAddress, CheckoutSteps.ShippingAddress },
                    JsonOptions);
                session.NextAction = CheckoutSteps.ShippingMethod;
                Touch(session, CheckoutSessionStates.Draft, CheckoutSteps.ShippingMethod, DateTimeOffset.UtcNow);
            }
            else
            {
                session.CompletedStepsJson = JsonSerializer.Serialize(
                    new[] { CheckoutSteps.Entry, CheckoutSteps.BillingAddress, CheckoutSteps.ShippingAddress, CheckoutSteps.ShippingMethod },
                    JsonOptions);
                session.NextAction = CheckoutSteps.PaymentMethod;
                Touch(session, CheckoutSessionStates.Draft, CheckoutSteps.PaymentMethod, DateTimeOffset.UtcNow);
            }
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
            var shippingOptions = await this.ResolveShippingOptionsAsync(
                session,
                resolution.Cart!,
                request.ShippingOptionKey,
                cancellationToken);

            if (shippingOptions.ShippingRequired
                && (string.IsNullOrWhiteSpace(session.ShippingAddress1)
                || string.IsNullOrWhiteSpace(session.ShippingCity)
                || string.IsNullOrWhiteSpace(session.ShippingPostalCode)
                || string.IsNullOrWhiteSpace(session.ShippingCountryCode)))
            {
                return Failed<StorefrontCheckoutSessionResult>(ServiceResponseType.Conflict, "Shipping address must be selected first.");
            }

            if (!shippingOptions.ShippingRequired)
            {
                if (!string.Equals(request.ShippingOptionKey, ShippingNotRequiredOptionKey, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(request.ShippingOptionKey))
                {
                    return Failed<StorefrontCheckoutSessionResult>(ServiceResponseType.ValidationError, "Shipping option is not available.");
                }

                session.SelectedShippingOptionJson = null;
                var taxResult = await this.CalculateShippingTaxAsync(
                    session.StoreId,
                    CreateShippingAddress(session),
                    session.CurrencyCode,
                    session.Subtotal,
                    shippingTotal: 0m,
                    cancellationToken);
                session.ShippingTotal = 0m;
                session.TaxTotal = taxResult.TaxTotal;
                session.GrandTotal = this.moneyRoundingService.RoundOrderTotal(
                    session.Subtotal + session.TaxTotal - session.DiscountTotal,
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

            if (shippingOptions.Errors.Count > 0)
            {
                return Failed<StorefrontCheckoutSessionResult>(
                    ServiceResponseType.Conflict,
                    shippingOptions.Errors[0]);
            }

            var option = shippingOptions.Options
                .FirstOrDefault(candidate => string.Equals(candidate.Key, request.ShippingOptionKey, StringComparison.OrdinalIgnoreCase));
            if (option is null)
            {
                return Failed<StorefrontCheckoutSessionResult>(ServiceResponseType.ValidationError, "Shipping option is not available.");
            }

            session.SelectedShippingOptionJson = JsonSerializer.Serialize(option with { Selected = true }, JsonOptions);
            var selectedTaxResult = await this.CalculateShippingTaxAsync(
                session.StoreId,
                CreateShippingAddress(session),
                session.CurrencyCode,
                session.Subtotal,
                option.Price,
                cancellationToken);
            session.ShippingTotal = option.Price;
            session.TaxTotal = selectedTaxResult.TaxTotal;
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
            var shippingOptions = await this.ResolveShippingOptionsAsync(
                session,
                resolution.Cart!,
                ParseSelectedShippingOption(session.SelectedShippingOptionJson)?.Key,
                cancellationToken);
            if (shippingOptions.ShippingRequired && shippingOptions.Options.All(option => !option.Selected))
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
            var shippingOptions = await this.ResolveShippingOptionsAsync(
                session,
                cart,
                selectedShippingOption?.Key,
                cancellationToken);
            selectedShippingOption = shippingOptions.Options.FirstOrDefault(option => option.Selected);
            var paymentMethods = shippingOptions.ShippingRequired && selectedShippingOption is null
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

            if (shippingOptions.ShippingRequired && shippingAddress is null)
            {
                issues.Add(new StorefrontCheckoutValidationIssue(
                    "shipping.address_required",
                    "Shipping address is required.",
                    "shippingAddress"));
            }

            if (shippingOptions.ShippingRequired && selectedShippingOption is null)
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
                && (!shippingOptions.ShippingRequired || selectedShippingOption is not null)
                && selectedPaymentMethod is not null;
            var nextRequiredStep = placeOrderAllowed
                ? CheckoutSteps.PlaceOrder
                : ResolveNextRequiredStep(billingAddress, shippingOptions.ShippingRequired ? shippingAddress : null, selectedShippingOption, selectedPaymentMethod, shippingOptions.ShippingRequired);
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
                    shippingOptions.ShippingRequired ? shippingAddress : null,
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

            var packageLines = await this.BuildShippingPackageLinesAsync(request.StoreId, cart.Lines, cancellationToken);
            var shippingRateCurrency = this.ResolveShippingRateCurrency(cart.Lines, currencyCode, subtotal);
            var shippingResult = await this.CalculateShippingAsync(
                request.StoreId,
                cart.Id,
                cart.PublicId,
                shippingAddress,
                currencyCode,
                subtotal,
                shippingRateCurrency.BaseCurrencyCode,
                shippingRateCurrency.BaseSubtotal,
                packageLines,
                cancellationToken);
            issues.AddRange(ToShippingIssues(shippingResult));
            var selectedShippingOption = shippingResult.ShippingRequired
                ? shippingResult.Options.FirstOrDefault()
                : null;
            if (shippingResult.ShippingRequired && selectedShippingOption is null)
            {
                issues.Add(new StorefrontCheckoutValidationIssue(
                    "shipping.option_unavailable",
                    "Shipping option is not available.",
                    "shippingOptionKey"));
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
            selectedShippingOption = isValid && selectedShippingOption is not null
                ? selectedShippingOption with { Selected = true }
                : selectedShippingOption;
            var shippingTotal = selectedShippingOption?.Price ?? 0m;
            var taxResult = await this.CalculateShippingTaxAsync(
                request.StoreId,
                shippingAddress,
                currencyCode,
                subtotal,
                shippingTotal,
                cancellationToken);
            var grandTotal = this.moneyRoundingService.RoundOrderTotal(
                subtotal + shippingTotal + taxResult.TaxTotal,
                currencyCode);
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
                BillingAddressSnapshotJson = shippingAddress is null ? null : JsonSerializer.Serialize(shippingAddress, JsonOptions),
                ShippingAddressSource = request.ShippingAddressId.HasValue ? "saved" : "direct",
                ShippingFullName = NormalizeNullable(shippingAddress?.FullName) ?? string.Empty,
                ShippingEmail = NormalizeNullable(shippingAddress?.Email) ?? string.Empty,
                ShippingPhone = NormalizeNullable(shippingAddress?.Phone),
                ShippingAddress1 = NormalizeNullable(shippingAddress?.Address1) ?? string.Empty,
                ShippingAddress2 = NormalizeNullable(shippingAddress?.Address2),
                ShippingCity = NormalizeNullable(shippingAddress?.City) ?? string.Empty,
                ShippingState = NormalizeNullable(shippingAddress?.State),
                ShippingPostalCode = NormalizeNullable(shippingAddress?.PostalCode) ?? string.Empty,
                ShippingCountryCode = NormalizeNullable(shippingAddress?.CountryCode)?.ToUpperInvariant() ?? string.Empty,
                SelectedShippingOptionJson = selectedShippingOption is null ? null : JsonSerializer.Serialize(selectedShippingOption, JsonOptions),
                PaymentMethodKey = paymentMethodKey,
                Subtotal = subtotal,
                ShippingTotal = shippingTotal,
                TaxTotal = taxResult.TaxTotal,
                DiscountTotal = 0m,
                GrandTotal = grandTotal,
                CurrencyCode = currencyCode,
                BaseCurrencyCode = rateSnapshot.BaseCurrencyCode,
                BaseSubtotal = rateSnapshot.BaseTotalAmount,
                BaseGrandTotal = rateSnapshot.BaseTotalAmount + shippingTotal,
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

            if (request.ExpectedCheckoutVersion < 1)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.ValidationError, "Checkout version is required.");
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

            if (session.CheckoutVersion != request.ExpectedCheckoutVersion)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Checkout version is stale.");
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

            var selectedShippingOption = ParseSelectedShippingOption(session.SelectedShippingOptionJson);
            var shippingAddress = CreateShippingAddress(session);

            if (ParseValidationIssues(session.ValidationIssuesJson).Count > 0)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Checkout review has validation issues.");
            }

            var paymentMethodKey = NormalizeKey(session.PaymentMethodKey);
            var capabilityResult = this.paymentProviderCapabilityRegistry.Get(paymentMethodKey);
            if (!capabilityResult.Success || capabilityResult.Payload is null)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Payment provider is not available for order placement.");
            }

            var capability = capabilityResult.Payload;
            if (!capability.Installed || !capability.Active)
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
            var subtotal = this.moneyRoundingService.RoundOrderTotal(
                lines.Sum(line => this.moneyRoundingService.RoundLineTotal(line.CartLine.Quantity * line.UnitPrice, currencyCode)),
                currencyCode);
            var shippingRateCurrency = this.ResolveShippingRateCurrency(cart.Lines, currencyCode, subtotal);
            var shippingResult = await this.CalculateShippingAsync(
                request.StoreId,
                cart.Id,
                cart.PublicId,
                shippingAddress,
                currencyCode,
                subtotal,
                shippingRateCurrency.BaseCurrencyCode,
                shippingRateCurrency.BaseSubtotal,
                await this.BuildShippingPackageLinesAsync(request.StoreId, cart.Lines, cancellationToken),
                cancellationToken);
            if (shippingResult.ShippingRequired && shippingAddress is null)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Shipping address is not ready for order placement.");
            }

            if (shippingResult.Errors.Count > 0)
            {
                return Failed<StorefrontPlaceOrderResult>(
                    ServiceResponseType.Conflict,
                    shippingResult.Errors[0]);
            }

            var currentSelectedShippingOption = shippingResult.Options
                .FirstOrDefault(option => string.Equals(option.Key, selectedShippingOption?.Key, StringComparison.OrdinalIgnoreCase));
            if (shippingResult.ShippingRequired && currentSelectedShippingOption is null)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Shipping option is not available.");
            }

            var shippingTotal = currentSelectedShippingOption?.Price ?? 0m;
            var taxResult = await this.CalculateShippingTaxAsync(
                request.StoreId,
                shippingAddress,
                currencyCode,
                subtotal,
                shippingTotal,
                cancellationToken);
            var totalAmount = this.moneyRoundingService.RoundOrderTotal(
                subtotal + shippingTotal + taxResult.TaxTotal - session.DiscountTotal,
                currencyCode);
            if (totalAmount != session.GrandTotal
                || shippingTotal != session.ShippingTotal
                || taxResult.TaxTotal != session.TaxTotal)
            {
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, "Shipping option has changed.");
            }

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

            if (IsAsyncPaymentMethod(capability.MethodType))
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

            var now = DateTimeOffset.UtcNow;
            var paymentAttempt = new PaymentAttempt
            {
                Id = Guid.NewGuid(),
                PublicId = Guid.NewGuid(),
                StoreId = request.StoreId,
                CheckoutSessionId = session.Id,
                PaymentMethodKey = paymentMethodKey,
                ProviderKey = capability.SystemName,
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

            ServiceResponse<PaymentProviderOperationResult> paymentResult;
            try
            {
                var provider = this.paymentProviderResolver.Resolve(paymentAttempt.ProviderKey);
                paymentResult = await provider.CreatePaymentSessionAsync(
                    new CreatePaymentProviderSessionRequest(
                        request.StoreId,
                        session.PublicId,
                        paymentAttempt.PublicId,
                        paymentMethodKey,
                        paymentAttempt.ProviderKey,
                        paymentAmount,
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
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, ex.Message);
            }

            if (!paymentResult.Success || paymentResult.Payload is null)
            {
                paymentAttempt.State = PaymentAttemptStates.Failed;
                paymentAttempt.FailureCode = paymentResult.Payload?.SafeFailureCode ?? "provider_session_failed";
                paymentAttempt.FailureMessage = paymentResult.Payload?.SafeFailureMessage
                    ?? paymentResult.Message
                    ?? "Payment provider session could not be created.";
                this.context.PaymentAttempts.Add(paymentAttempt);
                await this.context.SaveChangesAsync(cancellationToken);
                return Failed<StorefrontPlaceOrderResult>(
                    paymentResult.ResponseType is ServiceResponseType.Success ? ServiceResponseType.Conflict : paymentResult.ResponseType,
                    paymentAttempt.FailureMessage);
            }

            var operation = paymentResult.Payload;
            if (!string.Equals(operation.RecommendedState, PaymentAttemptStates.Captured, StringComparison.OrdinalIgnoreCase))
            {
                paymentAttempt.State = PaymentAttemptStates.Failed;
                paymentAttempt.FailureCode = "payment.state_not_captured";
                paymentAttempt.FailureMessage = "Payment provider did not complete the payment.";
                this.context.PaymentAttempts.Add(paymentAttempt);
                await this.context.SaveChangesAsync(cancellationToken);
                return Failed<StorefrontPlaceOrderResult>(ServiceResponseType.Conflict, paymentAttempt.FailureMessage);
            }

            await using var transaction = this.context.Database.IsRelational()
                ? await this.context.Database.BeginTransactionAsync(cancellationToken)
                : null;

            var placement = await this.orderPlacementService.PlaceAsync(
                new OrderPlacementRequest(
                    request.StoreId,
                    session,
                    paymentAttempt,
                    new OrderSnapshotInput(
                        OrderStatuses.Processing,
                        PaymentStatuses.Paid,
                        paymentMethodKey,
                        now,
                        operation.MetadataJson,
                        currencyCode,
                        totalAmount,
                        new OrderPlacementCurrencySnapshot(
                            rateSnapshot.BaseCurrencyCode,
                            rateSnapshot.BaseTotalAmount,
                            rateSnapshot.ExchangeRate,
                            rateSnapshot.ExchangeRateProviderKey,
                            rateSnapshot.ExchangeRateSource,
                            rateSnapshot.ExchangeRateEffectiveAtUtc,
                            rateSnapshot.ExchangeRateExpiresAtUtc),
                        shippingResult.ShippingRequired ? ShippingStatuses.NotYetShipped : ShippingStatuses.ShippingNotRequired,
                        currentSelectedShippingOption)),
                cancellationToken);
            if (!placement.Success || placement.Order is null)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return Failed<StorefrontPlaceOrderResult>(placement.ResponseType, placement.Message);
            }

            var order = placement.Order;
            this.context.PaymentAttempts.Add(paymentAttempt);
            this.AppendPaymentAudit(
                paymentAttempt,
                oldState: null,
                PaymentAttemptStates.Created,
                "payment_attempt.created",
                "Payment attempt created.",
                operation.MetadataJson);
            session.IdempotencyKey = idempotencyKey;
            session.BaseCurrencyCode = rateSnapshot.BaseCurrencyCode;
            session.BaseSubtotal = rateSnapshot.BaseTotalAmount;
            session.BaseGrandTotal = rateSnapshot.BaseTotalAmount;
            session.ExchangeRate = rateSnapshot.ExchangeRate;
            session.ExchangeRateProviderKey = rateSnapshot.ExchangeRateProviderKey;
            session.ExchangeRateSource = rateSnapshot.ExchangeRateSource;
            session.ExchangeRateEffectiveAtUtc = rateSnapshot.ExchangeRateEffectiveAtUtc;
            session.ExchangeRateExpiresAtUtc = rateSnapshot.ExchangeRateExpiresAtUtc;

            paymentAttempt.State = PaymentAttemptStates.Captured;
            paymentAttempt.ProviderSessionId = operation.ProviderSessionId;
            paymentAttempt.ProviderReference = operation.ProviderReference;
            paymentAttempt.NextActionType = operation.ActionType;
            paymentAttempt.NextActionUrl = operation.ActionUrl;
            paymentAttempt.MetadataJson = operation.MetadataJson;
            paymentAttempt.UpdatedAtUtc = now;
            this.AppendPaymentAudit(
                paymentAttempt,
                PaymentAttemptStates.Created,
                PaymentAttemptStates.Captured,
                "payment_attempt.captured",
                "Payment attempt captured during checkout.",
                operation.MetadataJson);

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
            this.AppendPaymentAudit(
                paymentAttempt,
                oldState: null,
                PaymentAttemptStates.Created,
                "payment_attempt.created",
                "Payment attempt created.",
                providerMetadataJson: null);
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

            ServiceResponse<PaymentProviderOperationResult> providerResult;
            try
            {
                var provider = this.paymentProviderResolver.Resolve(paymentAttempt.ProviderKey);
                providerResult = await provider.CreatePaymentSessionAsync(
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
                providerResult = new ServiceResponse<PaymentProviderOperationResult>(false, ex.Message)
                {
                    ResponseType = ServiceResponseType.Conflict,
                };
            }

            if (!providerResult.Success || providerResult.Payload is null)
            {
                paymentAttempt.State = PaymentAttemptStates.Failed;
                paymentAttempt.FailureCode = providerResult.Payload?.SafeFailureCode ?? "provider_session_failed";
                paymentAttempt.FailureMessage = providerResult.Payload?.SafeFailureMessage
                    ?? providerResult.Message
                    ?? "Payment provider session could not be created.";
                paymentAttempt.UpdatedAtUtc = DateTimeOffset.UtcNow;
                this.AppendPaymentAudit(
                    paymentAttempt,
                    PaymentAttemptStates.Created,
                    PaymentAttemptStates.Failed,
                    "payment_attempt.failed",
                    paymentAttempt.FailureMessage,
                    providerResult.Payload?.MetadataJson);
                await this.context.SaveChangesAsync(cancellationToken);
                return Failed<StorefrontPlaceOrderResult>(
                    providerResult.ResponseType is ServiceResponseType.Success ? ServiceResponseType.Conflict : providerResult.ResponseType,
                    paymentAttempt.FailureMessage);
            }

            var providerSession = providerResult.Payload;
            paymentAttempt.State = NormalizeNullable(providerSession.RecommendedState) ?? PaymentAttemptStates.RequiresAction;
            paymentAttempt.ProviderSessionId = providerSession.ProviderSessionId;
            paymentAttempt.ProviderReference = providerSession.ProviderReference;
            paymentAttempt.NextActionType = providerSession.ActionType;
            paymentAttempt.NextActionUrl = providerSession.ActionUrl;
            paymentAttempt.MetadataJson = providerSession.MetadataJson;
            paymentAttempt.UpdatedAtUtc = DateTimeOffset.UtcNow;
            this.AppendPaymentAudit(
                paymentAttempt,
                PaymentAttemptStates.Created,
                paymentAttempt.State,
                $"payment_attempt.{paymentAttempt.State}",
                "Payment attempt requires provider action.",
                providerSession.MetadataJson);

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
            var shippingOptions = await this.ResolveShippingOptionsAsync(
                session,
                cart,
                selectedShippingOption?.Key,
                cancellationToken);
            selectedShippingOption = shippingOptions.Options.FirstOrDefault(option => option.Selected);
            var resolvedPaymentMethods = paymentMethods
                ?? (shippingOptions.ShippingRequired && selectedShippingOption is null
                    ? []
                    : await this.ResolvePaymentMethodOptionsAsync(session, session.PaymentMethodKey, cancellationToken));

            return ToSessionResult(session, cart, shippingOptions.ShippingRequired, selectedShippingOption, shippingOptions.Options, resolvedPaymentMethods);
        }

        private StorefrontCheckoutSessionResult ToSessionResult(
            CheckoutSession session,
            StorefrontCartSessionDto cart,
            bool shippingRequired,
            StorefrontCheckoutShippingOption? selectedShippingOption,
            IReadOnlyList<StorefrontCheckoutShippingOption> shippingOptions,
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
                shippingRequired,
                selectedShippingOption,
                shippingOptions,
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
            StorefrontCheckoutPaymentMethodOption? selectedPaymentMethod,
            bool shippingRequired = true)
        {
            if (billingAddress is null)
            {
                return CheckoutSteps.BillingAddress;
            }

            if (shippingRequired && shippingAddress is null)
            {
                return CheckoutSteps.ShippingAddress;
            }

            if (shippingRequired && selectedShippingOption is null)
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

        private async Task<CheckoutShippingCalculationResult> ResolveShippingOptionsAsync(
            CheckoutSession session,
            StorefrontCartSessionDto cart,
            string? selectedKey,
            CancellationToken cancellationToken)
        {
            var currencyCode = NormalizeCurrency(session.CurrencyCode)
                ?? NormalizeCurrency(cart.CurrencyCode)
                ?? DefaultCurrencyCode;
            var subtotal = this.moneyRoundingService.RoundOrderTotal(
                cart.Lines.Sum(line => line.LineTotal ?? line.LineSubtotal ?? 0m),
                currencyCode);
            var rateCurrency = this.ResolveShippingRateCurrency(cart.Lines, currencyCode, subtotal);
            var packageLines = await this.BuildShippingPackageLinesAsync(session.StoreId, cart.Lines, cancellationToken);
            var result = await this.CalculateShippingAsync(
                session.StoreId,
                cart.Id,
                cart.PublicId,
                CreateShippingAddress(session),
                currencyCode,
                subtotal,
                rateCurrency.BaseCurrencyCode,
                rateCurrency.BaseSubtotal,
                packageLines,
                cancellationToken);

            return result with
            {
                Options = result.Options
                    .Select(option => option with
                    {
                        Selected = string.Equals(option.Key, selectedKey, StringComparison.OrdinalIgnoreCase),
                    })
                    .ToArray(),
            };
        }

        private async Task<CheckoutShippingCalculationResult> CalculateShippingAsync(
            Guid storeId,
            Guid? cartId,
            Guid? cartPublicId,
            StorefrontCheckoutShippingAddressDto? address,
            string checkoutCurrencyCode,
            decimal checkoutSubtotal,
            string rateCurrencyCode,
            decimal rateSubtotal,
            IReadOnlyList<ShippingPackageLine> packageLines,
            CancellationToken cancellationToken)
        {
            var normalizedCheckoutCurrency = NormalizeCurrency(checkoutCurrencyCode) ?? DefaultCurrencyCode;
            var normalizedRateCurrency = NormalizeCurrency(rateCurrencyCode) ?? normalizedCheckoutCurrency;
            var request = new ShippingOptionsRequest(
                storeId,
                cartId,
                cartPublicId,
                ToShippingAddressSnapshot(address),
                normalizedRateCurrency,
                rateSubtotal,
                packageLines);
            var result = await this.shippingCalculator.GetOptionsAsync(request, cancellationToken);
            if (!result.Success || result.Payload is null)
            {
                return new CheckoutShippingCalculationResult(
                    ShippingRequired: packageLines.Any(line => line.Quantity > 0 && line.ShippingRequired),
                    Options: [],
                    Warnings: [],
                    Errors: [result.Message ?? "Shipping options could not be calculated."]);
            }

            var options = new List<StorefrontCheckoutShippingOption>();
            var errors = new List<string>(result.Payload.Errors);
            foreach (var option in result.Payload.Options)
            {
                var mapped = await this.MapShippingOptionAsync(
                    storeId,
                    option,
                    normalizedCheckoutCurrency,
                    normalizedRateCurrency,
                    cancellationToken);
                if (!mapped.Success || mapped.Payload is null)
                {
                    errors.Add(mapped.Message ?? "Shipping rate currency conversion is not available.");
                    continue;
                }

                options.Add(mapped.Payload);
            }

            return new CheckoutShippingCalculationResult(
                result.Payload.ShippingRequired,
                options,
                result.Payload.Warnings,
                errors);
        }

        private ShippingRateCurrency ResolveShippingRateCurrency(
            IReadOnlyList<StorefrontCartLineDto> cartLines,
            string checkoutCurrencyCode,
            decimal checkoutSubtotal)
        {
            var convertedLines = cartLines
                .Where(line => line.ExchangeRateSnapshot.HasValue
                    && !string.IsNullOrWhiteSpace(line.BaseCurrencyCodeSnapshot))
                .ToArray();
            if (convertedLines.Length == 0)
            {
                var currencyCode = NormalizeCurrency(checkoutCurrencyCode) ?? DefaultCurrencyCode;
                return new ShippingRateCurrency(currencyCode, checkoutSubtotal);
            }

            var baseCurrencyCode = NormalizeCurrency(convertedLines[0].BaseCurrencyCodeSnapshot) ?? DefaultCurrencyCode;
            var baseSubtotal = this.moneyRoundingService.RoundOrderTotal(
                cartLines.Sum(line => (line.BaseUnitPriceSnapshot ?? line.UnitPriceSnapshot ?? 0m) * Math.Max(0, line.Quantity)),
                baseCurrencyCode);
            return new ShippingRateCurrency(baseCurrencyCode, baseSubtotal);
        }

        private ShippingRateCurrency ResolveShippingRateCurrency(
            IEnumerable<CartLine> cartLines,
            string checkoutCurrencyCode,
            decimal checkoutSubtotal)
        {
            var materialized = cartLines.ToArray();
            var convertedLines = materialized
                .Where(line => line.ExchangeRateSnapshot.HasValue
                    && !string.IsNullOrWhiteSpace(line.BaseCurrencyCodeSnapshot))
                .ToArray();
            if (convertedLines.Length == 0)
            {
                var currencyCode = NormalizeCurrency(checkoutCurrencyCode) ?? DefaultCurrencyCode;
                return new ShippingRateCurrency(currencyCode, checkoutSubtotal);
            }

            var baseCurrencyCode = NormalizeCurrency(convertedLines[0].BaseCurrencyCodeSnapshot) ?? DefaultCurrencyCode;
            var baseSubtotal = this.moneyRoundingService.RoundOrderTotal(
                materialized.Sum(line => (line.BaseUnitPriceSnapshot ?? line.UnitPriceSnapshot ?? 0m) * Math.Max(0, line.Quantity)),
                baseCurrencyCode);
            return new ShippingRateCurrency(baseCurrencyCode, baseSubtotal);
        }

        private async Task<ShippingTaxCalculationResult> CalculateShippingTaxAsync(
            Guid storeId,
            StorefrontCheckoutShippingAddressDto? address,
            string currencyCode,
            decimal subtotal,
            decimal shippingTotal,
            CancellationToken cancellationToken)
        {
            var request = new ShippingTaxCalculationRequest(
                storeId,
                ToShippingAddressSnapshot(address),
                NormalizeCurrency(currencyCode) ?? DefaultCurrencyCode,
                subtotal,
                shippingTotal);

            var result = await this.shippingTaxCalculator.CalculateAsync(request, cancellationToken);
            return result with
            {
                TaxTotal = this.moneyRoundingService.RoundOrderTotal(result.TaxTotal, request.CurrencyCode),
            };
        }

        private async Task<IReadOnlyList<ShippingPackageLine>> BuildShippingPackageLinesAsync(
            Guid storeId,
            IReadOnlyList<StorefrontCartLineDto> cartLines,
            CancellationToken cancellationToken)
        {
            var productIds = cartLines.Select(line => line.ProductId).Distinct().ToArray();
            var products = await this.LoadProductShippingMetadataAsync(storeId, productIds, cancellationToken);

            return cartLines
                .Select(line => ToShippingPackageLine(
                    line.ProductId,
                    line.ProductVariantId,
                    line.Quantity,
                    products.GetValueOrDefault(line.ProductId)))
                .ToArray();
        }

        private async Task<IReadOnlyList<ShippingPackageLine>> BuildShippingPackageLinesAsync(
            Guid storeId,
            IEnumerable<CartLine> cartLines,
            CancellationToken cancellationToken)
        {
            var materialized = cartLines.ToArray();
            var productIds = materialized.Select(line => line.ProductId).Distinct().ToArray();
            var products = await this.LoadProductShippingMetadataAsync(storeId, productIds, cancellationToken);

            return materialized
                .Select(line => ToShippingPackageLine(
                    line.ProductId,
                    line.ProductVariantId,
                    line.Quantity,
                    products.GetValueOrDefault(line.ProductId)))
                .ToArray();
        }

        private async Task<IReadOnlyDictionary<Guid, ProductShippingMetadata>> LoadProductShippingMetadataAsync(
            Guid storeId,
            IReadOnlyList<Guid> productIds,
            CancellationToken cancellationToken)
        {
            return await this.context.Products
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id)
                    && (product.StoreId == storeId || product.StoreId == null))
                .Select(product => new
                {
                    product.Id,
                    product.ShippingRequired,
                    product.FreeShipping,
                    product.Weight,
                    product.Length,
                    product.Width,
                    product.Height,
                    product.ShippingSurcharge,
                })
                .ToDictionaryAsync(
                    product => product.Id,
                    product => new ProductShippingMetadata(
                        product.ShippingRequired,
                        product.FreeShipping,
                        product.Weight,
                        product.Length,
                        product.Width,
                        product.Height,
                        product.ShippingSurcharge),
                    cancellationToken);
        }

        private static ShippingPackageLine ToShippingPackageLine(
            Guid productId,
            Guid? productVariantId,
            int quantity,
            ProductShippingMetadata? product)
        {
            return new ShippingPackageLine(
                productId,
                productVariantId,
                Math.Max(0, quantity),
                product?.ShippingRequired ?? true,
                product?.FreeShipping ?? false,
                product?.Weight,
                product?.Length,
                product?.Width,
                product?.Height,
                product?.ShippingSurcharge);
        }

        private async Task<ServiceResponse<StorefrontCheckoutShippingOption>> MapShippingOptionAsync(
            Guid storeId,
            ShippingOptionDto option,
            string checkoutCurrencyCode,
            string rateCurrencyCode,
            CancellationToken cancellationToken)
        {
            var optionCurrencyCode = NormalizeCurrency(option.CurrencyCode) ?? rateCurrencyCode;
            var normalizedCheckoutCurrency = NormalizeCurrency(checkoutCurrencyCode) ?? DefaultCurrencyCode;
            var normalizedRateCurrency = NormalizeCurrency(rateCurrencyCode) ?? normalizedCheckoutCurrency;

            if (!string.Equals(optionCurrencyCode, normalizedRateCurrency, StringComparison.Ordinal)
                && !string.Equals(optionCurrencyCode, normalizedCheckoutCurrency, StringComparison.Ordinal))
            {
                return Failed<StorefrontCheckoutShippingOption>(
                    ServiceResponseType.Conflict,
                    "Shipping provider returned an unsupported currency.");
            }

            var price = this.moneyRoundingService.RoundOrderTotal(option.Rate, optionCurrencyCode);
            if (option.Rate == 0m)
            {
                price = 0m;
            }
            else if (!string.Equals(optionCurrencyCode, normalizedCheckoutCurrency, StringComparison.Ordinal))
            {
                var conversion = await this.moneyConversionService.ConvertFromBaseAsync(
                    storeId,
                    option.Rate,
                    normalizedCheckoutCurrency,
                    cancellationToken);
                if (!conversion.Success || conversion.Payload is null)
                {
                    return Failed<StorefrontCheckoutShippingOption>(
                        conversion.ResponseType,
                        conversion.Message ?? "Shipping rate currency conversion is not available.");
                }

                price = this.moneyRoundingService.RoundOrderTotal(
                    conversion.Payload.ConvertedAmount,
                    conversion.Payload.TargetCurrencyCode);
            }

            return Succeeded(
                "Shipping option mapped.",
                new StorefrontCheckoutShippingOption(
                option.Key,
                option.ProviderSystemName,
                option.MethodCode,
                option.DisplayName,
                option.Description,
                price,
                normalizedCheckoutCurrency,
                option.DeliveryEstimateText,
                Selected: false));
        }

        private static ShippingAddressSnapshot? ToShippingAddressSnapshot(StorefrontCheckoutShippingAddressDto? address)
        {
            return address is null
                ? null
                : new ShippingAddressSnapshot(
                    address.FullName,
                    null,
                    address.Address1,
                    address.Address2,
                    address.City,
                    address.State,
                    address.PostalCode,
                    address.CountryCode,
                    address.Phone,
                    address.Email);
        }

        private static IReadOnlyList<StorefrontCheckoutValidationIssue> ToShippingIssues(CheckoutShippingCalculationResult result)
        {
            return result.Errors
                .Select(error => new StorefrontCheckoutValidationIssue(
                    "shipping.option_unavailable",
                    error,
                    "shippingOptionKey"))
                .Concat(result.Warnings.Select(warning => new StorefrontCheckoutValidationIssue(
                    "shipping.warning",
                    warning,
                    "shippingOptionKey")))
                .ToArray();
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
                    this.ResolvePaymentNextActionKind(method.PaymentMethodKey),
                    string.Equals(method.PaymentMethodKey, selectedKey, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
        }

        private StorefrontCheckoutPaymentMethodOption? CreateSelectedPaymentMethod(string? paymentMethodKey)
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
                this.ResolvePaymentNextActionKind(key),
                Selected: true);
        }

        private static bool SupportsValue(IReadOnlyList<string> supportedValues, string? value)
        {
            return supportedValues.Count == 0
                || (value is not null && supportedValues.Contains(value, StringComparer.OrdinalIgnoreCase));
        }

        private void AppendPaymentAudit(
            PaymentAttempt attempt,
            string? oldState,
            string newState,
            string eventType,
            string message,
            string? providerMetadataJson)
        {
            var normalizedMessage = NormalizeNullable(message) ?? "Payment attempt updated.";
            this.context.PaymentAttemptAuditLogs.Add(new PaymentAttemptAuditLog
            {
                Id = Guid.NewGuid(),
                StoreId = attempt.StoreId,
                OrderId = attempt.OrderId,
                PaymentAttemptId = attempt.Id,
                ProviderKey = attempt.ProviderKey,
                EventType = NormalizeNullable(eventType) is { Length: <= 128 } normalizedEventType
                    ? normalizedEventType
                    : "payment_attempt.updated",
                OldState = NormalizeKey(oldState),
                NewState = NormalizeKey(newState) ?? PaymentAttemptStates.Created,
                Message = normalizedMessage.Length > 512 ? normalizedMessage[..512] : normalizedMessage,
                MetadataJson = BuildPaymentAuditMetadataJson(attempt, providerMetadataJson),
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
        }

        private static string BuildPaymentAuditMetadataJson(PaymentAttempt attempt, string? providerMetadataJson)
        {
            return JsonSerializer.Serialize(new
            {
                providerReference = NormalizeNullable(attempt.ProviderReference),
                providerSessionId = NormalizeNullable(attempt.ProviderSessionId),
                failureCode = NormalizeNullable(attempt.FailureCode),
                hasProviderMetadata = !string.IsNullOrWhiteSpace(providerMetadataJson),
            }, JsonOptions);
        }

        private string ResolvePaymentNextActionKind(string paymentMethodKey)
        {
            var capability = this.paymentProviderCapabilityRegistry.Get(paymentMethodKey);
            if (!capability.Success || capability.Payload is null)
            {
                return PaymentProviderActionTypes.None;
            }

            return IsAsyncPaymentMethod(capability.Payload.MethodType)
                ? PaymentProviderActionTypes.Redirect
                : PaymentProviderActionTypes.None;
        }

        private static bool IsAsyncPaymentMethod(string methodType)
        {
            return string.Equals(methodType, PaymentProviderMethodTypes.Redirect, StringComparison.OrdinalIgnoreCase);
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

        private sealed record ProductShippingMetadata(
            bool ShippingRequired,
            bool FreeShipping,
            decimal? Weight,
            decimal? Length,
            decimal? Width,
            decimal? Height,
            decimal? ShippingSurcharge);

        private sealed record CheckoutShippingCalculationResult(
            bool ShippingRequired,
            IReadOnlyList<StorefrontCheckoutShippingOption> Options,
            IReadOnlyList<string> Warnings,
            IReadOnlyList<string> Errors);

        private sealed record ShippingRateCurrency(
            string BaseCurrencyCode,
            decimal BaseSubtotal);

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
