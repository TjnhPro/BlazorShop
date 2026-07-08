namespace BlazorShop.Web.Authentication.Providers
{
    using BlazorShop.Web.Shared.Helper.Contracts;

    using Microsoft.JSInterop;

    public sealed class AuthenticationSessionSyncService : BlazorShop.Web.Shared.Authentication.AuthenticationSessionSyncService, IAuthenticationSessionSyncService
    {
        public AuthenticationSessionSyncService(
            IAuthenticationSessionRefresher sessionRefresher,
            ITokenService tokenService,
            IAuthenticatedClientStateCleaner clientStateCleaner,
            IAuthenticationStateNotifier authenticationStateNotifier,
            IJSRuntime jsRuntime)
            : base(sessionRefresher, tokenService, clientStateCleaner, authenticationStateNotifier, jsRuntime)
        {
        }
    }
}
