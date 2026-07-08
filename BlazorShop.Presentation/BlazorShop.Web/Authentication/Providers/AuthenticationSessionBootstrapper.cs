namespace BlazorShop.Web.Authentication.Providers
{
    using BlazorShop.Web.Shared.Helper.Contracts;

    public sealed class AuthenticationSessionBootstrapper : BlazorShop.Web.Shared.Authentication.AuthenticationSessionBootstrapper, IAuthenticationSessionBootstrapper
    {
        public AuthenticationSessionBootstrapper(ITokenService tokenService, IAuthenticationSessionRefresher sessionRefresher)
            : base(tokenService, sessionRefresher)
        {
        }
    }
}
