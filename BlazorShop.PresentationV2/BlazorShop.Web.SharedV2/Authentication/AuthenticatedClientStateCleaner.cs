namespace BlazorShop.Web.SharedV2.Authentication
{
    public class AuthenticatedClientStateCleaner : IAuthenticatedClientStateCleaner
    {
        public virtual Task ClearAsync()
        {
            return Task.CompletedTask;
        }
    }
}
