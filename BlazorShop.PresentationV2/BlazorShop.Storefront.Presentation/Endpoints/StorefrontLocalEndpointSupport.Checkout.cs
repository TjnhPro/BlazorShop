namespace BlazorShop.Storefront.Presentation.Endpoints
{
    using BlazorShop.Storefront.Client;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Presentation.PagePatterns;
    using BlazorShop.Storefront.Runtime;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Http;

    internal static partial class StorefrontLocalEndpointSupport
    {
        internal static StorefrontCheckoutAddressStepRequest BuildCheckoutAddressStepRequest(StorefrontCheckoutForm form)
        {
            var shippingAddressId = form.ShippingAddressId is { } shippingId && shippingId != Guid.Empty
                ? shippingId
                : (Guid?)null;
            var billingAddressId = form.BillingAddressId is { } billingId && billingId != Guid.Empty
                ? billingId
                : shippingAddressId;
            var directAddress = shippingAddressId.HasValue
                ? null
                : BuildCheckoutAddress(form);

            return new StorefrontCheckoutAddressStepRequest
            {
                BillingAddressId = billingAddressId,
                ShippingAddressId = shippingAddressId,
                UseBillingAddressAsShippingAddress = form.UseShippingAddressAsBillingAddress,
                BillingAddress = billingAddressId.HasValue ? null : directAddress,
                ShippingAddress = shippingAddressId.HasValue || form.UseShippingAddressAsBillingAddress ? null : directAddress,
            };
        }

        internal static StorefrontCheckoutShippingAddress BuildCheckoutAddress(StorefrontCheckoutForm form)
        {
            var email = form.ShippingEmail?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                email = form.CustomerEmail?.Trim();
            }

            return new StorefrontCheckoutShippingAddress
            {
                FullName = form.ShippingFullName?.Trim() ?? form.CustomerName?.Trim() ?? string.Empty,
                Email = email ?? string.Empty,
                Phone = NormalizeOptionalFormValue(form.ShippingPhone),
                Address1 = form.ShippingAddress1?.Trim() ?? string.Empty,
                Address2 = NormalizeOptionalFormValue(form.ShippingAddress2),
                City = form.ShippingCity?.Trim() ?? string.Empty,
                State = NormalizeOptionalFormValue(form.ShippingState),
                PostalCode = form.ShippingPostalCode?.Trim() ?? string.Empty,
                CountryCode = form.ShippingCountryCode?.Trim() ?? string.Empty,
            };
        }

        internal static StorefrontCheckoutAddressStepRequest ToCheckoutAddressStepRequest(StorefrontBrowserCheckoutAddressRequest request)
        {
            return new StorefrontCheckoutAddressStepRequest
            {
                BillingAddressId = request.BillingAddressId,
                ShippingAddressId = request.ShippingAddressId,
                UseBillingAddressAsShippingAddress = request.UseShippingAddressAsBillingAddress,
                BillingAddress = request.BillingAddressId.HasValue ? null : ToCheckoutAddress(request.BillingAddress),
                ShippingAddress = request.ShippingAddressId.HasValue ? null : ToCheckoutAddress(request.ShippingAddress),
            };
        }

        internal static StorefrontCheckoutShippingAddress? ToCheckoutAddress(StorefrontBrowserCheckoutAddress? address)
        {
            if (address is null)
            {
                return null;
            }

            return new StorefrontCheckoutShippingAddress
            {
                FullName = address.FullName.Trim(),
                Email = address.Email.Trim(),
                Phone = NormalizeOptionalFormValue(address.Phone),
                Address1 = address.Address1.Trim(),
                Address2 = NormalizeOptionalFormValue(address.Address2),
                City = address.City.Trim(),
                State = NormalizeOptionalFormValue(address.State),
                PostalCode = address.PostalCode.Trim(),
                CountryCode = address.CountryCode.Trim().ToUpperInvariant(),
            };
        }

        internal static async Task<(string? CartToken, IResult? Failure)> ValidateLocalCheckoutCommandAsync(
            HttpContext httpContext,
            IAntiforgery antiforgery,
            StorefrontCartTokenService cartTokenService,
            Guid checkoutSessionId,
            int expectedCartVersion,
            CancellationToken cancellationToken)
        {
            StorefrontResponseHeaders.ApplyPrivatePage(httpContext);

            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return (null, LocalApiValidationError("Security validation failed. Refresh the page and try again."));
            }

            if (checkoutSessionId == Guid.Empty)
            {
                return (null, LocalApiValidationError("Checkout session is required."));
            }

            var cartToken = httpContext.Request.Cookies[StorefrontCookieNames.CartToken];
            if (string.IsNullOrWhiteSpace(cartToken))
            {
                return (null, LocalConflict("Your cart is empty."));
            }

            var cartResolution = await cartTokenService.ResolveAsync(httpContext, importLegacyCart: false, cancellationToken: cancellationToken);
            if (!cartResolution.Success || cartResolution.Cart?.Lines.Count is null or 0)
            {
                return (null, LocalConflict("Your cart is empty."));
            }

            if (expectedCartVersion > 0 && expectedCartVersion != cartResolution.Cart.Version)
            {
                return (null, LocalConflict("Your cart changed. Review the latest cart and try checkout again."));
            }

            return (cartToken, null);
        }

        internal static async Task<IResult> ToLocalCheckoutStateResultAsync(
            StorefrontRuntimeSubmitResult<StorefrontCheckoutSessionResponse> result,
            IStorefrontDisplayContextProvider displayContextProvider,
            IStorefrontPriceFormatter priceFormatter,
            CancellationToken cancellationToken)
        {
            if (!result.Success || result.Value is null)
            {
                return result.Error?.Status == StorefrontRuntimeStatusCodes.Conflict
                    ? LocalConflict(result.Error.Message)
                    : LocalApiValidationError(result.Error?.Message);
            }

            var displayContext = await displayContextProvider.GetAsync(cancellationToken);
            return Results.Ok(ToBrowserCheckoutState(result.Value, displayContext, priceFormatter));
        }

        internal static StorefrontBrowserCheckoutState CreateEmptyCheckoutState(string message)
        {
            return new StorefrontBrowserCheckoutState(
                false,
                message,
                null,
                0,
                0,
                "empty",
                "cart",
                false,
                false,
                false,
                string.Empty,
                [],
                [],
                [],
                []);
        }

        internal static StorefrontBrowserCheckoutState ToBrowserCheckoutState(
            StorefrontCheckoutSessionResponse session,
            StorefrontDisplayContext displayContext,
            IStorefrontPriceFormatter priceFormatter)
        {
            var checkoutContext = displayContext with { CurrencyCode = NormalizeCurrencyCode(session.CurrencyCode) ?? displayContext.CurrencyCode };
            return new StorefrontBrowserCheckoutState(
                true,
                null,
                session.CheckoutSessionId,
                NormalizeVersion(session.CheckoutVersion),
                NormalizeVersion(session.CartVersion),
                NormalizeText(session.State, "started"),
                NormalizeText(session.CurrentStep, "cart"),
                session.IsActive == true,
                session.ShippingRequired == true,
                false,
                priceFormatter.Format(ToMoney(session.GrandTotal), checkoutContext),
                session.Lines.Select(line => new StorefrontBrowserCheckoutLine(
                    line.LineId.GetValueOrDefault(),
                    line.ProductId.GetValueOrDefault(),
                    line.ProductVariantId,
                    Math.Max(1, NormalizeVersion(line.Quantity)),
                    priceFormatter.Format(ToMoney(line.UnitPrice), checkoutContext with { CurrencyCode = NormalizeCurrencyCode(line.CurrencyCode) ?? checkoutContext.CurrencyCode }),
                    priceFormatter.Format(ToMoney(line.LineTotal), checkoutContext with { CurrencyCode = NormalizeCurrencyCode(line.CurrencyCode) ?? checkoutContext.CurrencyCode }))).ToArray(),
                session.ShippingOptions.Select(option => new StorefrontBrowserCheckoutOption(
                    NormalizeText(option.Key, "shipping"),
                    NormalizeText(option.DisplayName, "Shipping"),
                    option.Description,
                    priceFormatter.Format(ToMoney(option.Price), checkoutContext with { CurrencyCode = NormalizeCurrencyCode(option.CurrencyCode) ?? checkoutContext.CurrencyCode }),
                    option.Selected == true)).ToArray(),
                session.PaymentMethods.Select(method => new StorefrontBrowserCheckoutOption(
                    NormalizeText(method.Key, "payment"),
                    NormalizeText(method.DisplayName, "Payment method"),
                    method.Description,
                    null,
                    method.Selected == true)).ToArray(),
                session.Issues.Select(issue => new StorefrontBrowserCheckoutIssue(
                    NormalizeText(issue.Code, "checkout"),
                    NormalizeText(issue.Message, "Checkout could not be completed."),
                    issue.Field)).ToArray());
        }

        internal static StorefrontBrowserCheckoutState ToBrowserCheckoutReviewState(
            StorefrontCheckoutReviewResponse review,
            StorefrontDisplayContext displayContext,
            IStorefrontPriceFormatter priceFormatter)
        {
            var checkoutContext = displayContext with { CurrencyCode = NormalizeCurrencyCode(review.CurrencyCode) ?? displayContext.CurrencyCode };
            return new StorefrontBrowserCheckoutState(
                true,
                review.PlaceOrderAllowed == true ? "Checkout is ready to place." : review.Issues.FirstOrDefault()?.Message,
                review.CheckoutSessionId,
                NormalizeVersion(review.CheckoutVersion),
                NormalizeVersion(review.CartVersion),
                NormalizeText(review.State, "review"),
                NormalizeText(review.CurrentStep, "review"),
                review.IsActive == true,
                review.SelectedShippingOption is not null,
                review.PlaceOrderAllowed == true,
                priceFormatter.Format(ToMoney(review.GrandTotal), checkoutContext),
                review.Lines.Select(line => new StorefrontBrowserCheckoutLine(
                    line.LineId.GetValueOrDefault(),
                    line.ProductId.GetValueOrDefault(),
                    line.ProductVariantId,
                    Math.Max(1, NormalizeVersion(line.Quantity)),
                    priceFormatter.Format(ToMoney(line.UnitPrice), checkoutContext with { CurrencyCode = NormalizeCurrencyCode(line.CurrencyCode) ?? checkoutContext.CurrencyCode }),
                    priceFormatter.Format(ToMoney(line.LineTotal), checkoutContext with { CurrencyCode = NormalizeCurrencyCode(line.CurrencyCode) ?? checkoutContext.CurrencyCode }))).ToArray(),
                review.SelectedShippingOption is null
                    ? []
                    : [new StorefrontBrowserCheckoutOption(
                        NormalizeText(review.SelectedShippingOption.Key, "shipping"),
                        NormalizeText(review.SelectedShippingOption.DisplayName, "Shipping"),
                        review.SelectedShippingOption.Description,
                        priceFormatter.Format(ToMoney(review.SelectedShippingOption.Price), checkoutContext with { CurrencyCode = NormalizeCurrencyCode(review.SelectedShippingOption.CurrencyCode) ?? checkoutContext.CurrencyCode }),
                        true)],
                review.SelectedPaymentMethod is null
                    ? []
                    : [new StorefrontBrowserCheckoutOption(
                        NormalizeText(review.SelectedPaymentMethod.Key, "payment"),
                        NormalizeText(review.SelectedPaymentMethod.DisplayName, "Payment method"),
                        review.SelectedPaymentMethod.Description,
                        null,
                        true)],
                review.Issues.Select(issue => new StorefrontBrowserCheckoutIssue(
                    NormalizeText(issue.Code, "checkout"),
                    NormalizeText(issue.Message, "Checkout could not be completed."),
                    issue.Field)).ToArray());
        }

        internal static string? ResolveShippingOptionKey(StorefrontCheckoutSessionResponse session)
        {
            return session.SelectedShippingOption?.Key
                ?? session.ShippingOptions.FirstOrDefault(option => option.Selected == true)?.Key
                ?? session.ShippingOptions.FirstOrDefault()?.Key;
        }

        internal static string? ResolvePaymentMethodKey(StorefrontCheckoutForm form, StorefrontCheckoutSessionResponse session)
        {
            var requested = form.PaymentMethodKey?.Trim();
            if (!string.IsNullOrWhiteSpace(requested))
            {
                return requested;
            }

            return session.SelectedPaymentMethod?.Key
                ?? session.PaymentMethods.FirstOrDefault(option => option.Selected == true)?.Key
                ?? session.PaymentMethods.FirstOrDefault()?.Key;
        }

        internal static string BuildCheckoutErrorUrl(string? message)
        {
            return StorefrontRoutes.Checkout
                + QueryString.Create("error", string.IsNullOrWhiteSpace(message) ? "Checkout could not be completed." : message);
        }

        internal static IResult LocalConflict(string? message)
        {
            return LocalCheckoutError(message, StatusCodes.Status409Conflict, "conflict");
        }

        internal static IResult LocalApiValidationError(string? message)
        {
            return LocalCheckoutError(message, StatusCodes.Status400BadRequest, "validation_error");
        }

        internal static IResult LocalUnavailable(string? message)
        {
            return LocalCheckoutError(message, StatusCodes.Status503ServiceUnavailable, "storefront.unavailable");
        }

        private static IResult LocalCheckoutError(string? message, int statusCode, string code)
        {
            return Results.Json(
                new StorefrontLocalCartErrorResponse(
                    string.IsNullOrWhiteSpace(message) ? "The request could not be completed." : message,
                    code,
                    System.Diagnostics.Activity.Current?.TraceId.ToString(),
                    [],
                    statusCode is StatusCodes.Status408RequestTimeout or StatusCodes.Status429TooManyRequests || statusCode >= 500,
                    statusCode),
                statusCode: statusCode);
        }

        private static string? NormalizeOptionalFormValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        internal static decimal ToMoney(double? value)
        {
            return Convert.ToDecimal(value.GetValueOrDefault());
        }

        internal static decimal? ToNullableMoney(double? value)
        {
            return value.HasValue ? Convert.ToDecimal(value.Value) : null;
        }

        internal static int NormalizeVersion(int? value)
        {
            return Math.Max(0, value.GetValueOrDefault());
        }

        internal static string NormalizeText(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        internal static string? NormalizeCurrencyCode(string? currencyCode)
        {
            var normalized = currencyCode?.Trim().ToUpperInvariant();
            return normalized is { Length: 3 } && normalized.All(char.IsLetter)
                ? normalized
                : null;
        }
    }
}
