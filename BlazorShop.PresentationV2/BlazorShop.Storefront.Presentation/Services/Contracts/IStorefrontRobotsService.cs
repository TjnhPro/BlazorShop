namespace BlazorShop.Storefront.Presentation.Contracts
{
    public interface IStorefrontRobotsService
    {
        Task<string> GenerateAsync(CancellationToken cancellationToken = default);
    }
}