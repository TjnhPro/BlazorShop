namespace BlazorShop.Web.SharedV2.Authentication
{
    using BlazorShop.Web.SharedV2.Models;

    public interface IAuthenticationSessionRefresher
    {
        Task<LoginResponse?> TryRefreshAsync(bool clearTokenOnFailure = true);
    }
}
