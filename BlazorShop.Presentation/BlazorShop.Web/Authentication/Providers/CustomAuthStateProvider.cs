namespace BlazorShop.Web.Authentication.Providers
{
    using BlazorShop.Web.Shared.Helper.Contracts;

    public class CustomAuthStateProvider : BlazorShop.Web.Shared.Authentication.CustomAuthStateProvider
    {
        public CustomAuthStateProvider(ITokenService tokenService)
            : base(tokenService)
        {
        }
    }
}
