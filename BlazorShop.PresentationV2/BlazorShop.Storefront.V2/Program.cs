using BlazorShop.Storefront.Configuration;
using BlazorShop.Storefront.Endpoints;
using BlazorShop.Storefront.Options;
using BlazorShop.Storefront;

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
    .AddAdditionalAssemblies(typeof(BlazorShop.Storefront.Components.Account.StorefrontAccountApp).Assembly);

app.Run();

public partial class Program;
