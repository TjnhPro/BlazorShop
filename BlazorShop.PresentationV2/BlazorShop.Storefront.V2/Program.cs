using System.Globalization;

using BlazorShop.Application.Diagnostics;
using BlazorShop.Application.DTOs.UserIdentity;
using BlazorShop.Application.CommerceNode.VariationTemplates;
using BlazorShop.Application.Services;
using BlazorShop.Application.Services.Contracts;
using BlazorShop.Storefront.Configuration;
using BlazorShop.Storefront.Endpoints;
using BlazorShop.Storefront.Options;
using BlazorShop.Storefront;
using BlazorShop.Storefront.Components.Browser;
using BlazorShop.Storefront.Services;
using BlazorShop.Storefront.Services.Contracts;
using BlazorShop.Storefront.WASM;
using BlazorShop.Web.SharedV2;
using BlazorShop.Web.SharedV2.Models;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var storefrontRateLimitingOptions = builder.Configuration
    .GetSection(StorefrontRateLimitingOptions.SectionName)
    .Get<StorefrontRateLimitingOptions>() ?? new StorefrontRateLimitingOptions();

builder.AddServiceDefaults();

builder.Services.AddStorefrontV2Services(
    builder.Configuration,
    storefrontRateLimitingOptions,
    StorefrontRateLimitPolicies.ConfigureStorefrontRateLimiter,
    StorefrontApiEndpointResolver.ConfigureStorefrontHttpClient);

var app = builder.Build();

app.UseStorefrontV2HostPipeline(storefrontRateLimitingOptions);
app.MapStaticAssets();
app.MapGet("/favicon.ico", () => Results.Redirect("/icon-192.png", permanent: false));
app.MapDefaultEndpoints();
app.MapStorefrontAuthFormEndpoints();
app.MapStorefrontCartEndpoints();
app.MapStorefrontAccountEndpoints();
app.MapStorefrontCheckoutEndpoints();
app.MapStorefrontConsentEndpoints();
app.MapStorefrontSeoEndpoints();
app.MapStorefrontMediaEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorShop.Storefront.WASM._Imports).Assembly);

app.Run();

public partial class Program;
