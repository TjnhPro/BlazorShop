namespace BlazorShop.Web.SharedV2.Authentication
{
    using Microsoft.JSInterop;

    public class AuthenticationSessionEventPublisher : IAuthenticationSessionEventPublisher, IAsyncDisposable
    {
        private const string ModulePath = "./js/authSessionSync.js";

        private readonly IJSRuntime jsRuntime;
        private IJSObjectReference? module;

        public AuthenticationSessionEventPublisher(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public Task PublishSignedInAsync()
        {
            return this.PublishAsync("signed-in");
        }

        public Task PublishSignedOutAsync()
        {
            return this.PublishAsync("signed-out");
        }

        public Task PublishSessionRefreshedAsync()
        {
            return this.PublishAsync("session-refreshed");
        }

        public async ValueTask DisposeAsync()
        {
            if (this.module is not null)
            {
                await this.module.DisposeAsync();
            }
        }

        private async Task PublishAsync(string eventType)
        {
            try
            {
                this.module ??= await this.jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
                await this.module.InvokeVoidAsync("publish", eventType);
            }
            catch
            {
                // Cross-tab sync should not block the local auth flow.
            }
        }
    }
}
