namespace BlazorShop.Storefront.Endpoints
{
    using BlazorShop.Storefront.Configuration;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Models;
    using BlazorShop.Storefront.Services;
    using BlazorShop.Storefront.Services.Contracts;
    using BlazorShop.Web.SharedV2;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Mvc;

    using static BlazorShop.Storefront.Endpoints.StorefrontLocalEndpointSupport;

    public static class StorefrontAccountEndpoints
    {
        public static WebApplication MapStorefrontAccountEndpoints(this WebApplication app)
        {
            app.MapGet("/api/account/profile", async (
                IStorefrontSessionResolver sessionResolver,
                IStorefrontCustomerClient apiClient,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                StorefrontResponseHeaders.ApplyPrivatePage(httpContext);
                var session = await ResolveLocalCustomerSessionAsync(sessionResolver, cancellationToken);
                if (session.Failure is not null)
                {
                    return session.Failure;
                }
            
                var result = await apiClient.GetCustomerProfileAsync(session.AccessToken!, cancellationToken);
                return result.Success && result.Data is not null
                    ? Results.Ok(ToBrowserProfile(result.Data))
                    : Results.Json(new StorefrontLocalApiErrorResponse(result.Message), statusCode: StatusCodes.Status503ServiceUnavailable);
            });
            app.MapPut("/api/account/profile", async (
                StorefrontBrowserCustomerProfileUpdateRequest request,
                IStorefrontSessionResolver sessionResolver,
                IStorefrontCustomerClient apiClient,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
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
            
                if (string.IsNullOrWhiteSpace(request.FullName) || !IsValidEmail(request.Email))
                {
                    return LocalApiValidationError("Full name and valid email are required.");
                }
            
                var result = await apiClient.UpdateCustomerProfileAsync(
                    session.AccessToken!,
                    ToCustomerProfileUpdateRequest(request),
                    cancellationToken);
                return result.Success && result.Data is not null
                    ? Results.Ok(ToBrowserProfile(result.Data))
                    : LocalApiValidationError(result.Message);
            });
            app.MapGet("/api/account/addresses", async (
                IStorefrontSessionResolver sessionResolver,
                IStorefrontCustomerClient apiClient,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                StorefrontResponseHeaders.ApplyPrivatePage(httpContext);
                var session = await ResolveLocalCustomerSessionAsync(sessionResolver, cancellationToken);
                if (session.Failure is not null)
                {
                    return session.Failure;
                }
            
                var result = await apiClient.GetCustomerAddressesAsync(session.AccessToken!, cancellationToken);
                return result.Success
                    ? Results.Ok((result.Data ?? []).Select(ToBrowserAddress).ToArray())
                    : Results.Json(new StorefrontLocalApiErrorResponse(result.Message), statusCode: StatusCodes.Status503ServiceUnavailable);
            });
            app.MapPost("/api/account/addresses", async (
                StorefrontBrowserCustomerAddressRequest request,
                IStorefrontSessionResolver sessionResolver,
                IStorefrontCustomerClient apiClient,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
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
            
                var result = await apiClient.CreateCustomerAddressAsync(session.AccessToken!, ToCustomerAddressRequest(request), cancellationToken);
                return result.Success && result.Data is not null
                    ? Results.Ok(ToBrowserAddress(result.Data))
                    : LocalApiValidationError(result.Message);
            });
            app.MapPut("/api/account/addresses/{addressId:guid}", async (
                Guid addressId,
                StorefrontBrowserCustomerAddressRequest request,
                IStorefrontSessionResolver sessionResolver,
                IStorefrontCustomerClient apiClient,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
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
            
                var result = await apiClient.UpdateCustomerAddressAsync(session.AccessToken!, addressId, ToCustomerAddressRequest(request), cancellationToken);
                return result.Success && result.Data is not null
                    ? Results.Ok(ToBrowserAddress(result.Data))
                    : LocalApiValidationError(result.Message);
            });
            app.MapDelete("/api/account/addresses/{addressId:guid}", async (
                Guid addressId,
                IStorefrontSessionResolver sessionResolver,
                IStorefrontCustomerClient apiClient,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
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
            
                var result = await apiClient.DeleteCustomerAddressAsync(session.AccessToken!, addressId, cancellationToken);
                return result.Success
                    ? Results.Ok(new StorefrontBrowserAccountCommandResult(true, "Address deleted."))
                    : LocalApiValidationError(result.Message);
            });
            app.MapPost("/api/account/addresses/{addressId:guid}/default-shipping", async (
                Guid addressId,
                IStorefrontSessionResolver sessionResolver,
                IStorefrontCustomerClient apiClient,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                return await ExecuteDefaultAddressLocalCommandAsync(addressId, setShippingDefault: true, sessionResolver, apiClient, antiforgery, httpContext, cancellationToken);
            });
            app.MapPost("/api/account/addresses/{addressId:guid}/default-billing", async (
                Guid addressId,
                IStorefrontSessionResolver sessionResolver,
                IStorefrontCustomerClient apiClient,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                return await ExecuteDefaultAddressLocalCommandAsync(addressId, setShippingDefault: false, sessionResolver, apiClient, antiforgery, httpContext, cancellationToken);
            });
            app.MapGet("/api/account/orders", async (
                int? page,
                int? pageSize,
                IStorefrontSessionResolver sessionResolver,
                IStorefrontCustomerClient apiClient,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                StorefrontResponseHeaders.ApplyPrivatePage(httpContext);
                var session = await ResolveLocalCustomerSessionAsync(sessionResolver, cancellationToken);
                if (session.Failure is not null)
                {
                    return session.Failure;
                }
            
                var result = await apiClient.GetCustomerOrdersAsync(
                    session.AccessToken!,
                    Math.Max(1, page.GetValueOrDefault(1)),
                    Math.Clamp(pageSize.GetValueOrDefault(10), 1, 25),
                    cancellationToken);
                return result.Success && result.Data is not null
                    ? Results.Ok(ToBrowserOrderList(result.Data))
                    : Results.Json(new StorefrontLocalApiErrorResponse(result.Message), statusCode: StatusCodes.Status503ServiceUnavailable);
            });
            app.MapGet("/api/account/orders/{orderReference}", async (
                string orderReference,
                IStorefrontSessionResolver sessionResolver,
                IStorefrontCustomerClient apiClient,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                StorefrontResponseHeaders.ApplyPrivatePage(httpContext);
                var session = await ResolveLocalCustomerSessionAsync(sessionResolver, cancellationToken);
                if (session.Failure is not null)
                {
                    return session.Failure;
                }
            
                var result = await apiClient.GetCustomerOrderAsync(session.AccessToken!, orderReference, cancellationToken);
                return result.Success && result.Data is not null
                    ? Results.Ok(ToBrowserOrderDetail(result.Data, receiptMode: false))
                    : Results.Json(new StorefrontLocalApiErrorResponse(result.Message), statusCode: StatusCodes.Status404NotFound);
            });
            app.MapGet("/api/account/orders/{orderReference}/receipt", async (
                string orderReference,
                IStorefrontSessionResolver sessionResolver,
                IStorefrontCustomerClient apiClient,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                StorefrontResponseHeaders.ApplyPrivatePage(httpContext);
                var session = await ResolveLocalCustomerSessionAsync(sessionResolver, cancellationToken);
                if (session.Failure is not null)
                {
                    return session.Failure;
                }
            
                var result = await apiClient.GetCustomerOrderReceiptAsync(session.AccessToken!, orderReference, cancellationToken);
                return result.Success && result.Data is not null
                    ? Results.Ok(ToBrowserOrderDetail(result.Data, receiptMode: true))
                    : Results.Json(new StorefrontLocalApiErrorResponse(result.Message), statusCode: StatusCodes.Status404NotFound);
            });
            app.MapPost("/api/account/change-password", async (
                StorefrontChangePasswordForm request,
                IStorefrontSessionResolver sessionResolver,
                IStorefrontAuthClient authClient,
                IAntiforgery antiforgery,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
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
            
                if (string.IsNullOrWhiteSpace(request.CurrentPassword)
                    || string.IsNullOrWhiteSpace(request.NewPassword)
                    || string.IsNullOrWhiteSpace(request.ConfirmPassword))
                {
                    return LocalApiValidationError("All password fields are required.");
                }
            
                if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
                {
                    return LocalApiValidationError("Passwords do not match.");
                }
            
                var result = await authClient.ChangePasswordAsync(
                    session.AccessToken!,
                    new ChangePassword
                    {
                        CurrentPassword = request.CurrentPassword,
                        NewPassword = request.NewPassword,
                        ConfirmPassword = request.ConfirmPassword,
                    },
                    cancellationToken);
                return result.Success
                    ? Results.Ok(new StorefrontBrowserAccountCommandResult(true, "Password changed."))
                    : LocalApiValidationError(result.Message);
            });

            return app;
        }
    }
}

