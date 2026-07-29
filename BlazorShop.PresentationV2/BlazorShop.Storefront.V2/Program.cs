using BlazorShop.Storefront.Browser;
using BlazorShop.Storefront.Presentation.Hosting;
using BlazorShop.Storefront.V2;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddStorefrontApplication(builder.Configuration);
builder.Services.AddStorefrontBrowserControllers();
builder.Services.AddV2FoundationViews();

var app = builder.Build();

app.UseStorefrontApplication();
app.MapDefaultEndpoints();
app.MapStorefrontApplication(
    typeof(V2FoundationViewRegistration),
    typeof(BlazorShop.Storefront.V2.WASM.Components.Account.StorefrontAccountApp).Assembly);

app.Run();

public partial class Program;
