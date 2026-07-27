using BlazorShop.Storefront.Configuration;
using BlazorShop.Storefront.Endpoints;
using BlazorShop.Storefront.Presentation.App;
using BlazorShop.Storefront.Presentation.Endpoints;
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
builder.Services.AddV2FoundationViews();

var app = builder.Build();

app.UseStorefrontV2HostPipeline(storefrontRateLimitingOptions);
app.MapStaticAssets();
app.MapGet("/favicon.ico", () => Results.Redirect("/icon-192.png", permanent: false));
app.MapDefaultEndpoints();
app.MapStorefrontPresentationAuthEndpoints();
app.MapStorefrontAuthFormEndpoints();
app.MapStorefrontPresentationCartEndpoints();
app.MapStorefrontAccountEndpoints();
app.MapStorefrontPresentationCheckoutEndpoints();
app.MapStorefrontConsentEndpoints();
app.MapStorefrontPresentationSeoEndpoints();
app.MapStorefrontMediaEndpoints();
app.MapRazorComponents<StorefrontApp>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(V2FoundationViewRegistration).Assembly,
        typeof(BlazorShop.Storefront.V2.WASM.Components.Account.StorefrontAccountApp).Assembly);

app.Run();

public partial class Program;
