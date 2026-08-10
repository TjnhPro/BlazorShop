namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.V2.WASM.Components.System;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class StorefrontComponentMvpLabTests
{
    [Fact]
    public void LabComposesBrandLogoInSsrSectionWithoutOwningRoute()
    {
        var source = ReadRepositoryFile(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/System/StorefrontComponentMvpLab.razor");

        Assert.Contains("data-storefront-component-mvp-section=\"ssr\"", source, StringComparison.Ordinal);
        Assert.Contains("<StorefrontBrandLogo Context=\"Context.BrandLogo\" Classes=\"BrandLogoClasses\" />", source, StringComparison.Ordinal);
        Assert.Contains("Root: \"bs-storefront-component-mvp__brand\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("data-storefront-component-mvp-placeholder=\"ssr\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@page", source, StringComparison.Ordinal);
    }

    [Fact]
    public void LabPlacesHybridWrapperWithInteractiveWebAssemblyRenderMode()
    {
        var source = ReadRepositoryFile(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/System/StorefrontComponentMvpLab.razor");

        Assert.Contains("data-storefront-component-mvp-section=\"hybrid\"", source, StringComparison.Ordinal);
        Assert.Contains("<StorefrontHybridRuntimeProbeSection @rendermode=\"InteractiveWebAssembly\" />", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveServer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveAuto", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@page", source, StringComparison.Ordinal);
    }

    [Fact]
    public void LabPlacesWasmHostRailWrapperWithSameOriginBffAction()
    {
        var lab = ReadRepositoryFile(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/System/StorefrontComponentMvpLab.razor");
        var wrapper = ReadRepositoryFile(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/Components/Catalog/StorefrontDiscountedProductRailSection.razor");

        Assert.Contains("data-storefront-component-mvp-section=\"wasmhost\"", lab, StringComparison.Ordinal);
        Assert.Contains("<StorefrontDiscountedProductRailSection @rendermode=\"InteractiveWebAssembly\" />", lab, StringComparison.Ordinal);
        Assert.Contains("StorefrontDiscountedProductRailActionDescriptor", wrapper, StringComparison.Ordinal);
        Assert.Contains("\"/api/catalog/discounted-products\"", wrapper, StringComparison.Ordinal);
        Assert.DoesNotContain("api/storefront/stores", wrapper, StringComparison.Ordinal);
        Assert.DoesNotContain("CommerceNode", wrapper, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", wrapper, StringComparison.Ordinal);
        Assert.DoesNotContain("data-storefront-component-mvp-placeholder=\"wasmhost\"", lab, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HybridWrapperRendersProbePrerenderMarkupWhenRenderedAsStaticHtml()
    {
        var html = await RenderHybridWrapperAsync();

        Assert.Contains("data-storefront-component=\"hybrid-runtime-probe\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-hybrid-probe", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-runtime-state=\"prerender\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-hybrid-value", html, StringComparison.Ordinal);
        Assert.Contains(">0</output>", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-hybrid-action", html, StringComparison.Ordinal);
    }

    private static async Task<string> RenderHybridWrapperAsync()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<StorefrontHybridRuntimeProbeSection>();
            return component.ToHtmlString();
        });
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string ReadRepositoryFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
