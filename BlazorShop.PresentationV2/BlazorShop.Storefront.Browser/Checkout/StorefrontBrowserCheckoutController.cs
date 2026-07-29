using BlazorShop.Storefront.Components.Browser;
using BlazorShop.Storefront.Components.Headless.Checkout;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorShop.Storefront.Browser.Checkout;

public sealed class StorefrontBrowserCheckoutController : IStorefrontBrowserCheckoutController
{
    private readonly IServiceProvider _services;
    private StorefrontCheckoutActionDescriptor _actions = StorefrontCheckoutActionDescriptor.Empty;
    private StorefrontFeatureDataMode _dataMode = StorefrontFeatureDataMode.BrowserFetch;
    private bool _showPanel = true;
    private bool _initialized;
    private Guid? _checkoutSessionId;
    private string _idempotencyKey = Guid.NewGuid().ToString("N");

    public StorefrontBrowserCheckoutController(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public StorefrontBrowserCheckoutControllerState State { get; } = new();

    public void Initialize(
        StorefrontBrowserCheckoutState initialState,
        bool showPanel,
        StorefrontFeatureDataMode dataMode,
        StorefrontCheckoutActionDescriptor actions)
    {
        _showPanel = showPanel;
        _dataMode = dataMode;
        _actions = actions ?? StorefrontCheckoutActionDescriptor.Empty;
        State.ApiAvailable = ResolveApiClient() is not null;

        if (_initialized && !ShouldAcceptInitialState(initialState))
        {
            return;
        }

        ApplyCheckoutState(initialState);
        _initialized = true;
    }

    public Task<bool> HydrateAsync(CancellationToken cancellationToken = default)
    {
        if (!_showPanel || _dataMode == StorefrontFeatureDataMode.InitialSnapshot)
        {
            return Task.FromResult(false);
        }

        return RefreshAsync(cancellationToken);
    }

    public Task<bool> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var apiClient = ResolveApiClient();
        if (!_showPanel || apiClient is null || string.IsNullOrWhiteSpace(_actions.CurrentCheckoutRoute))
        {
            State.ApiAvailable = apiClient is not null;
            return Task.FromResult(false);
        }

        State.ApiAvailable = true;
        return LoadAsync(
            client => client.GetAsync<StorefrontBrowserCheckoutState>(_actions.CurrentCheckoutRoute, cancellationToken));
    }

    public Task<bool> SelectShippingAsync(string key, CancellationToken cancellationToken = default)
    {
        return SelectAsync(_actions.ShippingMethodRoute, key, cancellationToken);
    }

    public Task<bool> SelectPaymentAsync(string key, CancellationToken cancellationToken = default)
    {
        return SelectAsync(_actions.PaymentMethodRoute, key, cancellationToken);
    }

    public Task<bool> ReviewAsync(CancellationToken cancellationToken = default)
    {
        if (State.Checkout.CheckoutSessionId is not { } sessionId)
        {
            return Task.FromResult(false);
        }

        return LoadAsync(
            client => client.PostJsonAsync<StorefrontBrowserCheckoutReviewRequest, StorefrontBrowserCheckoutState>(
                _actions.ReviewRoute,
                new StorefrontBrowserCheckoutReviewRequest
                {
                    CheckoutSessionId = sessionId,
                    ExpectedCartVersion = State.Checkout.CartVersion,
                    TermsAccepted = true,
                },
                cancellationToken));
    }

    public async Task<StorefrontBrowserCheckoutPlaceOrderOutcome> PlaceOrderAsync(CancellationToken cancellationToken = default)
    {
        if (State.Checkout.CheckoutSessionId is not { } sessionId)
        {
            return new StorefrontBrowserCheckoutPlaceOrderOutcome(false, RedirectUrl: null);
        }

        var apiClient = ResolveApiClient();
        if (apiClient is null)
        {
            State.ApiAvailable = false;
            return new StorefrontBrowserCheckoutPlaceOrderOutcome(false, RedirectUrl: null);
        }

        State.ApiAvailable = true;
        State.Loading = true;
        State.Error = null;
        var result = await apiClient.PostJsonAsync<StorefrontBrowserCheckoutPlaceOrderRequest, StorefrontBrowserCheckoutPlaceOrderResult>(
            _actions.PlaceOrderRoute,
            new StorefrontBrowserCheckoutPlaceOrderRequest
            {
                CheckoutSessionId = sessionId,
                ExpectedCheckoutVersion = State.Checkout.CheckoutVersion,
                ExpectedCartVersion = State.Checkout.CartVersion,
                IdempotencyKey = _idempotencyKey,
            },
            cancellationToken).ConfigureAwait(false);
        State.Loading = false;

        if (!result.Success || result.Data is null)
        {
            State.Error = result.Message;
            return new StorefrontBrowserCheckoutPlaceOrderOutcome(true, RedirectUrl: null);
        }

        if (result.Data.Success)
        {
            RotateIdempotencyKey();
        }

        return new StorefrontBrowserCheckoutPlaceOrderOutcome(true, result.Data.RedirectUrl);
    }

    private Task<bool> SelectAsync(string route, string key, CancellationToken cancellationToken)
    {
        if (State.Checkout.CheckoutSessionId is not { } sessionId)
        {
            return Task.FromResult(false);
        }

        return LoadAsync(
            client => client.PostJsonAsync<StorefrontBrowserCheckoutSelectionRequest, StorefrontBrowserCheckoutState>(
                route,
                new StorefrontBrowserCheckoutSelectionRequest
                {
                    CheckoutSessionId = sessionId,
                    ExpectedCartVersion = State.Checkout.CartVersion,
                    Key = key,
                },
                cancellationToken));
    }

    private async Task<bool> LoadAsync(Func<StorefrontLocalApiClient, Task<StorefrontLocalApiResult<StorefrontBrowserCheckoutState>>> action)
    {
        var apiClient = ResolveApiClient();
        if (apiClient is null)
        {
            State.ApiAvailable = false;
            return false;
        }

        State.ApiAvailable = true;
        State.Loading = true;
        State.Error = null;
        var result = await action(apiClient).ConfigureAwait(false);
        State.Loading = false;
        if (result.Success && result.Data is not null)
        {
            ApplyCheckoutState(result.Data);
            return true;
        }

        State.Error = result.Message;
        return true;
    }

    private StorefrontLocalApiClient? ResolveApiClient()
    {
        return _services.GetService<StorefrontLocalApiClient>();
    }

    private bool ShouldAcceptInitialState(StorefrontBrowserCheckoutState initialState)
    {
        var current = State.Checkout;
        if (initialState.CheckoutSessionId != current.CheckoutSessionId)
        {
            return true;
        }

        if (initialState.CheckoutVersion > current.CheckoutVersion
            || initialState.CartVersion > current.CartVersion)
        {
            return true;
        }

        if ((initialState.CheckoutVersion > 0 && initialState.CheckoutVersion < current.CheckoutVersion)
            || (initialState.CartVersion > 0 && initialState.CartVersion < current.CartVersion))
        {
            return false;
        }

        return initialState.HasCart != current.HasCart
            || !LineIdentityMatches(initialState.Lines, current.Lines);
    }

    private void ApplyCheckoutState(StorefrontBrowserCheckoutState checkout)
    {
        if (checkout.CheckoutSessionId != _checkoutSessionId)
        {
            _checkoutSessionId = checkout.CheckoutSessionId;
            RotateIdempotencyKey();
        }

        State.Checkout = checkout;
    }

    private void RotateIdempotencyKey()
    {
        _idempotencyKey = Guid.NewGuid().ToString("N");
    }

    private static bool LineIdentityMatches(
        IReadOnlyList<StorefrontBrowserCheckoutLine> left,
        IReadOnlyList<StorefrontBrowserCheckoutLine> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (left[index].LineId != right[index].LineId
                || left[index].Quantity != right[index].Quantity)
            {
                return false;
            }
        }

        return true;
    }
}
