using Microsoft.JSInterop;

namespace BlazorShop.Storefront.Components.Browser;

public sealed class StorefrontAntiforgeryTokenReader : IStorefrontAntiforgeryTokenReader, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

    public StorefrontAntiforgeryTokenReader(IJSRuntime jsRuntime)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);

        _moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/BlazorShop.Storefront.Components/js/storefrontWasmInterop.js").AsTask());
    }

    public async ValueTask<StorefrontAntiforgeryToken?> ReadAsync(CancellationToken cancellationToken = default)
    {
        var module = await _moduleTask.Value.ConfigureAwait(false);
        return await module.InvokeAsync<StorefrontAntiforgeryToken?>(
            "readAntiforgery",
            cancellationToken).ConfigureAwait(false);
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
