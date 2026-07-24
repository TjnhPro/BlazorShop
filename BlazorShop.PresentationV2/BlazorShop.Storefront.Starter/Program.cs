using BlazorShop.Storefront.Runtime;
using BlazorShop.Storefront.Starter;
using BlazorShop.Storefront.Starter.Components;
using BlazorShop.Storefront.Starter.Endpoints;
using BlazorShop.Storefront.Starter.Features;
using BlazorShop.Storefront.Starter.Options;
using BlazorShop.Storefront.Starter.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<StarterStorefrontOptions>()
    .Bind(builder.Configuration.GetSection(StarterStorefrontOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddStorefrontRuntime(options =>
{
    var starterOptions = builder.Configuration.GetSection(StarterStorefrontOptions.SectionName).Get<StarterStorefrontOptions>()
        ?? new StarterStorefrontOptions();
    options.CommerceNodeBaseUrl = starterOptions.CommerceNodeBaseUrl;
    options.StoreKey = starterOptions.StoreKey;
    options.PublicBaseUrl = starterOptions.PublicBaseUrl;
});
builder.Services.AddStorefrontGeneratedClients();
builder.Services.AddScoped<StorefrontBootstrapService>();
builder.Services.AddSingleton(_ =>
    StarterFeatureManifest.Load(Path.Combine(builder.Environment.ContentRootPath, "Features", "feature-manifest.json")));
builder.Services.AddScoped<StarterFeatureActivationService>();

builder.Services.AddRazorComponents();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseAntiforgery();
app.MapStarterBffEndpoints();
app.MapStarterSeoEndpoints();

app.MapRazorComponents<App>();

app.Run();

namespace BlazorShop.Storefront.Starter
{
    public partial class Program;

}
