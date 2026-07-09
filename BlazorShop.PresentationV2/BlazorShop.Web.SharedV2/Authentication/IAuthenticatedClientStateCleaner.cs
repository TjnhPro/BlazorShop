namespace BlazorShop.Web.SharedV2.Authentication
{
    public interface IAuthenticatedClientStateCleaner
    {
        Task ClearAsync();
    }
}
