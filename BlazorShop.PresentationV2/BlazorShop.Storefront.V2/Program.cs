using BlazorShop.Application.Diagnostics;
using BlazorShop.Application.DTOs.UserIdentity;
using BlazorShop.Application.Options;
using BlazorShop.Application.Services;
using BlazorShop.Application.Services.Contracts;
using BlazorShop.Storefront.Configuration;
using BlazorShop.Storefront.Options;
using BlazorShop.Storefront;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;

using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;

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
builder.Services.AddRazorComponents();
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

app.UseStaticFiles();
app.UseMiddleware<StorefrontPublicRedirectMiddleware>();
app.Use(async (context, next) =>
{
    StorefrontResponseHeaders.RegisterErrorStatusHeaders(context);
    await next();
});
app.UseAntiforgery();
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
app.MapGet(StorefrontRoutes.Checkout, async (HttpContext httpContext, IStorefrontClientAppUrlResolver clientAppUrlResolver, IStorefrontSessionResolver sessionResolver, CancellationToken cancellationToken) =>
{
    StorefrontResponseHeaders.ApplyPrivatePage(httpContext);

    var session = await sessionResolver.GetCurrentUserAsync(cancellationToken);
    if (!session.IsAuthenticated)
    {
        return Results.Redirect(StorefrontReturnUrl.BuildSignInUrl(StorefrontRoutes.Checkout));
    }

    var targetPath = "/account/checkout";
    return CreateClientRedirectResult(clientAppUrlResolver, targetPath);
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
app.MapRazorComponents<App>();

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

static IResult CreateClientRedirectResult(IStorefrontClientAppUrlResolver clientAppUrlResolver, string targetPath)
{
    if (string.IsNullOrWhiteSpace(clientAppUrlResolver.ResolveBaseUrl()))
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Redirect(clientAppUrlResolver.ResolveUrl(targetPath));
}

public partial class Program;
