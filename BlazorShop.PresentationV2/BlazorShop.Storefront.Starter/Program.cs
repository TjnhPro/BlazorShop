using BlazorShop.Storefront.Client;
using BlazorShop.Storefront.Starter;
using BlazorShop.Storefront.Starter.Components;
using BlazorShop.Storefront.Starter.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<StarterStorefrontOptions>()
    .Bind(builder.Configuration.GetSection(StarterStorefrontOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient(StarterStorefrontClientFactory.HttpClientName);
builder.Services.AddScoped<StarterStorefrontClientFactory>();
builder.Services.AddScoped(sp => sp.GetRequiredService<StarterStorefrontClientFactory>().CreateStoreClient());

builder.Services.AddRazorComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>();

app.Run();

namespace BlazorShop.Storefront.Starter
{
    public partial class Program;

    public sealed class StarterStorefrontClientFactory
    {
        public const string HttpClientName = "CommerceNodeStorefront";

        private readonly IHttpClientFactory httpClientFactory;
        private readonly Microsoft.Extensions.Options.IOptions<StarterStorefrontOptions> options;

        public StarterStorefrontClientFactory(
            IHttpClientFactory httpClientFactory,
            Microsoft.Extensions.Options.IOptions<StarterStorefrontOptions> options)
        {
            this.httpClientFactory = httpClientFactory;
            this.options = options;
        }

        public StorefrontStoreClient CreateStoreClient()
        {
            var httpClient = this.httpClientFactory.CreateClient(HttpClientName);
            return new StorefrontStoreClient(this.options.Value.CommerceNodeBaseUrl, httpClient);
        }
    }
}
