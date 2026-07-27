namespace BlazorShop.Storefront.Configuration
{
    using BlazorShop.Storefront.Options;
    using BlazorShop.Storefront.Services;

    public static class StorefrontApplicationBuilderExtensions
    {
        public static WebApplication UseStorefrontV2HostPipeline(
            this WebApplication app,
            StorefrontRateLimitingOptions rateLimitingOptions)
        {
            ArgumentNullException.ThrowIfNull(app);
            ArgumentNullException.ThrowIfNull(rateLimitingOptions);

            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }

            app.UseForwardedHeaders();
            app.UseStaticFiles();
            app.UseMiddleware<StorefrontCurrentStoreMiddleware>();
            app.UseMiddleware<StorefrontPublicRedirectMiddleware>();
            if (rateLimitingOptions.Enabled)
            {
                app.UseRateLimiter();
            }

            return app;
        }
    }
}
