namespace BlazorShop.Web.SharedV2V2.Authentication
{
    using BlazorShop.Web.SharedV2V2.Helper.Contracts;

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
            var token = await this.tokenService.GetJwtTokenAsync(AuthStorageConstants.JwtTokenKey);
            if (!string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            await this.sessionRefresher.TryRefreshAsync(clearTokenOnFailure: false);
        }
    }
}
