namespace BlazorShop.Web.Shared.Authentication
{
    public interface IAuthenticationSessionSyncService : IAsyncDisposable
    {
        Task InitializeAsync();
    }
}
