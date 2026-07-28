using BlazorShop.Storefront.Starter;
using BlazorShop.Storefront.Presentation.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddStorefrontApplication(builder.Configuration)
    .AddStarterFoundationViews();

var app = builder.Build();

app.UseStorefrontApplication();
app.MapStorefrontApplication(typeof(StarterFoundationViewRegistration));

app.Run();

namespace BlazorShop.Storefront.Starter
{
    public partial class Program;

}
