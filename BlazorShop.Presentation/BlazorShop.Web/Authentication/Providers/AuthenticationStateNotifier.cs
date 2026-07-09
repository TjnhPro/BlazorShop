namespace BlazorShop.Web.Authentication.Providers
{
    public sealed class AuthenticationStateNotifier : BlazorShop.Web.Shared.Authentication.AuthenticationStateNotifier, IAuthenticationStateNotifier
    {
        public AuthenticationStateNotifier(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
    }
}
