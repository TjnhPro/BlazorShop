using BlazorShop.Storefront.Components.Browser;
using BlazorShop.Storefront.Components.Headless.Cart;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorShop.Storefront.Browser.Cart;

public sealed class StorefrontBrowserCartController : IStorefrontBrowserCartController
{
    private readonly IServiceProvider _services;
    private StorefrontCartActionDescriptor _actions = StorefrontCartActionDescriptor.Empty;
    private StorefrontFeatureDataMode _dataMode = StorefrontFeatureDataMode.BrowserFetch;
    private bool _initialized;

    public StorefrontBrowserCartController(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public StorefrontBrowserCartState State { get; } = new();

    public void Initialize(
        StorefrontBrowserCart? initialCart,
        IReadOnlyList<StorefrontBrowserCartAlert> initialAlerts,
        StorefrontFeatureDataMode dataMode,
        StorefrontCartActionDescriptor actions)
    {
        _dataMode = dataMode;
        _actions = actions ?? StorefrontCartActionDescriptor.Empty;
        State.ApiAvailable = ResolveApiClient() is not null;

        if (_initialized)
        {
            return;
        }

        State.Cart = initialCart;
        State.Alerts = initialAlerts.Count > 0 ? [.. initialAlerts] : [];
        _initialized = true;
    }

    public async Task<bool> HydrateAsync(CancellationToken cancellationToken = default)
    {
        State.ApiAvailable = ResolveApiClient() is not null;
        if (!State.ApiAvailable)
        {
            return false;
        }

        if (ShouldFetchAfterHydration())
        {
            return await LoadAsync(cancellationToken).ConfigureAwait(false);
        }

        if (State.Cart is not null)
        {
            await PublishCartChangedAsync(State.Cart.Count, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    public async Task<bool> LoadAsync(CancellationToken cancellationToken = default)
    {
        var apiClient = ResolveApiClient();
        if (apiClient is null || string.IsNullOrWhiteSpace(_actions.CurrentCartRoute))
        {
            State.ApiAvailable = false;
            return false;
        }

        State.ApiAvailable = true;
        var result = await apiClient.GetAsync<StorefrontBrowserCart>(_actions.CurrentCartRoute, cancellationToken).ConfigureAwait(false);
        return await ApplyCartResultAsync(result, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> UpdateQuantityAsync(Guid lineId, object? value, CancellationToken cancellationToken = default)
    {
        var apiClient = ResolveApiClient();
        var line = FindLine(lineId);
        if (apiClient is null
            || line is null
            || IsMutationBusy(lineId)
            || !int.TryParse(Convert.ToString(value), out var quantity))
        {
            State.ApiAvailable = apiClient is not null;
            return false;
        }

        if (quantity < line.QuantityMinimum)
        {
            AddError($"Minimum quantity for {line.DisplayName} is {line.QuantityMinimum}.");
            return true;
        }

        State.ApiAvailable = true;
        State.BusyLineId = lineId;
        try
        {
            var result = await apiClient.PutJsonAsync<StorefrontBrowserCartQuantityRequest, StorefrontBrowserCart>(
                _actions.UpdateLineRoute(lineId),
                new StorefrontBrowserCartQuantityRequest(quantity),
                cancellationToken).ConfigureAwait(false);
            return await ApplyCartResultAsync(result, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            State.BusyLineId = null;
        }
    }

    public async Task<bool> RemoveLineAsync(Guid lineId, CancellationToken cancellationToken = default)
    {
        var apiClient = ResolveApiClient();
        if (apiClient is null || FindLine(lineId) is null || IsMutationBusy(lineId))
        {
            State.ApiAvailable = apiClient is not null;
            return false;
        }

        State.ApiAvailable = true;
        State.BusyLineId = lineId;
        try
        {
            var result = await apiClient.DeleteAsync<StorefrontBrowserCart>(
                _actions.RemoveLineRoute(lineId),
                cancellationToken).ConfigureAwait(false);
            return await ApplyCartResultAsync(result, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            State.BusyLineId = null;
        }
    }

    public async Task<bool> ClearAsync(CancellationToken cancellationToken = default)
    {
        var apiClient = ResolveApiClient();
        if (apiClient is null
            || State.Clearing
            || State.BusyLineId.HasValue
            || string.IsNullOrWhiteSpace(_actions.ClearCartRoute))
        {
            State.ApiAvailable = apiClient is not null;
            return false;
        }

        State.ApiAvailable = true;
        State.Clearing = true;
        try
        {
            var result = await apiClient.DeleteAsync<StorefrontBrowserCart>(
                _actions.ClearCartRoute,
                cancellationToken).ConfigureAwait(false);
            return await ApplyCartResultAsync(result, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            State.Clearing = false;
        }
    }

    public bool IsDisabled(Guid lineId) => !State.ApiAvailable || IsBusy(lineId);

    public bool IsMutationBusy(Guid lineId) => State.BusyLineId.HasValue || State.Clearing || IsBusy(lineId);

    public bool IsClearDisabled() => !State.ApiAvailable || State.Clearing || State.BusyLineId.HasValue;

    private bool ShouldFetchAfterHydration()
    {
        return _dataMode switch
        {
            StorefrontFeatureDataMode.InitialSnapshot => false,
            StorefrontFeatureDataMode.BrowserFetch => State.Cart is null,
            StorefrontFeatureDataMode.RefreshAfterHydration => true,
            _ => State.Cart is null
        };
    }

    private async Task<bool> ApplyCartResultAsync(
        StorefrontLocalApiResult<StorefrontBrowserCart> result,
        CancellationToken cancellationToken)
    {
        if (result.Success && result.Data is not null)
        {
            State.Cart = result.Data;
            State.Alerts = [.. result.Data.Warnings.Select(warning => new StorefrontBrowserCartAlert("warning", warning.Message))];
            await PublishCartChangedAsync(result.Data.Count, cancellationToken).ConfigureAwait(false);
            return true;
        }

        AddError(string.IsNullOrWhiteSpace(result.Message) ? "Cart could not be updated." : result.Message);
        return true;
    }

    private void AddError(string message)
    {
        var alerts = State.Alerts
            .Where(alert => !string.Equals(alert.Level, "error", StringComparison.OrdinalIgnoreCase))
            .ToList();
        alerts.Insert(0, new StorefrontBrowserCartAlert("error", message));
        State.Alerts = alerts;
    }

    private StorefrontBrowserCartLine? FindLine(Guid lineId)
    {
        return State.Lines.FirstOrDefault(candidate => candidate.LineId == lineId);
    }

    private bool IsBusy(Guid lineId) => State.BusyLineId == lineId || State.Clearing;

    private StorefrontLocalApiClient? ResolveApiClient()
    {
        return _services.GetService<StorefrontLocalApiClient>();
    }

    private async Task PublishCartChangedAsync(int count, CancellationToken cancellationToken)
    {
        var publisher = _services.GetService<IStorefrontBrowserCartEventPublisher>();
        if (publisher is not null)
        {
            await publisher.PublishCartChangedAsync(count, cancellationToken).ConfigureAwait(false);
        }
    }
}
