namespace BlazorShop.Web.Shared.Authentication
{
    using Microsoft.AspNetCore.Components.Authorization;
    using Microsoft.Extensions.DependencyInjection;

    public class AuthenticationStateNotifier : IAuthenticationStateNotifier
    {
        private readonly IServiceProvider serviceProvider;

        public AuthenticationStateNotifier(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void NotifyAuthenticationState()
        {
            if (this.serviceProvider.GetService<AuthenticationStateProvider>() is CustomAuthStateProvider authStateProvider)
            {
                authStateProvider.NotifyAuthenticationState();
            }
        }
    }
}
