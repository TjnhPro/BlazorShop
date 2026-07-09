namespace BlazorShop.Web.SharedV2.Authentication
{
    public interface IAuthenticationSessionSyncService : IAsyncDisposable
    {
        Task InitializeAsync();
    }
}
