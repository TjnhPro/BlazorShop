namespace BlazorShop.Storefront.Endpoints
{
    using System.Globalization;

    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2;
    using BlazorShop.Web.SharedV2.Models;

    using Microsoft.AspNetCore.Antiforgery;
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

        internal static StorefrontCheckoutPreviewShippingAddress BuildCheckoutAddress(StorefrontCheckoutForm form)
    {
        var email = form.ShippingEmail?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            email = form.CustomerEmail?.Trim();
        }
    
        return new StorefrontCheckoutPreviewShippingAddress
        {
            FullName = form.ShippingFullName?.Trim() ?? form.CustomerName?.Trim() ?? string.Empty,
            Email = email ?? string.Empty,
            Phone = form.ShippingPhone?.Trim(),
            Address1 = form.ShippingAddress1?.Trim() ?? string.Empty,
            Address2 = form.ShippingAddress2?.Trim(),
            City = form.ShippingCity?.Trim() ?? string.Empty,
            State = form.ShippingState?.Trim(),
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

        internal static StorefrontCheckoutPreviewShippingAddress? ToCheckoutAddress(StorefrontBrowserCheckoutAddress? address)
    {
        if (address is null)
        {
            return null;
        }
    
        return new StorefrontCheckoutPreviewShippingAddress
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
        IStorefrontCartClient apiClient,
        Guid checkoutSessionId,
        int expectedCartVersion,
        CancellationToken cancellationToken)
    {
        var antiforgeryFailure = await ValidateLocalCartAntiforgeryAsync(httpContext, antiforgery);
        if (antiforgeryFailure is not null)
        {
            return (null, antiforgeryFailure);
        }
    
        if (checkoutSessionId == Guid.Empty)
        {
            return (null, Results.BadRequest(new StorefrontLocalApiErrorResponse("Checkout session is required.")));
        }
    
        var cartToken = httpContext.Request.Cookies[StorefrontCookieNames.CartToken];
        if (string.IsNullOrWhiteSpace(cartToken))
        {
            return (null, Results.Json(new StorefrontLocalApiErrorResponse("Your cart is empty."), statusCode: StatusCodes.Status409Conflict));
        }
    
        var cartResult = await apiClient.GetCartAsync(cartToken, cancellationToken);
        if (!cartResult.Success || cartResult.Data is null || cartResult.Data.Lines.Count == 0)
        {
            return (null, Results.Json(new StorefrontLocalApiErrorResponse("Your cart is empty."), statusCode: StatusCodes.Status409Conflict));
        }
    
        if (expectedCartVersion > 0 && expectedCartVersion != cartResult.Data.Version)
        {
            return (null, Results.Json(new StorefrontLocalApiErrorResponse("Your cart changed. Review the latest cart and try checkout again."), statusCode: StatusCodes.Status409Conflict));
        }
    
        return (cartToken, null);
    }

        internal static async Task<IResult> ToLocalCheckoutStateResultAsync(
        StorefrontSubmitResult<StorefrontCheckoutSessionResponse> result,
        IStorefrontDisplayContextProvider displayContextProvider,
        IStorefrontPriceFormatter priceFormatter,
        CancellationToken cancellationToken)
    {
        if (!result.Success || result.Data is null)
        {
            return Results.Json(new StorefrontLocalApiErrorResponse(result.Message), statusCode: StatusCodes.Status400BadRequest);
        }
    
        var displayContext = await displayContextProvider.GetAsync(cancellationToken);
        return Results.Ok(ToBrowserCheckoutState(result.Data, displayContext, priceFormatter));
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
        var checkoutContext = displayContext with { CurrencyCode = session.CurrencyCode };
        return new StorefrontBrowserCheckoutState(
            true,
            null,
            session.CheckoutSessionId,
            session.CheckoutVersion,
            session.CartVersion,
            session.State,
            session.CurrentStep,
            session.IsActive,
            session.ShippingRequired,
            false,
            priceFormatter.Format(session.GrandTotal, checkoutContext),
            session.Lines.Select(line => new StorefrontBrowserCheckoutLine(
                line.LineId,
                line.ProductId,
                line.ProductVariantId,
                line.Quantity,
                priceFormatter.Format(line.UnitPrice, checkoutContext with { CurrencyCode = line.CurrencyCode }),
                priceFormatter.Format(line.LineTotal, checkoutContext with { CurrencyCode = line.CurrencyCode }))).ToArray(),
            session.ShippingOptions.Select(option => new StorefrontBrowserCheckoutOption(
                option.Key,
                option.DisplayName,
                option.Description,
                priceFormatter.Format(option.Price, checkoutContext with { CurrencyCode = option.CurrencyCode }),
                option.Selected)).ToArray(),
            session.PaymentMethods.Select(method => new StorefrontBrowserCheckoutOption(
                method.Key,
                method.DisplayName,
                method.Description,
                null,
                method.Selected)).ToArray(),
            session.Issues.Select(issue => new StorefrontBrowserCheckoutIssue(
                issue.Code,
                issue.Message,
                issue.Field)).ToArray());
    }

        internal static StorefrontBrowserCheckoutState ToBrowserCheckoutReviewState(
        StorefrontCheckoutReviewResponse review,
        StorefrontDisplayContext displayContext,
        IStorefrontPriceFormatter priceFormatter)
    {
        var checkoutContext = displayContext with { CurrencyCode = review.CurrencyCode };
        return new StorefrontBrowserCheckoutState(
            true,
            review.PlaceOrderAllowed ? "Checkout is ready to place." : review.Issues.FirstOrDefault()?.Message,
            review.CheckoutSessionId,
            review.CheckoutVersion,
            review.CartVersion,
            review.State,
            review.CurrentStep,
            review.IsActive,
            review.SelectedShippingOption is not null,
            review.PlaceOrderAllowed,
            priceFormatter.Format(review.GrandTotal, checkoutContext),
            review.Lines.Select(line => new StorefrontBrowserCheckoutLine(
                line.LineId,
                line.ProductId,
                line.ProductVariantId,
                line.Quantity,
                priceFormatter.Format(line.UnitPrice, checkoutContext with { CurrencyCode = line.CurrencyCode }),
                priceFormatter.Format(line.LineTotal, checkoutContext with { CurrencyCode = line.CurrencyCode }))).ToArray(),
            review.SelectedShippingOption is null
                ? []
                : [new StorefrontBrowserCheckoutOption(
                    review.SelectedShippingOption.Key,
                    review.SelectedShippingOption.DisplayName,
                    review.SelectedShippingOption.Description,
                    priceFormatter.Format(review.SelectedShippingOption.Price, checkoutContext with { CurrencyCode = review.SelectedShippingOption.CurrencyCode }),
                    true)],
            review.SelectedPaymentMethod is null
                ? []
                : [new StorefrontBrowserCheckoutOption(
                    review.SelectedPaymentMethod.Key,
                    review.SelectedPaymentMethod.DisplayName,
                    review.SelectedPaymentMethod.Description,
                    null,
                    true)],
            review.Issues.Select(issue => new StorefrontBrowserCheckoutIssue(
                issue.Code,
                issue.Message,
                issue.Field)).ToArray());
    }

        internal static string? ResolveShippingOptionKey(StorefrontCheckoutSessionResponse session)
    {
        return session.SelectedShippingOption?.Key
            ?? session.ShippingOptions.FirstOrDefault(option => option.Selected)?.Key
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
            ?? session.PaymentMethods.FirstOrDefault(option => option.Selected)?.Key
            ?? session.PaymentMethods.FirstOrDefault()?.Key;
    }

        internal static string BuildCheckoutErrorUrl(string? message)
    {
        return StorefrontRoutes.Checkout
            + QueryString.Create("error", string.IsNullOrWhiteSpace(message) ? "Checkout could not be completed." : message);
    }
    }
}
