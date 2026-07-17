using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using BlazorShop.Storefront.Components.Browser;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
});
builder.Services.AddScoped<IStorefrontAntiforgeryTokenReader, StorefrontAntiforgeryTokenReader>();
builder.Services.AddScoped<StorefrontLocalApiClient>();

await builder.Build().RunAsync();
