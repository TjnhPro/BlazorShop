namespace BlazorShop.Web.Shared.Authentication
{
    using BlazorShop.Web.Shared.Models;

    public interface IAuthenticationSessionRefresher
    {
        Task<LoginResponse?> TryRefreshAsync(bool clearTokenOnFailure = true);
    }
}
