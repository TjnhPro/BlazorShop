namespace BlazorShop.Web.Shared.Authentication
{
    public interface IAuthenticationSessionEventPublisher
    {
        Task PublishSignedInAsync();

        Task PublishSignedOutAsync();

        Task PublishSessionRefreshedAsync();
    }
}
