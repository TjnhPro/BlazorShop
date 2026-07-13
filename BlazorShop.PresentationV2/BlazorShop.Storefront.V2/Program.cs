using BlazorShop.Application.Diagnostics;
using BlazorShop.Application.DTOs.UserIdentity;
using BlazorShop.Application.DTOs.Payment;
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

using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SharedProcessCart = BlazorShop.Web.SharedV2.Models.Payment.ProcessCart;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IValidateOptions<StorefrontApiOptions>, StorefrontApiOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<ClientAppOptions>, StorefrontClientAppOptionsValidator>();
builder.Services.AddSingleton<IValidateOptions<StorefrontPublicUrlOptions>, StorefrontPublicUrlOptionsValidator>();
builder.Services.AddOptions<StorefrontApiOptions>()
    .Bind(builder.Configuration.GetSection(StorefrontApiOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<ClientAppOptions>()
    .Bind(builder.Configuration.GetSection(ClientAppOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddOptions<StorefrontPublicUrlOptions>()
    .Bind(builder.Configuration.GetSection(StorefrontPublicUrlOptions.SectionName))
    .ValidateOnStart();
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

app.UseStaticFiles();
app.UseMiddleware<StorefrontPublicRedirectMiddleware>();
app.Use(async (context, next) =>
{
    StorefrontResponseHeaders.RegisterErrorStatusHeaders(context);
    await next();
});
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
        },
        cancellationToken);

    if (!result.Success || result.Data is null || !result.Data.Success || string.IsNullOrWhiteSpace(result.Data.Token))
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
app.MapPost(StorefrontRoutes.Checkout, async (
    [FromForm] StorefrontCheckoutForm form,
    StorefrontApiClient apiClient,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    StorefrontResponseHeaders.ApplyPrivatePage(httpContext);

    var carts = ReadCartCookie(httpContext.Request.Cookies[StorefrontCookieNames.Cart]);
    if (carts.Count == 0)
    {
        return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create("error", "Your cart is empty."));
    }

    var request = new StorefrontCheckoutRequest
    {
        CustomerEmail = form.CustomerEmail?.Trim() ?? string.Empty,
        CustomerName = form.CustomerName?.Trim() ?? string.Empty,
        PaymentMethodKey = form.PaymentMethodKey?.Trim() ?? string.Empty,
        Carts = carts.Select(MapCartItem).ToArray(),
        ShippingAddress = new CheckoutShippingAddress
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

    var result = await apiClient.CheckoutAsync(request, cancellationToken);
    if (!result.Success || result.Data is null)
    {
        return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create("error", result.Message));
    }

    httpContext.Response.Cookies.Delete(StorefrontCookieNames.Cart, new CookieOptions { Path = "/" });
    return Results.Redirect(StorefrontRoutes.Checkout + QueryString.Create("orderReference", result.Data.Reference));
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
    client.BaseAddress = ResolveApiBaseAddress(configuration);

    var storeKey = ResolveStoreKey(configuration);
    if (!string.IsNullOrWhiteSpace(storeKey))
    {
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Store-Key", storeKey);
    }
}

static string? ResolveStoreKey(IConfiguration configuration)
{
    return FirstNonEmpty(
        configuration[$"{StorefrontApiOptions.SectionName}:StoreKey"],
        configuration["StoreKey"],
        configuration["STORE_KEY"]);
}

static string? FirstNonEmpty(params string?[] values)
{
    foreach (var value in values)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }
    }

    return null;
}

static List<SharedProcessCart> ReadCartCookie(string? rawCart)
{
    if (string.IsNullOrWhiteSpace(rawCart))
    {
        return [];
    }

    try
    {
        return JsonSerializer.Deserialize<List<SharedProcessCart>>(rawCart, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?.Where(item => item.ProductId != Guid.Empty && item.Quantity > 0)
            .ToList()
            ?? [];
    }
    catch (JsonException)
    {
        return [];
    }
}

static ProcessCart MapCartItem(SharedProcessCart item)
{
    return new ProcessCart
    {
        ProductId = item.ProductId,
        ProductVariantId = item.ProductVariantId ?? item.VariantId,
        Quantity = item.Quantity,
        SelectedAttributes = item.SelectedAttributes?
            .Select(attribute => new SelectedAttributeDto(attribute.Name, attribute.Value))
            .ToArray(),
    };
}

public partial class Program;
