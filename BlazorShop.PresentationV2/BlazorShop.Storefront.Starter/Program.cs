using BlazorShop.Storefront.Starter;
using BlazorShop.Storefront.Starter.Features;
using BlazorShop.Storefront.Presentation.Hosting;
using BlazorShop.Storefront.Starter.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<StarterStorefrontOptions>()
    .Bind(builder.Configuration.GetSection(StarterStorefrontOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddStorefrontApplication(builder.Configuration);
builder.Services.AddSingleton(_ =>
    StarterFeatureManifest.Load(Path.Combine(builder.Environment.ContentRootPath, "Features", "feature-manifest.json")));
builder.Services.AddScoped<StarterFeatureActivationService>();
builder.Services.AddStarterFoundationViews();

var app = builder.Build();

app.UseStorefrontApplication();
app.MapStorefrontApplication(typeof(StarterFoundationViewRegistration));

app.Run();

namespace BlazorShop.Storefront.Starter
{
    public partial class Program;

}
