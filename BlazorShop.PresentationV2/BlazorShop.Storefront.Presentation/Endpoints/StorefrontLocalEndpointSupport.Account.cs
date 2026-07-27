namespace BlazorShop.Storefront.Presentation.Endpoints
{
    using System.Diagnostics;
    using System.Globalization;
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Models;
    using BlazorShop.Storefront.Presentation.PagePatterns;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Http;
    internal static class StorefrontPresentationAccountEndpointSupport
    {
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
            return (null, LocalSignInRequired());
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
            : LocalApiValidationError(result.Message);
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

        internal static StorefrontBrowserAccountOrderList ToBrowserOrderList(BlazorShop.Storefront.Models.PagedResult<StorefrontCustomerOrderListItemResponse> orders)
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

        internal static string FormatMoney(decimal amount, string? currencyCode)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{amount:0.00} {currencyCode ?? string.Empty}").Trim();
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
            return LocalCartValidationError("Security validation failed. Refresh the page and try again.");
        }
    }

        internal static IResult LocalApiValidationError(string? message)
    {
        return LocalApiError(message, StatusCodes.Status400BadRequest, "validation_error");
    }

        internal static IResult LocalCartValidationError(string? message)
    {
        return LocalCartError(message, StatusCodes.Status400BadRequest, "validation_error");
    }

        internal static IResult LocalSignInRequired()
    {
        return LocalApiError("Sign in is required.", StatusCodes.Status401Unauthorized, "authentication_required");
    }

        internal static IResult LocalNotFound(string? message)
    {
        return LocalApiError(message, StatusCodes.Status404NotFound, "not_found");
    }

        internal static IResult LocalUnavailable(string? message)
    {
        return LocalApiError(message, StatusCodes.Status503ServiceUnavailable, "service_unavailable", retryable: true);
    }

        private static IResult LocalApiError(
        string? message,
        int statusCode,
        string? code = null,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null,
        bool? retryable = null)
    {
        return Results.Json(
            new StorefrontLocalApiErrorResponse(
                NormalizeLocalErrorMessage(message),
                code ?? DefaultLocalErrorCode(statusCode),
                CurrentTraceId(),
                NormalizeFieldErrors(fieldErrors),
                retryable ?? IsRetryableLocalError(statusCode),
                statusCode),
            statusCode: statusCode);
    }

        private static IResult LocalCartError(
        string? message,
        int statusCode,
        string? code = null,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null,
        bool? retryable = null)
    {
        return Results.Json(
            new StorefrontLocalCartErrorResponse(
                NormalizeLocalErrorMessage(message),
                code ?? DefaultLocalErrorCode(statusCode),
                CurrentTraceId(),
                NormalizeFieldErrors(fieldErrors),
                retryable ?? IsRetryableLocalError(statusCode),
                statusCode),
            statusCode: statusCode);
    }

        private static Dictionary<string, string[]> NormalizeFieldErrors(IReadOnlyDictionary<string, string[]>? fieldErrors)
    {
        return fieldErrors is { Count: > 0 }
            ? fieldErrors
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value is { Length: > 0 })
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
    }

        private static string? CurrentTraceId()
    {
        return Activity.Current?.TraceId.ToString();
    }

        private static string DefaultLocalErrorCode(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "validation_error",
            StatusCodes.Status401Unauthorized => "authentication_required",
            StatusCodes.Status404NotFound => "not_found",
            StatusCodes.Status408RequestTimeout => "timeout",
            StatusCodes.Status409Conflict => "conflict",
            StatusCodes.Status422UnprocessableEntity => "unprocessable",
            StatusCodes.Status429TooManyRequests => "rate_limited",
            >= 500 => "service_unavailable",
            _ => "http_" + statusCode.ToString(CultureInfo.InvariantCulture),
        };
    }

        private static bool IsRetryableLocalError(int statusCode)
    {
        return statusCode is StatusCodes.Status408RequestTimeout or StatusCodes.Status429TooManyRequests
            || statusCode >= 500;
    }

        private static string NormalizeLocalErrorMessage(string? message)
    {
        return string.IsNullOrWhiteSpace(message)
            ? "The request could not be completed."
            : message;
    }
    }
}
