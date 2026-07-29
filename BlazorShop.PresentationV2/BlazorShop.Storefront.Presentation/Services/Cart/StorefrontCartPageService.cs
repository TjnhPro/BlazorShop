namespace BlazorShop.Storefront.Presentation.Services.Cart;

using BlazorShop.Storefront.Components.Browser;
using BlazorShop.Storefront.Presentation.Services;
using BlazorShop.Storefront.Presentation.Services.Browser;
using BlazorShop.Storefront.Presentation.Contracts;
using Microsoft.AspNetCore.Http;

public sealed class StorefrontCartPageService
{
    private readonly StorefrontCartTokenService cartTokenService;
    private readonly IStorefrontDisplayContextProvider displayContextProvider;
    private readonly IStorefrontPriceFormatter priceFormatter;
    private readonly StorefrontBrowserActionDescriptorProvider actionDescriptorProvider;

    public StorefrontCartPageService(
        StorefrontCartTokenService cartTokenService,
        IStorefrontDisplayContextProvider displayContextProvider,
        IStorefrontPriceFormatter priceFormatter,
        StorefrontBrowserActionDescriptorProvider actionDescriptorProvider)
    {
        this.cartTokenService = cartTokenService;
        this.displayContextProvider = displayContextProvider;
        this.priceFormatter = priceFormatter;
        this.actionDescriptorProvider = actionDescriptorProvider;
    }

    public async Task<StorefrontCartPageContext> GetAsync(
        HttpContext? httpContext,
        CancellationToken cancellationToken = default)
    {
        var alerts = new List<StorefrontBrowserCartAlert>();
        var displayContext = await this.displayContextProvider.GetAsync(cancellationToken);
        var cartResolution = await this.cartTokenService.ResolveAsync(httpContext, cancellationToken: cancellationToken);
        if (!cartResolution.Success)
        {
            alerts.Add(new StorefrontBrowserCartAlert("error", cartResolution.Message));
            return CreateContext(null, alerts, this.actionDescriptorProvider);
        }

        var cart = StorefrontCartPresentationMapper.ToLocalCartResponse(
            cartResolution.Cart,
            displayContext,
            this.priceFormatter);
        foreach (var warning in cart.Warnings)
        {
            alerts.Add(new StorefrontBrowserCartAlert("warning", warning.Message));
        }

        return CreateContext(cart, alerts, this.actionDescriptorProvider);
    }

    private static StorefrontCartPageContext CreateContext(
        StorefrontBrowserCart? cart,
        IReadOnlyList<StorefrontBrowserCartAlert> alerts,
        StorefrontBrowserActionDescriptorProvider actionDescriptorProvider)
    {
        return new StorefrontCartPageContext(
            cart,
            alerts,
            StorefrontRoutes.Checkout,
            StorefrontRoutes.NewReleases,
            StorefrontRoutes.TodaysDeals,
            StorefrontLinkContext.Default)
        {
            CartActions = actionDescriptorProvider.CreateCartActions(),
        };
    }
}
