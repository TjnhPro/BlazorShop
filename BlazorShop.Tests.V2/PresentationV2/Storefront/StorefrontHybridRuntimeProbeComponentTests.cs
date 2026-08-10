namespace BlazorShop.Tests.PresentationV2.Storefront;

using BlazorShop.Storefront.Components.Contracts.System;
using BlazorShop.Storefront.Components.WasmHost.System;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class StorefrontHybridRuntimeProbeComponentTests
{
    [Fact]
    public async Task RendersPrerenderStateWithStableMarkersAndHostSuppliedClasses()
    {
        var html = await RenderAsync();

        Assert.Contains("data-storefront-component=\"hybrid-runtime-probe\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-hybrid-probe", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-runtime-state=\"prerender\"", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-hybrid-state", html, StringComparison.Ordinal);
        Assert.Contains("Before WASM", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-hybrid-value", html, StringComparison.Ordinal);
        Assert.Contains(">0</output>", html, StringComparison.Ordinal);
        Assert.Contains("data-storefront-hybrid-action", html, StringComparison.Ordinal);
        Assert.Contains("Increment", html, StringComparison.Ordinal);
        Assert.Contains("class=\"probe-root\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"probe-action\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceHasNoApiTransportServerDependencyOrRenderMode()
    {
        var source = File.ReadAllText(RepositoryPath(
            "BlazorShop.PresentationV2/BlazorShop.Storefront.Components.WasmHost/System/StorefrontHybridRuntimeProbe.razor"));

        Assert.Contains("RendererInfo.IsInteractive", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IHttpContextAccessor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@inject", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/api/", source, StringComparison.Ordinal);
        Assert.DoesNotContain("api/storefront", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@rendermode", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveServer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveAuto", source, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(StorefrontHybridRuntimeProbe.Labels)] = new StorefrontHybridRuntimeProbeLabels(
                    "Runtime probe",
                    "Before WASM",
                    "After WASM",
                    "Counter",
                    "Increment"),
                [nameof(StorefrontHybridRuntimeProbe.Classes)] = new StorefrontHybridRuntimeProbeClasses(
                    Root: "probe-root",
                    Heading: "probe-heading",
                    State: "probe-state",
                    ValueGroup: "probe-value-group",
                    ValueLabel: "probe-value-label",
                    Value: "probe-value",
                    Action: "probe-action"),
            });

            var component = await renderer.RenderComponentAsync<StorefrontHybridRuntimeProbe>(parameters);
            return component.ToHtmlString();
        });
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
