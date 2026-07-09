using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorShop.ControlPlane.Web;
using BlazorShop.Web.SharedV2;
using BlazorShop.Web.SharedV2.BrowserStorage;
using BlazorShop.Web.SharedV2.BrowserStorage.Contracts;
using BlazorShop.Web.SharedV2.CookieStorage;
using BlazorShop.Web.SharedV2.CookieStorage.Contracts;
using BlazorShop.Web.SharedV2.Authentication;
using BlazorShop.Web.SharedV2.Helper;
using BlazorShop.Web.SharedV2.Helper.Contracts;
using BlazorShop.Web.SharedV2.Services;
using BlazorShop.Web.SharedV2.Services.Contracts;
using BlazorShop.ControlPlane.Web.Services.Authentication;
using BlazorShop.ControlPlane.Web.Services.Actions;
using BlazorShop.ControlPlane.Web.Services.Common;
using BlazorShop.ControlPlane.Web.Services.Credentials;
using BlazorShop.ControlPlane.Web.Services.Dashboard;
using BlazorShop.ControlPlane.Web.Services.Health;
using BlazorShop.ControlPlane.Web.Services.Nodes;
using BlazorShop.ControlPlane.Web.Services.Stores;
using BlazorShop.ControlPlane.Web.Services.Users;
using Microsoft.AspNetCore.Components.Authorization;

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
builder.Services.AddScoped<BlazorShop.Web.SharedV2.Services.Contracts.IAuthenticationService, ControlPlaneAuthenticationService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<IAuthenticationStateNotifier, AuthenticationStateNotifier>();
builder.Services.AddScoped<IAuthenticatedClientStateCleaner, AuthenticatedClientStateCleaner>();
builder.Services.AddScoped<IAuthenticationSessionRefresher, AuthenticationSessionRefresher>();
builder.Services.AddScoped<IAuthenticationSessionBootstrapper, AuthenticationSessionBootstrapper>();
builder.Services.AddScoped<IAuthenticationSessionEventPublisher, AuthenticationSessionEventPublisher>();
builder.Services.AddScoped<IAuthenticationSessionSyncService, AuthenticationSessionSyncService>();
builder.Services.AddScoped<BrowserCredentialsHandler>();
builder.Services.AddScoped<RefreshTokenHandler>();
builder.Services.AddScoped<IControlPlaneActionClient, ControlPlaneActionClient>();
builder.Services.AddScoped<IControlPlaneApiClient, ControlPlaneApiClient>();
builder.Services.AddScoped<IControlPlaneCredentialClient, ControlPlaneCredentialClient>();
builder.Services.AddScoped<IControlPlaneDashboardClient, ControlPlaneDashboardClient>();
builder.Services.AddScoped<IControlPlaneHealthClient, ControlPlaneHealthClient>();
builder.Services.AddScoped<IControlPlaneNodeClient, ControlPlaneNodeClient>();
builder.Services.AddScoped<IControlPlaneStoreClient, ControlPlaneStoreClient>();
builder.Services.AddScoped<IControlPlaneUserClient, ControlPlaneUserClient>();
builder.Services.AddHttpClient(
    HttpClientNames.Public,
    client => client.BaseAddress = apiBaseAddress)
    .AddHttpMessageHandler<BrowserCredentialsHandler>();
builder.Services.AddHttpClient(
    HttpClientNames.Private,
    client => client.BaseAddress = apiBaseAddress)
    .AddHttpMessageHandler<BrowserCredentialsHandler>()
    .AddHttpMessageHandler<RefreshTokenHandler>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

var host = builder.Build();
await host.Services.GetRequiredService<IAuthenticationSessionBootstrapper>().RestoreAsync();
await host.RunAsync();

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
