namespace BlazorShop.Web.SharedV2V2.Authentication
{
    public interface IAuthenticationSessionSyncService : IAsyncDisposable
    {
        Task InitializeAsync();
    }
}
