namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Net.Mail;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Carts;
    using BlazorShop.Application.CommerceNode.Checkout;
    using BlazorShop.Application.CommerceNode.Customers;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class StorefrontCheckoutService : IStorefrontCheckoutService
    {
        private const string DefaultCurrencyCode = "USD";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly CommerceNodeDbContext context;
        private readonly IStorefrontCartService cartService;
        private readonly IStorefrontCustomerService customerService;

        public StorefrontCheckoutService(
            CommerceNodeDbContext context,
            IStorefrontCartService cartService,
            IStorefrontCustomerService customerService)
        {
            this.context = context;
            this.cartService = cartService;
            this.customerService = customerService;
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

        private static string? NormalizeCurrency(string? value)
        {
            var normalized = NormalizeNullable(value);
            return normalized is null ? null : normalized.ToUpperInvariant();
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
    }
}
