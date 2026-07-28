namespace BlazorShop.Storefront.Presentation.Hosting;

using System.Reflection;
using BlazorShop.Storefront.Presentation.Options;
using BlazorShop.Storefront.Presentation.App;
using BlazorShop.Storefront.Presentation.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

public static class StorefrontApplicationBuilderExtensions
{
    public static WebApplication UseStorefrontApplication(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseForwardedHeaders();
        app.UseStaticFiles();
        app.UseMiddleware<StorefrontCurrentStoreMiddleware>();
        app.UseMiddleware<StorefrontPublicRedirectMiddleware>();

        var rateLimitingOptions = app.Services.GetRequiredService<IOptions<StorefrontRateLimitingOptions>>().Value;
        if (rateLimitingOptions.Enabled)
        {
            app.UseRateLimiter();
        }

        app.UseStorefrontPresentation();

        return app;
    }

    public static WebApplication MapStorefrontApplication(
        this WebApplication app,
        Type viewRegistrationType,
        params Assembly[] additionalAssemblies)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(viewRegistrationType);

        var applicationOptions = app.Services.GetRequiredService<IOptions<StorefrontApplicationOptions>>().Value;

        app.MapStaticAssets();
        if (!string.IsNullOrWhiteSpace(applicationOptions.FaviconRedirectPath))
        {
            app.MapGet("/favicon.ico", () => Results.Redirect(applicationOptions.FaviconRedirectPath, permanent: false));
        }

        app.MapStorefrontPresentation();

        var componentAssemblies = new[] { viewRegistrationType.Assembly }
            .Concat(additionalAssemblies)
            .Distinct()
            .ToArray();
        var components = app.MapRazorComponents<StorefrontApp>();
        if (applicationOptions.EnableInteractiveWebAssembly && additionalAssemblies.Length > 0)
        {
            components.AddInteractiveWebAssemblyRenderMode();
        }

        components.AddAdditionalAssemblies(componentAssemblies);

        return app;
    }
}
