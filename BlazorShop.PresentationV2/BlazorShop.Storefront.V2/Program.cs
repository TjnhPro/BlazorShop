using System.Threading.RateLimiting;

using BlazorShop.Application.Diagnostics;
using BlazorShop.Application.DTOs.UserIdentity;
using BlazorShop.Application.CommerceNode.VariationTemplates;
using BlazorShop.Application.Options;
using BlazorShop.Application.Services;
using BlazorShop.Application.Services.Contracts;
using BlazorShop.Storefront.Configuration;
using BlazorShop.Storefront.Options;
using BlazorShop.Storefront;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;
using BlazorShop.Storefront.WASM;
using BlazorShop.Web.SharedV2;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
const string StorefrontLocalCartRateLimitPolicyName = "storefront-local-cart";
const string StorefrontConsentVisitorCookieName = "bs-consent-visitor";
var storefrontRateLimitingOptions = builder.Configuration
    .GetSection(StorefrontRateLimitingOptions.SectionName)
    .Get<StorefrontRateLimitingOptions>() ?? new StorefrontRateLimitingOptions();

builder.AddServiceDefaults();

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.AddSingleton<IValidateOptions<StorefrontApiOptions>, StorefrontApiOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<ClientAppOptions>, StorefrontClientAppOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<StorefrontPublicUrlOptions>, StorefrontPublicUrlOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<StorefrontStoreResolutionOptions>, StorefrontStoreResolutionOptionsValidator>();
builder.Services.ConfigureOptions<StorefrontForwardedHeadersOptionsSetup>();
builder.Services.AddOptions<StorefrontApiOptions>()
    .Bind(builder.Configuration.GetSection(StorefrontApiOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<ClientAppOptions>()
    .Bind(builder.Configuration.GetSection(ClientAppOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<StorefrontPublicUrlOptions>()
    .Bind(builder.Configuration.GetSection(StorefrontPublicUrlOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<StorefrontStoreResolutionOptions>()
    .Bind(builder.Configuration.GetSection(StorefrontStoreResolutionOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<StorefrontRateLimitingOptions>()
    .Bind(builder.Configuration.GetSection(StorefrontRateLimitingOptions.SectionName));
if (storefrontRateLimitingOptions.Enabled)
{
    builder.Services.AddRateLimiter(options => ConfigureStorefrontRateLimiter(options, storefrontRateLimitingOptions));
}

builder.Services
    .AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddSingleton<ISeoMetadataBuilder, SeoMetadataBuilder>();
builder.Services.AddScoped<IStorefrontClientAppUrlResolver, StorefrontClientAppUrlResolver>();
builder.Services.AddScoped<IStorefrontPublicUrlResolver, StorefrontPublicUrlResolver>();
builder.Services.AddScoped<IStorefrontRobotsService, StorefrontRobotsService>();
builder.Services.AddScoped<IStorefrontSeoSettingsProvider, StorefrontSeoSettingsProvider>();
builder.Services.AddScoped<IStorefrontSeoComposer, StorefrontSeoComposer>();
builder.Services.AddScoped<IStorefrontStructuredDataComposer, StorefrontStructuredDataComposer>();
builder.Services.AddScoped<IStorefrontSitemapService, StorefrontSitemapService>();
builder.Services.AddScoped<IStorefrontCurrentStoreProvider, StorefrontCurrentStoreProvider>();
builder.Services.AddScoped<IStorefrontDisplayContextProvider, StorefrontDisplayContextProvider>();
builder.Services.AddScoped<IStorefrontPageNavigationProvider, StorefrontPageNavigationProvider>();
builder.Services.AddScoped<IStorefrontNavigationProvider, StorefrontNavigationProvider>();
builder.Services.AddScoped<IStorefrontPriceFormatter, StorefrontPriceFormatter>();
builder.Services.AddScoped<StorefrontCartTokenService>();
builder.Services.AddHttpClient<IStorefrontSessionResolver, StorefrontSessionResolver>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    ConfigureStorefrontHttpClient(client, configuration);
});
builder.Services.AddHttpClient<IStorefrontAuthClient, StorefrontAuthClient>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    ConfigureStorefrontHttpClient(client, configuration);
});
builder.Services.AddHttpClient<StorefrontApiClient>((serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    ConfigureStorefrontHttpClient(client, configuration);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseForwardedHeaders();
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    StorefrontResponseHeaders.RegisterErrorStatusHeaders(context);
    await next();
});
app.UseMiddleware<StorefrontCurrentStoreMiddleware>();
app.UseMiddleware<StorefrontPublicRedirectMiddleware>();
if (storefrontRateLimitingOptions.Enabled)
{
    app.UseRateLimiter();
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapDefaultEndpoints();
app.MapPost(StorefrontRoutes.SignIn, async (
    [FromForm] StorefrontLoginForm form,
    IStorefrontAuthClient authClient,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var safeReturnUrl = StorefrontReturnUrl.Normalize(form.ReturnUrl);
    if (string.IsNullOrWhiteSpace(form.Email) || string.IsNullOrWhiteSpace(form.Password))
    {
        return Results.Redirect(StorefrontReturnUrl.BuildSignInUrl(safeReturnUrl, "Email and password are required."));
    }

    var result = await authClient.LoginAsync(
        new LoginUser
        {
            Email = form.Email.Trim(),
            Password = form.Password,
            CaptchaToken = form.CaptchaToken,
        },
        cancellationToken);

    if (!result.Success || result.Data is null || string.IsNullOrWhiteSpace(result.Data.AccessToken))
    {
        return Results.Redirect(StorefrontReturnUrl.BuildSignInUrl(safeReturnUrl, result.Message));
    }

    StorefrontCookieBridge.CopySetCookieHeaders(result.SetCookieHeaders, httpContext.Response);
    return Results.Redirect(safeReturnUrl);
});
app.MapPost(StorefrontRoutes.Register, async (
    [FromForm] StorefrontRegisterForm form,
    IStorefrontAuthClient authClient,
    CancellationToken cancellationToken) =>
{
    var safeReturnUrl = StorefrontReturnUrl.Normalize(form.ReturnUrl);
    if (string.IsNullOrWhiteSpace(form.FullName)
        || string.IsNullOrWhiteSpace(form.Email)
        || string.IsNullOrWhiteSpace(form.Password)
        || string.IsNullOrWhiteSpace(form.ConfirmPassword))
    {
        return Results.Redirect(StorefrontReturnUrl.BuildRegisterUrl(safeReturnUrl, "All fields are required."));
    }

    if (!string.Equals(form.Password, form.ConfirmPassword, StringComparison.Ordinal))
    {
        return Results.Redirect(StorefrontReturnUrl.BuildRegisterUrl(safeReturnUrl, "Passwords do not match."));
    }

    var result = await authClient.RegisterAsync(
        new CreateUser
        {
            FullName = form.FullName.Trim(),
            Email = form.Email.Trim(),
            Password = form.Password,
            ConfirmPassword = form.ConfirmPassword,
            CaptchaToken = form.CaptchaToken,
        },
        cancellationToken);

    if (!result.Success)
    {
        return Results.Redirect(StorefrontReturnUrl.BuildRegisterUrl(safeReturnUrl, result.Message));
    }

    return Results.Redirect(StorefrontReturnUrl.BuildSignInUrl(safeReturnUrl, registered: true));
});
app.MapPost(StorefrontRoutes.Logout, async (
    [FromForm] StorefrontLogoutForm form,
    IStorefrontAuthClient authClient,
    IConfiguration configuration,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var safeReturnUrl = StorefrontReturnUrl.Normalize(form.ReturnUrl);
    var cookieHeader = StorefrontAuthCookies.BuildRefreshTokenCookieHeader(httpContext.Request, configuration);
    var userAgent = httpContext.Request.Headers.UserAgent.ToString();

    var result = await authClient.LogoutAsync(cookieHeader, userAgent, cancellationToken);
    StorefrontCookieBridge.CopySetCookieHeaders(result.SetCookieHeaders, httpContext.Response);

    if (result.SetCookieHeaders.Count == 0)
    {
        httpContext.Response.Cookies.Delete(
            StorefrontAuthCookies.GetRefreshTokenCookieName(configuration),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
            });
    }

    return Results.Redirect(safeReturnUrl);
});
app.MapPost(StorefrontRoutes.CurrencyPreference, async (
    [FromForm] StorefrontCurrencyPreferenceForm form,
    StorefrontApiClient apiClient,
    HttpContext httpContext,
    IHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    var safeReturnUrl = StorefrontReturnUrl.Normalize(form.ReturnUrl);
    var requestedCurrencyCode = NormalizeCurrencyCode(form.CurrencyCode);
    if (requestedCurrencyCode is null)
    {
        httpContext.Response.Cookies.Delete(StorefrontCookieNames.CurrencyPreference, new CookieOptions { Path = "/" });
        return Results.Redirect(safeReturnUrl);
    }

    var result = await apiClient.SetCurrencyPreferenceAsync(
        new StorefrontCurrencyPreferenceRequest { CurrencyCode = requestedCurrencyCode },
        cancellationToken);
    if (!result.Success || result.Data is null || !result.Data.RequestedCurrencySupported || !result.Data.CheckoutCurrencyEnabled)
    {
        httpContext.Response.Cookies.Delete(StorefrontCookieNames.CurrencyPreference, new CookieOptions { Path = "/" });
        return Results.Redirect(safeReturnUrl);
    }

    httpContext.Response.Cookies.Append(
        StorefrontCookieNames.CurrencyPreference,
        result.Data.CurrencyCode,
        new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromDays(30),
        });

    return Results.Redirect(safeReturnUrl);
});
app.MapPost(StorefrontRoutes.Checkout, async (
    [FromForm] StorefrontCheckoutForm form,
    StorefrontApiClient apiClient,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    StorefrontResponseHeaders.ApplyPrivatePage(httpContext);

    var cartToken = httpContext.Request.Cookies[StorefrontCookieNames.CartToken];
    if (string.IsNullOrWhiteSpace(cartToken))
    {
        return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create("error", "Your cart is empty."));
    }

    var cartResult = await apiClient.GetCartAsync(cartToken, cancellationToken);
    if (!cartResult.Success || cartResult.Data is null || cartResult.Data.Lines.Count == 0)
    {
        return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create("error", "Your cart is empty."));
    }

    var previewResult = await apiClient.PreviewCheckoutAsync(
        cartToken,
        BuildCheckoutPreviewRequest(form, form.CartVersion > 0 ? form.CartVersion : cartResult.Data.Version),
        cancellationToken);
    if (!previewResult.Success || previewResult.Data is null)
    {
        return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create("error", previewResult.Message));
    }

    if (!previewResult.Data.IsValid)
    {
        var firstIssue = previewResult.Data.Issues.FirstOrDefault();
        return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create(
            "error",
            firstIssue?.Message ?? "Review checkout details before placing the order."));
    }

    var placeOrderResult = await apiClient.PlaceOrderAsync(
        new StorefrontPlaceOrderRequest
        {
            CheckoutSessionId = previewResult.Data.CheckoutSessionId,
            ExpectedCartVersion = previewResult.Data.CartVersion,
            IdempotencyKey = string.IsNullOrWhiteSpace(form.IdempotencyKey)
                ? Guid.NewGuid().ToString("N")
                : form.IdempotencyKey.Trim(),
        },
        cancellationToken);
    if (!placeOrderResult.Success || placeOrderResult.Data is null)
    {
        return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create("error", placeOrderResult.Message));
    }

    httpContext.Response.Cookies.Delete(StorefrontCookieNames.Cart, new CookieOptions { Path = "/" });
    httpContext.Response.Cookies.Delete(StorefrontCookieNames.CartToken, new CookieOptions { Path = "/" });
    var nextAction = placeOrderResult.Data.NextAction;
    var nextActionUrl = nextAction?.Url;
    if (string.Equals(nextAction?.Type, "redirect", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(nextActionUrl))
    {
        return Results.Redirect(nextActionUrl);
    }

    if (string.IsNullOrWhiteSpace(placeOrderResult.Data.Reference))
    {
        return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create("error", "Order confirmation is not available yet."));
    }

    return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create("orderReference", placeOrderResult.Data.Reference));
});
app.MapGet("/api/cart", async (
    StorefrontCartTokenService cartTokenService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var result = await cartTokenService.ResolveAsync(httpContext, cancellationToken: cancellationToken);
    return result.Success
        ? Results.Ok(ToLocalCartResponse(result.Cart))
        : Results.Ok(ToLocalCartResponse(null));
});
app.MapPost("/api/product-selection-preview", async (
    StorefrontLocalProductSelectionPreviewRequest request,
    StorefrontApiClient apiClient,
    IStorefrontDisplayContextProvider displayContextProvider,
    IStorefrontPriceFormatter priceFormatter,
    CancellationToken cancellationToken) =>
{
    if (request.ProductId == Guid.Empty || request.Quantity < 1)
    {
        return Results.BadRequest(new StorefrontLocalCartErrorResponse("Product and quantity are required."));
    }

    var displayContext = await displayContextProvider.GetAsync(cancellationToken);
    var currencyCode = NormalizeCurrencyCode(request.CurrencyCode) ?? displayContext.CurrencyCode;
    var result = await apiClient.PreviewProductSelectionAsync(
        request.ProductId,
        new StorefrontProductSelectionPreviewRequest
        {
            ProductVariantId = request.ProductVariantId,
            SelectedAttributes = request.SelectedAttributes,
            Quantity = request.Quantity,
            CurrencyCode = currencyCode,
        },
        cancellationToken);

    if (!result.Success || result.Data is null)
    {
        return Results.BadRequest(new StorefrontLocalCartErrorResponse(result.Message));
    }

    var preview = result.Data;
    var previewContext = displayContext with { CurrencyCode = preview.CurrencyCode };
    return Results.Ok(new StorefrontLocalProductSelectionPreviewResponse(
        preview.ProductId,
        preview.ProductVariantId,
        preview.IsValid,
        preview.IsAvailable,
        preview.CanAddToCart,
        preview.ValidationMessages,
        preview.SelectedAttributes
            .Select(attribute => new SelectedAttributeDto(attribute.Name, attribute.Value))
            .ToArray(),
        preview.AttributeSignature,
        preview.Sku,
        preview.DisplayName,
        preview.UnitPrice,
        preview.ComparePrice,
        preview.CurrencyCode,
        priceFormatter.Format(preview.UnitPrice, previewContext),
        preview.ComparePrice.HasValue ? priceFormatter.Format(preview.ComparePrice.Value, previewContext) : null,
        preview.StockQuantity,
        preview.MinQuantity,
        preview.MaxQuantity,
        preview.PrimaryImageUrl));
});
app.MapPost("/api/cart/lines", async (
    StorefrontLocalCartLineRequest request,
    StorefrontCartTokenService cartTokenService,
    IAntiforgery antiforgery,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var antiforgeryFailure = await ValidateLocalCartAntiforgeryAsync(httpContext, antiforgery);
    if (antiforgeryFailure is not null)
    {
        return antiforgeryFailure;
    }

    if (request.ProductId == Guid.Empty || request.Quantity < 1)
    {
        return Results.BadRequest(new StorefrontLocalCartErrorResponse("Product and quantity are required."));
    }

    var result = await cartTokenService.AddLineAsync(
        httpContext,
        new StorefrontCartLineCreateRequest
        {
            ProductId = request.ProductId,
            ProductVariantId = request.ProductVariantId,
            CurrencyCode = request.CurrencyCode,
            Quantity = request.Quantity,
            SelectedAttributes = request.SelectedAttributes,
        },
        cancellationToken);

    return ToLocalCartMutationResult(result);
}).RequireRateLimiting(StorefrontLocalCartRateLimitPolicyName);
app.MapPut("/api/cart/lines/{lineId:guid}", async (
    Guid lineId,
    StorefrontLocalCartQuantityRequest request,
    StorefrontCartTokenService cartTokenService,
    IAntiforgery antiforgery,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var antiforgeryFailure = await ValidateLocalCartAntiforgeryAsync(httpContext, antiforgery);
    if (antiforgeryFailure is not null)
    {
        return antiforgeryFailure;
    }

    if (request.Quantity < 1)
    {
        return Results.BadRequest(new StorefrontLocalCartErrorResponse("Quantity must be at least 1."));
    }

    var result = await cartTokenService.UpdateLineAsync(httpContext, lineId, request.Quantity, cancellationToken);
    return ToLocalCartMutationResult(result);
}).RequireRateLimiting(StorefrontLocalCartRateLimitPolicyName);
app.MapDelete("/api/cart/lines/{lineId:guid}", async (
    Guid lineId,
    StorefrontCartTokenService cartTokenService,
    IAntiforgery antiforgery,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var antiforgeryFailure = await ValidateLocalCartAntiforgeryAsync(httpContext, antiforgery);
    if (antiforgeryFailure is not null)
    {
        return antiforgeryFailure;
    }

    var result = await cartTokenService.RemoveLineAsync(httpContext, lineId, cancellationToken);
    return ToLocalCartMutationResult(result);
}).RequireRateLimiting(StorefrontLocalCartRateLimitPolicyName);
app.MapDelete("/api/cart", async (
    StorefrontCartTokenService cartTokenService,
    IAntiforgery antiforgery,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var antiforgeryFailure = await ValidateLocalCartAntiforgeryAsync(httpContext, antiforgery);
    if (antiforgeryFailure is not null)
    {
        return antiforgeryFailure;
    }

    var result = await cartTokenService.ClearAsync(httpContext, cancellationToken);
    return ToLocalCartMutationResult(result);
}).RequireRateLimiting(StorefrontLocalCartRateLimitPolicyName);
app.MapGet("/api/consent/current", async (
    StorefrontApiClient apiClient,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    StorefrontResponseHeaders.ApplyPrivatePage(httpContext);
    var visitorKey = ResolveConsentVisitorKey(httpContext, createIfMissing: true);
    var result = await apiClient.GetConsentAsync(visitorKey, cancellationToken);
    return result.Success
        ? Results.Ok(result.Data)
        : Results.Json(new StorefrontLocalCartErrorResponse(result.Message), statusCode: StatusCodes.Status503ServiceUnavailable);
});
app.MapPost("/api/consent", async (
    StorefrontConsentSaveRequest request,
    StorefrontApiClient apiClient,
    IAntiforgery antiforgery,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var antiforgeryFailure = await ValidateLocalCartAntiforgeryAsync(httpContext, antiforgery);
    if (antiforgeryFailure is not null)
    {
        return antiforgeryFailure;
    }

    var visitorKey = ResolveConsentVisitorKey(httpContext, createIfMissing: true);
    var result = await apiClient.SaveConsentAsync(visitorKey, request, cancellationToken);
    return result.Success
        ? Results.Ok(result.Data)
        : Results.Json(new StorefrontLocalCartErrorResponse(result.Message), statusCode: StatusCodes.Status400BadRequest);
});
app.MapPost("/api/consent/revoke", async (
    StorefrontApiClient apiClient,
    IAntiforgery antiforgery,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    var antiforgeryFailure = await ValidateLocalCartAntiforgeryAsync(httpContext, antiforgery);
    if (antiforgeryFailure is not null)
    {
        return antiforgeryFailure;
    }

    var visitorKey = ResolveConsentVisitorKey(httpContext, createIfMissing: true);
    var result = await apiClient.RevokeConsentAsync(visitorKey, cancellationToken);
    return result.Success
        ? Results.Ok(result.Data)
        : Results.Json(new StorefrontLocalCartErrorResponse(result.Message), statusCode: StatusCodes.Status400BadRequest);
});
app.MapGet(StorefrontRoutes.Robots, async (HttpContext httpContext, IStorefrontRobotsService robotsService, CancellationToken cancellationToken) =>
{
    try
    {
        var content = await robotsService.GenerateAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            SeoRuntimeLogger.PublicDiscoveryRobotsFailure(app.Logger, StorefrontRoutes.Robots, "empty_document");
            StorefrontResponseHeaders.ApplyServiceUnavailable(httpContext);
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        StorefrontResponseHeaders.ApplyRobotsDocument(httpContext.Response);
        return Results.Text(content, "text/plain; charset=utf-8");
    }
    catch (Exception exception)
    {
        SeoRuntimeLogger.PublicDiscoveryRobotsFailure(app.Logger, exception, StorefrontRoutes.Robots, "generation_exception");
        StorefrontResponseHeaders.ApplyServiceUnavailable(httpContext);
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
});
app.MapGet(StorefrontRoutes.Sitemap, async (HttpContext httpContext, IStorefrontSitemapService sitemapService, CancellationToken cancellationToken) =>
{
    try
    {
        var result = await sitemapService.GenerateAsync(cancellationToken);
        if (result.IsServiceUnavailable)
        {
            SeoRuntimeLogger.PublicDiscoverySitemapFailure(app.Logger, StorefrontRoutes.Sitemap, "upstream_service_unavailable");
            StorefrontResponseHeaders.ApplySitemapUnavailable(httpContext.Response);
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        if (string.IsNullOrWhiteSpace(result.Content))
        {
            SeoRuntimeLogger.PublicDiscoverySitemapFailure(app.Logger, StorefrontRoutes.Sitemap, "empty_document");
            StorefrontResponseHeaders.ApplySitemapUnavailable(httpContext.Response);
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        StorefrontResponseHeaders.ApplySitemapDocument(httpContext.Response);
        return Results.Text(result.Content, "application/xml; charset=utf-8");
    }
    catch (Exception exception)
    {
        SeoRuntimeLogger.PublicDiscoverySitemapFailure(app.Logger, exception, StorefrontRoutes.Sitemap, "generation_exception");
        StorefrontResponseHeaders.ApplySitemapUnavailable(httpContext.Response);
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
});
app.MapGet("/media/products/{mediaPublicId:guid}", async (
    Guid mediaPublicId,
    HttpContext httpContext,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    return await ProxyCommerceNodeMediaAsync(
        $"media/products/{mediaPublicId:D}",
        httpContext,
        httpClientFactory,
        configuration,
        cancellationToken);
});
app.MapGet("/media/assets/{assetPublicId:guid}/{fileName}", async (
    Guid assetPublicId,
    string fileName,
    HttpContext httpContext,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    return await ProxyCommerceNodeMediaAsync(
        $"media/assets/{assetPublicId:D}/{Uri.EscapeDataString(fileName)}",
        httpContext,
        httpClientFactory,
        configuration,
        cancellationToken);
});
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorShop.Storefront.WASM._Imports).Assembly);

app.Run();

static Uri ResolveApiBaseAddress(IConfiguration configuration)
{
    var configuredBaseAddress = configuration[$"{StorefrontApiOptions.SectionName}:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(configuredBaseAddress)
        && Uri.TryCreate(configuredBaseAddress, UriKind.Absolute, out var configuredUri))
    {
        return configuredUri;
    }

    return new Uri("https+http://apiservice/api/");
}

static Uri ResolveCommerceNodeBaseAddress(IConfiguration configuration)
{
    var apiBaseAddress = ResolveApiBaseAddress(configuration);
    return new UriBuilder(apiBaseAddress)
    {
        Path = "/",
        Query = string.Empty,
        Fragment = string.Empty,
    }.Uri;
}

static async Task<IResult> ProxyCommerceNodeMediaAsync(
    string mediaPath,
    HttpContext httpContext,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    CancellationToken cancellationToken)
{
    var storeKey = ResolveStoreKey(configuration);
    if (string.IsNullOrWhiteSpace(storeKey))
    {
        return Results.NotFound();
    }

    var client = httpClientFactory.CreateClient();
    var targetUri = new Uri(
        ResolveCommerceNodeBaseAddress(configuration),
        $"{mediaPath}{httpContext.Request.QueryString}");

    using var request = new HttpRequestMessage(HttpMethod.Get, targetUri);
    request.Headers.TryAddWithoutValidation("X-Store-Key", storeKey);

    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
        return Results.StatusCode((int)response.StatusCode);
    }

    CopyHeaderIfPresent(response, httpContext.Response, "Cache-Control");
    CopyHeaderIfPresent(response, httpContext.Response, "ETag");
    CopyHeaderIfPresent(response, httpContext.Response, "Last-Modified");
    CopyHeaderIfPresent(response, httpContext.Response, "X-Content-Type-Options");

    var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
    return Results.File(content, contentType);
}

static void CopyHeaderIfPresent(HttpResponseMessage source, HttpResponse destination, string headerName)
{
    if (source.Headers.TryGetValues(headerName, out var values)
        || source.Content.Headers.TryGetValues(headerName, out values))
    {
        destination.Headers[headerName] = string.Join(",", values);
    }
}

static void ConfigureStorefrontHttpClient(HttpClient client, IConfiguration configuration)
{
    client.BaseAddress = ResolveScopedStorefrontApiBaseAddress(configuration);
}

static Uri ResolveScopedStorefrontApiBaseAddress(IConfiguration configuration)
{
    var apiBaseAddress = ResolveApiBaseAddress(configuration);
    var storeKey = ResolveStoreKey(configuration);
    if (string.IsNullOrWhiteSpace(storeKey))
    {
        return apiBaseAddress;
    }

    var path = apiBaseAddress.AbsolutePath.TrimEnd('/')
        + "/storefront/stores/"
        + Uri.EscapeDataString(storeKey)
        + "/";

    return new UriBuilder(apiBaseAddress)
    {
        Path = path,
        Query = string.Empty,
        Fragment = string.Empty,
    }.Uri;
}

static string? ResolveStoreKey(IConfiguration configuration)
{
    return StorefrontStoreKeyResolver.Resolve(configuration);
}

static StorefrontCheckoutPreviewRequest BuildCheckoutPreviewRequest(StorefrontCheckoutForm form, int expectedCartVersion)
{
    return new StorefrontCheckoutPreviewRequest
    {
        ExpectedCartVersion = expectedCartVersion,
        CustomerEmail = form.CustomerEmail?.Trim() ?? string.Empty,
        CustomerName = form.CustomerName?.Trim() ?? string.Empty,
        PaymentMethodKey = form.PaymentMethodKey?.Trim() ?? string.Empty,
        ShippingAddress = new StorefrontCheckoutPreviewShippingAddress
        {
            FullName = form.ShippingFullName?.Trim() ?? string.Empty,
            Email = form.ShippingEmail?.Trim() ?? form.CustomerEmail?.Trim() ?? string.Empty,
            Phone = form.ShippingPhone?.Trim(),
            Address1 = form.ShippingAddress1?.Trim() ?? string.Empty,
            Address2 = form.ShippingAddress2?.Trim(),
            City = form.ShippingCity?.Trim() ?? string.Empty,
            State = form.ShippingState?.Trim(),
            PostalCode = form.ShippingPostalCode?.Trim() ?? string.Empty,
            CountryCode = form.ShippingCountryCode?.Trim() ?? string.Empty,
        },
    };
}

static string? NormalizeCurrencyCode(string? currencyCode)
{
    var normalized = currencyCode?.Trim().ToUpperInvariant();
    return normalized is { Length: 3 } && normalized.All(char.IsLetter)
        ? normalized
        : null;
}

static IResult ToLocalCartMutationResult(StorefrontCartMutationResult result)
{
    if (result.Success)
    {
        return Results.Ok(ToLocalCartResponse(result.Cart));
    }

    return Results.Json(
        new StorefrontLocalCartErrorResponse(result.Message),
        statusCode: StatusCodes.Status400BadRequest);
}

static void ConfigureStorefrontRateLimiter(RateLimiterOptions options, StorefrontRateLimitingOptions rateLimitingOptions)
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        StorefrontResponseHeaders.ApplyPrivatePage(httpContext);
        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers["Retry-After"] = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        await httpContext.Response.WriteAsJsonAsync(
            new StorefrontLocalCartErrorResponse("Too many cart requests. Try again shortly."),
            cancellationToken);
    };

    options.AddPolicy(
        StorefrontLocalCartRateLimitPolicyName,
        httpContext => CreateStorefrontRateLimitPartition(httpContext, rateLimitingOptions.Cart));
}

static RateLimitPartition<string> CreateStorefrontRateLimitPartition(
    HttpContext httpContext,
    StorefrontRateLimitPolicyOptions policyOptions)
{
    var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
    var storeKey = StorefrontStoreKeyResolver.Resolve(configuration) ?? "unknown-store";
    var route = httpContext.GetEndpoint()?.DisplayName
        ?? httpContext.Request.Path.Value
        ?? "unknown-route";
    var actor = $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    var partitionKey = string.Join('|', storeKey, route, actor);

    return RateLimitPartition.GetFixedWindowLimiter(
        partitionKey,
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = Math.Clamp(policyOptions.PermitLimit, 1, 10_000),
            Window = TimeSpan.FromSeconds(Math.Clamp(policyOptions.WindowSeconds, 1, 3600)),
            QueueLimit = Math.Clamp(policyOptions.QueueLimit, 0, 1000),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true,
        });
}

static async Task<IResult?> ValidateLocalCartAntiforgeryAsync(HttpContext httpContext, IAntiforgery antiforgery)
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

static string ResolveConsentVisitorKey(HttpContext httpContext, bool createIfMissing)
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

static StorefrontLocalCartResponse ToLocalCartResponse(StorefrontCartResponse? cart)
{
    var lines = cart?.Lines ?? [];
    var count = cart is not null && cart.SummaryCount > 0
        ? cart.SummaryCount
        : lines.Sum(line => Math.Max(0, line.Quantity));
    return new StorefrontLocalCartResponse(
        Count: count,
        Version: cart?.Version ?? 0,
        Lines: lines);
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

public sealed record StorefrontLocalCartResponse(
    int Count,
    int Version,
    IReadOnlyList<StorefrontCartLineResponse> Lines);

public sealed record StorefrontLocalCartErrorResponse(string Message);

public partial class Program;
