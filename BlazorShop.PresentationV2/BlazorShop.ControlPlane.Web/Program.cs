using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorShop.ControlPlane.Web;
using BlazorShop.Web.Shared;
using BlazorShop.Web.Shared.BrowserStorage;
using BlazorShop.Web.Shared.BrowserStorage.Contracts;
using BlazorShop.Web.Shared.CookieStorage;
using BlazorShop.Web.Shared.CookieStorage.Contracts;
using BlazorShop.Web.Shared.Helper;
using BlazorShop.Web.Shared.Helper.Contracts;
using BlazorShop.Web.Shared.Services;
using BlazorShop.Web.Shared.Services.Contracts;
using BlazorShop.ControlPlane.Web.Services.Credentials;
using BlazorShop.ControlPlane.Web.Services.Nodes;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseAddress = ResolveApiBaseAddress(builder);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<IBrowserCookieStorageService, BrowserCookieStorageService>();
builder.Services.AddSingleton<IBrowserSessionStorageService, BrowserSessionStorageService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IHttpClientHelper, HttpClientHelper>();
builder.Services.AddScoped<IApiCallHelper, ApiCallHelper>();
builder.Services.AddSingleton<IToastService, ToastService>();
builder.Services.AddScoped<IControlPlaneCredentialClient, ControlPlaneCredentialClient>();
builder.Services.AddScoped<IControlPlaneNodeClient, ControlPlaneNodeClient>();
builder.Services.AddHttpClient(
    Constant.ApiClient.PublicName,
    client => client.BaseAddress = apiBaseAddress);
builder.Services.AddHttpClient(
    Constant.ApiClient.PrivateName,
    client => client.BaseAddress = apiBaseAddress);

await builder.Build().RunAsync();

static Uri ResolveApiBaseAddress(WebAssemblyHostBuilder builder)
{
    var configuredBaseAddress = builder.Configuration["ControlPlaneApi:BaseUrl"];

    if (!string.IsNullOrWhiteSpace(configuredBaseAddress) &&
        Uri.TryCreate(configuredBaseAddress, UriKind.Absolute, out var configuredUri))
    {
        return configuredUri;
    }

    return new Uri(new Uri(builder.HostEnvironment.BaseAddress), "api/");
}
