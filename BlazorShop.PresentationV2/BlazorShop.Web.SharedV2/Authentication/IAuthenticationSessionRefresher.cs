namespace BlazorShop.Web.SharedV2V2.Authentication
{
    using BlazorShop.Web.SharedV2V2.Models;

    public interface IAuthenticationSessionRefresher
    {
        Task<LoginResponse?> TryRefreshAsync(bool clearTokenOnFailure = true);
    }
}
