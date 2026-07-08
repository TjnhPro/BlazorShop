namespace BlazorShop.Web.Shared.Authentication
{
    public interface IAuthenticatedClientStateCleaner
    {
        Task ClearAsync();
    }
}
