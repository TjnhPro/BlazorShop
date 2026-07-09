namespace BlazorShop.Web.Authentication.Providers
{
    public class RefreshTokenHandler : BlazorShop.Web.Shared.Authentication.RefreshTokenHandler
    {
        public RefreshTokenHandler(IAuthenticationSessionRefresher sessionRefresher)
            : base(sessionRefresher)
        {
        }
    }
}
