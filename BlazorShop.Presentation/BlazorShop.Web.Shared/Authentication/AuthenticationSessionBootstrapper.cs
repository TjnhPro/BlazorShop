namespace BlazorShop.Web.Shared.Authentication
{
    using BlazorShop.Web.Shared.Helper.Contracts;

    public class AuthenticationSessionBootstrapper : IAuthenticationSessionBootstrapper
    {
        private readonly ITokenService tokenService;
        private readonly IAuthenticationSessionRefresher sessionRefresher;

        public AuthenticationSessionBootstrapper(ITokenService tokenService, IAuthenticationSessionRefresher sessionRefresher)
        {
            this.tokenService = tokenService;
            this.sessionRefresher = sessionRefresher;
        }

        public async Task RestoreAsync()
        {
            var token = await this.tokenService.GetJwtTokenAsync(Constant.TokenStorage.Key);
            if (!string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            await this.sessionRefresher.TryRefreshAsync(clearTokenOnFailure: false);
        }
    }
}
