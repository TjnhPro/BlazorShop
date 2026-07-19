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

    internal static class StorefrontLocalEndpointSupport
    {
        private const string StorefrontConsentVisitorCookieName = "bs-consent-visitor";
    
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
    
        internal static string? NormalizeCurrencyCode(string? currencyCode)
    {
        var normalized = currencyCode?.Trim().ToUpperInvariant();
        return normalized is { Length: 3 } && normalized.All(char.IsLetter)
            ? normalized
            : null;
    }
    
        internal static bool IsValidEmail(string? email)
    {
        return !string.IsNullOrWhiteSpace(email)
            && email.Length <= 254
            && new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email);
    }
    
        internal static string? NormalizeOptionalFormValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
    
        internal static StorefrontCustomerAddressRequest BuildCustomerAddressRequest(StorefrontAccountAddressForm form)
    {
        var (firstName, lastName) = SplitFullName(NormalizeOptionalFormValue(form.FullName));
        return new StorefrontCustomerAddressRequest
        {
            FirstName = firstName,
            LastName = lastName,
            Company = NormalizeOptionalFormValue(form.Company),
            Email = NormalizeOptionalFormValue(form.Email),
            Phone = NormalizeOptionalFormValue(form.Phone),
            Address1 = NormalizeOptionalFormValue(form.Address1) ?? string.Empty,
            Address2 = NormalizeOptionalFormValue(form.Address2),
            City = NormalizeOptionalFormValue(form.City) ?? string.Empty,
            StateProvinceCode = NormalizeOptionalFormValue(form.StateProvinceCode),
            StateProvinceName = NormalizeOptionalFormValue(form.StateProvinceName),
            PostalCode = NormalizeOptionalFormValue(form.PostalCode) ?? string.Empty,
            CountryCode = NormalizeOptionalFormValue(form.CountryCode) ?? string.Empty,
            IsDefaultShipping = form.IsDefaultShipping,
            IsDefaultBilling = form.IsDefaultBilling,
        };
    }
    
        internal static (string FirstName, string LastName) SplitFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (string.Empty, string.Empty);
        }
    
        var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 1
            ? (parts[0], string.Empty)
            : (parts[0], parts[1]);
    }
    
        internal static async Task<(bool Success, string? Message)> ExecuteCustomerAddressCommandAsync(
        IStorefrontCustomerClient apiClient,
        string bearerToken,
        StorefrontAccountAddressForm form,
        CancellationToken cancellationToken)
    {
        var action = NormalizeOptionalFormValue(form.Action)?.ToLowerInvariant();
        switch (action)
        {
            case "create":
            {
                var result = await apiClient.CreateCustomerAddressAsync(
                    bearerToken,
                    BuildCustomerAddressRequest(form),
                    cancellationToken);
                return (result.Success, result.Message);
            }
    
            case "update" when form.AddressId is { } addressId:
            {
                var result = await apiClient.UpdateCustomerAddressAsync(
                    bearerToken,
                    addressId,
                    BuildCustomerAddressRequest(form),
                    cancellationToken);
                return (result.Success, result.Message);
            }
    
            case "delete" when form.AddressId is { } addressId:
            {
                var result = await apiClient.DeleteCustomerAddressAsync(bearerToken, addressId, cancellationToken);
                return (result.Success, result.Message);
            }
    
            case "default-shipping" when form.AddressId is { } addressId:
            {
                var result = await apiClient.SetDefaultShippingAddressAsync(bearerToken, addressId, cancellationToken);
                return (result.Success, result.Message);
            }
    
            case "default-billing" when form.AddressId is { } addressId:
            {
                var result = await apiClient.SetDefaultBillingAddressAsync(bearerToken, addressId, cancellationToken);
                return (result.Success, result.Message);
            }
    
            default:
                return (false, "Address action is required.");
        }
    }
    
        internal static async Task<(string? AccessToken, IResult? Failure)> ResolveLocalCustomerSessionAsync(
        IStorefrontSessionResolver sessionResolver,
        CancellationToken cancellationToken)
    {
        var session = await sessionResolver.GetCurrentUserAsync(cancellationToken);
        if (!session.IsAuthenticated || string.IsNullOrWhiteSpace(session.AccessToken))
        {
            return (null, Results.Json(
                new StorefrontLocalApiErrorResponse("Sign in is required."),
                statusCode: StatusCodes.Status401Unauthorized));
        }
    
        return (session.AccessToken, null);
    }
    
        internal static async Task<IResult> ExecuteDefaultAddressLocalCommandAsync(
        Guid addressId,
        bool setShippingDefault,
        IStorefrontSessionResolver sessionResolver,
        IStorefrontCustomerClient apiClient,
        IAntiforgery antiforgery,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var antiforgeryFailure = await ValidateLocalCartAntiforgeryAsync(httpContext, antiforgery);
        if (antiforgeryFailure is not null)
        {
            return antiforgeryFailure;
        }
    
        var session = await ResolveLocalCustomerSessionAsync(sessionResolver, cancellationToken);
        if (session.Failure is not null)
        {
            return session.Failure;
        }
    
        var result = setShippingDefault
            ? await apiClient.SetDefaultShippingAddressAsync(session.AccessToken!, addressId, cancellationToken)
            : await apiClient.SetDefaultBillingAddressAsync(session.AccessToken!, addressId, cancellationToken);
        return result.Success && result.Data is not null
            ? Results.Ok(ToBrowserAddress(result.Data))
            : Results.Json(new StorefrontLocalApiErrorResponse(result.Message), statusCode: StatusCodes.Status400BadRequest);
    }
    
        internal static StorefrontBrowserCustomerProfile ToBrowserProfile(StorefrontCustomerProfileResponse profile)
    {
        return new StorefrontBrowserCustomerProfile(
            profile.CustomerPublicId,
            profile.Email,
            profile.FullName,
            profile.FirstName,
            profile.LastName,
            profile.Company,
            profile.PhoneNumber,
            profile.PreferredLanguage,
            profile.PreferredCurrencyCode,
            profile.CreatedAtUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            profile.LastActivityAtUtc?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
    
        internal static StorefrontCustomerProfileUpdateRequest ToCustomerProfileUpdateRequest(StorefrontBrowserCustomerProfileUpdateRequest request)
    {
        return new StorefrontCustomerProfileUpdateRequest
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            FirstName = NormalizeOptionalFormValue(request.FirstName),
            LastName = NormalizeOptionalFormValue(request.LastName),
            Company = NormalizeOptionalFormValue(request.Company),
            PhoneNumber = NormalizeOptionalFormValue(request.PhoneNumber),
            PreferredLanguage = NormalizeOptionalFormValue(request.PreferredLanguage),
            PreferredCurrencyCode = NormalizeCurrencyCode(request.PreferredCurrencyCode),
        };
    }
    
        internal static StorefrontBrowserCustomerAddress ToBrowserAddress(StorefrontCustomerAddressResponse address)
    {
        return new StorefrontBrowserCustomerAddress(
            address.PublicId,
            address.FullName,
            address.Company,
            address.Email,
            address.Phone,
            address.Address1,
            address.Address2,
            address.City,
            address.PostalCode,
            address.CountryCode,
            address.StateProvinceCode,
            address.StateProvinceName,
            address.IsDefaultShipping,
            address.IsDefaultBilling);
    }
    
        internal static StorefrontCustomerAddressRequest ToCustomerAddressRequest(StorefrontBrowserCustomerAddressRequest request)
    {
        var (firstName, lastName) = SplitFullName(NormalizeOptionalFormValue(request.FullName));
        return new StorefrontCustomerAddressRequest
        {
            FirstName = firstName,
            LastName = lastName,
            Company = NormalizeOptionalFormValue(request.Company),
            Email = NormalizeOptionalFormValue(request.Email),
            Phone = NormalizeOptionalFormValue(request.Phone),
            Address1 = NormalizeOptionalFormValue(request.Address1) ?? string.Empty,
            Address2 = NormalizeOptionalFormValue(request.Address2),
            City = NormalizeOptionalFormValue(request.City) ?? string.Empty,
            StateProvinceCode = NormalizeOptionalFormValue(request.StateProvinceCode),
            StateProvinceName = NormalizeOptionalFormValue(request.StateProvinceName),
            PostalCode = NormalizeOptionalFormValue(request.PostalCode) ?? string.Empty,
            CountryCode = NormalizeOptionalFormValue(request.CountryCode)?.ToUpperInvariant() ?? string.Empty,
            IsDefaultShipping = request.IsDefaultShipping,
            IsDefaultBilling = request.IsDefaultBilling,
        };
    }
    
        internal static StorefrontBrowserAccountOrderList ToBrowserOrderList(PagedResult<StorefrontCustomerOrderListItemResponse> orders)
    {
        return new StorefrontBrowserAccountOrderList(
            orders.Items.Select(ToBrowserOrderListItem).ToArray(),
            orders.PageNumber,
            orders.PageSize,
            orders.TotalCount,
            orders.TotalPages);
    }
    
        internal static StorefrontBrowserAccountOrderListItem ToBrowserOrderListItem(StorefrontCustomerOrderListItemResponse order)
    {
        return new StorefrontBrowserAccountOrderListItem(
            order.Reference,
            order.CreatedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            order.OrderStatus,
            order.PaymentStatus,
            order.ShippingStatus,
            FormatMoney(order.TotalAmount, order.CurrencyCode),
            order.ItemCount);
    }
    
        internal static StorefrontBrowserAccountOrderDetail ToBrowserOrderDetail(StorefrontCustomerOrderDetailResponse order, bool receiptMode)
    {
        var currencyCode = order.CurrencyCode;
        var totals = order.TotalBreakdown;
        return new StorefrontBrowserAccountOrderDetail(
            order.Reference,
            receiptMode,
            order.CreatedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            order.OrderStatus,
            order.PaymentStatus,
            order.ShippingStatus,
            FormatMoney(order.TotalAmount, currencyCode),
            ToBrowserOrderAddress(order.ShippingAddress),
            order.BillingAddress is null ? null : ToBrowserOrderAddress(order.BillingAddress),
            order.Lines.Select(line => new StorefrontBrowserAccountOrderLine(
                line.ProductName,
                line.Sku,
                line.Quantity,
                FormatMoney(line.LineTotal, currencyCode))).ToArray(),
            new StorefrontBrowserOrderTotals(
                FormatMoney(totals?.Subtotal ?? 0m, currencyCode),
                FormatMoney(totals?.ShippingTotal ?? 0m, currencyCode),
                FormatMoney(totals?.TaxTotal ?? 0m, currencyCode),
                FormatMoney(totals?.DiscountTotal ?? 0m, currencyCode),
                FormatMoney(totals?.GrandTotal ?? order.TotalAmount, currencyCode)));
    }
    
        internal static StorefrontBrowserOrderAddress ToBrowserOrderAddress(StorefrontShippingAddressResponse address)
    {
        return new StorefrontBrowserOrderAddress(
            address.FullName,
            address.Email,
            address.Phone,
            address.Address1,
            address.Address2,
            address.City,
            address.State,
            address.PostalCode,
            address.CountryCode);
    }
    
        internal static string FormatMoney(decimal amount, string? currencyCode)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{amount:0.00} {currencyCode ?? string.Empty}").Trim();
    }
    
        internal static async Task<IResult> ToLocalCartMutationResultAsync(
        StorefrontCartMutationResult result,
        IStorefrontDisplayContextProvider displayContextProvider,
        IStorefrontPriceFormatter priceFormatter,
        CancellationToken cancellationToken)
    {
        if (result.Success)
        {
            var displayContext = await displayContextProvider.GetAsync(cancellationToken);
            return Results.Ok(ToLocalCartResponse(result.Cart, displayContext, priceFormatter));
        }
    
        return Results.Json(
            new StorefrontLocalCartErrorResponse(result.Message),
            statusCode: StatusCodes.Status400BadRequest);
    }
    
        internal static async Task<IResult?> ValidateLocalCartAntiforgeryAsync(HttpContext httpContext, IAntiforgery antiforgery)
    {
        StorefrontResponseHeaders.ApplyPrivatePage(httpContext);
    
        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
            return null;
        }
        catch (AntiforgeryValidationException)
        {
            return Results.Json(
                new StorefrontLocalCartErrorResponse("Security validation failed. Refresh the page and try again."),
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
    
        internal static string ResolveConsentVisitorKey(HttpContext httpContext, bool createIfMissing)
    {
        if (httpContext.Request.Cookies.TryGetValue(StorefrontConsentVisitorCookieName, out var existing)
            && !string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }
    
        if (!createIfMissing)
        {
            return string.Empty;
        }
    
        var visitorKey = Guid.NewGuid().ToString("N");
        httpContext.Response.Cookies.Append(
            StorefrontConsentVisitorCookieName,
            visitorKey,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(180),
            });
        return visitorKey;
    }
    
        internal static StorefrontBrowserCart ToLocalCartResponse(
        StorefrontCartResponse? cart,
        StorefrontDisplayContext displayContext,
        IStorefrontPriceFormatter priceFormatter)
    {
        var lines = ToLocalCartLines(cart?.Lines ?? [], cart?.CurrencyCode, displayContext, priceFormatter);
        var count = cart is not null && cart.SummaryCount > 0
            ? cart.SummaryCount
            : lines.Sum(line => Math.Max(0, line.Quantity));
        var currencyCode = NormalizeCurrencyCode(cart?.CurrencyCode) ?? lines
            .Select(line => line.CurrencyCode)
            .Distinct(StringComparer.Ordinal)
            .SingleOrDefault()
            ?? displayContext.CurrencyCode;
        var subtotal = cart?.Subtotal ?? lines.Sum(line => line.LineTotal);
        var grandTotal = cart?.GrandTotal ?? lines.Sum(line => line.LineTotal);
    
        return new StorefrontBrowserCart(
            count,
            cart?.Version ?? 0,
            lines,
            currencyCode,
            subtotal,
            FormatLocalCartPrice(subtotal, currencyCode, displayContext, priceFormatter),
            grandTotal,
            FormatLocalCartPrice(grandTotal, currencyCode, displayContext, priceFormatter),
            cart?.CheckoutAllowed ?? lines.All(line => !line.IsUnavailable),
            (cart?.Warnings ?? [])
                .Select(warning => new StorefrontBrowserCartWarning(warning.Message))
                .ToArray(),
            (cart?.Adjustments ?? [])
                .Select(adjustment => new StorefrontBrowserCartAdjustment(
                    adjustment.Label,
                    adjustment.Amount,
                    FormatLocalCartPrice(adjustment.Amount, NormalizeCurrencyCode(adjustment.CurrencyCode) ?? currencyCode, displayContext, priceFormatter)))
                .ToArray());
    }
    
        internal static IReadOnlyList<StorefrontBrowserCartLine> ToLocalCartLines(
        IEnumerable<StorefrontCartLineResponse> cartItems,
        string? cartCurrencyCode,
        StorefrontDisplayContext displayContext,
        IStorefrontPriceFormatter priceFormatter)
    {
        var lines = new List<StorefrontBrowserCartLine>();
        foreach (var cartItem in cartItems)
        {
            var quantity = Math.Max(1, cartItem.Quantity);
            var currencyCode = NormalizeCurrencyCode(cartItem.CurrencyCodeSnapshot) ?? NormalizeCurrencyCode(cartCurrencyCode) ?? displayContext.CurrencyCode;
            var unitPrice = cartItem.UnitPrice ?? cartItem.UnitPriceSnapshot ?? 0m;
            var lineTotal = cartItem.LineTotal ?? cartItem.LineSubtotal ?? (unitPrice * quantity);
            lines.Add(new StorefrontBrowserCartLine(
                cartItem.LineId,
                cartItem.ProductId,
                cartItem.ProductVariantId,
                string.IsNullOrWhiteSpace(cartItem.DisplayName) ? "Cart item" : cartItem.DisplayName,
                ResolveLocalCartProductUrl(cartItem),
                cartItem.ImageUrl,
                quantity,
                unitPrice,
                FormatLocalCartPrice(unitPrice, currencyCode, displayContext, priceFormatter),
                lineTotal,
                FormatLocalCartPrice(lineTotal, currencyCode, displayContext, priceFormatter),
                currencyCode,
                ResolveLocalCartSelectedAttributes(cartItem.SelectedAttributes),
                Math.Max(1, cartItem.QuantityMinimum),
                cartItem.QuantityMaximum,
                Math.Max(1, cartItem.QuantityStep),
                (cartItem.Warnings ?? [])
                    .Select(warning => warning.Message)
                    .Where(message => !string.IsNullOrWhiteSpace(message))
                    .Select(message => new StorefrontBrowserCartWarning(message))
                    .ToArray(),
                !cartItem.Purchasable || (cartItem.Warnings?.Count ?? 0) > 0));
        }
    
        return lines;
    }
    
        internal static string FormatLocalCartPrice(
        decimal amount,
        string currencyCode,
        StorefrontDisplayContext displayContext,
        IStorefrontPriceFormatter priceFormatter)
    {
        return priceFormatter.Format(amount, displayContext with { CurrencyCode = currencyCode });
    }
    
        internal static string? ResolveLocalCartProductUrl(StorefrontCartLineResponse cartItem)
    {
        if (!string.IsNullOrWhiteSpace(cartItem.ProductSlug))
        {
            return StorefrontRoutes.Product(cartItem.ProductSlug);
        }
    
        return string.IsNullOrWhiteSpace(cartItem.ProductUrl) ? null : cartItem.ProductUrl;
    }
    
        internal static string? ResolveLocalCartSelectedAttributes(IReadOnlyList<StorefrontCartSelectedAttributeResponse>? attributes)
    {
        var attributeText = string.Join(
            " / ",
            (attributes ?? [])
                .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Name) || !string.IsNullOrWhiteSpace(attribute.Value))
                .Select(attribute => $"{attribute.Name}: {attribute.Value}"));
        return string.IsNullOrWhiteSpace(attributeText) ? null : attributeText;
    }
    
    }

    public sealed class StorefrontLocalCartLineRequest
    {
        public Guid ProductId { get; set; }
    
        public Guid? ProductVariantId { get; set; }
    
        public string? CurrencyCode { get; set; }
    
        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }
    
        public int Quantity { get; set; } = 1;
    }
    
    public sealed class StorefrontLocalProductSelectionPreviewRequest
    {
        public Guid ProductId { get; set; }
    
        public Guid? ProductVariantId { get; set; }
    
        public IReadOnlyList<SelectedAttributeDto>? SelectedAttributes { get; set; }
    
        public int Quantity { get; set; } = 1;
    
        public string? CurrencyCode { get; set; }
    }
    
    public sealed record StorefrontLocalProductSelectionPreviewResponse(
        Guid ProductId,
        Guid? ProductVariantId,
        bool IsValid,
        bool IsAvailable,
        bool CanAddToCart,
        IReadOnlyList<string> ValidationMessages,
        IReadOnlyList<SelectedAttributeDto> SelectedAttributes,
        string? AttributeSignature,
        string? Sku,
        string? DisplayName,
        decimal UnitPrice,
        decimal? ComparePrice,
        string CurrencyCode,
        string FormattedUnitPrice,
        string? FormattedComparePrice,
        int StockQuantity,
        int MinQuantity,
        int MaxQuantity,
        string? PrimaryImageUrl);
    
    public sealed class StorefrontLocalCartQuantityRequest
    {
        public int Quantity { get; set; }
    }
    
    public sealed class StorefrontCurrencyPreferenceForm
    {
        public string? CurrencyCode { get; set; }
    
        public string? ReturnUrl { get; set; }
    }
    
    
}

