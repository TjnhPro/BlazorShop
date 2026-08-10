namespace BlazorShop.Storefront.Presentation.Hosting;

using BlazorShop.Storefront.Presentation.Endpoints;
using BlazorShop.Storefront.Presentation.PagePatterns;
using Microsoft.AspNetCore.Builder;

public static class StorefrontPresentationApplicationBuilderExtensions
{
    public static WebApplication UseStorefrontPresentation(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Use(async (context, next) =>
        {
            StorefrontResponseHeaders.RegisterErrorStatusHeaders(context);
            await next();
        });
        app.UseAntiforgery();

        return app;
    }

    public static WebApplication MapStorefrontPresentation(this WebApplication endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapStorefrontPresentationAuthEndpoints();
        endpoints.MapStorefrontPresentationPreferenceEndpoints();
        endpoints.MapStorefrontPresentationCartEndpoints();
        endpoints.MapStorefrontPresentationAccountEndpoints();
        endpoints.MapStorefrontPresentationCheckoutEndpoints();
        endpoints.MapStorefrontPresentationConsentEndpoints();
        endpoints.MapStorefrontPresentationContactEndpoints();
        endpoints.MapStorefrontPresentationSeoEndpoints();
        endpoints.MapStorefrontPresentationMediaEndpoints();

        return endpoints;
    }
}
