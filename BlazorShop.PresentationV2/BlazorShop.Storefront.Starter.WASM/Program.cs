using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using BlazorShop.Storefront.Browser;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddStorefrontBrowserRuntime(builder.HostEnvironment);

await builder.Build().RunAsync();
