namespace BlazorShop.Web.Shared.Authentication
{
    public interface IAuthenticationSessionBootstrapper
    {
        Task RestoreAsync();
    }
}
