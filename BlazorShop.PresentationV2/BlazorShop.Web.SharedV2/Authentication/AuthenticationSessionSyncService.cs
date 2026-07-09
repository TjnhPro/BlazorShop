namespace BlazorShop.Web.SharedV2V2.Authentication
{
    using BlazorShop.Web.SharedV2V2.Helper.Contracts;

    using Microsoft.JSInterop;

    public class AuthenticationSessionSyncService : IAuthenticationSessionSyncService
    {
        private const string ModulePath = "./js/authSessionSync.js";

        private readonly IAuthenticationSessionRefresher sessionRefresher;
        private readonly ITokenService tokenService;
        private readonly IAuthenticatedClientStateCleaner clientStateCleaner;
        private readonly IAuthenticationStateNotifier authenticationStateNotifier;
        private readonly IJSRuntime jsRuntime;

        private IJSObjectReference? module;
        private DotNetObjectReference<AuthenticationSessionSyncService>? selfReference;

        public AuthenticationSessionSyncService(
            IAuthenticationSessionRefresher sessionRefresher,
            ITokenService tokenService,
            IAuthenticatedClientStateCleaner clientStateCleaner,
            IAuthenticationStateNotifier authenticationStateNotifier,
            IJSRuntime jsRuntime)
        {
            this.sessionRefresher = sessionRefresher;
            this.tokenService = tokenService;
            this.clientStateCleaner = clientStateCleaner;
            this.authenticationStateNotifier = authenticationStateNotifier;
            this.jsRuntime = jsRuntime;
        }

        public async Task InitializeAsync()
        {
            if (this.module is not null)
            {
                return;
            }

            this.module = await this.jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
            this.selfReference = DotNetObjectReference.Create(this);
            await this.module.InvokeVoidAsync("subscribe", this.selfReference);
        }

        [JSInvokable]
        public async Task HandleAuthSessionEventAsync(string eventType)
        {
            switch (eventType)
            {
                case "signed-in":
                case "session-refreshed":
                    await this.sessionRefresher.TryRefreshAsync(clearTokenOnFailure: false);
                    break;
                case "signed-out":
                    await this.tokenService.RemoveJwtTokenAsync(AuthStorageConstants.JwtTokenKey);
                    await this.clientStateCleaner.ClearAsync();
                    this.authenticationStateNotifier.NotifyAuthenticationState();
                    break;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (this.module is not null)
            {
                try
                {
                    await this.module.InvokeVoidAsync("unsubscribe");
                    await this.module.DisposeAsync();
                }
                catch
                {
                    // Disposal should never block app shutdown.
                }
            }

            this.selfReference?.Dispose();
        }
    }
}
