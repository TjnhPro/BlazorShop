namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Presentation.DependencyInjection;
using BlazorShop.Storefront.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PresentationAddressClient = BlazorShop.Storefront.Services.Contracts.IStorefrontAddressClient;
using PresentationAuthClient = BlazorShop.Storefront.Services.Contracts.IStorefrontAuthClient;
using PresentationCartClient = BlazorShop.Storefront.Services.Contracts.IStorefrontCartClient;
using PresentationCatalogClient = BlazorShop.Storefront.Services.Contracts.IStorefrontCatalogClient;
using PresentationCheckoutClient = BlazorShop.Storefront.Services.Contracts.IStorefrontCheckoutClient;
using PresentationContentClient = BlazorShop.Storefront.Services.Contracts.IStorefrontContentClient;
using PresentationCustomerClient = BlazorShop.Storefront.Services.Contracts.IStorefrontCustomerClient;
using PresentationCurrentStoreProvider = BlazorShop.Storefront.Services.Contracts.IStorefrontCurrentStoreProvider;
using PresentationDisplayContextProvider = BlazorShop.Storefront.Services.Contracts.IStorefrontDisplayContextProvider;
using PresentationPaymentClient = BlazorShop.Storefront.Services.Contracts.IStorefrontPaymentClient;
using PresentationPriceFormatter = BlazorShop.Storefront.Services.Contracts.IStorefrontPriceFormatter;
using PresentationSessionResolver = BlazorShop.Storefront.Services.Contracts.IStorefrontSessionResolver;
using PresentationStoreConfigurationClient = BlazorShop.Storefront.Services.Contracts.IStorefrontStoreConfigurationClient;

public sealed class StorefrontPresentationCutoverGuardrailTests
{
    private const string CutoverTodo = "SPF16 guardrail placeholder; enable after the matching cutover phase implements the final state.";

    [Fact]
    public void StorefrontPresentation_DIGraph_IsHostIndependent()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddStorefrontRuntime(options =>
        {
            options.StoreKey = "sample";
            options.CommerceNodeBaseUrl = "https://commerce-node.example/";
        });
        services.AddStorefrontPlatformRuntime();
        services.AddStorefrontPresentation(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;

        AssertPresentationOwned(scoped.GetRequiredService<PresentationAddressClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationAuthClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationCartClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationCatalogClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationCheckoutClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationContentClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationCustomerClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationCurrentStoreProvider>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationDisplayContextProvider>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationPaymentClient>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationPriceFormatter>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationSessionResolver>());
        AssertPresentationOwned(scoped.GetRequiredService<PresentationStoreConfigurationClient>());
    }

    [Fact(Skip = CutoverTodo)]
    public void StorefrontVisualViews_DoNotOwnRoutesOrSeoHead()
    {
    }

    [Fact(Skip = CutoverTodo)]
    public void StorefrontStarter_ViewsRenderPresentationContextsOnly()
    {
    }

    [Fact(Skip = CutoverTodo)]
    public void StorefrontRoutes_ArePresentationAssemblyOnly()
    {
    }

    private static void AssertPresentationOwned(object service)
    {
        Assert.DoesNotContain(
            "BlazorShop.Storefront.V2",
            service.GetType().Assembly.GetName().Name,
            StringComparison.Ordinal);
    }
}
