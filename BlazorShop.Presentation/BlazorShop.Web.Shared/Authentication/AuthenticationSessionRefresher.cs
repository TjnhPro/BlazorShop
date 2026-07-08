namespace BlazorShop.Web.Shared.Authentication
{
    using System.Net;

    using BlazorShop.Web.Shared.Helper.Contracts;
    using BlazorShop.Web.Shared.Models;
    using BlazorShop.Web.Shared.Services.Contracts;

    public class AuthenticationSessionRefresher : IAuthenticationSessionRefresher
    {
        private readonly ITokenService tokenService;
        private readonly IAuthenticationService authenticationService;
        private readonly IAuthenticationStateNotifier authenticationStateNotifier;
        private readonly IAuthenticatedClientStateCleaner clientStateCleaner;
        private readonly IAuthenticationSessionEventPublisher sessionEventPublisher;

        public AuthenticationSessionRefresher(
            ITokenService tokenService,
            IAuthenticationService authenticationService,
            IAuthenticationStateNotifier authenticationStateNotifier,
            IAuthenticatedClientStateCleaner clientStateCleaner,
            IAuthenticationSessionEventPublisher sessionEventPublisher)
        {
            this.tokenService = tokenService;
            this.authenticationService = authenticationService;
            this.authenticationStateNotifier = authenticationStateNotifier;
            this.clientStateCleaner = clientStateCleaner;
            this.sessionEventPublisher = sessionEventPublisher;
        }

        public async Task<LoginResponse?> TryRefreshAsync(bool clearTokenOnFailure = true)
        {
            var result = await this.authenticationService.ReviveToken();

            if (result.Success && !string.IsNullOrWhiteSpace(result.Data?.Token))
            {
                await this.tokenService.StoreJwtTokenAsync(Constant.TokenStorage.Key, result.Data.Token);
                this.authenticationStateNotifier.NotifyAuthenticationState();
                return result.Data;
            }

            if (clearTokenOnFailure && ShouldClearToken(result.StatusCode))
            {
                await this.tokenService.RemoveJwtTokenAsync(Constant.TokenStorage.Key);
                await this.clientStateCleaner.ClearAsync();
                this.authenticationStateNotifier.NotifyAuthenticationState();
                await this.sessionEventPublisher.PublishSignedOutAsync();
            }

            return null;
        }

        private static bool ShouldClearToken(HttpStatusCode? statusCode)
        {
            return statusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
        }
    }
}
