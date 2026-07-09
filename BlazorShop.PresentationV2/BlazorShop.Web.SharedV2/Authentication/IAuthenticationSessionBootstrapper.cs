namespace BlazorShop.Web.SharedV2V2.Authentication
{
    public interface IAuthenticationSessionBootstrapper
    {
        Task RestoreAsync();
    }
}
