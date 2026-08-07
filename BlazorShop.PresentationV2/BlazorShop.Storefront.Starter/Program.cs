using BlazorShop.Storefront.Browser;
using BlazorShop.Storefront.Starter;
using BlazorShop.Storefront.Presentation.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStorefrontApplication(builder.Configuration);
builder.Services.AddStorefrontBrowserControllers();
builder.Services.AddStarterFoundationViews();

var app = builder.Build();

app.UseStorefrontApplication();
app.MapStorefrontApplication(
    typeof(StarterFoundationViewRegistration),
    typeof(BlazorShop.Storefront.Starter.WASM.StarterWasmAssemblyMarker).Assembly);

app.Run();

namespace BlazorShop.Storefront.Starter
{
    public partial class Program;

}
