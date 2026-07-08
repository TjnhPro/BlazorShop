namespace BlazorShop.Web.Authentication.Providers
{
    using Microsoft.JSInterop;

    public sealed class AuthenticationSessionEventPublisher : BlazorShop.Web.Shared.Authentication.AuthenticationSessionEventPublisher, IAuthenticationSessionEventPublisher
    {
        public AuthenticationSessionEventPublisher(IJSRuntime jsRuntime)
            : base(jsRuntime)
        {
        }
    }
}
