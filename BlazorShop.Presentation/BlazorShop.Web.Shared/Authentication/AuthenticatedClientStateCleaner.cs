namespace BlazorShop.Web.Shared.Authentication
{
    public class AuthenticatedClientStateCleaner : IAuthenticatedClientStateCleaner
    {
        public virtual Task ClearAsync()
        {
            return Task.CompletedTask;
        }
    }
}
