namespace BlazorShop.Web.Authentication.Providers
{
    using BlazorShop.Web.Shared.Helper.Contracts;
    using BlazorShop.Web.Shared.Services.Contracts;

    public sealed class AuthenticationSessionRefresher : BlazorShop.Web.Shared.Authentication.AuthenticationSessionRefresher, IAuthenticationSessionRefresher
    {
        public AuthenticationSessionRefresher(
            ITokenService tokenService,
            IAuthenticationService authenticationService,
            IAuthenticationStateNotifier authenticationStateNotifier,
            IAuthenticatedClientStateCleaner clientStateCleaner,
            IAuthenticationSessionEventPublisher sessionEventPublisher)
            : base(tokenService, authenticationService, authenticationStateNotifier, clientStateCleaner, sessionEventPublisher)
        {
        }
    }
}
