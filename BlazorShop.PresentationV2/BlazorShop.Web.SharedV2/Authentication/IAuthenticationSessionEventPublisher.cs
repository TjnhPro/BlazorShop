namespace BlazorShop.Web.SharedV2V2.Authentication
{
    public interface IAuthenticationSessionEventPublisher
    {
        Task PublishSignedInAsync();

        Task PublishSignedOutAsync();

        Task PublishSessionRefreshedAsync();
    }
}
