namespace BlazorShop.Web.SharedV2.Authentication
{
    public interface IAuthenticationSessionBootstrapper
    {
        Task RestoreAsync();
    }
}
