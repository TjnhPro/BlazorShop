using Microsoft.JSInterop;

namespace BlazorShop.Storefront.Browser;

public sealed class StorefrontBrowserCartEventPublisher : IStorefrontBrowserCartEventPublisher, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public StorefrontBrowserCartEventPublisher(IJSRuntime jsRuntime)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);

        _moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/BlazorShop.Storefront.Components/js/storefrontWasmInterop.js").AsTask());
    }

    public async ValueTask PublishCartChangedAsync(int count, CancellationToken cancellationToken = default)
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module.InvokeVoidAsync(
            "publishCartChanged",
            cancellationToken,
            count).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_moduleTask.IsValueCreated)
        {
            return;
        }

        var module = await _moduleTask.Value.ConfigureAwait(false);
        await module.DisposeAsync().ConfigureAwait(false);
    }
}
